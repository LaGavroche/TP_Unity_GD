using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

public static class CreateWristHUD
{
    const string ControllerName = "Left Controller";   // mets "Right Controller" pour un montage à droite

    [MenuItem("Tools/Create Wrist HUD")]
    static void Create()
    {
        var hand = GameObject.Find(ControllerName);
        if (hand == null)
        {
            EditorUtility.DisplayDialog("Wrist HUD",
                "Objet '" + ControllerName + "' introuvable. Ouvre la scène qui contient le XR Origin (Scene_Map).", "OK");
            return;
        }

        var old = hand.transform.Find("WristHUD");
        if (old != null) Undo.DestroyObjectImmediate(old.gameObject);

        // Canvas World Space : 300 x 160 unités à l'échelle 0.0004 => 12 x 6.4 cm
        var go = new GameObject("WristHUD", typeof(Canvas), typeof(CanvasGroup), typeof(WristHUD));
        Undo.RegisterCreatedObjectUndo(go, "Create Wrist HUD");
        go.transform.SetParent(hand.transform, false);

        var canvas = go.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.WorldSpace;

        var rt = go.GetComponent<RectTransform>();
        rt.sizeDelta = new Vector2(300f, 160f);
        rt.localScale = Vector3.one * 0.0004f;
        rt.localPosition = new Vector3(0f, 0.05f, 0.02f);       // au-dessus de la manette, côté poignet
        rt.localRotation = Quaternion.Euler(60f, 0f, 0f);        // incliné vers les yeux du joueur

        // Fond sombre semi-transparent
        var bg = new GameObject("Background", typeof(RectTransform), typeof(Image));
        bg.transform.SetParent(go.transform, false);
        var img = bg.GetComponent<Image>();
        img.color = new Color(0f, 0f, 0f, 0.6f);
        img.raycastTarget = false;
        Stretch(bg.GetComponent<RectTransform>());

        var timer = CreateText("TimerText", go.transform, "60", 100, new Vector2(0f, 30f), new Vector2(280f, 100f));
        var score = CreateText("ScoreText", go.transform, "0 / 10", 55, new Vector2(0f, -45f), new Vector2(280f, 60f));

        // Branche les textes sur le Game End Controller
        var end = Object.FindFirstObjectByType<GameEndController>();
        if (end != null)
        {
            Undo.RecordObject(end, "Create Wrist HUD");
            end.timerText = timer;
            end.scoreText = score;
            EditorUtility.SetDirty(end);
        }
        else
        {
            Debug.LogWarning("Wrist HUD créé, mais aucun GameEndController dans la scène : " +
                             "glisse TimerText et ScoreText dans ses champs à la main.");
        }

        Selection.activeGameObject = go;
        EditorSceneManager.MarkSceneDirty(go.scene);
    }

    static void Stretch(RectTransform rt)
    {
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = rt.offsetMax = Vector2.zero;
    }

    static TextMeshProUGUI CreateText(string name, Transform parent, string text, float size, Vector2 pos, Vector2 dim)
    {
        var go = new GameObject(name, typeof(RectTransform), typeof(TextMeshProUGUI));
        go.transform.SetParent(parent, false);
        var t = go.GetComponent<TextMeshProUGUI>();
        t.text = text;
        t.fontSize = size;
        t.fontStyle = FontStyles.Bold;
        t.alignment = TextAlignmentOptions.Center;
        t.color = Color.white;
        t.raycastTarget = false;
        var rt = t.rectTransform;
        rt.anchoredPosition = pos;
        rt.sizeDelta = dim;
        return t;
    }
}
