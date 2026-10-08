using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR.Hands;
using UnityEngine.XR.Hands.Processing;
using UnityEngine.XR.Interaction.Toolkit.Interactors;

/// <summary>
/// Ferme les doigts (suivi des mains) et la poignée des manettes
/// quand une hache peut être prise, est tenue, puis relâchée.
/// </summary>
public class HandGrabPoseDriver : MonoBehaviour, IXRHandProcessor
{
    const float CloseSpeed = 4.5f;
    const float FullCurlDegrees = 68f;

    static HandGrabPoseDriver instance;
    static readonly List<XRHandSubsystem> subsystems = new List<XRHandSubsystem>();
    static readonly XRHandJointID[] curlJoints =
    {
        XRHandJointID.ThumbProximal,
        XRHandJointID.ThumbDistal,
        XRHandJointID.IndexProximal,
        XRHandJointID.IndexIntermediate,
        XRHandJointID.IndexDistal,
        XRHandJointID.MiddleProximal,
        XRHandJointID.MiddleIntermediate,
        XRHandJointID.MiddleDistal,
        XRHandJointID.RingProximal,
        XRHandJointID.RingIntermediate,
        XRHandJointID.RingDistal,
        XRHandJointID.LittleProximal,
        XRHandJointID.LittleIntermediate,
        XRHandJointID.LittleDistal,
    };

    static int requestFrame = -1;
    static float leftDesired;
    static float rightDesired;
    static float leftCurl;
    static float rightCurl;

    readonly List<ControllerGrip> grips = new List<ControllerGrip>();
    bool gripsCached;

    public int callbackOrder => 10;

    public static void Request(InteractorHandedness hand, float curl)
    {
        Ensure();
        if (requestFrame != Time.frameCount)
        {
            requestFrame = Time.frameCount;
            leftDesired = 0f;
            rightDesired = 0f;
        }

        if (hand == InteractorHandedness.Left)
            leftDesired = Mathf.Max(leftDesired, curl);
        else if (hand == InteractorHandedness.Right)
            rightDesired = Mathf.Max(rightDesired, curl);
    }

    public static void Ensure()
    {
        if (instance != null)
            return;

        var driverObject = new GameObject(nameof(HandGrabPoseDriver));
        DontDestroyOnLoad(driverObject);
        instance = driverObject.AddComponent<HandGrabPoseDriver>();
    }

    void Awake()
    {
        SubsystemManager.GetSubsystems(subsystems);
        for (int i = 0; i < subsystems.Count; i++)
            subsystems[i].RegisterProcessor(this);
    }

    void OnDestroy()
    {
        for (int i = 0; i < subsystems.Count; i++)
        {
            if (subsystems[i] != null)
                subsystems[i].UnregisterProcessor(this);
        }

        if (instance == this)
            instance = null;
    }

    public void ProcessJoints(XRHandSubsystem subsystem, XRHandSubsystem.UpdateSuccessFlags successFlags, XRHandSubsystem.UpdateType updateType)
    {
        Apply(subsystem, subsystem.leftHand, leftCurl);
        Apply(subsystem, subsystem.rightHand, rightCurl);
    }

    void LateUpdate()
    {
        float step = CloseSpeed * Time.deltaTime;
        leftCurl = Mathf.MoveTowards(leftCurl, leftDesired, step);
        rightCurl = Mathf.MoveTowards(rightCurl, rightDesired, step);
        AnimateControllers();
    }

    static void Apply(XRHandSubsystem subsystem, XRHand hand, float curl)
    {
        if (!hand.isTracked || curl <= 0.001f)
            return;

        var joints = hand.GetRawJointArray();
        float degrees = FullCurlDegrees * curl;
        for (int i = 0; i < curlJoints.Length; i++)
        {
            int index = curlJoints[i].ToIndex();
            if (index < 0 || index >= joints.Length)
                continue;

            XRHandJoint joint = joints[index];
            if (!joint.TryGetPose(out Pose pose))
                continue;

            float amount = curlJoints[i].ToString().Contains("Thumb") ? degrees * 0.65f : degrees;
            pose.rotation = pose.rotation * Quaternion.Euler(amount, 0f, 0f);
            joint.SetPose(pose);
            joints[index] = joint;
        }

        subsystem.SetCorrespondingHand(hand);
    }

    void AnimateControllers()
    {
        if (!gripsCached)
            CacheControllerGrips();

        for (int i = 0; i < grips.Count; i++)
        {
            ControllerGrip grip = grips[i];
            if (grip.transform == null)
                continue;

            float desired = grip.hand == InteractorHandedness.Left ? leftCurl : rightCurl;
            if (desired <= 0.001f)
                continue;

            Vector3 position = grip.transform.localPosition;
            position.x = Mathf.Lerp(grip.openX, grip.closedX, desired);
            grip.transform.localPosition = position;
        }
    }

    void CacheControllerGrips()
    {
        gripsCached = true;
        XRBaseInteractor[] interactors = FindObjectsByType<XRBaseInteractor>(FindObjectsSortMode.None);
        for (int i = 0; i < interactors.Length; i++)
        {
            Transform grip = FindGrip(interactors[i].transform);
            if (grip == null)
                continue;

            grips.Add(new ControllerGrip
            {
                hand = interactors[i].handedness,
                transform = grip,
                openX = grip.localPosition.x,
                closedX = grip.localPosition.x + 0.0015f,
            });
        }
    }

    static Transform FindGrip(Transform root)
    {
        Transform[] children = root.GetComponentsInChildren<Transform>(true);
        for (int i = 0; i < children.Length; i++)
        {
            if (children[i].name == "Grip")
                return children[i];
        }

        return null;
    }

    struct ControllerGrip
    {
        public InteractorHandedness hand;
        public Transform transform;
        public float openX;
        public float closedX;
    }
}
