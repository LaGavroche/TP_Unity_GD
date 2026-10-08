using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Filtering;
using UnityEngine.XR.Interaction.Toolkit.Interactables;
using UnityEngine.XR.Interaction.Toolkit.Interactors;

/// <summary>
/// Objet lançable. La vitesse dépend du geste et de la masse,
/// la rotation ralentit quand l'objet est plus long et plus lourd.
/// </summary>
[RequireComponent(typeof(Rigidbody))]
public class ThrowableAxe : MonoBehaviour
{
    [SerializeField]
    [Tooltip("Masse en kilogrammes. Une masse plus haute sort plus lentement de la main.")]
    float mass = 1.5f;

    [SerializeField]
    [Tooltip("Longueur visée du manche, en mètres.")]
    float targetLength = 0.9f;

    [SerializeField]
    [Tooltip("Vitesse maximale du lancer, en m/s.")]
    float maxThrowSpeed = 6.3f;

    [SerializeField]
    [Tooltip("Portée maximale du lancer, en mètres. 0 = illimité.")]
    float maxThrowDistance = 4f;

    [SerializeField]
    [Tooltip("Marge autour du manche, en mètres. La saisie ne fonctionne que sur le manche.")]
    float retrieveDistance = 0.04f;

    [SerializeField]
    [Tooltip("Délai avant le retour au point de départ, une fois l'objet arrêté.")]
    float respawnDelay = 3f;

    [SerializeField]
    [Tooltip("La hache reste en place tant qu'elle n'a pas été lancée.")]
    bool holdUntilThrown = true;

    [SerializeField]
    [Tooltip("Vitesse de la main en dessous de laquelle l'objet tombe sans rotation forcée.")]
    float minThrowSpeed = 1.1f;

    [SerializeField]
    [Tooltip("Part du geste transmise à l'objet avant la limite de vitesse.")]
    float throwVelocityScale = 1.15f;

    [SerializeField]
    [Tooltip("Tours par seconde d'un objet de référence lancé à pleine vitesse.")]
    float referenceSpin = 1.8f;

    [SerializeField]
    [Tooltip("Masse de référence utilisée pour comparer les objets.")]
    float referenceMass = 1.5f;

    [SerializeField]
    [Tooltip("Longueur de référence utilisée pour comparer les objets.")]
    float referenceLength = 0.9f;

    [SerializeField]
    [Tooltip("1 ou -1. Inverse le sens de rotation.")]
    float spinSign = 1f;

    const float GrabPadding = 0.03f;
    const float SettleSpeed = 0.35f;

    Rigidbody body;
    XRGrabInteractable grab;
    Transform grip;
    Transform handle;

    BoxCollider handleGrabCollider;
    Transform grabZone;
    Renderer grabZoneRenderer;
    Material grabZoneMaterial;
    Transform grabHint;
    TextMesh grabHintText;
    static readonly Color ZoneIdleColor = new Color(0.2f, 0.85f, 0.35f, 0.28f);
    static readonly Color ZoneReadyColor = new Color(0.25f, 1f, 0.4f, 0.45f);
    static readonly Color HintColor = new Color(0.55f, 1f, 0.62f, 1f);

    readonly List<Collider> grabColliders = new List<Collider>();
    XRBaseInteractor[] interactors = System.Array.Empty<XRBaseInteractor>();
    XRSelectFilterDelegate selectFilter;

    Vector3 previousPosition;
    Vector3 smoothedVelocity;
    Vector3 pendingThrowVelocity;
    Vector3 handleAxisLocal = Vector3.up;
    Vector3 throwOrigin;
    Vector3 spawnPosition;
    Quaternion spawnRotation;
    float measuredLength = 0.9f;
    float respawnTimer = -1f;
    bool hasBeenThrown;
    bool listening;
    bool limitThrowDistance;
    InteractorHandedness heldHand = InteractorHandedness.None;

    void Awake()
    {
        body = GetComponent<Rigidbody>();
        body.isKinematic = true;
        body.useGravity = false;
        
    }

    void KeepPiecesTogether()
    {
        Transform head = FindExact("Head");
        Transform hundle = FindExact("Hundle");
        if (head == null || hundle == null)
            return;

        StripOwnPhysics(head.gameObject);
        StripOwnPhysics(hundle.gameObject);

        if (head.parent != hundle.parent && hundle.parent != null)
            head.SetParent(hundle.parent, false);

        head.localPosition = Vector3.zero;
        head.localRotation = Quaternion.identity;
        head.localScale = Vector3.one;
        hundle.localPosition = Vector3.zero;
        hundle.localRotation = Quaternion.identity;
        hundle.localScale = Vector3.one;
    }

    static void StripOwnPhysics(GameObject piece)
    {
        Rigidbody pieceBody = piece.GetComponent<Rigidbody>();
        if (pieceBody != null)
            DestroyImmediate(pieceBody);

        Collider[] colliders = piece.GetComponents<Collider>();
        for (int i = 0; i < colliders.Length; i++)
            DestroyImmediate(colliders[i]);
    }

    void Start()
    {
        KeepPiecesTogether();
        FitVisualScale();
        measuredLength = Mathf.Max(LongestMeshLength(), 0.05f);
        PlaceGrip();
        BuildColliders();
        ConfigureBody();
        CreateGrabGuides();

        spawnPosition = transform.position;
        spawnRotation = transform.rotation;
        previousPosition = transform.position;
        interactors = FindObjectsByType<XRBaseInteractor>(FindObjectsSortMode.None);
        HandGrabPoseDriver.Ensure();
        Subscribe();
    }

    void OnEnable()
    {
        Subscribe();
    }

    void OnDisable()
    {
        if (grab != null && listening)
        {
            grab.selectEntered.RemoveListener(OnGrabbed);
            grab.selectExited.RemoveListener(OnReleased);
            if (selectFilter != null)
                grab.selectFilters.Remove(selectFilter);
        }

        listening = false;
    }

    void Subscribe()
    {
        if (grab == null || listening)
            return;

        grab.selectEntered.AddListener(OnGrabbed);
        grab.selectExited.AddListener(OnReleased);
        listening = true;
    }

    void Update()
    {
        float dt = Mathf.Max(Time.deltaTime, 0.0001f);
        Vector3 instant = (transform.position - previousPosition) / dt;
        if (grab != null && grab.isSelected)
            smoothedVelocity = Vector3.Lerp(smoothedVelocity, instant, 1f - Mathf.Exp(-14f * dt));
        previousPosition = transform.position;

        bool held = grab != null && grab.isSelected;
        bool inReach = !held && IsHandNear(InteractorHandedness.None);
        UpdateGrabGuides(inReach, held);

        if (respawnTimer >= 0f && !held)
        {
            respawnTimer -= dt;
            if (respawnTimer <= 0f)
                Respawn();
        }
    }

    void LateUpdate()
    {
        HandGrabPoseDriver.Request(InteractorHandedness.Left, CurlFor(InteractorHandedness.Left));
        HandGrabPoseDriver.Request(InteractorHandedness.Right, CurlFor(InteractorHandedness.Right));
    }

    void FixedUpdate()
    {
        if (body == null || (grab != null && grab.isSelected))
            return;

        if (limitThrowDistance && maxThrowDistance > 0f)
        {
            Vector3 fromOrigin = body.position - throwOrigin;
            if (fromOrigin.sqrMagnitude > maxThrowDistance * maxThrowDistance)
            {
                body.position = throwOrigin + fromOrigin.normalized * maxThrowDistance;
                body.linearVelocity = Vector3.zero;
                body.angularVelocity = Vector3.zero;
                limitThrowDistance = false;
            }
        }

        if (!hasBeenThrown)
            return;

        float speed = body.linearVelocity.magnitude;
        if (speed < SettleSpeed)
        {
            if (respawnTimer < 0f)
                respawnTimer = respawnDelay;
        }
        else
        {
            respawnTimer = -1f;
        }
    }

    float CurlFor(InteractorHandedness hand)
    {
        if (grab != null && grab.isSelected && heldHand == hand)
            return 1f;

        if (IsHandNear(hand))
            return 0.42f;

        return 0f;
    }

    bool IsHandNear(InteractorHandedness hand)
    {
        for (int i = 0; i < interactors.Length; i++)
        {
            XRBaseInteractor interactor = interactors[i];
            if (interactor == null || !interactor.isActiveAndEnabled)
                continue;

            if (hand != InteractorHandedness.None && interactor.handedness != hand)
                continue;

            if (IsPointOnHandle(interactor.GetAttachTransform(grab).position))
                return true;
        }

        return false;
    }

    bool IsPointOnHandle(Vector3 worldPoint)
    {
        if (handleGrabCollider == null)
            return false;

        Vector3 closest = handleGrabCollider.ClosestPoint(worldPoint);
        float extra = Mathf.Max(retrieveDistance, 0f);
        return (worldPoint - closest).sqrMagnitude <= extra * extra;
    }

    void OnGrabbed(SelectEnterEventArgs args)
    {
        limitThrowDistance = false;
        respawnTimer = -1f;
        heldHand = args.interactorObject.handedness;
    }

    void OnReleased(SelectExitEventArgs args)
    {
        heldHand = InteractorHandedness.None;
        if (args.isCanceled)
            return;

        hasBeenThrown = true;
        pendingThrowVelocity = smoothedVelocity;
        StartCoroutine(ApplyThrowAfterDetach());
    }

    IEnumerator ApplyThrowAfterDetach()
    {
        yield return new WaitForEndOfFrame();

        if (body == null || (grab != null && grab.isSelected))
            yield break;

        body.isKinematic = false;
        body.useGravity = true;

        float userSpeed = pendingThrowVelocity.magnitude;
        float massRatio = referenceMass / Mathf.Max(mass, 0.05f);
        float speed = Mathf.Min(userSpeed * throwVelocityScale * massRatio, maxThrowSpeed);

        throwOrigin = body.position;
        if (speed < minThrowSpeed)
        {
            limitThrowDistance = false;
            body.linearVelocity = pendingThrowVelocity * massRatio;
            yield break;
        }

        Vector3 direction = pendingThrowVelocity.sqrMagnitude > 0.0001f
            ? pendingThrowVelocity.normalized
            : transform.forward;

        limitThrowDistance = maxThrowDistance > 0f;
        body.linearVelocity = direction * speed;
        body.angularVelocity = SpinAxis(direction) * SpinRadians(speed);
    }

    float SpinRadians(float speed)
    {
        float throwAmount = Mathf.InverseLerp(minThrowSpeed, Mathf.Max(maxThrowSpeed, minThrowSpeed + 0.01f), speed);
        float inertia = (mass / Mathf.Max(referenceMass, 0.05f)) * (measuredLength / Mathf.Max(referenceLength, 0.05f));
        float revolutions = referenceSpin * throwAmount / Mathf.Max(inertia, 0.2f);
        return revolutions * Mathf.PI * 2f * Mathf.Sign(spinSign);
    }

    Vector3 SpinAxis(Vector3 direction)
    {
        Vector3 handleDirection = transform.TransformDirection(handleAxisLocal);
        if (handleDirection.sqrMagnitude < 0.0001f)
            handleDirection = transform.up;
        handleDirection.Normalize();

        Vector3 axis = Vector3.Cross(handleDirection, direction);
        if (axis.sqrMagnitude < 0.04f)
        {
            axis = Vector3.Cross(direction, Vector3.up);
            if (axis.sqrMagnitude < 0.04f)
                axis = Vector3.Cross(direction, Vector3.right);
        }

        return axis.normalized;
    }

    void Respawn()
    {
        respawnTimer = -1f;
        hasBeenThrown = false;
        limitThrowDistance = false;
        smoothedVelocity = Vector3.zero;
        body.linearVelocity = Vector3.zero;
        body.angularVelocity = Vector3.zero;
        body.useGravity = !holdUntilThrown;
        body.isKinematic = holdUntilThrown;
        body.position = spawnPosition;
        body.rotation = spawnRotation;
        transform.SetPositionAndRotation(spawnPosition, spawnRotation);
    }

    bool CanSelect(IXRSelectInteractor interactor, IXRSelectInteractable interactable)
    {
        Vector3 point = interactor.GetAttachTransform(interactable).position;
        return IsPointOnHandle(point);
    }

    void ConfigureBody()
    {
        body.mass = mass;
        body.useGravity = !holdUntilThrown;
        body.isKinematic = holdUntilThrown;
        body.interpolation = RigidbodyInterpolation.Interpolate;
        body.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
        body.maxAngularVelocity = 40f;
        body.linearDamping = 0.05f;
        body.angularDamping = 0.04f;
    }

    void ConfigureGrab()
    {
        grab = GetComponent<XRGrabInteractable>();
        if (grab == null)
            grab = gameObject.AddComponent<XRGrabInteractable>();

        grab.interactionLayers = -1;
        grab.movementType = XRBaseInteractable.MovementType.Instantaneous;
        grab.attachTransform = grip;
        grab.useDynamicAttach = false;
        grab.matchAttachPosition = true;
        grab.matchAttachRotation = true;
        grab.throwOnDetach = false;
        grab.forceGravityOnDetach = true;

        grab.colliders.Clear();
        for (int i = 0; i < grabColliders.Count; i++)
        {
            if (grabColliders[i] != null)
                grab.colliders.Add(grabColliders[i]);
        }

        selectFilter = new XRSelectFilterDelegate(CanSelect);
        grab.selectFilters.Add(selectFilter);
    }

    void FitVisualScale()
    {
        float length = LongestMeshLength();
        if (length < 0.01f || targetLength <= 0f)
            return;

        float factor = targetLength / length;
        if (Mathf.Abs(factor - 1f) < 0.08f)
            return;

        for (int i = 0; i < transform.childCount; i++)
        {
            Transform child = transform.GetChild(i);
            if (child.name == "Poignee")
                continue;
            child.localScale *= factor;
        }
    }

    void BuildColliders()
    {
        handle = FindExact("Manche");

        grabColliders.Clear();
        handleGrabCollider = handle != null ? handle.GetComponent<BoxCollider>() : null;
        if (handleGrabCollider != null)
            grabColliders.Add(handleGrabCollider);

        ConfigureGrab();
    }

    Transform FindExact(string name)
    {
        Transform[] transforms = GetComponentsInChildren<Transform>(true);
        for (int i = 0; i < transforms.Length; i++)
        {
            if (string.Equals(transforms[i].name, name, System.StringComparison.OrdinalIgnoreCase))
                return transforms[i];
        }

        return null;
    }

    BoxCollider FitBox(Transform target, float padding)
    {
        MeshFilter filter = target.GetComponent<MeshFilter>();
        if (filter == null)
            filter = target.GetComponentInChildren<MeshFilter>();

        BoxCollider col = target.GetComponent<BoxCollider>();
        if (col == null)
            col = target.gameObject.AddComponent<BoxCollider>();

        if (filter != null && filter.sharedMesh != null)
        {
            Vector3 scale = filter.transform.lossyScale;
            Vector3 pad = new Vector3(
                padding / Mathf.Max(Mathf.Abs(scale.x), 0.0001f),
                padding / Mathf.Max(Mathf.Abs(scale.y), 0.0001f),
                padding / Mathf.Max(Mathf.Abs(scale.z), 0.0001f));
            col.center = filter.sharedMesh.bounds.center;
            col.size = filter.sharedMesh.bounds.size + pad * 2f;
        }

        col.enabled = true;
        col.isTrigger = false;
        return col;
    }

    float LongestMeshLength()
    {
        float longest = 0f;
        MeshFilter[] filters = GetComponentsInChildren<MeshFilter>();
        for (int i = 0; i < filters.Length; i++)
        {
            Mesh mesh = filters[i].sharedMesh;
            if (mesh == null)
                continue;

            Vector3 size = Vector3.Scale(mesh.bounds.size, Abs(filters[i].transform.lossyScale));
            longest = Mathf.Max(longest, size.x, size.y, size.z);
        }

        return longest;
    }

    static Vector3 Abs(Vector3 value)
    {
        return new Vector3(Mathf.Abs(value.x), Mathf.Abs(value.y), Mathf.Abs(value.z));
    }

    void PlaceGrip()
    {
        Transform handlePart = FindNamed("Hundle");
        if (handlePart == null)
            handlePart = FindNamed("Handle");
        Transform head = FindNamed("Head");

        Vector3 handlePoint = boundsCenterOf(handlePart);
        Vector3 headPoint = boundsCenterOf(head);
        if (handlePart != null && head != null && (headPoint - handlePoint).sqrMagnitude > 0.0004f)
        {
            Vector3 worldAxis = (headPoint - handlePoint).normalized;
            handleAxisLocal = transform.InverseTransformDirection(worldAxis).normalized;
            CreateGrip(handlePoint, worldAxis);
            return;
        }

        Bounds bounds = ComputeLocalBounds();
        handleAxisLocal = Vector3.up;
        float longest = bounds.size.y;
        if (bounds.size.x > longest)
        {
            handleAxisLocal = Vector3.right;
            longest = bounds.size.x;
        }

        if (bounds.size.z > longest)
            handleAxisLocal = Vector3.forward;

        Vector3 gripLocal = bounds.center - handleAxisLocal.normalized * (longest * 0.32f);
        CreateGrip(transform.TransformPoint(gripLocal), transform.TransformDirection(handleAxisLocal));
    }

    void CreateGrip(Vector3 worldPosition, Vector3 handleWorldAxis)
    {
        grip = transform.Find("Poignee");
        if (grip == null)
        {
            var gripObject = new GameObject("Poignee");
            grip = gripObject.transform;
            grip.SetParent(transform, false);
        }

        Vector3 up = handleWorldAxis.sqrMagnitude > 0.0001f ? handleWorldAxis.normalized : Vector3.up;
        Vector3 forward = Vector3.Cross(up, Vector3.right);
        if (forward.sqrMagnitude < 0.01f)
            forward = Vector3.Cross(up, Vector3.forward);

        grip.SetPositionAndRotation(worldPosition, Quaternion.LookRotation(forward.normalized, up));
    }

    Transform FindNamed(string token)
    {
        Transform[] transforms = GetComponentsInChildren<Transform>(true);
        for (int i = 0; i < transforms.Length; i++)
        {
            if (transforms[i].name.IndexOf(token, System.StringComparison.OrdinalIgnoreCase) >= 0)
                return transforms[i];
        }

        return null;
    }

    Vector3 boundsCenterOf(Transform part)
    {
        if (part == null)
            return transform.position;

        Renderer renderer = part.GetComponent<Renderer>();
        if (renderer == null)
            renderer = part.GetComponentInChildren<Renderer>();
        return renderer != null ? renderer.bounds.center : part.position;
    }

    Bounds ComputeLocalBounds()
    {
        Renderer[] renderers = GetComponentsInChildren<Renderer>();
        bool hasBounds = false;
        Bounds local = new Bounds(Vector3.zero, Vector3.zero);

        for (int i = 0; i < renderers.Length; i++)
        {
            Bounds world = renderers[i].bounds;
            Vector3 center = world.center;
            Vector3 extents = world.extents;
            for (int x = -1; x <= 1; x += 2)
            {
                for (int y = -1; y <= 1; y += 2)
                {
                    for (int z = -1; z <= 1; z += 2)
                    {
                        Vector3 corner = transform.InverseTransformPoint(center + Vector3.Scale(extents, new Vector3(x, y, z)));
                        if (!hasBounds)
                        {
                            local = new Bounds(corner, Vector3.zero);
                            hasBounds = true;
                        }
                        else
                        {
                            local.Encapsulate(corner);
                        }
                    }
                }
            }
        }

        return local;
    }

    void CreateGrabGuides()
    {
        if (handleGrabCollider == null)
            return;

        var zoneObject = GameObject.CreatePrimitive(PrimitiveType.Cube);
        zoneObject.name = "Zone de prise";
        grabZone = zoneObject.transform;
        FitZoneToHandle();

        Collider zoneCollider = zoneObject.GetComponent<Collider>();
        if (zoneCollider != null)
            DestroyImmediate(zoneCollider);

        grabZoneRenderer = zoneObject.GetComponent<Renderer>();
        grabZoneMaterial = CreateTransparentMaterial(ZoneIdleColor);
        grabZoneRenderer.sharedMaterial = grabZoneMaterial;
        grabZoneRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        grabZoneRenderer.receiveShadows = false;

        var hintObject = new GameObject("Message de prise");
        grabHint = hintObject.transform;
        PlaceGrabHint();

        grabHintText = hintObject.AddComponent<TextMesh>();
        grabHintText.text = "Tu peux saisir\nSerre la gâchette";
        grabHintText.anchor = TextAnchor.MiddleCenter;
        grabHintText.alignment = TextAlignment.Center;
        grabHintText.fontSize = 64;
        grabHintText.characterSize = 0.025f;
        grabHintText.color = HintColor;
        grabHintText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");

        Renderer hintRenderer = grabHintText.GetComponent<Renderer>();
        Material hintMaterial = CreateTransparentMaterial(HintColor);
        hintMaterial.DisableKeyword("_ALPHAPREMULTIPLY_ON");
        if (grabHintText.font != null)
            hintMaterial.SetTexture("_BaseMap", grabHintText.font.material.mainTexture);
        hintRenderer.sharedMaterial = hintMaterial;
        hintRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        hintRenderer.receiveShadows = false;
        grabHint.gameObject.SetActive(false);
    }

    void FitZoneToHandle()
    {
        if (grabZone == null || handleGrabCollider == null)
            return;

        Transform parent = handleGrabCollider.transform;
        grabZone.SetParent(parent, false);
        grabZone.localRotation = Quaternion.identity;
        grabZone.localPosition = handleGrabCollider.center;

        Vector3 scale = parent.lossyScale;
        float extra = Mathf.Max(retrieveDistance, 0f);
        Vector3 pad = new Vector3(
            extra / Mathf.Max(Mathf.Abs(scale.x), 0.0001f),
            extra / Mathf.Max(Mathf.Abs(scale.y), 0.0001f),
            extra / Mathf.Max(Mathf.Abs(scale.z), 0.0001f));
        grabZone.localScale = handleGrabCollider.size + pad * 2f;
    }

    void PlaceGrabHint()
    {
        if (grabHint == null || handleGrabCollider == null)
            return;

        grabHint.SetParent(handleGrabCollider.transform, false);
        Vector3 size = handleGrabCollider.size;
        Vector3 offset = Vector3.right * (size.x * 0.5f + 0.08f);
        if (size.z <= size.x && size.z <= size.y)
            offset = Vector3.forward * (size.z * 0.5f + 0.08f);
        else if (size.y <= size.x && size.y <= size.z)
            offset = Vector3.up * (size.y * 0.5f + 0.08f);

        grabHint.localPosition = handleGrabCollider.center + offset;
    }

    void UpdateGrabGuides(bool inReach, bool held)
    {
        if (grabZone != null)
            grabZone.gameObject.SetActive(!held);

        if (grabZoneMaterial != null && !held)
        {
            Color color = inReach ? ZoneReadyColor : ZoneIdleColor;
            if (inReach)
            {
                float pulse = 0.75f + 0.25f * Mathf.Sin(Time.time * 7f);
                color.a *= pulse;
            }

            grabZoneMaterial.SetColor("_BaseColor", color);
            grabZoneMaterial.color = color;
        }

        if (grabHint != null)
        {
            grabHint.gameObject.SetActive(inReach);
            if (inReach)
                FaceCamera(grabHint);
        }
    }

    static void FaceCamera(Transform target)
    {
        Camera cam = Camera.main;
        if (cam == null)
            return;

        Vector3 towardCamera = cam.transform.position - target.position;
        if (towardCamera.sqrMagnitude < 0.0001f)
            return;

        target.rotation = Quaternion.LookRotation(towardCamera);
    }

    static Material CreateTransparentMaterial(Color color)
    {
        Shader shader = Shader.Find("Universal Render Pipeline/Unlit");
        if (shader == null)
            shader = Shader.Find("Unlit/Color");

        var material = new Material(shader);
        material.SetFloat("_Surface", 1f);
        material.SetFloat("_Blend", 0f);
        material.SetOverrideTag("RenderType", "Transparent");
        material.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
        material.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
        material.SetInt("_ZWrite", 0);
        material.SetInt("_Cull", (int)UnityEngine.Rendering.CullMode.Off);
        material.DisableKeyword("_ALPHATEST_ON");
        material.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
        material.EnableKeyword("_ALPHAPREMULTIPLY_ON");
        material.renderQueue = 3000;
        material.SetColor("_BaseColor", color);
        material.color = color;
        return material;
    }
}
