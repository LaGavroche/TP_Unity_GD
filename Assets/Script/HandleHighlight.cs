using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

[RequireComponent(typeof(XRGrabInteractable))]
public class HandleHighlight : MonoBehaviour
{
    public Color color = new Color(0.2f, 1f, 0.6f, 1f);
    [Range(0f, 1f)] public float hoverAlpha = 0.55f;
    [Range(0f, 1f)] public float idleAlpha = 0.15f;
    public bool alwaysVisible = true;
    public float pulseSpeed = 4f;
    public float pulseAmount = 0.15f;
    public float padding = 1.2f;
    public float fadeSpeed = 10f;

    private XRGrabInteractable grab;
    private Material mat;
    private Renderer rend;
    private float alpha;

    void Start()
    {
        grab = GetComponent<XRGrabInteractable>();

        BoxCollider box = null;
        if (grab.colliders.Count > 0) box = grab.colliders[0] as BoxCollider;
        if (box == null)
        {
            Transform h = transform.Find("handle");
            if (h != null) box = h.GetComponent<BoxCollider>();
        }
        if (box == null)
        {
            enabled = false;
            return;
        }

        GameObject visual = GameObject.CreatePrimitive(PrimitiveType.Cube);
        visual.name = "HandleHighlightVisual";
        Destroy(visual.GetComponent<Collider>());
        visual.transform.SetParent(box.transform, false);
        visual.transform.localPosition = box.center;
        visual.transform.localRotation = Quaternion.identity;
        visual.transform.localScale = box.size * padding;

        rend = visual.GetComponent<Renderer>();
        rend.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        rend.receiveShadows = false;
        mat = new Material(Shader.Find("Sprites/Default"));
        rend.material = mat;
        SetAlpha(0f);
    }

    void Update()
    {
        if (mat == null || rend == null)
        {
            enabled = false;
            return;
        }

        float target = 0f;
        if (grab != null && !grab.isSelected)
        {
            if (grab.isHovered) target = hoverAlpha;
            else if (alwaysVisible) target = idleAlpha;
        }

        alpha = Mathf.MoveTowards(alpha, target, fadeSpeed * Time.deltaTime);
        float pulse = 1f + Mathf.Sin(Time.time * pulseSpeed) * pulseAmount;
        SetAlpha(alpha * pulse);
    }

    void SetAlpha(float a)
    {
        if (mat == null || rend == null)
            return;

        Color c = color;
        c.a = Mathf.Clamp01(a);
        mat.color = c;
        rend.enabled = c.a > 0.01f;
    }

    void OnDestroy()
    {
        if (mat != null) Destroy(mat);
    }
}
