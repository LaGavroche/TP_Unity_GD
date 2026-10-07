using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

// Crée de quoi marquer des points : une table avec des balles attrapables devant le joueur
// et une cible au sol (zone de score) quelques mètres plus loin.
public static class CreateThrowTestSetup
{
    const int BallCount = 6;
    const float TableDistance = 0.9f;    // table devant le joueur (m)
    const float TableHeight = 0.9f;
    const float TargetDistance = 5f;     // cible devant le joueur (m)
    const float TargetSize = 1.8f;       // côté de la zone de score (m)

    [MenuItem("Tools/Create Throw Test Setup")]
    static void Create()
    {
        var rig = GameObject.Find("XR Origin (XR Rig)");
        if (rig == null)
        {
            EditorUtility.DisplayDialog("Throw Test", "XR Origin (XR Rig) introuvable dans la scène ouverte.", "OK");
            return;
        }

        var old = GameObject.Find("ThrowTestSetup");
        if (old != null) Undo.DestroyObjectImmediate(old);

        var root = new GameObject("ThrowTestSetup");
        Undo.RegisterCreatedObjectUndo(root, "Create Throw Test Setup");

        // Repère : devant le joueur, au niveau du sol
        Vector3 forward = rig.transform.forward;
        forward.y = 0f;
        forward.Normalize();
        Vector3 floor = rig.transform.position;

        var ballMat = GetMaterial("Assets/Materials/ThrowBall.mat", new Color(0.95f, 0.45f, 0.1f));
        var tableMat = GetMaterial("Assets/Materials/ThrowTable.mat", new Color(0.35f, 0.25f, 0.2f));
        var targetMat = GetMaterial("Assets/Materials/ThrowTarget.mat", new Color(0.1f, 0.8f, 0.3f));

        // Table
        var table = GameObject.CreatePrimitive(PrimitiveType.Cube);
        table.name = "Table";
        table.transform.SetParent(root.transform, false);
        table.transform.position = floor + forward * TableDistance + Vector3.up * (TableHeight / 2f);
        table.transform.rotation = Quaternion.LookRotation(forward);
        table.transform.localScale = new Vector3(1.4f, TableHeight, 0.5f);
        table.GetComponent<MeshRenderer>().sharedMaterial = tableMat;

        // Balles attrapables, posées sur la table
        for (int i = 0; i < BallCount; i++)
        {
            float offset = (i - (BallCount - 1) / 2f) * 0.2f;
            var ball = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            ball.name = "Ball_" + (i + 1);
            ball.transform.SetParent(root.transform, false);
            ball.transform.localScale = Vector3.one * 0.15f;
            ball.transform.position = table.transform.position
                                      + table.transform.right * offset
                                      + Vector3.up * (TableHeight / 2f + 0.1f);
            ball.GetComponent<MeshRenderer>().sharedMaterial = ballMat;

            var rb = ball.AddComponent<Rigidbody>();
            rb.mass = 0.3f;
            rb.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;   // évite de traverser à haute vitesse
            rb.interpolation = RigidbodyInterpolation.Interpolate;

            var grab = ball.AddComponent<XRGrabInteractable>();
            grab.throwOnDetach = true;
        }

        // Cible : zone de score invisible + disque vert au sol pour la voir
        var zone = new GameObject("ScoreZone", typeof(BoxCollider), typeof(ScoreZone));
        zone.transform.SetParent(root.transform, false);
        zone.transform.position = floor + forward * TargetDistance + Vector3.up * 0.75f;
        zone.transform.rotation = Quaternion.LookRotation(forward);
        var box = zone.GetComponent<BoxCollider>();
        box.size = new Vector3(TargetSize, 1.5f, TargetSize);
        box.isTrigger = true;

        var disc = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        disc.name = "TargetVisual";
        Object.DestroyImmediate(disc.GetComponent<Collider>());    // visuel seulement
        disc.transform.SetParent(root.transform, false);
        disc.transform.position = floor + forward * TargetDistance + Vector3.up * 0.02f;
        disc.transform.localScale = new Vector3(TargetSize, 0.02f, TargetSize);
        disc.GetComponent<MeshRenderer>().sharedMaterial = targetMat;

        Selection.activeGameObject = root;
        EditorSceneManager.MarkSceneDirty(root.scene);
        Debug.Log($"Throw Test Setup créé : {BallCount} balles sur la table, cible à {TargetDistance} m.");
    }

    static Material GetMaterial(string path, Color color)
    {
        if (!AssetDatabase.IsValidFolder("Assets/Materials")) AssetDatabase.CreateFolder("Assets", "Materials");
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
}
