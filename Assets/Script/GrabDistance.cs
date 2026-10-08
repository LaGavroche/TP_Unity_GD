using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit.Attachment;
using UnityEngine.XR.Interaction.Toolkit.Filtering;
using UnityEngine.XR.Interaction.Toolkit.Interactables;
using UnityEngine.XR.Interaction.Toolkit.Interactors;

[RequireComponent(typeof(XRGrabInteractable))]
public class GrabDistance : MonoBehaviour
{
    public float maxDistance = 3f;
    public bool snapToHand = true;

    XRGrabInteractable grab;
    XRSelectFilterDelegate selectFilter;
    XRHoverFilterDelegate hoverFilter;

    void Awake()
    {
        grab = GetComponent<XRGrabInteractable>();
        if (snapToHand) grab.farAttachMode = InteractableFarAttachMode.Near;

        selectFilter = new XRSelectFilterDelegate((interactor, interactable) =>
            grab.isSelected || InRange(interactor));
        hoverFilter = new XRHoverFilterDelegate((interactor, interactable) =>
            grab.isSelected || InRange(interactor));

        grab.selectFilters.Add(selectFilter);
        grab.hoverFilters.Add(hoverFilter);
    }

    void OnDestroy()
    {
        if (grab == null) return;
        grab.selectFilters.Remove(selectFilter);
        grab.hoverFilters.Remove(hoverFilter);
    }

    bool InRange(IXRInteractor interactor)
    {
        Vector3 from = interactor.transform.position;
        Vector3 target = transform.position;
        if (grab.colliders.Count > 0 && grab.colliders[0] != null)
            target = grab.colliders[0].ClosestPoint(from);
        return Vector3.Distance(from, target) <= maxDistance;
    }
}
