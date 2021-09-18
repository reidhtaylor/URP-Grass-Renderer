using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEditor;

public struct Vector3uint { public uint x; public uint y; public uint z; public Vector3uint(uint x, uint y, uint z) { this.x = x; this.y = y; this.z = z; }}

[ExecuteInEditMode]
public class CrossGrassManager : MonoBehaviour
{
    public static CrossGrassManager instance;
    private void Awake() {
        instance = this;
    }

    public ComputeShader grassComputeShader;
    public Material grassMaterial;

    public float grassWidth = 2;
    public float grassHeight = 0.8f;
    [Range(1, 5)]
    public int grassCrossCount;

    public Texture2D windNoise;
    public float windPower = 0.5f;
    public float windScale = 0.01f;
    public float windSpeed = 0.05f;
    
    [HideInInspector]
    public List<Vector3> vertexPoints;
    public List<Vector3> vertexNormals;

    [HideInInspector]
    public ComputeShader instancedComputeShader;
    [HideInInspector]
    public Material instancedMaterial;

    [System.Runtime.InteropServices.StructLayout(System.Runtime.InteropServices.LayoutKind.Sequential)]
    public struct SourceVertex {
        public Vector3 position;
        public Vector3 normal;
    }
    private const int SOURCE_VERTEX_STRIDE = sizeof(float) * (3 * 2);
    private ComputeBuffer sourceVerticesBuffer;

    private const int DRAW_STRIDE = sizeof(float) * (1 + (4 * 3));
    private ComputeBuffer drawBuffer;

    private ComputeBuffer argsBuffer;
    private int[] argsData = new int[4] { 0, 1, 0, 0 };

    private int kernelIndex;
    private Vector3uint threadGroupSize;
    private Vector3Int dispatchSize;

    private Bounds bounds;
        
    private bool initialized;

    public void OnEnable() {
        if (HasUserIssues() || vertexPoints.Count <= 0) return;

        if (initialized) OnDisable();
        initialized = true;

        instancedComputeShader = Instantiate(grassComputeShader);
        kernelIndex = instancedComputeShader.FindKernel("CalculateProceduralGeometry");
        instancedComputeShader.GetKernelThreadGroupSizes(kernelIndex, out threadGroupSize.x, out threadGroupSize.y, out threadGroupSize.z);
        instancedMaterial = Instantiate(grassMaterial);

        int numVertices = vertexPoints.Count;
        int numTrianglesPerCross = 4;

        SourceVertex[] vertices = new SourceVertex[numVertices];
        for (int i = 0; i < vertices.Length; i++) vertices[i] = new SourceVertex() { position = vertexPoints[i], normal = Vector3.up };
        sourceVerticesBuffer = new ComputeBuffer(vertices.Length, SOURCE_VERTEX_STRIDE, ComputeBufferType.Structured, ComputeBufferMode.Immutable);
        sourceVerticesBuffer.SetData(vertices);
        instancedComputeShader.SetBuffer(kernelIndex, "_SourceVertices", sourceVerticesBuffer);
        instancedComputeShader.SetInt("_NumSourceVertices", numVertices);

        drawBuffer = new ComputeBuffer(numVertices * numTrianglesPerCross, DRAW_STRIDE, ComputeBufferType.Append);
        drawBuffer.SetCounterValue(0);
        instancedComputeShader.SetBuffer(kernelIndex, "_DrawTriangles", drawBuffer);
        instancedMaterial.SetBuffer("_DrawTriangles", drawBuffer);

        argsBuffer = new ComputeBuffer(1, argsData.Length * sizeof(int), ComputeBufferType.IndirectArguments);
        argsBuffer.SetData(argsData);
        instancedComputeShader.SetBuffer(kernelIndex, "_IndirectArgs", argsBuffer);

        instancedComputeShader.SetFloat("_Width", grassWidth);
        instancedComputeShader.SetFloat("_Height", grassHeight);
        instancedComputeShader.SetInt("_CrossCount", grassCrossCount);
        instancedComputeShader.SetTexture(kernelIndex, "_WindNoiseTexture", windNoise);
        instancedComputeShader.SetFloat("_WindPower", windPower);
        instancedComputeShader.SetFloat("_WindScale", windScale);
        instancedComputeShader.SetFloat("_WindSpeed", windSpeed);
        
        dispatchSize.x = Mathf.CeilToInt((float)numVertices / threadGroupSize.x);
        dispatchSize.y = dispatchSize.z = 1;

        // TODO : Set correct bounds
        bounds = new Bounds();
        for (int i = 0; i < vertexPoints.Count; i++) {
            bounds.Encapsulate(vertexPoints[i]);
        }
        bounds.Expand(Mathf.Max(grassHeight, grassWidth));
    }
    private void OnDisable() {
        if (initialized) {
            if (instancedComputeShader != null) if (Application.isPlaying) Destroy(instancedComputeShader); else DestroyImmediate(instancedComputeShader);
            if (instancedMaterial != null) if (Application.isPlaying) Destroy(instancedMaterial); else DestroyImmediate(instancedMaterial);
            if (argsBuffer != null) argsBuffer.Release();
            if (sourceVerticesBuffer != null) sourceVerticesBuffer.Release();
            if (drawBuffer != null) drawBuffer.Release();
        }
        initialized = false;
    }

    private void LateUpdate() {
        if (HasUserIssues() || vertexPoints.Count <= 0) return;
        if (HasInternalIssues()) {
            OnEnable();
            return;
        }

        if (!Application.isPlaying) {
            OnEnable();
        }

        drawBuffer.SetCounterValue(0);
        argsBuffer.SetData(argsData);

        instancedComputeShader.Dispatch(kernelIndex, dispatchSize.x, dispatchSize.y, dispatchSize.z);

        Bounds b = TransformBounds(bounds);

        Graphics.DrawProceduralIndirect(instancedMaterial, b, MeshTopology.Triangles, argsBuffer, 0, null, null, ShadowCastingMode.Off, true, gameObject.layer);
    }

    #region Helpers
    public bool HasUserIssues() => (grassComputeShader == null || grassMaterial == null || windNoise == null);
    public bool HasInternalIssues() => (argsBuffer == null || sourceVerticesBuffer == null || drawBuffer == null);

    public Bounds TransformBounds(Bounds boundsOS) {
        var center = transform.TransformPoint(boundsOS.center);

        var extents = boundsOS.extents;
        var axisX = transform.TransformVector(extents.x, 0, 0);
        var axisY = transform.TransformVector(0, extents.y, 0);
        var axisZ = transform.TransformVector(0, 0, extents.z);

        return new Bounds { center = center, extents = extents };
    }

    public void AddBlades(List<SourceVertex> vertices) {
        for (int i = 0; i < vertices.Count; i++) {
            vertexPoints.Add(vertices[i].position);
            vertexNormals.Add(vertices[i].normal);
        }
    }
    public void RemoveBlades(List<int> indices) {
        List<int> sortedIndices = new List<int>();
        // Sorted from lowest to highest indices
        for (int i = 0; i < indices.Count; i++) {
            if (sortedIndices.Count <= 0 || indices[i] < sortedIndices[0]) {
                sortedIndices.Insert(0, indices[i]);
            }
            else {
                sortedIndices.Add(indices[i]);
            }
        }

        for (int i = sortedIndices.Count - 1; i > - 1; i--) {
            if (sortedIndices[i] < 0 || sortedIndices[i] >= vertexPoints.Count) continue;
            vertexPoints.RemoveAt(sortedIndices[i]);
            vertexNormals.RemoveAt(sortedIndices[i]);
        }
    }

    public void ResetPoints() {
        vertexPoints = new List<Vector3>();
        vertexNormals = new List<Vector3>();
    }
    #endregion
}