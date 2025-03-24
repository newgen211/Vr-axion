using UnityEngine;

public class CameraController : MonoBehaviour
{
    public Transform target; // The target (QuadGroup) to follow
    public float distance = 2f; // Distance from the target
    public float rotationSpeed = 100f; // Speed of rotation
    public float moveSpeed = 1f; // Speed of movement

    private float currentAngleY = 0f; // Current angle around Y-axis

    void Update()
    {
        // Rotate around the target using left and right arrow keys
        if (Input.GetKey(KeyCode.LeftArrow))
        {
            currentAngleY -= rotationSpeed * Time.deltaTime;
        }
        if (Input.GetKey(KeyCode.RightArrow))
        {
            currentAngleY += rotationSpeed * Time.deltaTime;
        }

        // Move forward and backward using up and down arrow keys
        if (Input.GetKey(KeyCode.UpArrow))
        {
            distance -= moveSpeed * Time.deltaTime; // Move closer
        }
        if (Input.GetKey(KeyCode.DownArrow))
        {
            distance += moveSpeed * Time.deltaTime; // Move further away
        }

        // Calculate the new position and rotation of the camera
        Vector3 direction = new Vector3(0, 0, -distance);
        Quaternion rotation = Quaternion.Euler(0, currentAngleY, 0);
        transform.position = target.position + rotation * direction; // Position the camera
        transform.LookAt(target.position); // Make the camera look at the target
    }
}
