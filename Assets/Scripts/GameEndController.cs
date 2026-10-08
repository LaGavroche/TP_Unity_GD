using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.XR.Interaction.Toolkit.Locomotion;

// Fin de partie en VR : timer + score.
//  - Score >= palier avant la fin du timer : WIN (musique + confettis, puis fondu au noir)
//  - Timer à 0 sans avoir atteint le palier : LOSE (musique + la sphère autour de la caméra
//    voit son alpha monter progressivement jusqu'à l'opacité totale)
// Dans les deux cas : déplacements gelés, écran noir, puis retour au menu.
// La sphère est celle que tu as placée sur la caméra du XR Origin (visible dans les deux yeux).
[RequireComponent(typeof(AudioSource))]
public class GameEndController : MonoBehaviour
{
    [Header("Partie")]
    public float timeLimit = 60f;       // durée du timer en secondes
    public int targetScore = 10;        // palier du niveau
    public bool autoStart = true;

    [Header("UI (facultatif, canvas en World Space)")]
    public TMP_Text timerText;
    public TMP_Text scoreText;
    [Tooltip("Autres affichages du timer (ex : canvas flottant dans le ciel)")]
    public TMP_Text[] extraTimerTexts;
    [Tooltip("Autres affichages du score (ex : canvas flottant dans le ciel)")]
    public TMP_Text[] extraScoreTexts;

    [Header("Sphère de fondu (sur la caméra du XR Origin)")]
    public Renderer fadeSphere;         // le MeshRenderer de ta sphère, avec le matériau TestMat

    [Header("Win")]
    public AudioClip winClip;
    public GameObject confettiPrefab;   // facultatif : sinon des confettis simples sont générés
    public Material confettiMaterial;   // à assigner pour les builds Quest/Android
    public float winDuration = 8f;      // durée avant le fondu de sortie
    public float winFadeOut = 1.5f;     // fondu au noir avant de quitter la scène

    [Header("Lose")]
    public AudioClip loseClip;
    public float fadeDuration = 4f;     // durée pour que l'alpha passe de 0 à loseMaxAlpha
    [Range(0f, 1f)]
    public float loseMaxAlpha = 0.75f;  // alpha final : < 1 pour garder un peu de transparence (effet gris)
    public float loseExtraDelay = 1f;   // pause après la fin du fondu et de la musique

    [Header("Retour")]
    public string menuScene = "Scene_GD_Group";
    public bool freezeLocomotion = true;   // coupe marche/téléportation pendant la fin

    [Header("Musique d'ambiance (facultatif)")]
    public AudioSource ambientSource;      // l'AudioSource de ta musique de niveau (PAS celle de cet objet)
    [Range(0f, 1f)]
    public float ambientDuckLevel = 0.1f;  // volume de l'ambiance à la fin, en proportion du volume actuel
    public float duckTime = 0.5f;          // durée de la baisse (s)

    public int Score { get; private set; }
    public float TimeLeft { get; private set; }
    public bool Ended { get; private set; }

    bool running;
    AudioSource audioSource;
    Material fadeMaterial;              // instance du matériau : l'asset TestMat n'est pas modifié

    void Awake()
    {
        audioSource = GetComponent<AudioSource>();
        audioSource.playOnAwake = false;
        audioSource.spatialBlend = 0f;   // musique 2D : entendue partout, indépendante de la tête

        if (fadeSphere != null)
        {
            fadeMaterial = fadeSphere.material;   // crée une instance du matériau de la sphère
            SetAlpha(0f);
            fadeSphere.enabled = false;            // invisible tant que le fondu n'a pas commencé

            if (fadeMaterial.HasProperty("_Surface") && fadeMaterial.GetFloat("_Surface") < 0.5f)
                Debug.LogWarning("GameEndController : le matériau de la sphère est en Surface Type 'Opaque'. " +
                                 "Mets-le en 'Transparent' pour que l'alpha fonctionne.", this);
        }
        else
        {
            Debug.LogWarning("GameEndController : aucune sphère assignée dans 'Fade Sphere'.", this);
        }
    }

    void OnDestroy()
    {
        if (fadeMaterial != null) Destroy(fadeMaterial);
    }

    void Start()
    {
        TimeLeft = timeLimit;
        UpdateUI();
        if (autoStart) StartTimer();
    }

    public void StartTimer() { running = true; }

    // À appeler depuis le futur système de score (ex : objet qui entre dans le donut)
    public void AddScore(int amount = 1)
    {
        if (Ended) return;
        Score += amount;
        UpdateUI();
        if (Score >= targetScore) Win();
    }

    void Update()
    {
        if (!running || Ended) return;

        TimeLeft -= Time.deltaTime;
        if (TimeLeft <= 0f)
        {
            TimeLeft = 0f;
            UpdateUI();
            Lose();
            return;
        }
        UpdateUI();
    }

    void UpdateUI()
    {
        string timer = Mathf.CeilToInt(TimeLeft).ToString();
        string score = Score + " / " + targetScore;

        if (timerText != null) timerText.text = timer;
        if (scoreText != null) scoreText.text = score;

        if (extraTimerTexts != null)
            foreach (var t in extraTimerTexts) if (t != null) t.text = timer;
        if (extraScoreTexts != null)
            foreach (var t in extraScoreTexts) if (t != null) t.text = score;
    }

    // Coupe les déplacements du joueur (stick, téléportation...) : plus confortable en VR
    void FreezePlayer()
    {
        if (!freezeLocomotion) return;
        foreach (var p in FindObjectsByType<LocomotionProvider>(FindObjectsSortMode.None))
            p.enabled = false;
    }

    // ---------- WIN ----------

    void Win()
    {
        if (Ended) return;
        Ended = true;
        running = false;
        StartCoroutine(WinRoutine());
    }

    // Baisse la musique d'ambiance pour que le son de fin (victoire ou défaite) se détache
    IEnumerator DuckAmbient()
    {
        if (ambientSource == null) yield break;

        float start = ambientSource.volume;
        float target = start * ambientDuckLevel;
        float t = 0f;
        while (t < duckTime)
        {
            t += Time.deltaTime;
            ambientSource.volume = Mathf.Lerp(start, target, t / duckTime);
            yield return null;
        }
        ambientSource.volume = target;
    }

    IEnumerator WinRoutine()
    {
        StartCoroutine(DuckAmbient());
        FreezePlayer();
        if (winClip != null) audioSource.PlayOneShot(winClip);
        SpawnConfetti();

        yield return new WaitForSeconds(Mathf.Max(0f, winDuration - winFadeOut));

        // Fondu au noir avant de charger : évite un changement de scène brutal dans le casque
        yield return FadeAlpha(winFadeOut, 1f);

        SceneManager.LoadScene(menuScene);
    }

    void SpawnConfetti()
    {
        var head = GetHead();
        Vector3 pos = (head != null ? head.position : Vector3.zero) + Vector3.up * 3f;

        if (confettiPrefab != null)
        {
            Instantiate(confettiPrefab, pos, Quaternion.identity);
            return;
        }

        var go = new GameObject("Confetti");
        go.transform.position = pos;
        var ps = go.AddComponent<ParticleSystem>();

        var main = ps.main;
        main.startLifetime = 5f;
        main.startSpeed = 1.5f;
        main.startSize = 0.08f;
        main.gravityModifier = 0.25f;
        main.maxParticles = 500;          // raisonnable pour un casque autonome
        main.simulationSpace = ParticleSystemSimulationSpace.World;

        var gradient = new Gradient();
        gradient.SetKeys(
            new[]
            {
                new GradientColorKey(Color.red, 0f),
                new GradientColorKey(Color.yellow, 0.25f),
                new GradientColorKey(Color.green, 0.5f),
                new GradientColorKey(Color.cyan, 0.75f),
                new GradientColorKey(Color.magenta, 1f)
            },
            new[] { new GradientAlphaKey(1f, 0f), new GradientAlphaKey(1f, 1f) });
        main.startColor = new ParticleSystem.MinMaxGradient(gradient) { mode = ParticleSystemGradientMode.RandomColor };

        var emission = ps.emission;
        emission.rateOverTime = 100f;

        var shape = ps.shape;
        shape.shapeType = ParticleSystemShapeType.Box;
        shape.scale = new Vector3(4f, 0.2f, 4f);

        var rot = ps.rotationOverLifetime;
        rot.enabled = true;
        rot.z = new ParticleSystem.MinMaxCurve(-3f, 3f);

        var rend = go.GetComponent<ParticleSystemRenderer>();
        if (confettiMaterial != null) rend.material = confettiMaterial;
        else
        {
            var shader = Shader.Find("Universal Render Pipeline/Particles/Unlit");
            if (shader != null) rend.material = new Material(shader);
        }

        Destroy(go, winDuration + 6f);
    }

    // ---------- LOSE ----------

    void Lose()
    {
        if (Ended) return;
        Ended = true;
        running = false;
        StartCoroutine(LoseRoutine());
    }

    IEnumerator LoseRoutine()
    {
        StartCoroutine(DuckAmbient());
        FreezePlayer();
        if (loseClip != null) audioSource.PlayOneShot(loseClip);

        // L'alpha de la sphère monte progressivement de 0 à loseMaxAlpha pendant fadeDuration
        yield return FadeAlpha(fadeDuration, loseMaxAlpha);

        // On laisse finir la musique avant de revenir au menu (écran déjà noir)
        float remaining = loseClip != null ? Mathf.Max(0f, loseClip.length - fadeDuration) : 0f;
        yield return new WaitForSeconds(remaining + loseExtraDelay);

        SceneManager.LoadScene(menuScene);
    }

    // ---------- Fondu : alpha de la sphère placée sur la caméra ----------

    IEnumerator FadeAlpha(float seconds, float targetAlpha)
    {
        if (fadeMaterial == null) yield break;

        fadeSphere.enabled = true;
        SetAlpha(0f);

        float t = 0f;
        while (t < seconds)
        {
            t += Time.deltaTime;
            SetAlpha(Mathf.Clamp01(t / seconds) * targetAlpha);
            yield return null;
        }
        SetAlpha(targetAlpha);
    }

    void SetAlpha(float a)
    {
        if (fadeMaterial == null) return;

        string prop = fadeMaterial.HasProperty("_BaseColor") ? "_BaseColor" : "_Color";
        Color c = fadeMaterial.GetColor(prop);
        c.a = a;
        fadeMaterial.SetColor(prop, c);
    }

    // Caméra du XR Origin (doit avoir le tag MainCamera)
    Transform GetHead()
    {
        var cam = Camera.main;
        if (cam == null) cam = FindFirstObjectByType<Camera>();
        return cam != null ? cam.transform : null;
    }
}
