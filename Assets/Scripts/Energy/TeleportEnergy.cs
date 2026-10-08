using UnityEngine;
using UnityEngine.Events;
using UnityEngine.XR.Interaction.Toolkit.Filtering;
using UnityEngine.XR.Interaction.Toolkit.Interactables;
using UnityEngine.XR.Interaction.Toolkit.Interactors;
using UnityEngine.XR.Interaction.Toolkit.Interactors.Visuals;
using UnityEngine.XR.Interaction.Toolkit.Inputs.Haptics;
using UnityEngine.XR.Interaction.Toolkit.Locomotion;
using UnityEngine.XR.Interaction.Toolkit.Locomotion.Teleportation;

/// <summary>
/// Énergie de téléportation : chaque téléportation coûte une charge, les charges reviennent avec le temps.
/// Quand il n'y a plus assez d'énergie, la sélection des cibles est bloquée : le rayon passe dans son état
/// "bloqué" (couleur No Energy Line Color + Blocked Reticle), distinct du rouge des zones interdites.
/// </summary>
public class TeleportEnergy : MonoBehaviour, IXRSelectFilter
{
    [Header("Énergie")]
    [SerializeField, Range(1, 8)] int m_MaxCharges = 3;
    [SerializeField] float m_CostPerTeleport = 1f;

    [Tooltip("Secondes pour recharger UNE charge.")]
    [SerializeField] float m_SecondsPerCharge = 1.5f;

    [Tooltip("Pause avant que la recharge reprenne après une téléportation.")]
    [SerializeField] float m_RechargeDelay = 0.3f;

    public enum Hand { Right, Left }

    [Header("Références (laisser vide = recherche automatique dans le rig du template VR)")]
    [SerializeField] TeleportationProvider m_TeleportationProvider;

    [Tooltip("Vide = tous les objets \"Teleport Interactor\" de la scène.")]
    [SerializeField] XRBaseInteractor[] m_TeleportInteractors;

    [Tooltip("Vide = cherche une jauge dans la scène, sinon crée Gauge Prefab sur la manette choisie.")]
    [SerializeField] EnergyArcGaugeUI m_Gauge;

    [Tooltip("Prefab de la jauge, instancié sur la manette si aucune jauge n'est dans la scène.")]
    [SerializeField] EnergyArcGaugeUI m_GaugePrefab;

    [Tooltip("Manette qui porte la jauge et qui vibre.")]
    [SerializeField] Hand m_GaugeHand = Hand.Right;

    [Header("Retour quand l'énergie manque")]
    [Tooltip("Vide = HapticImpulsePlayer de la manette choisie.")]
    [SerializeField] HapticImpulsePlayer m_Haptics;
    [SerializeField] float m_HapticAmplitude = 0.6f;
    [SerializeField] float m_HapticDuration = 0.1f;

    [Tooltip("Couleur du rayon quand l'énergie manque (appliquée au Blocked Color Gradient des rayons).")]
    [SerializeField] Color m_NoEnergyLineColor = new Color(0.55f, 0.55f, 0.55f);

    [Tooltip("Optionnel : jouer un son, etc.")]
    public UnityEvent onTeleportDenied;

    float m_Energy;
    float m_RechargeBlockedUntil;
    int m_LastDeniedFrame = -10;
    bool m_WasDenied;

    public float energy => m_Energy;
    public int maxCharges => m_MaxCharges;
    public bool hasEnoughEnergy => m_Energy >= m_CostPerTeleport;

    // Requis par IXRSelectFilter.
    public bool canProcess => isActiveAndEnabled;

    void Awake()
    {
        m_Energy = m_MaxCharges;

        AutoFindReferences();

        if (m_Gauge != null)
            m_Gauge.useDebugValues = false;

        ApplyNoEnergyLineColor();
    }

    void ApplyNoEnergyLineColor()
    {
        var gradient = new Gradient();
        gradient.SetKeys(
            new[] { new GradientColorKey(m_NoEnergyLineColor, 0f), new GradientColorKey(m_NoEnergyLineColor, 1f) },
            new[] { new GradientAlphaKey(1f, 0f), new GradientAlphaKey(0.5f, 1f) });

        foreach (var interactor in m_TeleportInteractors)
        {
            var lineVisual = interactor.GetComponent<XRInteractorLineVisual>();
            if (lineVisual != null)
                lineVisual.blockedColorGradient = gradient;
        }
    }

    void AutoFindReferences()
    {
        if (m_TeleportationProvider == null)
            m_TeleportationProvider = FindAnyObjectByType<TeleportationProvider>();

        // Ignore les cases vides de la liste (ex : taille 1 laissée à "None" dans l'Inspector).
        m_TeleportInteractors = System.Array.FindAll(m_TeleportInteractors ?? new XRBaseInteractor[0], i => i != null);
        if (m_TeleportInteractors.Length == 0)
            m_TeleportInteractors = FindTeleportInteractors();

        var controller = FindGaugeController();

        if (m_Gauge == null)
            m_Gauge = FindAnyObjectByType<EnergyArcGaugeUI>(FindObjectsInactive.Include);

        // Garde la position/rotation locale réglée dans le prefab.
        if (m_Gauge == null && m_GaugePrefab != null && controller != null)
            m_Gauge = Instantiate(m_GaugePrefab, controller, false);

        if (m_Haptics == null && controller != null)
            m_Haptics = controller.GetComponentInChildren<HapticImpulsePlayer>(true);

        if (m_TeleportInteractors.Length == 0)
            Debug.LogWarning("TeleportEnergy : aucun Teleport Interactor trouvé dans la scène.", this);
        if (m_Gauge == null)
            Debug.LogWarning("TeleportEnergy : aucune jauge trouvée (assigne Gauge ou Gauge Prefab).", this);
    }

    /// <summary>
    /// Tous les objets "Teleport Interactor" du rig du template VR.
    /// </summary>
    public static XRBaseInteractor[] FindTeleportInteractors()
    {
        // Le template désactive le Teleport Interactor tant que le joystick n'est pas poussé : on inclut les inactifs.
        var found = new System.Collections.Generic.List<XRBaseInteractor>();
        foreach (var interactor in FindObjectsByType<XRBaseInteractor>(FindObjectsInactive.Include, FindObjectsSortMode.None))
        {
            if (interactor.name.Contains("Teleport"))
                found.Add(interactor);
        }
        return found.ToArray();
    }

    /// <summary>
    /// Manette (ex : "Right Controller") parente du Teleport Interactor de la main choisie.
    /// </summary>
    Transform FindGaugeController()
    {
        var handName = m_GaugeHand == Hand.Right ? "Right" : "Left";
        foreach (var interactor in m_TeleportInteractors)
        {
            if (interactor == null)
                continue;

            for (var t = interactor.transform.parent; t != null; t = t.parent)
            {
                if (t.name.Contains(handName) && t.name.Contains("Controller"))
                    return t;
            }
        }

        return null;
    }

    void OnEnable()
    {
        if (m_TeleportationProvider != null)
            m_TeleportationProvider.locomotionStarted += OnTeleportStarted;

        foreach (var interactor in m_TeleportInteractors)
        {
            if (interactor == null)
                continue;
            interactor.selectFilters.Add(this);
        }
    }

    void OnDisable()
    {
        if (m_TeleportationProvider != null)
            m_TeleportationProvider.locomotionStarted -= OnTeleportStarted;

        foreach (var interactor in m_TeleportInteractors)
        {
            if (interactor == null)
                continue;
            interactor.selectFilters.Remove(this);
        }
    }

    void Update()
    {
        if (Time.time >= m_RechargeBlockedUntil && m_Energy < m_MaxCharges)
            m_Energy = Mathf.Min(m_Energy + Time.deltaTime / m_SecondsPerCharge, m_MaxCharges);

        // Le joueur vient de viser une zone de téléportation sans assez d'énergie : on le prévient une fois.
        var isDenied = Time.frameCount - m_LastDeniedFrame <= 1;
        if (isDenied && !m_WasDenied)
            NotifyDenied();
        m_WasDenied = isDenied;

        if (m_Gauge != null)
            m_Gauge.SetEnergy(m_Energy, m_MaxCharges);
    }

    void OnTeleportStarted(LocomotionProvider provider)
    {
        m_Energy = Mathf.Max(m_Energy - m_CostPerTeleport, 0f);
        m_RechargeBlockedUntil = Time.time + m_RechargeDelay;
    }

    void NotifyDenied()
    {
        if (m_Gauge != null)
            m_Gauge.FlashEmpty();
        if (m_Haptics != null)
            m_Haptics.SendHapticImpulse(m_HapticAmplitude, m_HapticDuration);
        onTeleportDenied?.Invoke();
    }

    bool Allows(object interactable)
    {
        // On ne filtre que les zones/ancres de téléportation.
        if (!(interactable is BaseTeleportationInteractable) || hasEnoughEnergy)
            return true;

        m_LastDeniedFrame = Time.frameCount;
        return false;
    }

    public bool Process(IXRSelectInteractor interactor, IXRSelectInteractable interactable) => Allows(interactable);
}
