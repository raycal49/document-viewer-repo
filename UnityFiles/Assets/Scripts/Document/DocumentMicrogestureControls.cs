using UnityEngine;

/// <summary>
/// Plan A microgesture integration:
/// - map one microgesture to previous page
/// - map one microgesture to next page
///
/// Set up LeftHandGestures/RightHandGestures GameObjects with OVRMicrogestureEventSource,
/// then assign them in this component.
/// </summary>
public class DocumentMicrogestureSwipeControls : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private DocumentNavigationController navigationController;
    [SerializeField] private OVRMicrogestureEventSource leftHandGestures;
    [SerializeField] private OVRMicrogestureEventSource rightHandGestures;

    [Header("Gesture mapping")]
    [Tooltip("Gesture value that should trigger previous page. Select in Inspector for your SDK version.")]
    [SerializeField]
    private OVRHand.MicrogestureType previousPageGesture = OVRHand.MicrogestureType.SwipeLeft;

    [Tooltip("Gesture value that should trigger next page. Select in Inspector for your SDK version.")]
    [SerializeField] private OVRHand.MicrogestureType nextPageGesture = OVRHand.MicrogestureType.SwipeRight;

    [Header("debounce")]
    [SerializeField] private float cooldownSeconds = 0.35f;

    [Header("Diagnostics")]
    [SerializeField] private bool logAcceptedGestures = true;
    [SerializeField] private bool logIgnoredGestures = false;

    private float _nextAcceptTime;

    private void OnEnable()
    {
        Subscribe(leftHandGestures);
        Subscribe(rightHandGestures);
    }

    private void OnDisable()
    {
        Unsubscribe(leftHandGestures);
        Unsubscribe(rightHandGestures);
    }

    private void Subscribe(OVRMicrogestureEventSource source)
    {
        if (source == null)
            return;

        source.GestureRecognizedEvent.AddListener(HandleGesture);
    }

    private void Unsubscribe(OVRMicrogestureEventSource source)
    {
        if (source == null)
            return;

        source.GestureRecognizedEvent.RemoveListener(HandleGesture);
    }

    private void HandleGesture(OVRHand.MicrogestureType gesture)
    {
        if (navigationController == null)
        {
            Debug.LogWarning("DocumentMicrogestureSwipeControls: DocumentNavigationController is not assigned.");
            return;
        }

        if (Time.unscaledTime < _nextAcceptTime)
        {
            if (logIgnoredGestures)
                Debug.Log($"DocumentMicrogestureSwipeControls: ignored gesture '{gesture}' during cooldown.");
            return;
        }

        var accepted = false;

        if (gesture.Equals(previousPageGesture))
        {
            accepted = navigationController.NavigatePrevious();
        }
        else if (gesture.Equals(nextPageGesture))
        {
            accepted = navigationController.NavigateNext();
        }
        else if (logIgnoredGestures)
        {
            Debug.Log($"DocumentMicrogestureSwipeControls: ignored unmapped gesture '{gesture}'.");
        }

        if (!accepted)
            return;

        _nextAcceptTime = Time.unscaledTime + Mathf.Max(0f, cooldownSeconds);

        if (logAcceptedGestures)
            Debug.Log($"DocumentMicrogestureSwipeControls: accepted gesture '{gesture}'.");
    }
}
