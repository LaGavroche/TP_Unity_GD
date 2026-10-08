using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

// Remplace les objets de test posés sur la table (Ball_*, Kunai_* ou Sword_*) par les prefabs
// récents : kunai au milieu, épées aux deux extrémités. Les objets gardent leur position et leur parent.
public static class ReplaceBallsWithKunai
{
    const string KunaiPath = "Assets/Export/KunaiBloody Variant 1.prefab";
    const string SwordPath = "Assets/Export/LongSword.prefab";

    [MenuItem("Tools/Replace Balls With Kunai")]
    static void Run()
    {
        var kunai = AssetDatabase.LoadAssetAtPath<GameObject>(KunaiPath);
        var sword = AssetDatabase.LoadAssetAtPath<GameObject>(SwordPath);
        if (kunai == null || sword == null)
        {
            EditorUtility.DisplayDialog("Armes",
                "Prefab introuvable :\n" + KunaiPath + "\n" + SwordPath, "OK");
            return;
        }

        // Objets de test à remplacer
        var items = new List<Transform>();
        foreach (var t in Object.FindObjectsByType<Transform>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            if (t.name.StartsWith("Ball_") || t.name.StartsWith("Kunai_") || t.name.StartsWith("Sword_"))
                items.Add(t);

        if (items.Count == 0)
        {
            EditorUtility.DisplayDialog("Armes", "Aucun objet de test (Ball_*, Kunai_*, Sword_*) dans la scène ouverte.", "OK");
            return;
        }

        // Orientation de la table, et ordre de gauche à droite le long de la table
        var table = GameObject.Find("Table");
        Quaternion rotation = table != null ? table.transform.rotation : Quaternion.identity;
        Vector3 right = rotation * Vector3.right;
        items.Sort((a, b) => Vector3.Dot(a.position, right).CompareTo(Vector3.Dot(b.position, right)));

        int kunaiCount = 0, swordCount = 0;
        for (int i = 0; i < items.Count; i++)
        {
            var old = items[i];
            bool isSword = (i == 0 || i == items.Count - 1) && items.Count > 2;   // épées aux deux bouts

            var prefab = isSword ? sword : kunai;
            var go = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
            Undo.RegisterCreatedObjectUndo(go, "Replace test items");

            go.name = isSword ? "Sword_" + (++swordCount) : "Kunai_" + (++kunaiCount);
            go.transform.SetParent(old.parent, true);
            go.transform.SetPositionAndRotation(old.position, rotation);

            Undo.DestroyObjectImmediate(old.gameObject);
        }

        EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
        Debug.Log($"Armes remplacées : {kunaiCount} kunai (récents) et {swordCount} épée(s).");
    }
}
