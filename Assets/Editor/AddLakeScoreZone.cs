using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

// Ajoute une zone de score au-dessus de chaque "Lake and Target" de la scène.
// Le lac du prefab de l'autre groupe est un disque "lava" avec un collider plat et non trigger :
// on ajoute par-dessus un volume trigger (ScoreZone) pour compter les objets qui y entrent.
public static class AddLakeScoreZone
{
    const string ZoneName = "LakeScoreZone";
    const float ZoneHeight = 1.6f;   // hauteur du volume au-dessus du lac (m)

    [MenuItem("Tools/Add Score Zone To Lake")]
    static void Run()
    {
        // Pour pouvoir relancer l'outil sans doublon
        foreach (var go in Object.FindObjectsByType<Transform>(FindObjectsInactive.Include, FindObjectsSortMode.None))
        {
            if (go != null && go.name == ZoneName)
                Undo.DestroyObjectImmediate(go.gameObject);
        }

        int count = 0;
        var capsules = Object.FindObjectsByType<CapsuleCollider>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        foreach (var cap in capsules)
        {
            // L'objet du lac s'appelle "lava " (avec une espace à la fin dans le prefab)
            if (cap.gameObject.name.Trim().ToLowerInvariant() != "lava") continue;

            // Taille et position du lac en coordonnées monde
            Transform t = cap.transform;
            Vector3 center = t.TransformPoint(cap.center);
            Vector3 s = t.lossyScale;
            float radius = cap.radius * Mathf.Max(Mathf.Abs(s.x), Mathf.Abs(s.z));

            var zoneGO = new GameObject(ZoneName);
            Undo.RegisterCreatedObjectUndo(zoneGO, "Add Lake Score Zone");
            zoneGO.transform.position = new Vector3(center.x, center.y + ZoneHeight / 2f, center.z);

            var trigger = zoneGO.AddComponent<CapsuleCollider>();
            trigger.direction = 1;                 // axe Y
            trigger.radius = radius;
            trigger.height = ZoneHeight;
            trigger.isTrigger = true;

            var zone = zoneGO.AddComponent<ScoreZone>();
            zone.requireGrabbable = false;         // compte tout objet avec un Rigidbody (pas les mains du joueur)

            // Rangé dans le prefab du lac : la zone suit la map si elle est déplacée
            var root = PrefabUtility.GetOutermostPrefabInstanceRoot(cap.gameObject);
            if (root != null)
                zoneGO.transform.SetParent(root.transform, true);

            count++;
        }

        if (count == 0)
        {
            EditorUtility.DisplayDialog("Lake Score Zone",
                "Aucun lac trouvé (objet 'lava' avec un Capsule Collider). " +
                "Vérifie que le prefab 'Scene 1' ou 'Lake and Target' est bien dans la scène ouverte.", "OK");
            return;
        }

        EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
        Debug.Log($"Add Score Zone To Lake : {count} zone(s) de score créée(s).");
    }
}
