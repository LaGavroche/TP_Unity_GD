using System;
using TMPro;
using UnityEditor;
using UnityEditor.Events;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

public static class CreateVRMenu
{
    [MenuItem("Tools/Create VR Menu")]
    static void Create()
    {
        // Canvas en World Space, 600x400 unités, échelle 0.001 => 60 x 40 cm
        var canvasGO = new GameObject("MenuCanvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        Undo.RegisterCreatedObjectUndo(canvasGO, "Create VR Menu");
        var canvas = canvasGO.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.WorldSpace;
        canvas.worldCamera = Camera.main;
        var canvasRT = canvasGO.GetComponent<RectTransform>();
        canvasRT.sizeDelta = new Vector2(600, 400);
        canvasRT.localScale = Vector3.one * 0.001f;
        canvasRT.position = new Vector3(0f, 1.5f, 1.5f);

        // Raycaster pour les rayons VR (ignoré s'il n'est pas disponible)
        var trackedType = Type.GetType(
            "UnityEngine.XR.Interaction.Toolkit.UI.TrackedDeviceGraphicRaycaster, Unity.XR.Interaction.Toolkit");
        if (trackedType != null) canvasGO.AddComponent(trackedType);

        var menu = canvasGO.AddComponent<MainMenu>();

        // Panneau du menu
        var menuPanel = CreatePanel("menu pannel", canvasGO.transform, new Color(0.1f, 0.1f, 0.15f, 0.9f));
        CreateText("Title", menuPanel.transform, "MENU", 48, new Vector2(0, 140), new Vector2(500, 70));
        var playBtn = CreateButton("Play", menuPanel.transform, "Play", new Vector2(0, 40));
        var creditBtn = CreateButton("Credit", menuPanel.transform, "Credit", new Vector2(0, -50));
        var exitBtn = CreateButton("Exit", menuPanel.transform, "Exit", new Vector2(0, -140));

        // Panneau des crédits (caché au départ)
        var creditsPanel = CreatePanel("CreditsPanel", canvasGO.transform, new Color(0.1f, 0.1f, 0.15f, 0.9f));
        CreateText("CreditsText", creditsPanel.transform, "Crédits\n\nÀ remplacer par tes noms", 36,
            new Vector2(0, 40), new Vector2(500, 220));
        var backBtn = CreateButton("Retour", creditsPanel.transform, "Retour", new Vector2(0, -140));
        creditsPanel.SetActive(false);

        // Branchements On Click
        UnityEventTools.AddPersistentListener(playBtn.onClick, menu.Play);
        UnityEventTools.AddPersistentListener(exitBtn.onClick, menu.Quit);

        UnityEventTools.AddBoolPersistentListener(creditBtn.onClick, creditsPanel.SetActive, true);
        UnityEventTools.AddBoolPersistentListener(creditBtn.onClick, menuPanel.SetActive, false);

        UnityEventTools.AddBoolPersistentListener(backBtn.onClick, creditsPanel.SetActive, false);
        UnityEventTools.AddBoolPersistentListener(backBtn.onClick, menuPanel.SetActive, true);

        // EventSystem (nécessaire pour cliquer)
        if (UnityEngine.Object.FindFirstObjectByType<UnityEngine.EventSystems.EventSystem>() == null)
        {
            var es = new GameObject("EventSystem", typeof(UnityEngine.EventSystems.EventSystem));
            Undo.RegisterCreatedObjectUndo(es, "Create VR Menu");
        }

        Selection.activeGameObject = canvasGO;
        EditorSceneManager.MarkSceneDirty(canvasGO.scene);
    }

    static GameObject CreatePanel(string name, Transform parent, Color color)
    {
        var go = new GameObject(name, typeof(RectTransform), typeof(Image));
        go.transform.SetParent(parent, false);
        go.GetComponent<Image>().color = color;
        var rt = go.GetComponent<RectTransform>();
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = rt.offsetMax = Vector2.zero;
        return go;
    }

    static TextMeshProUGUI CreateText(string name, Transform parent, string text, float size, Vector2 pos, Vector2 dim)
    {
        var go = new GameObject(name, typeof(RectTransform), typeof(TextMeshProUGUI));
        go.transform.SetParent(parent, false);
        var tmp = go.GetComponent<TextMeshProUGUI>();
        tmp.text = text;
        tmp.fontSize = size;
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.color = Color.white;
        var rt = go.GetComponent<RectTransform>();
        rt.anchoredPosition = pos;
        rt.sizeDelta = dim;
        return tmp;
    }

    static Button CreateButton(string name, Transform parent, string label, Vector2 pos)
    {
        var go = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(Button));
        go.transform.SetParent(parent, false);
        var rt = go.GetComponent<RectTransform>();
        rt.anchoredPosition = pos;
        rt.sizeDelta = new Vector2(300, 70);
        go.GetComponent<Image>().color = new Color(0.25f, 0.45f, 0.9f, 1f);

        var text = CreateText("Text (TMP)", go.transform, label, 32, Vector2.zero, rt.sizeDelta);
        text.rectTransform.anchorMin = Vector2.zero;
        text.rectTransform.anchorMax = Vector2.one;
        text.rectTransform.offsetMin = text.rectTransform.offsetMax = Vector2.zero;
        return go.GetComponent<Button>();
    }
}
