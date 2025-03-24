using UnityEngine;

[CreateAssetMenu(fileName = "AnnotationConfig", menuName = "VRAnnotation/AnnotationConfig", order = 1)]
public class AnnotationConfig : ScriptableObject
{
    [Header("Sphere & Line Settings")]
    public Vector3 sphereScale = new Vector3(0.05f, 0.05f, 0.05f);
    public float lineWidth = 0.05f;

    [Header("Texture & Annotation Thresholds")]
    public Color colorThreshold = new Color(0.1f, 0.1f, 0.1f);
    public float brightnessThreshold = 0.1f;
    public int clusterPixelCountThreshold = 50;
    public string defaultAnnotationText = "Neuron Annotation";
}
