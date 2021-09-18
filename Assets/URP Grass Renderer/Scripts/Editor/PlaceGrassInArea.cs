using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

public class PlaceGrassInArea : ScriptableWizard
{
    [MenuItem("STS URP Grass/Place grass in area")]
    public static void CreateWizard() {
        ScriptableWizard.DisplayWizard<PlaceGrassInArea>("Place grass in area", "Generate");
    }

    public Vector3 areaCenter;
    public Vector3 areaSize = new Vector3(150, 25, 150);
    [Space]
    [Range(0, 1)]
    public float density = 0.5f;
    [Space]
    [Range(0, 1)]
    public float proceduralNormal = 0.5f;

    void OnWizardCreate()
    {
        GrassManager grassManager = GrassManager.instance;
        List<GrassManager.SourceVertex> bladeVertices = new List<GrassManager.SourceVertex>();

        int bladeCount = (int)((areaSize.x * areaSize.z) * 30 * density);
        Ray ray = new Ray();
        RaycastHit hit;
        for (int i = 0; i < bladeCount; i++) {
            ray.direction = Vector3.down;

            Vector3 origin = areaCenter;
            origin.y += areaSize.y;
            origin.x += Random.Range(-1f, 1f) * (areaSize.x / 2f);
            origin.z += Random.Range(-1f, 1f) * (areaSize.z / 2f);
            ray.origin = origin;

            if (Physics.Raycast(ray, out hit, areaSize.y)) {
                bladeVertices.Add(new GrassManager.SourceVertex() {
                    position = hit.point, normal = Vector3.Lerp(Vector3.up, hit.normal, proceduralNormal),
                });
            }
            // Debug.DrawRay(origin, ray.direction * areaSize.y, Color.red, 0.1f);
        }

        Undo.RegisterCompleteObjectUndo(grassManager, "Generate mesh grass");
        grassManager.AddBlades(bladeVertices);
    }
}
