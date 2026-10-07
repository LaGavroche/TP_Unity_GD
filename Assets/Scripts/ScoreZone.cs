using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

// Zone de score : à mettre sur un objet avec un collider en "Is Trigger" (ex : le trou du donut).
// Quand un objet lancé entre dans la zone, il rapporte des points à GameEndController.
// Chaque objet n'est compté qu'une seule fois.
[RequireComponent(typeof(Collider))]
public class ScoreZone : MonoBehaviour
{
    public GameEndController gameEnd;       // laissé vide : trouvé automatiquement dans la scène
    public int points = 1;                  // points par objet
    public bool requireGrabbable = true;    // ne compte que les objets attrapables en VR (XR Grab Interactable)

    readonly HashSet<Rigidbody> counted = new HashSet<Rigidbody>();

    void Reset()
    {
        // À l'ajout du composant, le collider devient automatiquement un trigger
        GetComponent<Collider>().isTrigger = true;
    }

    void Start()
    {
        if (gameEnd == null) gameEnd = FindFirstObjectByType<GameEndController>();
        if (gameEnd == null)
            Debug.LogWarning("ScoreZone : aucun GameEndController dans la scène.", this);
    }

    void OnTriggerEnter(Collider other)
    {
        if (gameEnd == null || gameEnd.Ended) return;

        // Le joueur (mains, tête) ne doit pas marquer de points
        var body = other.attachedRigidbody;
        if (body == null) return;

        if (requireGrabbable && body.GetComponentInParent<XRGrabInteractable>() == null) return;

        if (!counted.Add(body)) return;   // déjà compté

        gameEnd.AddScore(points);
    }
}
