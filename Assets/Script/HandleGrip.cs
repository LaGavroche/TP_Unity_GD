using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

[DefaultExecutionOrder(-200)]
[RequireComponent(typeof(XRGrabInteractable))]
public class HandleGrip : MonoBehaviour
{
    [Tooltip("0 = contre la garde, 1 = contre l'anneau")]
    [Range(0f, 1f)] public float gripPosition = 0.3f;
    [Tooltip("Où commence le manche, en % de la longueur depuis la pointe")]
    [Range(0.3f, 0.9f)] public float guardRatio = 0.6f;
    public bool flipHandleSide = false;
    public Vector3 rotationOffset = Vector3.zero;
    public float handleThickness = 0.04f;

    struct Grip
    {
        public Vector3 handleCenter;
        public Vector3 handleSize;
        public Vector3 attachPos;
        public Quaternion attachRot;
    }

    void Awake()
    {
        if (!TryCompute(out Grip g)) return;
        XRGrabInteractable grab = GetComponent<XRGrabInteractable>();

        Transform handle = transform.Find("handle");
        if (handle == null)
        {
            handle = new GameObject("handle").transform;
            handle.SetParent(transform, false);
        }
        handle.localPosition = Vector3.zero;
        handle.localRotation = Quaternion.identity;
        handle.localScale = Vector3.one;

        BoxCollider col = handle.GetComponent<BoxCollider>();
        if (col == null) col = handle.gameObject.AddComponent<BoxCollider>();
        col.center = g.handleCenter;
        col.size = g.handleSize;
        col.isTrigger = false;

        Transform attach = transform.Find("AttachPoint");
        if (attach == null)
        {
            attach = new GameObject("AttachPoint").transform;
            attach.SetParent(transform, false);
        }
        attach.localPosition = g.attachPos;
        attach.localRotation = g.attachRot;
        attach.localScale = Vector3.one;

        grab.colliders.Clear();
        grab.colliders.Add(col);
        grab.attachTransform = attach;
        grab.useDynamicAttach = false;
        grab.attachEaseInTime = 0.1f;

        if (GetComponent<HandleHighlight>() == null)
            gameObject.AddComponent<HandleHighlight>();
    }

    bool TryCompute(out Grip g)
    {
        g = new Grip();
        MeshFilter mf = GetComponentInChildren<MeshFilter>();
        if (mf == null || mf.sharedMesh == null) return false;

        Bounds b = LocalBounds(mf);
        int axis = LongestAxis(b.size);
        float min = b.min[axis];
        float max = b.max[axis];
        float tipSide = flipHandleSide ? max : min;
        float endSide = flipHandleSide ? min : max;

        float guard = Mathf.Lerp(tipSide, endSide, guardRatio);

        Vector3 c = b.center;
        c[axis] = (guard + endSide) * 0.5f;
        g.handleCenter = c;

        Vector3 s = Vector3.one * handleThickness;
        s[axis] = Mathf.Abs(endSide - guard);
        g.handleSize = s;

        Vector3 p = b.center;
        p[axis] = Mathf.Lerp(guard, endSide, gripPosition);
        g.attachPos = p;

        Vector3 toTip = Vector3.zero;
        toTip[axis] = Mathf.Sign(tipSide - endSide);
        Vector3 up = axis == 1 ? Vector3.forward : Vector3.up;
        g.attachRot = Quaternion.LookRotation(toTip, up) * Quaternion.Euler(rotationOffset);
        return true;
    }

    void OnDrawGizmosSelected()
    {
        if (!TryCompute(out Grip g)) return;
        Gizmos.matrix = transform.localToWorldMatrix;
        Gizmos.color = Color.green;
        Gizmos.DrawWireCube(g.handleCenter, g.handleSize);
        Gizmos.color = Color.yellow;
        Gizmos.DrawSphere(g.attachPos, 0.012f);
        Gizmos.color = Color.blue;
        Gizmos.DrawLine(g.attachPos, g.attachPos + g.attachRot * Vector3.forward * 0.15f);
        Gizmos.color = Color.green;
        Gizmos.DrawLine(g.attachPos, g.attachPos + g.attachRot * Vector3.up * 0.08f);
    }

    Bounds LocalBounds(MeshFilter mf)
    {
        Bounds mb = mf.sharedMesh.bounds;
        Matrix4x4 m = transform.worldToLocalMatrix * mf.transform.localToWorldMatrix;
        Bounds result = new Bounds(m.MultiplyPoint3x4(mb.center), Vector3.zero);
        for (int i = 0; i < 8; i++)
        {
            Vector3 corner = mb.center + Vector3.Scale(mb.extents,
                new Vector3((i & 1) == 0 ? -1 : 1, (i & 2) == 0 ? -1 : 1, (i & 4) == 0 ? -1 : 1));
            result.Encapsulate(m.MultiplyPoint3x4(corner));
        }
        return result;
    }

    int LongestAxis(Vector3 s)
    {
        if (s.x >= s.y && s.x >= s.z) return 0;
        return s.y >= s.z ? 1 : 2;
    }
}
