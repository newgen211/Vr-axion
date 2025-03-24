using UnityEngine;
using System.Collections.Generic;

public class RaycastDraw : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Camera mainCamera;
    [SerializeField] private GameObject drawPrefab;

    [Header("Settings")]
    [SerializeField] private float drawDistance = 100f;
    [SerializeField] private LayerMask drawLayer;
    [SerializeField] private Vector3 sphereScale = new Vector3(0.05f, 0.05f, 0.05f); // Smaller spheres
    [SerializeField] private float lineWidth = 0.05f;
    [SerializeField] private Color colorThreshold = new Color(0.1f, 0.1f, 0.1f);
    [SerializeField] private string defaultAnnotationText = "Neuron Annotation";

    private List<GameObject> spheres = new List<GameObject>();
    private GameObject lastSphere = null;

    private bool isDrawing = false;
    private bool isMoveMode = false;
    private bool isAdjusting = false;
    private bool isAnnotationMode = false;  // New annotation mode
    private GameObject selectedSphere = null;

    void Update()
    {
        HandleInput();

        if (isDrawing)
        {
            ProcessDrawing();
        }
        else if (isMoveMode)
        {
            ProcessMoving();
        }
        else if (isAnnotationMode)
        {
            ProcessAnnotation();
        }
    }

    private void HandleInput()
    {
        if (Input.GetKeyDown(KeyCode.D))
        {
            isDrawing = !isDrawing;
            isMoveMode = false;
            isAnnotationMode = false;
        }
        if (Input.GetKeyDown(KeyCode.M))
        {
            isMoveMode = !isMoveMode;
            isDrawing = false;
            isAnnotationMode = false;
        }
        if (Input.GetKeyDown(KeyCode.A))
        {
            isAnnotationMode = !isAnnotationMode;
            isDrawing = false;
            isMoveMode = false;
        }
        if (Input.GetKeyDown(KeyCode.U))
        {
            UndoLastSphere();
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
                if (lastSphere != null)
                {
                    CreateConnection(lastSphere, newSphere);
                }
                spheres.Add(newSphere);
                lastSphere = newSphere;
            }
            else
            {
                Debug.Log("No valid hit found on any plane.");
            }
        }
    }

    private void ProcessMoving()
    {
        if (!isAdjusting && Input.GetMouseButtonDown(0))
        {
            Ray ray = mainCamera.ScreenPointToRay(Input.mousePosition);
            if (Physics.Raycast(ray, out RaycastHit hit))
            {
                if (hit.collider.CompareTag("Sphere"))
                {
                    selectedSphere = hit.collider.gameObject;
                    isAdjusting = true;
                }
            }
        }

        if (isAdjusting && Input.GetMouseButton(0))
        {
            Ray ray = mainCamera.ScreenPointToRay(Input.mousePosition);
            if (Physics.Raycast(ray, out RaycastHit hit, drawDistance, drawLayer))
            {
                selectedSphere.transform.position = hit.point;
                UpdateConnections(selectedSphere);
            }
        }

        if (isAdjusting && Input.GetMouseButtonUp(0))
        {
            isAdjusting = false;
            selectedSphere = null;
        }
    }

    // In annotation mode, clicking on a sphere toggles its annotation text.
    private void ProcessAnnotation()
    {
        if (Input.GetMouseButtonDown(0))
        {
            Ray ray = mainCamera.ScreenPointToRay(Input.mousePosition);
            if (Physics.Raycast(ray, out RaycastHit hit))
            {
                if (hit.collider.CompareTag("Sphere"))
                {
                    GameObject sphere = hit.collider.gameObject;
                    SphereAnnotation annotation = sphere.GetComponent<SphereAnnotation>();
                    if (annotation == null)
                    {
                        // If no annotation exists, add one with the default text.
                        annotation = sphere.AddComponent<SphereAnnotation>();
                        annotation.SetAnnotation(defaultAnnotationText);
                    }
                    else
                    {
                        // Toggle annotation visibility: if text is present, clear it; otherwise, reset it.
                        if (string.IsNullOrEmpty(annotation.AnnotationText))
                        {
                            annotation.SetAnnotation(defaultAnnotationText);
                        }
                        else
                        {
                            annotation.SetAnnotation("");
                        }
                    }
                }
            }
        }
    }

    private GameObject CreateSphere(Vector3 position)
    {
        GameObject sphere = Instantiate(drawPrefab, position, Quaternion.identity);
        sphere.tag = "Sphere";
        sphere.transform.localScale = sphereScale;
        return sphere;
    }

    private void CreateConnection(GameObject startSphere, GameObject endSphere)
    {
        LineRenderer lineRenderer = startSphere.AddComponent<LineRenderer>();
        lineRenderer.startWidth = lineWidth;
        lineRenderer.endWidth = lineWidth;
        lineRenderer.positionCount = 2;
        lineRenderer.useWorldSpace = true;
        lineRenderer.SetPosition(0, startSphere.transform.position);
        lineRenderer.SetPosition(1, endSphere.transform.position);

        SphereConnector connector = startSphere.GetComponent<SphereConnector>();
        if (connector == null)
        {
            connector = startSphere.AddComponent<SphereConnector>();
        }
        connector.lineRenderer = lineRenderer;
        connector.connectedSphere = endSphere;
    }

    private void UpdateConnections(GameObject movedSphere)
    {
        SphereConnector connector = movedSphere.GetComponent<SphereConnector>();
        if (connector != null && connector.lineRenderer != null)
        {
            connector.lineRenderer.SetPosition(0, movedSphere.transform.position);
        }

        SphereConnector[] allConnectors = FindObjectsOfType<SphereConnector>();
        foreach (SphereConnector other in allConnectors)
        {
            if (other.connectedSphere == movedSphere && other.lineRenderer != null)
            {
                other.lineRenderer.SetPosition(1, movedSphere.transform.position);
            }
        }
    }

    private void UndoLastSphere()
    {
        if (spheres.Count > 0)
        {
            GameObject last = spheres[spheres.Count - 1];
            SphereConnector connector = last.GetComponent<SphereConnector>();
            if (connector != null && connector.lineRenderer != null)
            {
                Destroy(connector.lineRenderer);
            }
            spheres.RemoveAt(spheres.Count - 1);
            Destroy(last);
            lastSphere = spheres.Count > 0 ? spheres[spheres.Count - 1] : null;
        }
    }

    // Returns the closest valid hit point based on texture opacity.
    private Vector3 GetValidHitPosition(Ray ray)
    {
        Vector3 validPosition = Vector3.zero;
        float closestDistance = float.MaxValue;
        RaycastHit[] hits = Physics.RaycastAll(ray, drawDistance, drawLayer);
        foreach (RaycastHit hit in hits)
        {
            if (hit.collider.TryGetComponent<Renderer>(out Renderer renderer) &&
                renderer.material.mainTexture is Texture2D texture)
            {
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
}

public class SphereConnector : MonoBehaviour
{
    public LineRenderer lineRenderer;
    public GameObject connectedSphere;
}
