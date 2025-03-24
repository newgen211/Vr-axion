using UnityEngine;

public class SphereAutoAnnotator : MonoBehaviour
{
    // Analyzes the sphere's texture and auto-annotates if bright pixels exceed the threshold.
    public void AutoAnnotate(GameObject sphere, AnnotationConfig config)
    {
        Renderer renderer = sphere.GetComponent<Renderer>();
        if (renderer != null && renderer.material.mainTexture is Texture2D texture)
        {
            Color[] pixels = texture.GetPixels();
            int brightPixelCount = 0;
            foreach (Color pixel in pixels)
            {
                if (pixel.r > config.brightnessThreshold ||
                    pixel.g > config.brightnessThreshold ||
                    pixel.b > config.brightnessThreshold)
                {
                    brightPixelCount++;
                }
            }

            if (brightPixelCount >= config.clusterPixelCountThreshold)
            {
                SphereAnnotation annotation = sphere.GetComponent<SphereAnnotation>();
                if (annotation == null)
                {
                    annotation = sphere.AddComponent<SphereAnnotation>();
                }
                annotation.SetAnnotation(config.defaultAnnotationText);
            }
        }
        else
        {
            Debug.LogWarning("Sphere does not have a texture suitable for analysis.");
        }
    }
}
