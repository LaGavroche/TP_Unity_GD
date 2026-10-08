using UnityEngine;

// À mettre sur le CreditsCanvas. Quand les crédits s'affichent, la musique du menu se met en pause
// et la musique des crédits démarre. Quand ils se ferment (bouton Retour), l'inverse.
// Fonctionne tout seul : aucun branchement dans les boutons.
public class CreditsMusic : MonoBehaviour
{
    public AudioSource menuMusic;       // l'AudioSource de la musique du menu
    public AudioSource creditsMusic;    // l'AudioSource avec le son Star Wars

    void OnEnable()
    {
        if (menuMusic != null) menuMusic.Pause();

        if (creditsMusic != null)
        {
            creditsMusic.time = 0f;      // repart du début à chaque ouverture
            creditsMusic.Play();
        }
    }

    void OnDisable()
    {
        if (creditsMusic != null) creditsMusic.Stop();
        if (menuMusic != null) menuMusic.UnPause();   // reprend là où elle s'était arrêtée
    }
}
