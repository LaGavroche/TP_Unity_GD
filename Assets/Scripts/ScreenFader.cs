using System.Collections;
using UnityEngine;

public class ScreenFader : MonoBehaviour
{
    public static ScreenFader Instance { get; private set; }

    [SerializeField] private float fadeDuration = 0.25f;

    private Renderer fadeRenderer;
    private static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");

    private void Awake()
    {
        Instance = this;
        fadeRenderer = GetComponent<Renderer>();
        SetAlpha(0f);
    }

    public IEnumerator FadeOut() { return Fade(0f, 1f); }
    public IEnumerator FadeIn()  { return Fade(1f, 0f); }

    private IEnumerator Fade(float from, float to)
    {
        float t = 0f;
        while (t < fadeDuration)
        {
            t += Time.deltaTime;
            SetAlpha(Mathf.Lerp(from, to, t / fadeDuration));
            yield return null;
        }
        SetAlpha(to);
    }

    private void SetAlpha(float alpha)
    {
        fadeRenderer.enabled = alpha > 0.001f;
        Color c = fadeRenderer.material.GetColor(BaseColorId);
        c.a = alpha;
        fadeRenderer.material.SetColor(BaseColorId, c);
    }
}