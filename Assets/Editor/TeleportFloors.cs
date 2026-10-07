using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Locomotion.Teleportation;

public static class TeleportFloors
{
    [MenuItem("Tools/Make Floors Teleportable")]
    static void Run()
    {
        int n = Apply(null);
        EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
        Debug.Log($"Make Floors Teleportable : {n} sol(s) rendus téléportables.");
    }

    // Ajoute un TeleportationArea (couche "Teleport", comme dans la scène du menu)
    // sur chaque objet dont le nom commence par "floor" et qui a un collider.
    // root = null : toute la scène active.
    public static int Apply(GameObject root)
    {
        int count = 0;
        var colliders = root != null
            ? root.GetComponentsInChildren<Collider>(true)
            : Object.FindObjectsByType<Collider>(FindObjectsInactive.Include, FindObjectsSortMode.None);

        int teleportMask = InteractionLayerMask.GetMask("Teleport");

        foreach (var col in colliders)
        {
            var go = col.gameObject;
            if (!go.name.StartsWith("floor", System.StringComparison.OrdinalIgnoreCase)) continue;
            if (go.GetComponentInParent<UnityEngine.XR.Interaction.Toolkit.XRInteractionManager>() != null) continue;

            var area = go.GetComponent<TeleportationArea>();
            if (area == null) area = Undo.AddComponent<TeleportationArea>(go);

            Undo.RecordObject(area, "Make Floors Teleportable");
            area.interactionLayers = teleportMask;
            EditorUtility.SetDirty(area);
            count++;
        }
        return count;
    }
}
