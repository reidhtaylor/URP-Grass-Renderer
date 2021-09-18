using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

public class GrassOnMeshWizard : ScriptableWizard
{
    [MenuItem("STS URP Grass/GrassOnMesh")]
    public static void CreateWizard() {
        ScriptableWizard.DisplayWizard<GrassOnMeshWizard>("Grass On Mesh", "Generate");
    }

    public Mesh mesh;
    [Space]
    public Vector3 worldPosition;
    public Vector3 rotationOffset;

    void OnWizardCreate()
    {
        GrassManager grassManager = GrassManager.instance;
        Vector3[] vertices = mesh.vertices;
        Vector3[] normals = mesh.normals;

        Vector3 middlePoint = Vector3.zero;
        for (int i = 0; i < vertices.Length; i++) middlePoint += vertices[i];
        middlePoint /= vertices.Length;

        List<GrassManager.SourceVertex> bladeVertices = new List<GrassManager.SourceVertex>();
        for (int i = 0; i < vertices.Length; i++) {
            vertices[i] = RotatePointAroundPivot(vertices[i], middlePoint, rotationOffset);
            vertices[i] += worldPosition;
            normals[i] = Quaternion.Euler(rotationOffset) * normals[i];
            bladeVertices.Add(new GrassManager.SourceVertex() {
                position = vertices[i],
                normal = normals[i], 
            });
        }

        Undo.RegisterCompleteObjectUndo(grassManager, "Generate mesh grass");
        grassManager.AddBlades(bladeVertices);
    }

    private Vector3 RotatePointAroundPivot(Vector3 point, Vector3 pivot, Vector3 rot) {
        Vector3 dir = point - pivot;;
        dir = Quaternion.Euler(rot) * dir;
        point = dir + pivot;
        return point;
    }
}
