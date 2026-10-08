using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Variante en arc de cercle de la jauge d'énergie (charges segmentées le long d'un anneau).
/// Même API que EnergyGaugeUI : la logique appelle SetEnergy() et FlashEmpty().
/// À placer sur un objet UI carré (ex : Canvas World Space 100 x 100).
/// </summary>
[RequireComponent(typeof(RectTransform))]
public class EnergyArcGaugeUI : MonoBehaviour
{
    [Header("Forme de l'arc")]
    [Tooltip("Angle total couvert par la jauge, en degrés (360 = cercle complet).")]
    [SerializeField, Range(30f, 360f)] float m_ArcAngle = 140f;

    [Tooltip("Angle du centre de l'arc. 0 = haut, 90 = droite, 180 = bas, -90 = gauche.")]
    [SerializeField, Range(-180f, 180f)] float m_ArcCenterAngle = 180f;

    [Tooltip("Espace entre deux charges, en degrés.")]
    [SerializeField, Range(0f, 20f)] float m_GapAngle = 6f;

    [Tooltip("Épaisseur de l'anneau (0 = très fin, 1 = disque plein).")]
    [SerializeField, Range(0.05f, 1f)] float m_Thickness = 0.25f;

    [Header("Couleurs")]
    [SerializeField] Color m_FullColor = new Color(0.2f, 0.8f, 1f);
    [SerializeField] Color m_ChargingColor = new Color(0.2f, 0.8f, 1f, 0.4f);
    [SerializeField] Color m_EmptyBackgroundColor = new Color(1f, 1f, 1f, 0.15f);
    [SerializeField] Color m_FlashColor = new Color(1f, 0.25f, 0.25f);

    [Header("Flash quand vide")]
    [SerializeField] int m_FlashCount = 3;
    [SerializeField] float m_FlashInterval = 0.12f;

    [Header("Test visuel (sans logique)")]
    [SerializeField] bool m_UseDebugValues = true;
    [SerializeField, Range(1, 8)] int m_DebugMaxCharges = 3;
    [SerializeField, Range(0f, 8f)] float m_DebugEnergy = 2.5f;

    const int k_TextureSize = 256;

    readonly List<Image> m_Fills = new List<Image>();
    readonly List<Image> m_Backgrounds = new List<Image>();
    Sprite m_RingSprite;
    float m_BuiltThickness = -1f;
    float m_CurrentEnergy;
    Coroutine m_FlashRoutine;

    /// <summary>
    /// Désactivé automatiquement par TeleportEnergy pour afficher la vraie énergie.
    /// </summary>
    public bool useDebugValues
    {
        get => m_UseDebugValues;
        set => m_UseDebugValues = value;
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
        EnsureRingSprite();
        EnsureSegmentCount(maxCharges);
        m_CurrentEnergy = Mathf.Clamp(current, 0f, maxCharges);

        LayoutSegments();
        if (m_FlashRoutine == null)
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

    void LayoutSegments()
    {
        var count = m_Fills.Count;
        if (count == 0)
            return;

        var segmentAngle = (m_ArcAngle - m_GapAngle * (count - 1)) / count;
        if (m_ArcAngle >= 360f)
            segmentAngle = (360f - m_GapAngle * count) / count;
        segmentAngle = Mathf.Max(segmentAngle, 0f);

        var startAngle = m_ArcCenterAngle - m_ArcAngle * 0.5f;

        for (var i = 0; i < count; i++)
        {
            // Radial360 depuis le haut, sens horaire : on tourne chaque segment jusqu'à son angle de départ.
            var angle = startAngle + i * (segmentAngle + m_GapAngle);
            var rotation = Quaternion.Euler(0f, 0f, -angle);

            m_Backgrounds[i].rectTransform.localRotation = rotation;
            m_Backgrounds[i].fillAmount = segmentAngle / 360f;

            var fill01 = Mathf.Clamp01(m_CurrentEnergy - i);
            m_Fills[i].fillAmount = segmentAngle / 360f * fill01;
        }
    }

    void ApplyColors()
    {
        for (var i = 0; i < m_Fills.Count; i++)
        {
            var fill01 = Mathf.Clamp01(m_CurrentEnergy - i);
            m_Fills[i].color = fill01 >= 1f ? m_FullColor : m_ChargingColor;
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
        if (m_Fills.Count == count)
            return;

        foreach (var bg in m_Backgrounds)
            Destroy(bg.gameObject);
        m_Fills.Clear();
        m_Backgrounds.Clear();

        for (var i = 0; i < count; i++)
        {
            // Fond (charge vide) + remplissage enfant qui hérite de la rotation.
            var background = CreateArcImage($"Segment_{i}", transform);
            var fill = CreateArcImage("Fill", background.transform);
            m_Backgrounds.Add(background);
            m_Fills.Add(fill);
        }
    }

    Image CreateArcImage(string objectName, Transform parent)
    {
        var go = new GameObject(objectName, typeof(RectTransform), typeof(Image));
        go.transform.SetParent(parent, false);

        var rect = (RectTransform)go.transform;
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;

        var image = go.GetComponent<Image>();
        image.sprite = m_RingSprite;
        image.raycastTarget = false;
        image.type = Image.Type.Filled;
        image.fillMethod = Image.FillMethod.Radial360;
        image.fillOrigin = (int)Image.Origin360.Top;
        image.fillClockwise = true;
        return image;
    }

    void EnsureRingSprite()
    {
        if (m_RingSprite != null && Mathf.Approximately(m_BuiltThickness, m_Thickness))
            return;

        m_BuiltThickness = m_Thickness;
        m_RingSprite = CreateRingSprite(m_Thickness);
        foreach (var image in m_Backgrounds)
            image.sprite = m_RingSprite;
        foreach (var image in m_Fills)
            image.sprite = m_RingSprite;
    }

    static Sprite CreateRingSprite(float thickness)
    {
        var texture = new Texture2D(k_TextureSize, k_TextureSize, TextureFormat.RGBA32, false)
        {
            wrapMode = TextureWrapMode.Clamp,
            filterMode = FilterMode.Bilinear,
        };

        var pixels = new Color32[k_TextureSize * k_TextureSize];
        var center = (k_TextureSize - 1) * 0.5f;
        var outer = k_TextureSize * 0.5f - 1f;
        var inner = outer * (1f - thickness);

        for (var y = 0; y < k_TextureSize; y++)
        {
            for (var x = 0; x < k_TextureSize; x++)
            {
                var distance = Vector2.Distance(new Vector2(x, y), new Vector2(center, center));
                // Bords lissés sur 1 pixel.
                var alpha = Mathf.Clamp01(outer - distance) * Mathf.Clamp01(distance - inner);
                pixels[y * k_TextureSize + x] = new Color32(255, 255, 255, (byte)(alpha * 255));
            }
        }

        texture.SetPixels32(pixels);
        texture.Apply();
        return Sprite.Create(texture, new Rect(0, 0, k_TextureSize, k_TextureSize), new Vector2(0.5f, 0.5f));
    }
}
