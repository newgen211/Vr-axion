// RaycastDraw.cs
using UnityEngine;
using System.Collections.Generic;

public class RaycastDraw : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Camera mainCamera;
    [SerializeField] private GameObject drawPrefab;
    private Plane movementPlane;
    private Vector3 grabOffset;

    [Header("Settings")]
    [SerializeField] private float drawDistance = 100f;
    [SerializeField] private LayerMask drawLayer;
    [SerializeField] private LayerMask sphereLayer;
    [SerializeField] private Vector3 sphereScale = new Vector3(0.05f, 0.05f, 0.05f);
    [SerializeField] private float lineWidth = 0.05f;
    [SerializeField] private Color colorThreshold = new Color(0.1f, 0.1f, 0.1f);
    [SerializeField] private string defaultAnnotationText = "Neuron Annotation";

    private List<List<GameObject>> allLines = new List<List<GameObject>>();
    private List<GameObject> currentLine = new List<GameObject>();
    private GameObject lastSphere = null;

    private bool isDrawing = false;
    private bool isMoveMode = false;
    private bool isAdjusting = false;
    private bool isAnnotationMode = false;
    private bool isConnectMode = false;
    private GameObject selectedSphere = null;

    private GameObject firstToConnect = null;
    private LineRenderer tempLine = null;

    [System.Serializable]
    public class AnnotationEntry
    {
        public Vector3 position;
        public string label;
    }

    void Update()
    {
        HandleInput();

        if (isDrawing) ProcessDrawing();
        else if (isMoveMode) ProcessMoving();
        else if (isAnnotationMode) ProcessAnnotation();
        else if (isConnectMode) ProcessManualConnect();
    }

    private void HandleInput()
    {
        if (Input.GetKeyDown(KeyCode.D)) { isDrawing = !isDrawing; isMoveMode = false; isAnnotationMode = false; isConnectMode = false; }
        if (Input.GetKeyDown(KeyCode.M)) { isMoveMode = !isMoveMode; isDrawing = false; isAnnotationMode = false; isConnectMode = false; }
        if (Input.GetKeyDown(KeyCode.A))
        {
            isAnnotationMode = !isAnnotationMode;
            isDrawing = false;
            isMoveMode = false;
            isConnectMode = false;
            if (!isAnnotationMode) SaveAnnotationsToJSON();
        }
        if (Input.GetKeyDown(KeyCode.C))
        {
            isConnectMode = !isConnectMode;
            isDrawing = false;
            isMoveMode = false;
            isAnnotationMode = false;
            Debug.Log("Connect Mode: " + (isConnectMode ? "ON" : "OFF"));
            if (!isConnectMode && tempLine != null)
            {
                Destroy(tempLine.gameObject);
                tempLine = null;
                firstToConnect = null;
            }
        }
        if (Input.GetKeyDown(KeyCode.U)) UndoLastSphere();
        if (isDrawing && Input.GetKeyDown(KeyCode.Return))
        {
            if (currentLine.Count > 0)
            {
                allLines.Add(new List<GameObject>(currentLine));
                currentLine.Clear();
                lastSphere = null;
            }
        }
        if (Input.GetKeyDown(KeyCode.E))
        {
            Ray ray = mainCamera.ScreenPointToRay(Input.mousePosition);
            if (Physics.Raycast(ray, out RaycastHit hit, drawDistance, sphereLayer))
            {
                if (hit.collider.CompareTag("Sphere"))
                {
                    GameObject sphere = hit.collider.gameObject;
                    lastSphere = sphere;
                    if (!currentLine.Contains(sphere)) currentLine.Add(sphere);
                    Debug.Log("Editing from existing sphere.");
                }
            }
        }
    }

    private void ProcessDrawing()
    {
        if (Input.GetMouseButtonDown(0))
        {
            Ray ray = mainCamera.ScreenPointToRay(Input.mousePosition);
            Vector3 hitPosition = GetValidHitPosition(ray);
            if (hitPosition != Vector3.zero)
            {
                GameObject newSphere = CreateSphere(hitPosition);
                if (lastSphere != null) CreateConnection(lastSphere, newSphere);
                currentLine.Add(newSphere);
                lastSphere = newSphere;
            }
        }
    }

    private void ProcessManualConnect()
    {
        Ray ray = mainCamera.ScreenPointToRay(Input.mousePosition);

        if (Input.GetMouseButtonDown(0))
        {
            if (Physics.Raycast(ray, out RaycastHit hit, drawDistance, sphereLayer))
            {
                if (hit.collider.CompareTag("Sphere"))
                {
                    if (firstToConnect == null)
                    {
                        firstToConnect = hit.collider.gameObject;
                        GameObject tempLineObj = new GameObject("TempLine");
                        tempLine = tempLineObj.AddComponent<LineRenderer>();
                        tempLine.material = new Material(Shader.Find("Sprites/Default"));
                        tempLine.startWidth = lineWidth;
                        tempLine.endWidth = lineWidth;
                        tempLine.positionCount = 2;
                        tempLine.useWorldSpace = true;
                        tempLine.startColor = Color.yellow;
                        tempLine.endColor = Color.yellow;
                        tempLine.SetPosition(0, firstToConnect.transform.position);
                        tempLine.SetPosition(1, firstToConnect.transform.position);
                    }
                    else
                    {
                        GameObject second = hit.collider.gameObject;
                        CreateConnection(firstToConnect, second);
                        Destroy(tempLine.gameObject);
                        tempLine = null;
                        firstToConnect = null;
                    }
                }
            }
        }

        if (firstToConnect != null && tempLine != null)
        {
            if (Physics.Raycast(ray, out RaycastHit hitPoint, drawDistance, drawLayer))
                tempLine.SetPosition(1, hitPoint.point);
            else
                tempLine.SetPosition(1, ray.origin + ray.direction * drawDistance);
        }
    }

    private void ProcessMoving()
    {
        Ray ray = mainCamera.ScreenPointToRay(Input.mousePosition);
        if (!isAdjusting && Input.GetMouseButtonDown(0))
        {
            if (Physics.Raycast(ray, out RaycastHit hit, drawDistance, sphereLayer))
            {
                if (hit.collider.CompareTag("Sphere"))
                {
                    selectedSphere = hit.collider.gameObject;
                    isAdjusting = true;
                    movementPlane = new Plane(mainCamera.transform.forward * -1f, selectedSphere.transform.position);
                    movementPlane.Raycast(ray, out float enter);
                    Vector3 hitPoint = ray.GetPoint(enter);
                    grabOffset = selectedSphere.transform.position - hitPoint;
                }
            }
        }
        if (isAdjusting && Input.GetMouseButton(0))
        {
            if (movementPlane.Raycast(ray, out float enter))
            {
                Vector3 point = ray.GetPoint(enter);
                selectedSphere.transform.position = point + grabOffset;
                UpdateConnections(selectedSphere);
            }
        }
        if (isAdjusting && Input.GetMouseButtonUp(0))
        {
            isAdjusting = false;
            selectedSphere = null;
        }
    }

    private void ProcessAnnotation()
    {
        if (Input.GetMouseButtonDown(0))
        {
            Ray ray = mainCamera.ScreenPointToRay(Input.mousePosition);
            if (Physics.Raycast(ray, out RaycastHit hit, drawDistance, sphereLayer))
            {
                if (hit.collider.CompareTag("Sphere"))
                {
                    GameObject sphere = hit.collider.gameObject;
                    SphereAnnotation annotation = sphere.GetComponent<SphereAnnotation>();
                    if (annotation == null) annotation = sphere.AddComponent<SphereAnnotation>();
                    annotation.SetAnnotation(string.IsNullOrEmpty(annotation.AnnotationText) ? defaultAnnotationText : "");
                }
            }
        }
    }

    private GameObject CreateSphere(Vector3 position)
    {
        GameObject sphere = Instantiate(drawPrefab, position, Quaternion.identity);
        sphere.tag = "Sphere";
        sphere.layer = LayerMask.NameToLayer("Sphere");
        sphere.transform.localScale = sphereScale;
        return sphere;
    }

    private void CreateConnection(GameObject startSphere, GameObject endSphere)
    {
        LineRenderer lineRenderer = startSphere.GetComponent<LineRenderer>();
        if (lineRenderer == null)
        {
            lineRenderer = startSphere.AddComponent<LineRenderer>();
            lineRenderer.material = new Material(Shader.Find("Sprites/Default"));
            lineRenderer.startColor = Color.white;
            lineRenderer.endColor = Color.white;
        }
        lineRenderer.startWidth = lineWidth;
        lineRenderer.endWidth = lineWidth;
        lineRenderer.positionCount = 2;
        lineRenderer.useWorldSpace = true;
        lineRenderer.SetPosition(0, startSphere.transform.position);
        lineRenderer.SetPosition(1, endSphere.transform.position);

        SphereConnector connector = startSphere.GetComponent<SphereConnector>();
        if (connector == null) connector = startSphere.AddComponent<SphereConnector>();
        connector.lineRenderer = lineRenderer;
        connector.connectedSphere = endSphere;
    }

    private void UpdateConnections(GameObject movedSphere)
    {
        SphereConnector connector = movedSphere.GetComponent<SphereConnector>();
        if (connector != null && connector.lineRenderer != null)
            connector.lineRenderer.SetPosition(0, movedSphere.transform.position);

        SphereConnector[] allConnectors = FindObjectsOfType<SphereConnector>();
        foreach (SphereConnector other in allConnectors)
        {
            if (other.connectedSphere == movedSphere && other.lineRenderer != null)
                other.lineRenderer.SetPosition(1, movedSphere.transform.position);
        }
    }

    private void UndoLastSphere()
    {
        if (currentLine.Count == 0) return;
        GameObject last = currentLine[^1];
        currentLine.RemoveAt(currentLine.Count - 1);
        Destroy(last);
        lastSphere = currentLine.Count > 0 ? currentLine[^1] : null;
    }

    private Vector3 GetValidHitPosition(Ray ray)
    {
        Vector3 validPosition = Vector3.zero;
        float closestDistance = float.MaxValue;
        RaycastHit[] hits = Physics.RaycastAll(ray, drawDistance, drawLayer);
        foreach (RaycastHit hit in hits)
        {
            if (hit.collider.TryGetComponent<Renderer>(out Renderer renderer) && renderer.material.mainTexture is Texture2D texture)
            {
                if (!texture.isReadable) continue;
                Vector2 uv = hit.textureCoord;
                int x = Mathf.FloorToInt(uv.x * texture.width);
                int y = Mathf.FloorToInt(uv.y * texture.height);
                Color pixelColor = texture.GetPixel(x, y);
                if (pixelColor.r > colorThreshold.r || pixelColor.g > colorThreshold.g || pixelColor.b > colorThreshold.b)
                {
                    if (hit.distance < closestDistance)
                    {
                        validPosition = hit.point;
                        closestDistance = hit.distance;
                    }
                }
            }
        }
        return validPosition;
    }

    private void SaveAnnotationsToJSON()
    {
        List<AnnotationEntry> annotations = new();
        foreach (var line in allLines)
        {
            foreach (var sphere in line)
            {
                SphereAnnotation sa = sphere.GetComponent<SphereAnnotation>();
                annotations.Add(new AnnotationEntry { position = sphere.transform.position, label = sa != null ? sa.AnnotationText : "" });
            }
        }
        foreach (var sphere in currentLine)
        {
            SphereAnnotation sa = sphere.GetComponent<SphereAnnotation>();
            annotations.Add(new AnnotationEntry { position = sphere.transform.position, label = sa != null ? sa.AnnotationText : "" });
        }
        string json = JsonUtility.ToJson(new Wrapper<AnnotationEntry> { items = annotations }, true);
        System.IO.File.WriteAllText(Application.dataPath + "/annotations.json", json);
    }

    [System.Serializable]
    private class Wrapper<T> { public List<T> items; }
}

public class SphereConnector : MonoBehaviour
{
    public LineRenderer lineRenderer;
    public GameObject connectedSphere;
}
