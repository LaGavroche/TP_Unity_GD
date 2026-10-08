using System.Collections.Generic;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

// Branche le canvas flottant "ScoreTimeSky" (prefab de l'autre groupe, rangé dans Scene 1)
// sur le GameEndController : ses textes ScoreText et TimerText se mettent à jour avec la partie.
public static class LinkSkyScoreTimer
{
    [MenuItem("Tools/Link Sky Score Timer")]
    static void Run()
    {
        var end = Object.FindFirstObjectByType<GameEndController>();
        if (end == null)
        {
            EditorUtility.DisplayDialog("Sky Score Timer", "Aucun GameEndController dans la scène ouverte.", "OK");
            return;
        }

        var scores = new List<TMP_Text>();
        var timers = new List<TMP_Text>();

        foreach (var t in Object.FindObjectsByType<Transform>(FindObjectsInactive.Include, FindObjectsSortMode.None))
        {
            if (!t.name.Contains("ScoreTimeSky")) continue;

            foreach (var text in t.GetComponentsInChildren<TMP_Text>(true))
            {
                string n = text.gameObject.name.ToLowerInvariant();
                if (n.Contains("score")) scores.Add(text);
                else if (n.Contains("timer") || n.Contains("time")) timers.Add(text);
            }
        }

        if (scores.Count == 0 && timers.Count == 0)
        {
            EditorUtility.DisplayDialog("Sky Score Timer",
                "Aucun objet 'ScoreTimeSky' avec ScoreText / TimerText trouvé dans la scène ouverte.", "OK");
            return;
        }

        Undo.RecordObject(end, "Link Sky Score Timer");
        end.extraScoreTexts = Merge(end.extraScoreTexts, scores);
        end.extraTimerTexts = Merge(end.extraTimerTexts, timers);
        EditorUtility.SetDirty(end);
        EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());

        Debug.Log($"Sky Score Timer : {scores.Count} texte(s) de score et {timers.Count} de timer branchés.");
    }

    // Garde ce qui est déjà dans la liste et ajoute les nouveaux sans doublon
    static TMP_Text[] Merge(TMP_Text[] existing, List<TMP_Text> added)
    {
        var result = new List<TMP_Text>();
        if (existing != null)
            foreach (var e in existing) if (e != null && !result.Contains(e)) result.Add(e);
        foreach (var a in added) if (!result.Contains(a)) result.Add(a);
        return result.ToArray();
    }
}
