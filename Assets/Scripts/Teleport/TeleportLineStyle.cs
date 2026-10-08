using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit.Interactors;
using UnityEngine.XR.Interaction.Toolkit.Interactors.Visuals;

/// <summary>
/// Applique le style médiéval aux rayons de téléportation au lancement (réticules, épaisseur, couleur).
/// Aucun réglage à faire sur le rig : il suffit que ce composant soit dans la scène.
/// </summary>
public class TeleportLineStyle : MonoBehaviour
{
    [Tooltip("Vide = tous les objets \"Teleport Interactor\" de la scène.")]
    [SerializeField] XRBaseInteractor[] m_TeleportInteractors;

    [Header("Réticules")]
    [SerializeField] GameObject m_Reticle;
    [SerializeField] GameObject m_BlockedReticle;

    [Header("Trait")]
    [SerializeField] float m_LineWidth = 0.03f;

    [Tooltip("Épaisseur le long du trait : 0 = manette, 1 = arrivée.")]
    [SerializeField] AnimationCurve m_WidthCurve = AnimationCurve.EaseInOut(0f, 1f, 1f, 0.2f);

    [Tooltip("Remplace la couleur du rayon quand la cible est valide.")]
    [SerializeField] bool m_OverrideValidColor = true;
    [SerializeField] Color m_ValidColor = new Color(0.89f, 0.66f, 0.23f);

    void Start()
    {
        var interactors = System.Array.FindAll(m_TeleportInteractors ?? new XRBaseInteractor[0], i => i != null);
        if (interactors.Length == 0)
            interactors = TeleportEnergy.FindTeleportInteractors();

        foreach (var interactor in interactors)
        {
            var lineVisual = interactor.GetComponent<XRInteractorLineVisual>();
            if (lineVisual != null)
                Apply(lineVisual);
        }
    }

    void Apply(XRInteractorLineVisual lineVisual)
    {
        lineVisual.lineWidth = m_LineWidth;
        lineVisual.widthCurve = m_WidthCurve;

        if (m_OverrideValidColor)
        {
            var gradient = new Gradient();
            gradient.SetKeys(
                new[] { new GradientColorKey(m_ValidColor, 0f), new GradientColorKey(m_ValidColor, 1f) },
                new[] { new GradientAlphaKey(1f, 0f), new GradientAlphaKey(0.6f, 1f) });
            lineVisual.validColorGradient = gradient;
        }

        // Si le rayon a déjà créé l'ancien réticule, on le supprime avant de mettre le nouveau.
        if (m_Reticle != null)
        {
            DestroyInstance(lineVisual.reticle);
            lineVisual.reticle = m_Reticle;
        }

        if (m_BlockedReticle != null)
        {
            DestroyInstance(lineVisual.blockedReticle);
            lineVisual.blockedReticle = m_BlockedReticle;
        }
    }

    static void DestroyInstance(GameObject reticle)
    {
        // Ne supprime que des objets de scène (instances), jamais un prefab du projet.
        if (reticle != null && reticle.scene.IsValid())
            Destroy(reticle);
    }
}
