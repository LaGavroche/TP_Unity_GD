using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;

public static class CreateHubSphere
{
    const float Radius = 6f;                          // rayon en mètres (pièce d'environ 12 m de diamètre)
    static readonly Vector3 Center = new Vector3(0f, 1.5f, 0f);

    [MenuItem("Tools/Create Hub Sphere")]
    static void Create()
    {
        // Matériau noir opaque, rendu à l'intérieur de la sphère (faces avant coupées)
        var shader = Shader.Find("Universal Render Pipeline/Unlit");
        if (shader == null)
        {
            EditorUtility.DisplayDialog("Hub", "Shader URP Unlit introuvable.", "OK");
            return;
        }

        if (!AssetDatabase.IsValidFolder("Assets/Materials"))
            AssetDatabase.CreateFolder("Assets", "Materials");

        const string matPath = "Assets/Materials/HubBlack.mat";
        var mat = AssetDatabase.LoadAssetAtPath<Material>(matPath);
        if (mat == null)
        {
            mat = new Material(shader);
            AssetDatabase.CreateAsset(mat, matPath);
        }
        mat.shader = shader;
        mat.SetColor("_BaseColor", Color.black);
        mat.SetFloat("_Surface", 0f);   // opaque
        mat.SetFloat("_Cull", 1f);      // Cull Front : on voit l'intérieur de la sphère
        EditorUtility.SetDirty(mat);

        // Supprime une ancienne sphère pour pouvoir relancer l'outil
        var old = GameObject.Find("HubSphere");
        if (old != null) Undo.DestroyObjectImmediate(old);

        var sphere = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        Undo.RegisterCreatedObjectUndo(sphere, "Create Hub Sphere");
        sphere.name = "HubSphere";
        sphere.transform.position = Center;
        sphere.transform.localScale = Vector3.one * Radius * 2f;

        // Aucun collider : le joueur et les rayons ne doivent pas la heurter
        Object.DestroyImmediate(sphere.GetComponent<Collider>());

        var r = sphere.GetComponent<MeshRenderer>();
        r.sharedMaterial = mat;
        r.shadowCastingMode = ShadowCastingMode.Off;
        r.receiveShadows = false;

        Selection.activeGameObject = sphere;
        EditorSceneManager.MarkSceneDirty(sphere.scene);
        AssetDatabase.SaveAssets();
    }
}
