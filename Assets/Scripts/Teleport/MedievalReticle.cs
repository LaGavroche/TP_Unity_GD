using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Réticule de téléportation au style médiéval, dessiné par code (aucun asset nécessaire).
/// Destination : anneau doré gravé qui tourne lentement + pointe de lance qui indique la direction d'arrivée.
/// Blocked : anneau gris + croix (pas assez d'énergie).
/// À mettre sur un GameObject vide transformé en prefab, puis à assigner dans Reticle / Blocked Reticle
/// du XR Interactor Line Visual du Teleport Interactor.
/// </summary>
public class MedievalReticle : MonoBehaviour
{
    public enum Style { Destination, Blocked }

    [SerializeField] Style m_Style = Style.Destination;

    [Tooltip("Diamètre du réticule au sol, en mètres.")]
    [SerializeField] float m_Diameter = 0.6f;

    [Tooltip("Hauteur au-dessus du sol pour éviter qu'il clignote avec le sol.")]
    [SerializeField] float m_HeightOffset = 0.02f;

    [Tooltip("Vitesse de rotation de l'anneau, en degrés par seconde (0 = fixe).")]
    [SerializeField] float m_RingRotationSpeed = 20f;

    [Header("Couleurs")]
    [SerializeField] Color m_DestinationColor = new Color(0.89f, 0.66f, 0.23f);
    [SerializeField] Color m_BlockedColor = new Color(0.55f, 0.55f, 0.55f);
    [SerializeField] Color m_OutlineColor = new Color(0.2f, 0.13f, 0.06f);

    const int k_TextureSize = 256;
    const float k_OutlineWidth = 0.035f;

    static readonly Dictionary<string, Sprite> s_Sprites = new Dictionary<string, Sprite>();

    Transform m_Ring;
    float m_RingAngle;

    void Awake()
    {
        var mainColor = m_Style == Style.Destination ? m_DestinationColor : m_BlockedColor;

        m_Ring = CreateLayer("Ring", GetSprite("Ring", RingShape, mainColor), 0);
        if (m_Style == Style.Destination)
            CreateLayer("Spear", GetSprite("Spear", SpearShape, mainColor), 1);
        else
            CreateLayer("Cross", GetSprite("Cross", CrossShape, mainColor), 1);
    }

    void Update()
    {
        if (m_Style != Style.Destination || Mathf.Approximately(m_RingRotationSpeed, 0f))
            return;

        m_RingAngle = (m_RingAngle + m_RingRotationSpeed * Time.deltaTime) % 360f;
        m_Ring.localRotation = Quaternion.Euler(0f, m_RingAngle, 0f) * Quaternion.Euler(90f, 0f, 0f);
    }

    Transform CreateLayer(string layerName, Sprite sprite, int sortingOrder)
    {
        var go = new GameObject(layerName);
        go.transform.SetParent(transform, false);
        // Couché au sol : le haut du sprite pointe vers l'avant du réticule (direction d'arrivée).
        go.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
        go.transform.localPosition = new Vector3(0f, m_HeightOffset + sortingOrder * 0.002f, 0f);
        go.transform.localScale = Vector3.one * m_Diameter;

        var spriteRenderer = go.AddComponent<SpriteRenderer>();
        spriteRenderer.sprite = sprite;
        spriteRenderer.sortingOrder = sortingOrder;
        spriteRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        spriteRenderer.receiveShadows = false;
        return go.transform;
    }

    Sprite GetSprite(string shapeName, System.Func<Vector2, float> shape, Color color)
    {
        var key = $"{shapeName}_{ColorUtility.ToHtmlStringRGBA(color)}_{ColorUtility.ToHtmlStringRGBA(m_OutlineColor)}";
        if (s_Sprites.TryGetValue(key, out var sprite) && sprite != null)
            return sprite;

        sprite = CreateSprite(shape, color, m_OutlineColor);
        s_Sprites[key] = sprite;
        return sprite;
    }

    // ---- Formes, en distance signée (négatif = à l'intérieur), coordonnées de -1 à 1 ----

    static float RingShape(Vector2 p)
    {
        var r = p.magnitude;
        var band = Mathf.Abs(r - 0.86f) - 0.09f;

        // 4 losanges évidés dans l'anneau, en diagonale.
        var angle = Mathf.Atan2(p.y, p.x);
        var snapped = (Mathf.Round((angle - Mathf.PI * 0.25f) / (Mathf.PI * 0.5f)) * (Mathf.PI * 0.5f)) + Mathf.PI * 0.25f;
        var local = Rotate(p - new Vector2(Mathf.Cos(snapped), Mathf.Sin(snapped)) * 0.86f, -snapped);
        var diamond = (Mathf.Abs(local.x) + Mathf.Abs(local.y)) * 0.7071f - 0.05f;
        band = Mathf.Max(band, -diamond);

        // 12 petites encoches à l'intérieur de l'anneau.
        var tickAngle = Mathf.Round(angle / (Mathf.PI / 6f)) * (Mathf.PI / 6f);
        var tick = Box(Rotate(p, -tickAngle) - new Vector2(0.7f, 0f), new Vector2(0.04f, 0.012f));

        return Mathf.Min(band, tick);
    }

    static readonly Vector2[] s_SpearPoints =
    {
        new Vector2(0f, 0.6f), new Vector2(0.24f, 0.1f), new Vector2(0.07f, 0.18f), new Vector2(0.07f, -0.42f),
        new Vector2(-0.07f, -0.42f), new Vector2(-0.07f, 0.18f), new Vector2(-0.24f, 0.1f),
    };

    static float SpearShape(Vector2 p) => Polygon(p, s_SpearPoints);

    static float CrossShape(Vector2 p)
    {
        var size = new Vector2(0.42f, 0.07f);
        return Mathf.Min(Box(Rotate(p, Mathf.PI * 0.25f), size), Box(Rotate(p, -Mathf.PI * 0.25f), size));
    }

    static float Box(Vector2 p, Vector2 halfSize)
    {
        var d = new Vector2(Mathf.Abs(p.x) - halfSize.x, Mathf.Abs(p.y) - halfSize.y);
        return Vector2.Max(d, Vector2.zero).magnitude + Mathf.Min(Mathf.Max(d.x, d.y), 0f);
    }

    static Vector2 Rotate(Vector2 p, float radians)
    {
        var c = Mathf.Cos(radians);
        var s = Mathf.Sin(radians);
        return new Vector2(p.x * c - p.y * s, p.x * s + p.y * c);
    }

    // Distance signée à un polygone quelconque (Inigo Quilez).
    static float Polygon(Vector2 p, Vector2[] v)
    {
        var d = Vector2.Dot(p - v[0], p - v[0]);
        var sign = 1f;
        for (int i = 0, j = v.Length - 1; i < v.Length; j = i, i++)
        {
            var e = v[j] - v[i];
            var w = p - v[i];
            var b = w - e * Mathf.Clamp01(Vector2.Dot(w, e) / Vector2.Dot(e, e));
            d = Mathf.Min(d, Vector2.Dot(b, b));

            var c1 = p.y >= v[i].y;
            var c2 = p.y < v[j].y;
            var c3 = e.x * w.y > e.y * w.x;
            if ((c1 && c2 && c3) || (!c1 && !c2 && !c3))
                sign = -sign;
        }

        return sign * Mathf.Sqrt(d);
    }

    static Sprite CreateSprite(System.Func<Vector2, float> shape, Color fill, Color outline)
    {
        var texture = new Texture2D(k_TextureSize, k_TextureSize, TextureFormat.RGBA32, false)
        {
            wrapMode = TextureWrapMode.Clamp,
            filterMode = FilterMode.Bilinear,
        };

        var pixels = new Color[k_TextureSize * k_TextureSize];
        var pixelSize = 2f / k_TextureSize;

        for (var y = 0; y < k_TextureSize; y++)
        {
            for (var x = 0; x < k_TextureSize; x++)
            {
                var p = new Vector2((x + 0.5f) * pixelSize - 1f, (y + 0.5f) * pixelSize - 1f);
                var d = shape(p);

                // Bord extérieur lissé, liseré sombre sur le pourtour (effet gravé / forgé).
                var alpha = Mathf.Clamp01(0.5f - d / pixelSize);
                var outlineMix = Mathf.Clamp01(0.5f + (d + k_OutlineWidth) / pixelSize);
                var color = Color.Lerp(fill, outline, outlineMix);
                color.a = alpha * fill.a;
                pixels[y * k_TextureSize + x] = color;
            }
        }

        texture.SetPixels(pixels);
        texture.Apply();
        return Sprite.Create(texture, new Rect(0, 0, k_TextureSize, k_TextureSize), new Vector2(0.5f, 0.5f), k_TextureSize);
    }
}
