using UnityEngine;

public class PalmCallButtonController : MonoBehaviour
{
    [Header("Assign in Inspector")]
    [SerializeField] private OVRHand leftHand;
    [SerializeField] private OVRSkeleton leftSkeleton;
    [SerializeField] private GameObject callButtonObject;
    [SerializeField] private Transform cameraRig;

    [Header("Tuning")]
    [SerializeField] private float palmFacingThreshold = 0.6f;
    [SerializeField] private float showDelay = 0.5f;
    [SerializeField] private float palmOffsetDistance = 0.15f;
    [SerializeField] private float fingerPinchThreshold = 0.3f;

    private float _palmFacingTimer = 0f;
    private bool _buttonVisible = false;

    void Update()
    {
        if (leftHand == null || !leftHand.IsTracked)
        {
            HideButton();
            return;
        }

        bool palmFacing = IsPalmFacingCamera();
        bool handOpen = IsHandOpen();

        if (palmFacing && handOpen)
        {
            _palmFacingTimer += Time.deltaTime;
            if (_palmFacingTimer >= showDelay && !_buttonVisible)
                ShowButton();

            if (_buttonVisible)
                UpdateButtonPosition();
        }
        else
        {
            _palmFacingTimer = 0f;
            if (_buttonVisible)
                HideButton();
        }
    }

    bool IsPalmFacingCamera()
    {
        var bones = leftSkeleton.Bones;
        if (bones == null || bones.Count == 0) return false;

        var wrist = bones[(int)OVRSkeleton.BoneId.Hand_WristRoot];
        Vector3 palmNormal = -wrist.Transform.up;
        Vector3 toCamera = (cameraRig.position - wrist.Transform.position).normalized;
        return Vector3.Dot(palmNormal, toCamera) > palmFacingThreshold;
    }

    bool IsHandOpen()
    {
        // GetFingerPinchStrength returns 0 (open) to 1 (fully pinched)
        // All fingers must be below the threshold to count as "open"
        float thumb  = leftHand.GetFingerPinchStrength(OVRHand.HandFinger.Thumb);
        float index  = leftHand.GetFingerPinchStrength(OVRHand.HandFinger.Index);
        float middle = leftHand.GetFingerPinchStrength(OVRHand.HandFinger.Middle);
        float ring   = leftHand.GetFingerPinchStrength(OVRHand.HandFinger.Ring);
        float pinky  = leftHand.GetFingerPinchStrength(OVRHand.HandFinger.Pinky);

        return thumb  < fingerPinchThreshold &&
               index  < fingerPinchThreshold &&
               middle < fingerPinchThreshold &&
               ring   < fingerPinchThreshold &&
               pinky  < fingerPinchThreshold;
    }

    void UpdateButtonPosition()
    {
        var wrist = leftSkeleton.Bones[(int)OVRSkeleton.BoneId.Hand_WristRoot];
        callButtonObject.transform.position = wrist.Transform.position
                                            + (-wrist.Transform.up) * palmOffsetDistance;
        callButtonObject.transform.LookAt(cameraRig.position);
        callButtonObject.transform.Rotate(0, 180f, 0);
    }

    void ShowButton()
    {
        _buttonVisible = true;
        callButtonObject.SetActive(true);
    }

    void HideButton()
    {
        _buttonVisible = false;
        callButtonObject.SetActive(false);
    }
}