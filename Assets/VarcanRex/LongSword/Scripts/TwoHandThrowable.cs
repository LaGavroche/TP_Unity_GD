using System.Collections;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;

[RequireComponent(typeof(Collider))]
public class TwoHandThrowable : MonoBehaviour
{
    [Header("Lancer")]
    public float mass = 3f;
    public float maxSpeed = 6.3f;
    public float maxRange = 4f;
    public int velocitySampleFrames = 6;
    public float maxAngularSpeed = 20f;

    [Header("Retour automatique optionnel")]
    public bool autoReturnToLastHand = false;
    public float autoReturnDelay = 2f;
    public float autoReturnSpeed = 8f;

    Rigidbody rb;
    UnityEngine.XR.Interaction.Toolkit.Interactables.XRGrabInteractable grab;
    UnityEngine.XR.Interaction.Toolkit.Interactors.XRBaseInteractor primary;
    UnityEngine.XR.Interaction.Toolkit.Interactors.XRBaseInteractor secondary;
    UnityEngine.XR.Interaction.Toolkit.Interactors.XRBaseInteractor lastInteractor;
    XRInteractionManager interactionManager;
    Vector3 primaryLastPos, secondaryLastPos;
    Quaternion primaryLastRot, secondaryLastRot;
    Vector3 primaryVelocity, secondaryVelocity;
    Vector3 primaryAngularVelocity, secondaryAngularVelocity;
    Vector3 lastThrowVelocity, lastThrowAngularVelocity, releasePoint;
    RigidbodyConstraints originalConstraints;
    Coroutine returnCoroutine;
    bool isThrown;

    void Awake()
    {
        rb = GetComponent<Rigidbody>();
        grab = GetComponent<UnityEngine.XR.Interaction.Toolkit.Interactables.XRGrabInteractable>();
        if (rb == null) rb = gameObject.AddComponent<Rigidbody>();
        rb.mass = mass;
        rb.isKinematic = false;
        rb.useGravity = true;
        rb.interpolation = RigidbodyInterpolation.Interpolate;
        rb.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
        rb.maxAngularVelocity = maxAngularSpeed;
        originalConstraints = rb.constraints;
        if (grab == null) grab = gameObject.AddComponent<UnityEngine.XR.Interaction.Toolkit.Interactables.XRGrabInteractable>();
        grab.selectEntered.AddListener(OnSelectEntered);
        grab.selectExited.AddListener(OnSelectExited);
    }

    void OnDestroy()
    {
        if (grab != null)
        {
            grab.selectEntered.RemoveListener(OnSelectEntered);
            grab.selectExited.RemoveListener(OnSelectExited);
        }
    }

    void OnSelectEntered(SelectEnterEventArgs args)
    {
        StopAutomaticReturn();
        interactionManager = args.manager;
        var inter = args.interactorObject as UnityEngine.XR.Interaction.Toolkit.Interactors.XRBaseInteractor;
        if (inter == null) return;
        rb.constraints = originalConstraints;
        isThrown = false;
        if (primary == null)
        {
            primary = inter;
            primaryLastPos = primary.transform.position;
            primaryLastRot = primary.transform.rotation;
            primaryVelocity = Vector3.zero;
            primaryAngularVelocity = Vector3.zero;
        }
        else if (secondary == null && inter != primary)
        {
            secondary = inter;
            secondaryLastPos = secondary.transform.position;
            secondaryLastRot = secondary.transform.rotation;
            secondaryVelocity = Vector3.zero;
            secondaryAngularVelocity = Vector3.zero;
        }
    }

    void OnSelectExited(SelectExitEventArgs args)
    {
        var inter = args.interactorObject as UnityEngine.XR.Interaction.Toolkit.Interactors.XRBaseInteractor;
        if (inter == null) return;
        lastInteractor = inter;
        if (inter == secondary) secondary = null;
        else if (inter == primary)
        {
            if (secondary != null)
            {
                primary = secondary;
                primaryLastPos = primary.transform.position;
                primaryLastRot = primary.transform.rotation;
                secondary = null;
            }
            else primary = null;
        }
        if (primary == null && secondary == null)
        {
            ReleaseAsProjectile();
            if (autoReturnToLastHand) returnCoroutine = StartCoroutine(ReturnToLastHand());
        }
    }

    void Update()
    {
        float dt = Mathf.Max(Time.deltaTime, 0.0001f);
        if (primary != null) UpdateHandVelocity(primary.transform, ref primaryLastPos, ref primaryLastRot, ref primaryVelocity, ref primaryAngularVelocity, dt);
        if (secondary != null) UpdateHandVelocity(secondary.transform, ref secondaryLastPos, ref secondaryLastRot, ref secondaryVelocity, ref secondaryAngularVelocity, dt);
        if (primary != null && secondary != null)
        {
            lastThrowVelocity = (primaryVelocity + secondaryVelocity) * 0.5f;
            lastThrowAngularVelocity = (primaryAngularVelocity + secondaryAngularVelocity) * 0.5f;
        }
        else if (primary != null)
        {
            lastThrowVelocity = primaryVelocity;
            lastThrowAngularVelocity = primaryAngularVelocity;
        }
        if (isThrown && Vector3.Distance(releasePoint, transform.position) >= maxRange)
        {
            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
            // Stop the throw at the range limit but keep gravity enabled so the object falls.
            rb.isKinematic = false;
            isThrown = false;
        }
    }

    void UpdateHandVelocity(Transform hand, ref Vector3 lastPosition, ref Quaternion lastRotation, ref Vector3 velocity, ref Vector3 angularVelocity, float dt)
    {
        float smoothing = 1f / Mathf.Max(velocitySampleFrames, 1);
        velocity = Vector3.Lerp(velocity, (hand.position - lastPosition) / dt, smoothing);
        Quaternion deltaRotation = hand.rotation * Quaternion.Inverse(lastRotation);
        deltaRotation.ToAngleAxis(out float angle, out Vector3 axis);
        if (angle > 180f) angle -= 360f;
        angularVelocity = Vector3.Lerp(angularVelocity, axis * (angle * Mathf.Deg2Rad / dt), smoothing);
        lastPosition = hand.position;
        lastRotation = hand.rotation;
    }

    void ReleaseAsProjectile()
    {
        rb.isKinematic = false;
        if (lastThrowVelocity.magnitude < 0.25f) lastThrowVelocity = transform.forward * 1.5f;
        if (lastThrowVelocity.magnitude > maxSpeed) lastThrowVelocity = lastThrowVelocity.normalized * maxSpeed;
        rb.linearVelocity = lastThrowVelocity;
        rb.angularVelocity = Vector3.ClampMagnitude(lastThrowAngularVelocity, maxAngularSpeed);
        releasePoint = transform.position;
        isThrown = true;
    }

    IEnumerator ReturnToLastHand()
    {
        yield return new WaitForSeconds(autoReturnDelay);
        if (lastInteractor == null || grab.isSelected) yield break;
        rb.isKinematic = true;
        rb.constraints = originalConstraints;
        float distance;
        do
        {
            if (lastInteractor == null || grab.isSelected) yield break;
            distance = Vector3.Distance(transform.position, lastInteractor.transform.position);
            transform.position = Vector3.MoveTowards(transform.position, lastInteractor.transform.position, autoReturnSpeed * Time.deltaTime);
            transform.rotation = Quaternion.Slerp(transform.rotation, lastInteractor.transform.rotation, 8f * Time.deltaTime);
            yield return null;
        } while (distance > 0.03f);
        if (interactionManager != null && !grab.isSelected)
        {
            interactionManager.SelectEnter(
                (UnityEngine.XR.Interaction.Toolkit.Interactors.IXRSelectInteractor)lastInteractor,
                (UnityEngine.XR.Interaction.Toolkit.Interactables.IXRSelectInteractable)grab);
        }
    }

    void StopAutomaticReturn()
    {
        if (returnCoroutine != null)
        {
            StopCoroutine(returnCoroutine);
            returnCoroutine = null;
        }
    }
}
