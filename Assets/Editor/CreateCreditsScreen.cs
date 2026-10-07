using System;
using TMPro;
using UnityEditor;
using UnityEditor.Events;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

public static class CreateCreditsScreen
{
    const string CreditsText =
        "<size=70%>Il y a bien longtemps, dans une salle de classe\nlointaine, très lointaine...</size>\n\n\n" +
        "<size=140%>ÉPISODE I</size>\n" +
        "<size=170%>LE PROJET VR</size>\n\n\n\n" +

        "<size=120%>ENCADREMENT</size>\n" +
        "<size=65%>Président Directeur Général, Capitaine,\nEmpereur, Guide et Grand Manitou,\nMaître des Arts Mystiques de Unity</size>\n" +
        "Rémi JAMOUS\n\n\n" +

        "<size=120%>GAME DESIGN</size>\n" +
        "Hamza BOUHOUCH\nRayan BOITEAU\nQuentin CHERON\n\n\n" +

        "<size=120%>DÉPLACEMENT</size>\n" +
        "Esteban BASSON\nMaxime GIRARDET\nYohann MATHIEUX\n\n\n" +

        "<size=120%>LEVEL DESIGN</size>\n" +
        "Alexis CROZIER\nClément BERARD\nMatéo DEPORT\n\n\n" +

        "<size=120%>INTERACTION DE LANCER</size>\n" +
        "Jordan JIMENEZ\nBranis KACI\nClément BARDIN\n\n\n\n" +

        "<size=130%>MERCI D'AVOIR JOUÉ</size>\n\n\n\n\n\n";

    [MenuItem("Tools/Create Credits Screen")]
    static void Create()
    {
        var menu = UnityEngine.Object.FindFirstObjectByType<MainMenu>(FindObjectsInactive.Include);
        if (menu == null)
        {
            EditorUtility.DisplayDialog("Credits", "MenuCanvas introuvable. Lance d'abord Tools > Create VR Menu.", "OK");
            return;
        }

        var menuRoot = menu.transform;
        var menuPanel = menuRoot.Find("menu pannel");
        var creditBtnT = menuRoot.Find("menu pannel/Credit");
        if (menuPanel == null || creditBtnT == null)
        {
            EditorUtility.DisplayDialog("Credits", "Il faut 'menu pannel' avec un bouton 'Credit' dans MenuCanvas.", "OK");
            return;
        }
        var creditBtn = creditBtnT.GetComponent<Button>();

        // Nettoyage : ancien petit panneau de crédits et ancien écran s'il existe
        var oldPanel = menuRoot.Find("CreditsPanel");
        if (oldPanel != null) Undo.DestroyObjectImmediate(oldPanel.gameObject);
        foreach (var c in UnityEngine.Object.FindObjectsByType<Canvas>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            if (c.gameObject.name == "CreditsCanvas") Undo.DestroyObjectImmediate(c.gameObject);

        // Grand écran : 1600 x 900 unités à l'échelle 0.004 => 6.4 m x 3.6 m
        var canvasGO = new GameObject("CreditsCanvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        Undo.RegisterCreatedObjectUndo(canvasGO, "Create Credits Screen");
        var canvas = canvasGO.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.WorldSpace;
        canvas.worldCamera = Camera.main;
        var canvasRT = canvasGO.GetComponent<RectTransform>();
        canvasRT.sizeDelta = new Vector2(1600, 900);
        canvasRT.localScale = Vector3.one * 0.0035f;
        canvasRT.position = new Vector3(0f, 2.2f, 4f);

        var trackedType = Type.GetType(
            "UnityEngine.XR.Interaction.Toolkit.UI.TrackedDeviceGraphicRaycaster, Unity.XR.Interaction.Toolkit");
        if (trackedType != null) canvasGO.AddComponent(trackedType);

        // Fond noir
        var bg = new GameObject("Background", typeof(RectTransform), typeof(Image));
        bg.transform.SetParent(canvasGO.transform, false);
        bg.GetComponent<Image>().color = Color.black;
        Stretch(bg.GetComponent<RectTransform>());

        // Plan incliné avec masque : le texte s'éloigne vers le haut
        var plane = new GameObject("CrawlPlane", typeof(RectTransform), typeof(RectMask2D));
        plane.transform.SetParent(canvasGO.transform, false);
        var planeRT = plane.GetComponent<RectTransform>();
        planeRT.sizeDelta = new Vector2(1100, 650);
        planeRT.anchoredPosition = new Vector2(0, 60);
        planeRT.localEulerAngles = new Vector3(45f, 0f, 0f);   // inverse le signe si l'inclinaison est à l'envers

        var textGO = new GameObject("CreditsText", typeof(RectTransform), typeof(TextMeshProUGUI), typeof(CreditsCrawl));
        textGO.transform.SetParent(plane.transform, false);
        var tmp = textGO.GetComponent<TextMeshProUGUI>();
        tmp.text = CreditsText;
        tmp.fontSize = 56;
        tmp.alignment = TextAlignmentOptions.Top;
        tmp.color = new Color(1f, 0.85f, 0.1f);
        var textRT = tmp.rectTransform;
        textRT.anchorMin = textRT.anchorMax = new Vector2(0.5f, 0f);
        textRT.pivot = new Vector2(0.5f, 1f);
        textRT.sizeDelta = new Vector2(1000, 100);
        textRT.anchoredPosition = Vector2.zero;
        textGO.GetComponent<CreditsCrawl>().viewport = planeRT;

        // Bouton Retour, en dehors du masque
        var backBtn = CreateButton("Retour", canvasGO.transform, "Retour", new Vector2(0, -400));

        // Branchements
        ClearListeners(creditBtn);
        UnityEventTools.AddBoolPersistentListener(creditBtn.onClick, canvasGO.SetActive, true);
        UnityEventTools.AddBoolPersistentListener(creditBtn.onClick, menuPanel.gameObject.SetActive, false);

        UnityEventTools.AddBoolPersistentListener(backBtn.onClick, canvasGO.SetActive, false);
        UnityEventTools.AddBoolPersistentListener(backBtn.onClick, menuPanel.gameObject.SetActive, true);

        canvasGO.SetActive(false);   // caché au départ

        Selection.activeGameObject = canvasGO;
        EditorSceneManager.MarkSceneDirty(canvasGO.scene);
    }

    static void ClearListeners(Button b)
    {
        while (b.onClick.GetPersistentEventCount() > 0)
            UnityEventTools.RemovePersistentListener(b.onClick, 0);
    }

    static void Stretch(RectTransform rt)
    {
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = rt.offsetMax = Vector2.zero;
    }

    static Button CreateButton(string name, Transform parent, string label, Vector2 pos)
    {
        var go = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(Button));
        go.transform.SetParent(parent, false);
        var rt = go.GetComponent<RectTransform>();
        rt.anchoredPosition = pos;
        rt.sizeDelta = new Vector2(300, 70);
        go.GetComponent<Image>().color = new Color(0.25f, 0.45f, 0.9f, 1f);

        var textGO = new GameObject("Text (TMP)", typeof(RectTransform), typeof(TextMeshProUGUI));
        textGO.transform.SetParent(go.transform, false);
        var t = textGO.GetComponent<TextMeshProUGUI>();
        t.text = label;
        t.fontSize = 32;
        t.alignment = TextAlignmentOptions.Center;
        t.color = Color.white;
        Stretch(t.rectTransform);
        return go.GetComponent<Button>();
    }
}
