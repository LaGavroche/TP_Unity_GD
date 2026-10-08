using System.Collections;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

[RequireComponent(typeof(Rigidbody), typeof(XRGrabInteractable))]
public class ThrowByMass : MonoBehaviour
{
    [Header("Portée selon la masse")]
    public float lightMass = 0.15f;
    public float heavyMass = 3f;
    public float maxRange = 40f;
    public float minRange = 4f;
    public float strongHandSpeed = 7f;
    public float gravityMultiplier = 1f;

    [Header("Rotation selon longueur et masse")]
    public float length = 0f;
    public float referenceSpin = 20f;
    public float referenceInertia = 0.0025f;
    public float strongHandSpin = 15f;
    public float flightAngularDamping = 0.05f;
    public float groundAngularDamping = 1f;

    [Header("Résultat (lecture seule)")]
    public float range;
    public float maxSpeed;
    public float maxSpin;

    private Rigidbody rb;
    private XRGrabInteractable grab;
    private bool flying;

    void Awake()
    {
        rb = GetComponent<Rigidbody>();
        grab = GetComponent<XRGrabInteractable>();
        Compute();
        grab.throwVelocityScale = maxSpeed / strongHandSpeed;
        grab.throwAngularVelocityScale = maxSpin / strongHandSpin;
        rb.maxAngularVelocity = maxSpin;
        grab.selectEntered.AddListener(OnGrab);
        grab.selectExited.AddListener(OnRelease);
    }

    void FixedUpdate()
    {
        if (grab.isSelected) return;

        if (rb.useGravity && gravityMultiplier != 1f)
            rb.AddForce(Physics.gravity * (gravityMultiplier - 1f), ForceMode.Acceleration);

        if (!flying && rb.linearVelocity.magnitude < 0.15f && rb.angularVelocity.magnitude < 0.5f)
            rb.Sleep();
    }

    void Compute()
    {
        Rigidbody body = rb != null ? rb : GetComponent<Rigidbody>();
        if (body == null) return;
        float mass = Mathf.Max(body.mass, 0.01f);

        float t = Mathf.InverseLerp(Mathf.Log(lightMass), Mathf.Log(heavyMass), Mathf.Log(mass));
        range = Mathf.Lerp(maxRange, minRange, t);
        maxSpeed = Mathf.Sqrt(range * Physics.gravity.magnitude * gravityMultiplier);

        float l = length > 0f ? length : MeasureLength();
        float inertia = mass * l * l / 12f;
        maxSpin = referenceSpin / Mathf.Sqrt(Mathf.Max(inertia, 0.0001f) / referenceInertia);
    }

    float MeasureLength()
    {
        MeshFilter mf = GetComponentInChildren<MeshFilter>();
        if (mf == null || mf.sharedMesh == null) return 0.3f;
        Vector3 s = Vector3.Scale(mf.sharedMesh.bounds.size, mf.transform.lossyScale);
        return Mathf.Max(s.x, s.y, s.z);
    }

    void OnGrab(SelectEnterEventArgs args)
    {
        flying = false;
    }

    void OnRelease(SelectExitEventArgs args)
    {
        flying = true;
        rb.angularDamping = flightAngularDamping;
        StartCoroutine(ClampAfterThrow());
    }

    IEnumerator ClampAfterThrow()
    {
        yield return null;
        yield return new WaitForFixedUpdate();
        rb.linearVelocity = Vector3.ClampMagnitude(rb.linearVelocity, maxSpeed);
        rb.angularVelocity = Vector3.ClampMagnitude(rb.angularVelocity, maxSpin);
    }

    void OnCollisionEnter(Collision collision)
    {
        if (!flying) return;
        flying = false;
        rb.angularDamping = groundAngularDamping;
    }

    void OnValidate()
    {
        Compute();
    }
}
