using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

public static class CreateMapScene
{
    const string ScenePath = "Assets/Scenes/Scene_Map.unity";
    const string MenuScenePath = "Assets/Scene_GD_Group.unity";
    const string RigPath = "Assets/Scene_GD_Group/XR Origin (XR Rig).prefab";
    const string MapPath = "Assets/CustomPrefabs/MainMap.prefab";

    // Position de départ du joueur (à ajuster selon l'ébauche du level design)
    static readonly Vector3 SpawnPos = new Vector3(4f, 0.1f, 4f);

    [MenuItem("Tools/Create Map Scene")]
    static void Create()
    {
        if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;

        var mapPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(MapPath);
        var rigPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(RigPath);
        if (mapPrefab == null || rigPrefab == null)
        {
            EditorUtility.DisplayDialog("Map Scene", "Prefab introuvable :\n" + MapPath + "\n" + RigPath, "OK");
            return;
        }

        if (!AssetDatabase.IsValidFolder("Assets/Scenes")) AssetDatabase.CreateFolder("Assets", "Scenes");

        var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

        // Lumière
        var lightGO = new GameObject("Directional Light");
        var light = lightGO.AddComponent<Light>();
        light.type = LightType.Directional;
        light.shadows = LightShadows.Soft;
        lightGO.transform.rotation = Quaternion.Euler(50f, -30f, 0f);

        // Map du level design
        var map = (GameObject)PrefabUtility.InstantiatePrefab(mapPrefab);
        map.transform.position = Vector3.zero;

        // Les tuiles n'ont pas de collider : on en ajoute dans la scène (le prefab n'est pas modifié)
        int added = 0;
        foreach (var mf in map.GetComponentsInChildren<MeshFilter>(true))
        {
            if (mf.sharedMesh == null || mf.GetComponent<Collider>() != null) continue;
            var mc = mf.gameObject.AddComponent<MeshCollider>();
            mc.sharedMesh = mf.sharedMesh;
            added++;
        }

        // Sols téléportables (rayon de téléportation)
        int teleportable = TeleportFloors.Apply(map);

        // Joueur VR
        var rig = (GameObject)PrefabUtility.InstantiatePrefab(rigPrefab);
        rig.transform.position = SpawnPos;

        new GameObject("XR Interaction Manager", typeof(UnityEngine.XR.Interaction.Toolkit.XRInteractionManager));
        new GameObject("EventSystem", typeof(UnityEngine.EventSystems.EventSystem),
            typeof(UnityEngine.XR.Interaction.Toolkit.UI.XRUIInputModule));

        EditorSceneManager.SaveScene(scene, ScenePath);

        // Build Settings : menu en 0, puis la map
        var list = new List<EditorBuildSettingsScene>();
        if (File.Exists(MenuScenePath)) list.Add(new EditorBuildSettingsScene(MenuScenePath, true));
        list.Add(new EditorBuildSettingsScene(ScenePath, true));
        foreach (var s in EditorBuildSettings.scenes)
            if (s.path != MenuScenePath && s.path != ScenePath) list.Add(s);
        EditorBuildSettings.scenes = list.ToArray();

        Debug.Log($"Scene_Map créée ({added} colliders, {teleportable} sols téléportables) et ajoutée aux Build Settings.");
    }
}
