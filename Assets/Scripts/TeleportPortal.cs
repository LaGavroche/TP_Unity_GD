using System.Collections;
using Unity.XR.CoreUtils;
using UnityEngine;

public class TeleportPortal : MonoBehaviour
{
    [Header("Dans le prefab")]
    [SerializeField] private Transform arrivalPoint;

    [Header("À régler dans la scène")]
    [SerializeField] private TeleportPortal targetPortal;

    [SerializeField] private float cooldown = 0.5f;

    private static bool isTeleporting;

    public Transform ArrivalPoint => arrivalPoint;

    private void OnTriggerEnter(Collider other)
    {
        if (isTeleporting) return;
        if (!other.CompareTag("Player")) return;

        if (targetPortal == null || targetPortal.ArrivalPoint == null)
        {
            Debug.LogWarning($"{name} : aucun portail cible assigné.", this);
            return;
        }

        var origin = other.GetComponentInParent<XROrigin>();
        if (origin == null) return;

        StartCoroutine(TeleportRoutine(origin));
    }

    private IEnumerator TeleportRoutine(XROrigin origin)
    {
        isTeleporting = true;

        if (ScreenFader.Instance != null)
            yield return StartCoroutine(ScreenFader.Instance.FadeOut());

        MovePlayer(origin, targetPortal.ArrivalPoint);

        if (ScreenFader.Instance != null)
            yield return StartCoroutine(ScreenFader.Instance.FadeIn());

        yield return new WaitForSeconds(cooldown);
        isTeleporting = false;
    }

    private void MovePlayer(XROrigin origin, Transform target)
    {
        var controller = origin.GetComponent<CharacterController>();
        if (controller != null) controller.enabled = false;

        Vector3 headHeight = origin.Origin.transform.up * origin.CameraInOriginSpaceHeight;
        origin.MoveCameraToWorldLocation(target.position + headHeight);
        origin.MatchOriginUpCameraForward(target.up, target.forward);

        if (controller != null) controller.enabled = true;
    }

    private void OnDrawGizmos()
    {
        if (arrivalPoint != null)
        {
            Gizmos.color = Color.green;
            Gizmos.DrawWireSphere(arrivalPoint.position, 0.25f);
        }
        if (targetPortal != null)
        {
            Gizmos.color = Color.cyan;
            Gizmos.DrawLine(transform.position, targetPortal.transform.position);
        }
    }
}