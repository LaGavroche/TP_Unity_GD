using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Affichage de la jauge d'énergie (charges segmentées) attachée à la manette.
/// Purement visuel : la logique appelle SetEnergy() et FlashEmpty().
/// </summary>
public class EnergyGaugeUI : MonoBehaviour
{
    [Header("Segments")]
    [Tooltip("Image modèle d'un segment (Image Type = Filled). Elle est dupliquée pour chaque charge.")]
    [SerializeField] Image m_SegmentTemplate;

    [Tooltip("Parent des segments (idéalement avec un Horizontal Layout Group).")]
    [SerializeField] Transform m_SegmentContainer;

    [Header("Couleurs")]
    [SerializeField] Color m_FullColor = new Color(0.2f, 0.8f, 1f);
    [SerializeField] Color m_ChargingColor = new Color(0.2f, 0.8f, 1f, 0.4f);
    [SerializeField] Color m_FlashColor = new Color(1f, 0.25f, 0.25f);

    [Header("Fond des segments vides")]
    [SerializeField] Color m_EmptyBackgroundColor = new Color(1f, 1f, 1f, 0.15f);

    [Header("Flash quand vide")]
    [SerializeField] int m_FlashCount = 3;
    [SerializeField] float m_FlashInterval = 0.12f;

    [Header("Test visuel (sans logique)")]
    [Tooltip("Si coché, la jauge affiche les valeurs de test ci-dessous.")]
    [SerializeField] bool m_UseDebugValues = true;
    [SerializeField, Range(1, 8)] int m_DebugMaxCharges = 3;
    [SerializeField, Range(0f, 8f)] float m_DebugEnergy = 2.5f;

    readonly List<Image> m_Fills = new List<Image>();
    readonly List<Image> m_Backgrounds = new List<Image>();
    float m_CurrentEnergy;
    Coroutine m_FlashRoutine;

    void Awake()
    {
        if (m_SegmentTemplate != null)
            m_SegmentTemplate.gameObject.SetActive(false);
    }

    void Update()
    {
        if (m_UseDebugValues)
            SetEnergy(Mathf.Min(m_DebugEnergy, m_DebugMaxCharges), m_DebugMaxCharges);
    }

    /// <summary>
    /// Met à jour l'affichage. Ex : SetEnergy(1.6f, 3) = 1 charge pleine + 1 charge à 60 %.
    /// </summary>
    public void SetEnergy(float current, int maxCharges)
    {
        EnsureSegmentCount(maxCharges);
        m_CurrentEnergy = Mathf.Clamp(current, 0f, maxCharges);

        if (m_FlashRoutine != null)
            return;

        ApplyColors();
    }

    /// <summary>
    /// Fait clignoter la jauge en rouge (à appeler quand une téléportation est refusée).
    /// </summary>
    [ContextMenu("Test Flash Empty")]
    public void FlashEmpty()
    {
        if (m_FlashRoutine != null)
            StopCoroutine(m_FlashRoutine);
        m_FlashRoutine = StartCoroutine(FlashRoutine());
    }

    void ApplyColors()
    {
        for (var i = 0; i < m_Fills.Count; i++)
        {
            var fill = Mathf.Clamp01(m_CurrentEnergy - i);
            m_Fills[i].fillAmount = fill;
            m_Fills[i].color = fill >= 1f ? m_FullColor : m_ChargingColor;
            m_Backgrounds[i].color = m_EmptyBackgroundColor;
        }
    }

    IEnumerator FlashRoutine()
    {
        for (var n = 0; n < m_FlashCount; n++)
        {
            foreach (var bg in m_Backgrounds)
                bg.color = m_FlashColor;
            yield return new WaitForSeconds(m_FlashInterval);

            foreach (var bg in m_Backgrounds)
                bg.color = m_EmptyBackgroundColor;
            yield return new WaitForSeconds(m_FlashInterval);
        }

        m_FlashRoutine = null;
        ApplyColors();
    }

    void EnsureSegmentCount(int count)
    {
        if (m_SegmentTemplate == null || m_SegmentContainer == null || m_Fills.Count == count)
            return;

        foreach (var bg in m_Backgrounds)
            Destroy(bg.gameObject);
        m_Fills.Clear();
        m_Backgrounds.Clear();

        for (var i = 0; i < count; i++)
        {
            // Fond (segment vide) + remplissage par-dessus.
            var background = Instantiate(m_SegmentTemplate, m_SegmentContainer);
            background.gameObject.SetActive(true);
            background.name = $"Segment_{i}";
            background.type = Image.Type.Simple;

            var fill = Instantiate(m_SegmentTemplate, background.transform);
            fill.gameObject.SetActive(true);
            fill.name = "Fill";
            fill.type = Image.Type.Filled;
            fill.fillMethod = Image.FillMethod.Horizontal;
            var rect = fill.rectTransform;
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;

            m_Backgrounds.Add(background);
            m_Fills.Add(fill);
        }
    }
}
