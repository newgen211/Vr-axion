using UnityEngine;

public class SphereAnnotation : MonoBehaviour
{
    [TextArea]
    public string AnnotationText = "";

    public void SetAnnotation(string newText)
    {
        AnnotationText = newText;
        Debug.Log("Annotation updated: " + AnnotationText);
    }
}
