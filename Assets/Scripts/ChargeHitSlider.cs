using UnityEngine;
using UnityEngine.UI;

public class ChargeHitSlider : MonoBehaviour
{
    public RectTransform canvasRect; // The RectTransform of the canvas
    public RectTransform sliderTransform; // The RectTransform of the slider
    public Slider slider; // The Slider component
    public Image sliderFill; // The Image component of the slider's fill
    public Image sliderRing; // The Image component of the slider's fill
    public Image sliderCenter; // The Image component of the slider's fill
    public Camera mainCamera; // The camera rendering the scene and UI
    public Gradient colorGradient; // The gradient for the slider's color transition
    public Ball ball; // The 3D ball object with a Collider

    private bool isDragging = false;
    private GameManager gm;
    private Vector2 direction;

    private void Start()
    {
        if (canvasRect == null || sliderTransform == null || slider == null || mainCamera == null || sliderFill == null || colorGradient == null || ball == null) {
            Debug.LogError("Assign all required references in the inspector.");
            return;
        }

        sliderTransform.gameObject.SetActive(false);
        gm = GameManager.Instance;
    }

    private void Update()
    {
        // Check if the mouse is pressed and over the ball
        if (Input.GetMouseButtonDown(0) && IsPointerOverBall() && !ball.IsMoving()) {
            isDragging = true;
            sliderTransform.gameObject.SetActive(true);
            slider.value = 0;
        }

        // Check if the mouse is released
        if (Input.GetMouseButtonUp(0) && isDragging) {
            if (IsPointerOverBall() || direction.magnitude == 0) {
                Debug.Log("Cancelled action");
            } else {
                Debug.Log($"Hit value: {slider.value}");
                ball.SetReposition();
                ball.SetVelocity(new Vector3(-direction.x, 0, -direction.y));
                gm.AddHit();
            }
            isDragging = false;
            sliderTransform.gameObject.SetActive(false);
        }

        // Update slider only while dragging
        if (isDragging) {
            RotateSliderTowardsCursor();
            UpdateSliderColor();
        }

        // Keep the canvas positioned below the ball (adjust the offset as necessary)
        transform.position = ball.transform.position - new Vector3(0, 0.09f, 0);
        // Make the canvas rotate with the ball, but remain flat
        transform.rotation = Quaternion.Euler(90, 0, 0); 
    }

    private void RotateSliderTowardsCursor()
    {
        // Get the mouse position in screen space
        Vector3 mouseScreenPosition = Input.mousePosition;

        // Convert the mouse position to local canvas space
        Vector2 mouseWorldPosition;
        RectTransformUtility.ScreenPointToLocalPointInRectangle(canvasRect, mouseScreenPosition, mainCamera, out mouseWorldPosition);

        // Calculate the direction from the slider center to the mouse position
        Vector2 directionVector = mouseWorldPosition - (Vector2)sliderTransform.localPosition;

        // Set the slider value based on the distance
        float maxDistance = canvasRect.rect.width / 2f; // Assuming the canvas is square
        float distance = Mathf.Clamp01(directionVector.magnitude / maxDistance);

        direction = directionVector.normalized * distance * ball.speed;

        // Check if the distance is greater than the minimum threshold
        if (!IsPointerOverBall()) {
            // Set the slider rotation to face the mouse position
            float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
            sliderTransform.localRotation = Quaternion.Euler(0, 0, angle);
            
            slider.value = distance;
        } else {
            slider.value = 0;
        }
    }

    private void UpdateSliderColor()
    {
        sliderFill.color = colorGradient.Evaluate(slider.value);
        sliderRing.color = colorGradient.Evaluate(slider.value);
        sliderCenter.color = colorGradient.Evaluate(slider.value);
    }

    private bool IsPointerOverBall()
    {
        Ray ray = mainCamera.ScreenPointToRay(Input.mousePosition);
        if (Physics.Raycast(ray, out RaycastHit hit)) {
            return hit.collider.gameObject == ball.gameObject;
        }
        return false;
    }
}
