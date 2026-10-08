using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit.Locomotion.Teleportation;

// Répartit des objets à lancer (kunai et épées) sur la map :
//  - quelques-uns sur chaque plateforme de destination des téléporteurs ;
//  - le reste un peu partout sur les sols de la map.
// Fixe aussi la valeur en points de chaque type d'objet (kunai 3, épée 10) sur les prefabs.
public static class ScatterThrowItems
{
    const string KunaiPath = "Assets/Export/KunaiBloody Variant 1.prefab";
    const string OldKunaiPath = "Assets/Prefabs/KunaiBloody Variant.prefab";
    const string SwordPath = "Assets/Export/LongSword.prefab";

    const int KunaiPoints = 3;
    const int SwordPoints = 10;

    // Quantités (les épées valent plus de points : il y en a moins)
    const int KunaiPerDestination = 2;      // par destination de téléporteur
    const int SwordEveryNthDestination = 2; // une épée sur une destination sur deux
    const int GroundKunai = 14;             // kunai répartis sur les sols
    const int GroundSwords = 3;             // épées réparties sur les sols

    const float RingRadius = 1.6f;          // distance autour du point d'arrivée d'un téléporteur
    const float MinDistanceToPlayer = 3f;
    const float MinDistanceBetweenItems = 1.5f;
    const string RootName = "ScatteredItems";

    [MenuItem("Tools/Scatter Throw Items")]
    static void Run()
    {
        SetPoints(KunaiPath, KunaiPoints);
        SetPoints(OldKunaiPath, KunaiPoints);
        SetPoints(SwordPath, SwordPoints);

        var kunai = AssetDatabase.LoadAssetAtPath<GameObject>(KunaiPath);
        var sword = AssetDatabase.LoadAssetAtPath<GameObject>(SwordPath);
        if (kunai == null || sword == null)
        {
            EditorUtility.DisplayDialog("Scatter", "Prefabs introuvables :\n" + KunaiPath + "\n" + SwordPath, "OK");
            return;
        }

        Physics.SyncTransforms();

        // Relance sans doublon
        var old = GameObject.Find(RootName);
        if (old != null) Undo.DestroyObjectImmediate(old);
        var root = new GameObject(RootName);
        Undo.RegisterCreatedObjectUndo(root, "Scatter Throw Items");

        var rig = GameObject.Find("XR Origin (XR Rig)");
        Vector3 rigPos = rig != null ? rig.transform.position : Vector3.zero;

        var rng = new System.Random(42);
        var placed = new List<Vector3>();
        int nKunai = 0, nSword = 0;

        // ----- 1) Plateformes de destination des téléporteurs -----
        var destinations = new List<Vector3>();
        foreach (var anchor in Object.FindObjectsByType<TeleportationAnchor>(FindObjectsInactive.Exclude, FindObjectsSortMode.None))
        {
            Transform t = anchor.teleportAnchorTransform != null ? anchor.teleportAnchorTransform : anchor.transform;
            Vector3 p = t.position;
            if (!destinations.Exists(d => (d - p).sqrMagnitude < 4f)) destinations.Add(p);
        }
        destinations.Sort((a, b) => a.x != b.x ? a.x.CompareTo(b.x) : a.z.CompareTo(b.z));

        for (int d = 0; d < destinations.Count; d++)
        {
            int swords = (d % SwordEveryNthDestination == 0) ? 1 : 0;
            int total = KunaiPerDestination + swords;
            for (int i = 0; i < total; i++)
            {
                bool isSword = i < swords;
                // Essaie plusieurs angles autour de la destination jusqu'à trouver une place libre
                for (int attempt = 0; attempt < 8; attempt++)
                {
                    float angle = (i * 360f / total + attempt * 45f + d * 30f) * Mathf.Deg2Rad;
                    Vector3 probe = destinations[d] + new Vector3(Mathf.Cos(angle), 0f, Mathf.Sin(angle)) * RingRadius;
                    if (TryFindSpot(probe, destinations[d].y + 3f, false, out var spot) &&
                        FarFromOthers(spot, placed, 0.8f))
                    {
                        Spawn(isSword ? sword : kunai, root.transform, spot, rng, isSword,
                              isSword ? "Sword_P" + (++nSword) : "Kunai_P" + (++nKunai));
                        placed.Add(spot);
                        break;
                    }
                }
            }
        }

        // ----- 2) Sols de la map : un peu partout -----
        Bounds? bounds = FloorBounds();
        if (bounds.HasValue)
        {
            int wantKunai = GroundKunai, wantSwords = GroundSwords;
            for (int attempt = 0; attempt < 600 && (wantKunai > 0 || wantSwords > 0); attempt++)
            {
                var b = bounds.Value;
                float x = Mathf.Lerp(b.min.x, b.max.x, (float)rng.NextDouble());
                float z = Mathf.Lerp(b.min.z, b.max.z, (float)rng.NextDouble());
                if (!TryFindSpot(new Vector3(x, 0f, z), b.max.y + 20f, true, out var spot)) continue;
                if (Vector3.Distance(spot, rigPos) < MinDistanceToPlayer) continue;
                if (!FarFromOthers(spot, placed, MinDistanceBetweenItems)) continue;

                bool isSword = wantSwords > 0 && (wantKunai == 0 || rng.NextDouble() < 0.18);
                Spawn(isSword ? sword : kunai, root.transform, spot, rng, isSword,
                      isSword ? "Sword_G" + (++nSword) : "Kunai_G" + (++nKunai));
                placed.Add(spot);
                if (isSword) wantSwords--; else wantKunai--;
            }
        }

        EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
        Debug.Log($"Scatter Throw Items : {nKunai} kunai ({KunaiPoints} pts) et {nSword} épée(s) ({SwordPoints} pts) répartis, " +
                  $"autour de {destinations.Count} destination(s) de téléporteur et sur les sols.");
        if (destinations.Count == 0)
            Debug.LogWarning("Aucun téléporteur (TeleportationAnchor) trouvé dans la scène : seuls les sols ont reçu des objets.");
    }

    // Valeur en points, ajoutée sur le prefab (donc sur toutes ses instances)
    static void SetPoints(string path, int points)
    {
        if (AssetDatabase.LoadAssetAtPath<GameObject>(path) == null) return;
        var contents = PrefabUtility.LoadPrefabContents(path);
        var value = contents.GetComponent<ScoreValue>();
        if (value == null) value = contents.AddComponent<ScoreValue>();
        value.points = points;
        PrefabUtility.SaveAsPrefabAsset(contents, path);
        PrefabUtility.UnloadPrefabContents(contents);
    }

    static Bounds? FloorBounds()
    {
        Bounds? total = null;
        foreach (var c in Object.FindObjectsByType<Collider>(FindObjectsInactive.Exclude, FindObjectsSortMode.None))
        {
            if (!c.gameObject.name.StartsWith("floor", System.StringComparison.OrdinalIgnoreCase)) continue;
            if (total == null) total = c.bounds; else { var b = total.Value; b.Encapsulate(c.bounds); total = b; }
        }
        return total;
    }

    // Cherche le sol sous un point et vérifie qu'il y a de la place (pas de mur ni d'objet)
    static bool TryFindSpot(Vector3 xz, float fromY, bool floorsOnly, out Vector3 spot)
    {
        spot = default;
        var origin = new Vector3(xz.x, fromY, xz.z);
        if (!Physics.Raycast(origin, Vector3.down, out var hit, 400f, ~0, QueryTriggerInteraction.Ignore)) return false;
        if (hit.normal.y < 0.9f) return false;
        if (floorsOnly && !hit.collider.gameObject.name.StartsWith("floor", System.StringComparison.OrdinalIgnoreCase)) return false;

        // De la place au-dessus du sol : l'objet ne doit pas apparaître dans un mur
        if (Physics.CheckSphere(hit.point + Vector3.up * 0.5f, 0.35f, ~0, QueryTriggerInteraction.Ignore)) return false;

        spot = hit.point;
        return true;
    }

    static bool FarFromOthers(Vector3 p, List<Vector3> others, float minDistance)
    {
        foreach (var o in others)
            if (Vector3.Distance(o, p) < minDistance) return false;
        return true;
    }

    static void Spawn(GameObject prefab, Transform parent, Vector3 spot, System.Random rng, bool isSword, string name)
    {
        var go = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
        Undo.RegisterCreatedObjectUndo(go, "Scatter Throw Items");
        go.name = name;
        go.transform.SetParent(parent, true);
        float yaw = (float)rng.NextDouble() * 360f;
        // Un peu au-dessus du sol : l'objet se pose tout seul au lancement du jeu
        go.transform.SetPositionAndRotation(spot + Vector3.up * (isSword ? 0.12f : 0.06f), Quaternion.Euler(0f, yaw, 0f));
    }
}
