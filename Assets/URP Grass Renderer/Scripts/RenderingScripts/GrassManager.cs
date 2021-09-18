using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEngine.Rendering;

[ExecuteInEditMode]
public class GrassManager : MonoBehaviour
{
    #region Singleton
    public static GrassManager instance;
    private void Awake() {
        instance = this;
    }
    #endregion

    public ComputeShader grassComputeShader;
    public Material grassMaterial;
    public GrassSettings grassSettings = new GrassSettings();

    public List<Vector3> vertexPoints = new List<Vector3>();
    public List<Vector3> vertexNormals = new List<Vector3>();

    [System.Runtime.InteropServices.StructLayout(System.Runtime.InteropServices.LayoutKind.Sequential)]
    struct FlattenData {
        public Vector3 position;
        public float radius;
        public float weight;
        public float maxLifetime;
        public float lifetime;
    };
    List<FlattenData> flattenData;
    private ComputeBuffer flattenBuffer;

    [System.Runtime.InteropServices.StructLayout(System.Runtime.InteropServices.LayoutKind.Sequential)]
    public struct SourceVertex {
        public Vector3 position;
        public Vector3 normal;
    }
    private ComputeBuffer sourceVertexBuffer;
    private ComputeBuffer drawBuffer;
    private ComputeBuffer argsBuffer;
    private ComputeShader instanceShader;
    private Material instanceMat;
    private int idGrassKernel;
    private int dispatchSize;
    private Bounds localBounds;

    private const int SOURCE_VERTEX_STRIDE = sizeof(float) * (3 + 3);
    private const int DRAW_STRIDE = sizeof(float) * (3 + (3 + 1) * 3);
    private const int INDIRECT_ARGS_STRIDE = sizeof(int) * 4;
    private const int FLATTEN_DATA_STRIDE = sizeof(float) * (3 + 1 + 1 + 1 + 1);

    private int[] argsBufferReset = new int[] { 0, 1, 0, 0 };

    private bool initialized;

    #region Initialization
    private void Start() {
        #if UNITY_EDITOR
        GrassManager[] grassManagers = FindObjectsOfType<GrassManager>();
        if (grassManagers.Length > 1) {
            Debug.LogWarning("There are multiple /'GrassManager/' types in your scene! Only one is needed...");
        }

        if (!Application.isPlaying) {
            grassSettings.TrySetDefault();
        }
        #endif
    }


    public void OnEnable() {
        if (HasIssues()) return;
        if (initialized) {
            OnDisable();
        }

        if (vertexPoints.Count <= 0 || vertexNormals.Count != vertexPoints.Count) return;
        SourceVertex[] vertices = new SourceVertex[vertexPoints.Count];
        for (int i = 0; i < vertices.Length; i++) {
            vertices[i] = new SourceVertex() {
                position = vertexPoints[i], 
                normal = vertexNormals[i], 
            };
        }

        initialized = true;

        int numSourceVertices = vertices.Length;
        int maxBladeSegments = grassSettings.maxSegments;
        int maxBladeTriangles = (maxBladeSegments - 1) * 2 + 1;
        
        instanceShader = Instantiate(grassComputeShader);
        instanceMat = Instantiate(grassMaterial);

        sourceVertexBuffer = new ComputeBuffer(vertices.Length, SOURCE_VERTEX_STRIDE, ComputeBufferType.Structured, ComputeBufferMode.Immutable);
        sourceVertexBuffer.SetData(vertices);
        drawBuffer = new ComputeBuffer(numSourceVertices * maxBladeTriangles, DRAW_STRIDE, ComputeBufferType.Append);
        drawBuffer.SetCounterValue(0);
        argsBuffer = new ComputeBuffer(1, INDIRECT_ARGS_STRIDE, ComputeBufferType.IndirectArguments);

        idGrassKernel = instanceShader.FindKernel("Main");

        // Buffer stuff
        instanceShader.SetBuffer(idGrassKernel, "_SourceVertices", sourceVertexBuffer);
        instanceShader.SetBuffer(idGrassKernel, "_DrawTriangles", drawBuffer);
        instanceShader.SetBuffer(idGrassKernel, "_IndirectArgsBuffer", argsBuffer);
        instanceShader.SetInt("_NumSourceVertices", numSourceVertices);

        instanceMat.SetBuffer("_DrawTriangles", drawBuffer);

        // Variable stuff
        instanceShader.SetInt("_MaxBladeSegments", maxBladeSegments);
        instanceShader.SetFloat("_MaxBendAngle", grassSettings.maxBendAngle);
        instanceShader.SetFloat("_BladeCurvature", grassSettings.bladeCurvature);
        instanceShader.SetFloat("_BladeHeight", grassSettings.bladeHeight);
        instanceShader.SetFloat("_BladeHeightVariance", grassSettings.bladeHeightVariance);
        instanceShader.SetFloat("_BladeWidth", grassSettings.bladeWidth);
        instanceShader.SetFloat("_BladeWidthVariance", grassSettings.bladeWidthVariance);
        instanceShader.SetTexture(idGrassKernel, "_WindNoiseTexture", grassSettings.windNoise);
        instanceShader.SetFloat("_WindTimeMult", grassSettings.windSpeed);
        instanceShader.SetFloat("_WindPosMult", grassSettings.windScale);
        instanceShader.SetFloat("_WindAmplitude", grassSettings.windAmount);
        instanceShader.SetFloat("_LodDistance", grassSettings.lodDistance);
        instanceShader.SetFloat("_ClipDistance", grassSettings.clipDistance);
        instanceShader.SetFloat("_ClipOffset", grassSettings.clipOffset);

        instanceShader.GetKernelThreadGroupSizes(idGrassKernel, out uint threadGroupSize, out _, out _);
        dispatchSize = Mathf.CeilToInt((float)numSourceVertices / threadGroupSize);

        UpdateBounds();
    }

    public void OnDisable() {
        if (initialized) {
            if (sourceVertexBuffer != null) sourceVertexBuffer.Release();
            if (drawBuffer != null) drawBuffer.Release();
            if (argsBuffer != null) argsBuffer.Release();
            if (flattenBuffer != null) flattenBuffer.Release();

            if (instanceShader != null) if (Application.isPlaying) Destroy(instanceShader); else DestroyImmediate(instanceShader);
            if (instanceMat != null) if (Application.isPlaying) Destroy(instanceMat); else DestroyImmediate(instanceMat);
        }
        initialized = false;
    }

    public void ForceRefresh() {
        OnDisable();
        OnEnable();
    }
    private void UpdateBounds() {
        Bounds b = new Bounds();
        for (int i = 0; i < vertexPoints.Count; i++) {
            b.Encapsulate(vertexPoints[i]);
        }
        localBounds = b;
        localBounds.Expand(Mathf.Max(grassSettings.bladeHeight + grassSettings.bladeHeightVariance, grassSettings.bladeWidth + grassSettings.bladeWidthVariance));
    }
    #endregion

    public void LateUpdate() {
        if (HasIssues() || vertexPoints.Count <= 0 || vertexNormals.Count != vertexPoints.Count) return;

        if (!Application.isPlaying) {
            ForceRefresh();
        }

        drawBuffer.SetCounterValue(0);
        argsBuffer.SetData(argsBufferReset);

        Bounds bounds = TransformBounds(localBounds);

        instanceShader.SetVector("_Time", new Vector4(0, Time.timeSinceLevelLoad, 0, 0));

        if (Application.isPlaying) UpdateCameraInfo(grassSettings.overrideLODCam == null ? Camera.main : grassSettings.overrideLODCam, true);
        else UpdateCameraInfo(null, false);

        UpdateFlattenData();

        instanceShader.Dispatch(idGrassKernel, dispatchSize, 1, 1);

        flattenBuffer.Release();

        Graphics.DrawProceduralIndirect(instanceMat, bounds, MeshTopology.Triangles, argsBuffer, 0, null, null, ShadowCastingMode.Off, true, gameObject.layer);
    }
    public void UpdateCameraInfo(Camera cam, bool useLod) {
        Vector4 camPosData = cam == null ? Vector3.zero : cam.transform.position;
        camPosData.w = useLod ? 0 : 1;
        instanceShader.SetVector("_CamPos", camPosData);
    }

    #region Flatten
    public static void RegisterFlatten(Transform t, float r, float w, float lt = 1) {
        if (instance == null) return;
        if (instance.flattenData == null) return;

        if (instance.flattenData.Count >= instance.grassSettings.maxFlattenCalculations) {
            instance.flattenData.RemoveAt(0);
        }

        instance.flattenData.Add(new FlattenData() {
            position = t.position, weight = w, radius = r, lifetime = lt,  maxLifetime = lt, 
        });
    }
    public void UpdateFlattenData() {
        if (flattenData == null || flattenData.Count <= 0) ClearFlattenData();

        float newLifetime = 0;
        for (int i = flattenData.Count - 1; i > 0; i--) {
            newLifetime = (flattenData[i].lifetime - Time.deltaTime);
            if (newLifetime > 0) {
                flattenData[i] = new FlattenData() {
                    position = flattenData[i].position, weight = flattenData[i].weight, radius = flattenData[i].radius, lifetime = newLifetime, maxLifetime = flattenData[i].maxLifetime
                };
            }
            else {
                flattenData.RemoveAt(i);
            }
        }

        flattenBuffer = new ComputeBuffer(flattenData.Count, FLATTEN_DATA_STRIDE,ComputeBufferType.Structured, ComputeBufferMode.Immutable);
        flattenBuffer.SetData(flattenData);

        instanceShader.SetInt("_NumFlattenData", flattenData.Count);
        instanceShader.SetBuffer(idGrassKernel, "_FlattenData", flattenBuffer);
    }
    public void ClearFlattenData() {
        flattenData = new List<FlattenData>();
        flattenData.Add(new FlattenData() {
            position = Vector3.zero, weight = 0, radius = 0, lifetime = -1, maxLifetime = -1
        });
    }
    #endregion

    #region Helper Methods
    public Bounds TransformBounds(Bounds boundsOS) {
        var center = transform.TransformPoint(boundsOS.center);

        var extents = boundsOS.extents;
        var axisX = transform.TransformVector(extents.x, 0, 0);
        var axisY = transform.TransformVector(0, extents.y, 0);
        var axisZ = transform.TransformVector(0, 0, extents.z);

        return new Bounds { center = center, extents = extents };
    }

    public bool HasIssues() => (grassComputeShader == null || grassMaterial == null || grassSettings.windNoise == null);

    public List<Vector3> GetPoints() => vertexPoints;
    public List<Vector3> GetNormals() => vertexNormals;

    public void AddBlade(SourceVertex vertex) {
        vertexPoints.Add(vertex.position);
        vertexNormals.Add(vertex.normal);
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