using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

public static class FitHubFloorAndWalls
{
    const int WallCount = 32;           // nombre de segments du cercle
    const float WallHeight = 4f;
    const float WallThickness = 0.4f;
    const float WallMargin = 0.5f;      // distance entre le mur et la paroi de la sphère

    [MenuItem("Tools/Fit Hub Floor And Walls")]
    static void Apply()
    {
        var sphere = GameObject.Find("HubSphere");
        var plane = GameObject.Find("Plane");
        if (sphere == null || plane == null)
        {
            EditorUtility.DisplayDialog("Hub", "Il faut un objet 'HubSphere' et un objet 'Plane' dans la scène.", "OK");
            return;
        }

        float diameter = sphere.transform.localScale.x;   // la sphère native fait 1 m de diamètre à l'échelle 1
        float radius = diameter / 2f;
        Vector3 center = sphere.transform.position;
        float floorY = plane.transform.position.y;

        // Le Plane natif fait 10 x 10 m : échelle = diamètre / 10
        Undo.RecordObject(plane.transform, "Fit Hub Floor");
        plane.transform.localScale = new Vector3(diameter / 10f, 1f, diameter / 10f);
        plane.transform.position = new Vector3(center.x, floorY, center.z);

        // Murs invisibles : un anneau de colliders (pas de renderer)
        var old = GameObject.Find("HubWalls");
        if (old != null) Undo.DestroyObjectImmediate(old);

        var root = new GameObject("HubWalls");
        Undo.RegisterCreatedObjectUndo(root, "Create Hub Walls");
        root.transform.position = new Vector3(center.x, floorY, center.z);

        float wallRadius = radius - WallMargin;          // face intérieure du mur
        float chord = 2f * wallRadius * Mathf.Sin(Mathf.PI / WallCount);
        float width = chord * 1.15f;                      // léger recouvrement entre segments

        for (int i = 0; i < WallCount; i++)
        {
            float angle = i * Mathf.PI * 2f / WallCount;
            var dir = new Vector3(Mathf.Cos(angle), 0f, Mathf.Sin(angle));

            var seg = new GameObject($"Wall_{i:00}");
            seg.transform.SetParent(root.transform, false);
            seg.transform.localPosition = dir * (wallRadius + WallThickness / 2f) + Vector3.up * (WallHeight / 2f);
            seg.transform.localRotation = Quaternion.LookRotation(-dir, Vector3.up);   // face vers le centre

            var box = seg.AddComponent<BoxCollider>();
            box.size = new Vector3(width, WallHeight, WallThickness);
        }

        EditorSceneManager.MarkSceneDirty(root.scene);
        Selection.activeGameObject = root;
        Debug.Log($"Plane réglé à {diameter} m, {WallCount} murs invisibles à {wallRadius} m du centre.");
    }
}
