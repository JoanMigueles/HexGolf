using UnityEngine;

public class CameraFollow : MonoBehaviour
{
    
    public float rotationSpeed = 5f; // Speed of rotation when dragging
    public float zoomSpeed = 10f; // Speed of zoom when scrolling
    public float minZoom = 5f; // Minimum zoom distance
    public float maxZoom = 20f; // Maximum zoom distance
    public Vector3 offset = new Vector3(0, 5, -10); // Default offset position from the ball

    private float currentZoom = 10f; // Current zoom level
    private float currentRotationX = 0f; // Current rotation around the X axis
    private float currentRotationY = 0f; // Current rotation around the Y axis
    private Transform ball; // The ball to follow


    private void Start()
    {
        currentZoom = offset.magnitude;
    }

    private void Update()
    {
        // Right mouse drag to rotate camera
        if (Input.GetMouseButton(1)) // Right mouse button
        {
            currentRotationX += Input.GetAxis("Mouse X") * rotationSpeed;
            currentRotationY -= Input.GetAxis("Mouse Y") * rotationSpeed;
            currentRotationY = Mathf.Clamp(currentRotationY, -90f, 90f); // Limit vertical rotation
        }

        // Zoom with mouse wheel
        float zoomInput = Input.GetAxis("Mouse ScrollWheel");
        if (zoomInput != 0f) {
            currentZoom -= zoomInput * zoomSpeed;
            currentZoom = Mathf.Clamp(currentZoom, minZoom, maxZoom); // Clamp zoom within range
        }
    }

    public void SetTarget(Transform target)
    {
        ball = target;
    }

    private void LateUpdate()
    {
        if (ball == null) return;

        // Calculate the rotation around the ball based on input
        Quaternion rotation = Quaternion.Euler(currentRotationY, currentRotationX, 0);

        // Apply the rotation and zoom to the camera's position
        Vector3 direction = rotation * new Vector3(0, 0, -currentZoom);
        transform.position = ball.position + direction;

        // Make the camera look at the ball
        transform.LookAt(ball.position);
    }
}
