using UnityEngine;

public class SphereAnnotation : MonoBehaviour
{
    [TextArea]
    public string AnnotationText = "";

    private TextMesh textMesh;

    void Start()
    {
        CreateTextMesh();
    }

    private void CreateTextMesh()
    {
        GameObject textObj = new GameObject("AnnotationText");
        textObj.transform.SetParent(transform);
        textObj.transform.localPosition = Vector3.up * 0.2f;
        textMesh = textObj.AddComponent<TextMesh>();
        textMesh.fontSize = 32;
        textMesh.characterSize = 0.1f;
        textMesh.alignment = TextAlignment.Center;
        textMesh.anchor = TextAnchor.MiddleCenter;
        textMesh.text = AnnotationText;
    }

    public void SetAnnotation(string newText)
    {
        AnnotationText = newText;
        if (textMesh != null)
        {
            textMesh.text = newText;
        }
        else
        {
            CreateTextMesh();
            textMesh.text = newText;
        }
    }
}
