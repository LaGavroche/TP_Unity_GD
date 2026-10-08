using UnityEngine;
using UnityEngine.InputSystem;

// HUD de poignet : n'est visible que quand le joueur regarde son poignet (comme une montre).
// À mettre sur le Canvas (World Space) enfant de la manette.
[RequireComponent(typeof(CanvasGroup))]
public class WristHUD : MonoBehaviour
{
    [Range(0f, 1f)] public float showDot = 0.55f;   // plus petit = visible sous un angle plus large
    public float maxDistance = 0.9f;                // distance max entre la tête et le poignet (m)
    public float fadeSpeed = 8f;

    [Header("Test sans casque (simulateur)")]
    public bool alwaysVisible = true;               // affiche le HUD en permanence (casque comme simulateur)
    public Key toggleKey = Key.F1;                  // appuie dessus pour activer/désactiver "alwaysVisible"

    CanvasGroup group;
    Transform head;

    void Awake()
    {
        group = GetComponent<CanvasGroup>();
        group.alpha = 0f;
        group.interactable = false;
        group.blocksRaycasts = false;   // ne doit pas gêner les rayons
    }

    void Update()
    {
        // Raccourci clavier pour tester le HUD sans avoir à lever la manette dans le simulateur
        var keyboard = Keyboard.current;
        if (keyboard != null && keyboard[toggleKey].wasPressedThisFrame)
            alwaysVisible = !alwaysVisible;

        if (alwaysVisible)
        {
            group.alpha = Mathf.MoveTowards(group.alpha, 1f, fadeSpeed * Time.deltaTime);
            return;
        }

        if (head == null)
        {
            var cam = Camera.main;
            if (cam == null) return;
            head = cam.transform;
        }

        Vector3 toHead = head.position - transform.position;
        float dist = toHead.magnitude;
        // La face visible d'un canvas regarde vers -forward
        float dot = Vector3.Dot(-transform.forward, toHead / Mathf.Max(dist, 0.0001f));

        bool visible = dist < maxDistance && dot > showDot;
        group.alpha = Mathf.MoveTowards(group.alpha, visible ? 1f : 0f, fadeSpeed * Time.deltaTime);
    }
}
