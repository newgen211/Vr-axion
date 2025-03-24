using UnityEngine;

public class StackRenderer : MonoBehaviour
{
    [Header("Image Stack Settings")]
    [SerializeField] private Texture2D[] imageStack; // Textures assigned in the Inspector
    [SerializeField] private Material material;       // Transparent material used for quads
    [SerializeField] private float spacing = 0.001f;    // Space between each quad

    private void Start()
    {
        if (imageStack == null || imageStack.Length == 0)
        {
            Debug.LogWarning("No textures assigned to imageStack.");
            return;
        }

        // Center the stack along the Z-axis.
        float totalDepth = (imageStack.Length - 1) * spacing;
        Vector3 startOffset = new Vector3(0f, 0f, -totalDepth * 0.5f);

        // Use bounds to calculate the overall size of the stack.
        Bounds stackBounds = new Bounds(transform.position, Vector3.zero);
        for (int i = 0; i < imageStack.Length; i++)
        {
            GameObject quad = GameObject.CreatePrimitive(PrimitiveType.Quad);
            quad.name = $"StackQuad_{i}";
            quad.transform.SetParent(transform, false);
            quad.transform.localPosition = startOffset + new Vector3(0f, 0f, i * spacing);

            // Create a unique material instance for each quad.
            Material matInstance = new Material(material);
            matInstance.mainTexture = imageStack[i];
            Renderer quadRenderer = quad.GetComponent<Renderer>();
            if (quadRenderer != null)
            {
                quadRenderer.material = matInstance;
            }

            // Expand the bounds to include this quad's position.
            stackBounds.Encapsulate(quad.transform.position);
        }

        // Add a BoxCollider to the parent object that encompasses the entire stack.
        BoxCollider stackCollider = gameObject.AddComponent<BoxCollider>();
        // Set the collider center relative to the parent.
        stackCollider.center = stackBounds.center - transform.position;
        stackCollider.size = stackBounds.size;
        // Mark the collider as a trigger so it doesn't interfere with physics.
        stackCollider.isTrigger = true;
    }
}
