using UnityEngine;

public class RotateQuads : MonoBehaviour
{
    public float rotationSpeed = 200f; // Speed of rotation
    private bool isRotating = false;    // Flag to check if rotation should happen

    void Update()
    {
        // Check if the user is clicking and holding the mouse
        if (Input.GetMouseButton(0))
        {
            isRotating = true; // Start rotating
            float rotationX = Input.GetAxis("Mouse Y") * rotationSpeed * Time.deltaTime;
            float rotationY = Input.GetAxis("Mouse X") * rotationSpeed * Time.deltaTime;

            // Rotate the parent GameObject based on mouse movement
            transform.Rotate(Vector3.right, rotationX);
            transform.Rotate(Vector3.up, -rotationY);
        }
        else
        {
            isRotating = false; // Stop rotating when not clicking
        }
    }
}
