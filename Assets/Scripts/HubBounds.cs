using UnityEngine;

// Garde le joueur dans le hub, quelle que soit la façon dont il se déplace
// (stick, téléportation, marche réelle). À mettre sur le XR Origin.
public class HubBounds : MonoBehaviour
{
    public Vector3 center = Vector3.zero;   // centre du hub (au sol)
    public float maxRadius = 5.2f;          // rayon max pour la tête du joueur
    public float fallY = -2f;               // en dessous de cette hauteur : retour au point de départ

    Camera head;
    Vector3 startPosition;

    void Awake()
    {
        head = GetComponentInChildren<Camera>();
        startPosition = transform.position;
    }

    void LateUpdate()
    {
        if (head == null) return;

        // Tombé hors de la scène : retour au départ
        if (head.transform.position.y < fallY)
        {
            transform.position = startPosition;
            return;
        }

        // Hors du cercle : on ramène l'origine pour que la tête reste dans le rayon
        Vector3 flat = head.transform.position - center;
        flat.y = 0f;
        float dist = flat.magnitude;
        if (dist > maxRadius)
        {
            transform.position -= flat.normalized * (dist - maxRadius);
        }
    }
}
