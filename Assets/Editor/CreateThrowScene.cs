using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

public static class CreateThrowScene
{
    const string ScenePath = "Assets/Scenes/Scene_Lancer.unity";
    const string MenuScenePath = "Assets/Scene_GD_Group.unity";
    const string RigPath = "Assets/Scene_GD_Group/XR Origin (XR Rig).prefab";
    const string MeshPath = "Assets/Meshes/Donut.asset";

    const float GroundSize = 40f;        // le Plane natif fait 10 m : scale 4 => 40 x 40 m
    const float MajorRadius = 4f;        // rayon du donut (centre du tube)
    const float MinorRadius = 0.5f;      // rayon du tube
    const float Lift = 0f;               // hauteur supplémentaire au-dessus du sol
    static readonly Vector3 SpawnPos = new Vector3(0f, 0f, -12f);   // le joueur démarre à 12 m, face au donut

    [MenuItem("Tools/Create Throw Scene")]
    static void Create()
    {
        if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;

        var rigPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(RigPath);
        if (rigPrefab == null)
        {
            EditorUtility.DisplayDialog("Throw Scene", "Prefab introuvable : " + RigPath, "OK");
            return;
        }

        EnsureFolder("Assets/Materials");
        EnsureFolder("Assets/Meshes");
        EnsureFolder("Assets/Scenes");

        var floorMat = GetMaterial("Assets/Materials/ThrowFloor.mat", new Color(0.25f, 0.27f, 0.30f));
        var donutMat = GetMaterial("Assets/Materials/Donut.mat", new Color(0.95f, 0.55f, 0.25f));

        AssetDatabase.DeleteAsset(MeshPath);
        var donutMesh = BuildTorus(MajorRadius, MinorRadius, 64, 32);
        donutMesh.name = "Donut";
        AssetDatabase.CreateAsset(donutMesh, MeshPath);

        var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

        // Lumière
        var lightGO = new GameObject("Directional Light");
        var light = lightGO.AddComponent<Light>();
        light.type = LightType.Directional;
        light.shadows = LightShadows.Soft;
        lightGO.transform.rotation = Quaternion.Euler(50f, -30f, 0f);

        // Sol 40 x 40
        var ground = GameObject.CreatePrimitive(PrimitiveType.Plane);
        ground.name = "Ground";
        ground.transform.localScale = new Vector3(GroundSize / 10f, 1f, GroundSize / 10f);
        ground.GetComponent<MeshRenderer>().sharedMaterial = floorMat;
        ground.isStatic = true;

        // Donut posé au milieu du sol (collider non convexe : le trou reste un vrai trou)
        var donut = new GameObject("Donut", typeof(MeshFilter), typeof(MeshRenderer), typeof(MeshCollider));
        donut.transform.position = new Vector3(0f, MinorRadius + Lift, 0f);
        donut.GetComponent<MeshFilter>().sharedMesh = donutMesh;
        donut.GetComponent<MeshRenderer>().sharedMaterial = donutMat;
        var col = donut.GetComponent<MeshCollider>();
        col.sharedMesh = donutMesh;
        col.convex = false;
        donut.isStatic = true;

        // Joueur VR
        var rig = (GameObject)PrefabUtility.InstantiatePrefab(rigPrefab);
        rig.transform.position = SpawnPos;
        rig.transform.rotation = Quaternion.identity;

        // XR Interaction Manager + EventSystem pour l'UI à venir (score)
        new GameObject("XR Interaction Manager", typeof(UnityEngine.XR.Interaction.Toolkit.XRInteractionManager));
        new GameObject("EventSystem", typeof(UnityEngine.EventSystems.EventSystem),
            typeof(UnityEngine.XR.Interaction.Toolkit.UI.XRUIInputModule));

        EditorSceneManager.SaveScene(scene, ScenePath);
        UpdateBuildSettings();
        AssetDatabase.SaveAssets();
        Debug.Log("Scene_Lancer créée et ajoutée aux Build Settings.");
    }

    static void UpdateBuildSettings()
    {
        // Ordre : menu en premier (index 0), puis la scène de lancer, puis le reste
        var result = new List<EditorBuildSettingsScene>();
        if (File.Exists(MenuScenePath)) result.Add(new EditorBuildSettingsScene(MenuScenePath, true));
        result.Add(new EditorBuildSettingsScene(ScenePath, true));
        foreach (var s in EditorBuildSettings.scenes)
            if (s.path != MenuScenePath && s.path != ScenePath)
                result.Add(s);
        EditorBuildSettings.scenes = result.ToArray();
    }

    static void EnsureFolder(string path)
    {
        if (AssetDatabase.IsValidFolder(path)) return;
        AssetDatabase.CreateFolder(Path.GetDirectoryName(path).Replace('\\', '/'), Path.GetFileName(path));
    }

    static Material GetMaterial(string path, Color color)
    {
        var mat = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (mat == null)
        {
            mat = new Material(Shader.Find("Universal Render Pipeline/Lit"));
            AssetDatabase.CreateAsset(mat, path);
        }
        mat.SetColor("_BaseColor", color);
        EditorUtility.SetDirty(mat);
        return mat;
    }

    // Tore généré par code (Unity n'a pas de tore natif)
    static Mesh BuildTorus(float major, float minor, int segments, int sides)
    {
        var verts = new Vector3[(segments + 1) * (sides + 1)];
        var uvs = new Vector2[verts.Length];
        var tris = new int[segments * sides * 6];

        for (int i = 0; i <= segments; i++)
        {
            float u = (float)i / segments;
            float theta = u * Mathf.PI * 2f;
            var center = new Vector3(Mathf.Cos(theta), 0f, Mathf.Sin(theta)) * major;

            for (int j = 0; j <= sides; j++)
            {
                float v = (float)j / sides;
                float phi = v * Mathf.PI * 2f;
                var dir = new Vector3(Mathf.Cos(theta) * Mathf.Cos(phi), Mathf.Sin(phi), Mathf.Sin(theta) * Mathf.Cos(phi));
                int idx = i * (sides + 1) + j;
                verts[idx] = center + dir * minor;
                uvs[idx] = new Vector2(u * 8f, v * 2f);
            }
        }

        int t = 0;
        for (int i = 0; i < segments; i++)
        {
            for (int j = 0; j < sides; j++)
            {
                int a = i * (sides + 1) + j;
                int b = (i + 1) * (sides + 1) + j;
                int c = b + 1;
                int d = a + 1;
                tris[t++] = a; tris[t++] = d; tris[t++] = b;
                tris[t++] = b; tris[t++] = d; tris[t++] = c;
            }
        }

        var mesh = new Mesh { vertices = verts, uv = uvs, triangles = tris };
        mesh.RecalculateNormals();
        mesh.RecalculateBounds();
        return mesh;
    }
}
