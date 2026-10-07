using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

public static class StyleMenuButtons
{
    [MenuItem("Tools/Style Menu Buttons (Highlight)")]
    static void Apply()
    {
        var hoverClip = AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/VRTemplateAssets/Audio/Button_14_hover.wav");
        if (hoverClip == null)
            Debug.LogWarning("Son de survol introuvable : assigne Hover Clip à la main sur les boutons.");

        int count = 0;
        foreach (var b in Object.FindObjectsByType<Button>(FindObjectsInactive.Include, FindObjectsSortMode.None))
        {
            // Uniquement les boutons des deux menus
            var canvas = b.GetComponentInParent<Canvas>(true);
            if (canvas == null || (canvas.name != "MenuCanvas" && canvas.name != "CreditsCanvas")) continue;

            Undo.RecordObject(b, "Style Menu Buttons");

            // L'Image reste blanche : la teinte du Button donne alors exactement la couleur voulue
            var img = b.GetComponent<Image>();
            if (img != null) { Undo.RecordObject(img, "Style Menu Buttons"); img.color = Color.white; }

            b.transition = Selectable.Transition.ColorTint;
            var c = b.colors;
            c.normalColor      = new Color(0.25f, 0.45f, 0.90f, 1f);   // bleu
            c.highlightedColor = new Color(0.55f, 0.75f, 1.00f, 1f);   // bleu clair au survol
            c.selectedColor    = new Color(0.55f, 0.75f, 1.00f, 1f);
            c.pressedColor     = new Color(0.15f, 0.30f, 0.70f, 1f);   // plus sombre au clic
            c.colorMultiplier  = 1f;
            c.fadeDuration     = 0.08f;
            b.colors = c;

            var hover = b.GetComponent<HoverScale>();
            if (hover == null) hover = Undo.AddComponent<HoverScale>(b.gameObject);
            Undo.RecordObject(hover, "Style Menu Buttons");
            hover.hoverClip = hoverClip;

            EditorUtility.SetDirty(b);
            count++;
        }

        EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
        Debug.Log($"Style Menu Buttons : {count} bouton(s) mis à jour.");
    }
}
