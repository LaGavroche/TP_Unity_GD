using UnityEngine;
using UnityEngine.EventSystems;

// Agrandit légèrement le bouton et joue un son quand le rayon de la manette le survole.
public class HoverScale : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    public float hoverScale = 1.1f;
    public float speed = 12f;

    public AudioClip hoverClip;
    [Range(0f, 1f)] public float volume = 0.6f;

    Vector3 baseScale;
    Vector3 target;

    void Awake()
    {
        baseScale = transform.localScale;
        target = baseScale;
    }

    void OnEnable()
    {
        transform.localScale = baseScale;
        target = baseScale;
    }

    void Update()
    {
        transform.localScale = Vector3.Lerp(transform.localScale, target, Time.unscaledDeltaTime * speed);
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        target = baseScale * hoverScale;
        if (hoverClip != null)
            AudioSource.PlayClipAtPoint(hoverClip, transform.position, volume);
    }

    public void OnPointerExit(PointerEventData eventData)  { target = baseScale; }
}
