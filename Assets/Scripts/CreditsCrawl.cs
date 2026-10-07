using TMPro;
using UnityEngine;

// Fait défiler un texte vers le haut, comme le générique de Star Wars.
// À placer sur le TextMeshProUGUI, enfant d'un conteneur avec un RectMask2D.
public class CreditsCrawl : MonoBehaviour
{
    public RectTransform viewport;   // zone visible (le conteneur avec le masque)
    public float speed = 80f;        // unités par seconde
    public bool loop = true;

    TextMeshProUGUI tmp;
    RectTransform rt;
    float endY;

    void Awake()
    {
        tmp = GetComponent<TextMeshProUGUI>();
        rt = tmp.rectTransform;
    }

    void OnEnable()
    {
        // Repart du bas à chaque fois que l'écran des crédits s'affiche
        tmp.ForceMeshUpdate();
        rt.sizeDelta = new Vector2(rt.sizeDelta.x, tmp.preferredHeight);
        rt.anchoredPosition = Vector2.zero;
        endY = viewport.rect.height + tmp.preferredHeight;
    }

    void Update()
    {
        rt.anchoredPosition += Vector2.up * speed * Time.deltaTime;

        if (rt.anchoredPosition.y > endY)
        {
            if (loop) rt.anchoredPosition = Vector2.zero;
            else enabled = false;
        }
    }
}
