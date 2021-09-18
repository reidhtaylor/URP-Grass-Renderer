using System.Collections;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEditor;

#if UNITY_EDITOR
[CustomEditor(typeof(CrossGrassManager))]
public class CrossGrassManagerEditor : Editor
{
    private int toolIndex = 0;

    private string[] tabText = new string[3] { "Settings", "Paint", "Erase" };

    // Settings
    private bool settingsOpen;
    private bool windOpen;

    // Brush
    private Vector2 brushRadiusRange = new Vector2(0.05f, 500f);
    private float brushRadius = 5;
    private Vector2 brushDensityRange = new Vector2(0.05f, 1f);
    private float brushDensity = 0.5f;
    private Vector2 brushSmoothnessRange = new Vector2(0f, 1f);
    private float brushSmoothness = 0.5f;

    private Color paintColor = new Color(0, 0, 1f, 1f);
    private Color eraseColor = new Color(0.7f, 0, 0, 1f);

    // Other
    private GUIStyle otherButtonTextStyle;

    private CrossGrassManager system;

    private void OnEnable() {
        system = target as CrossGrassManager;

        otherButtonTextStyle = new GUIStyle();
        otherButtonTextStyle.fontSize = 5;
        otherButtonTextStyle.normal.textColor = Color.white;
        otherButtonTextStyle.alignment = TextAnchor.MiddleCenter;
        otherButtonTextStyle.padding = new RectOffset(0, 0, 0, 1);

        SceneView.duringSceneGui -= OnSceneGUI;
        SceneView.duringSceneGui += OnSceneGUI;
    }
    private void OnDisable() {
        SceneView.duringSceneGui -= OnSceneGUI;
    }

    public override void OnInspectorGUI() {
        if (system.HasUserIssues()) {
            EditorGUILayout.HelpBox("There are compile issues. Make sure all fields are set.", MessageType.Error);
        }
        if (Application.isPlaying) GUI.enabled = false;

        float otherButtonWidth = 20;
        Rect toolRect = EditorGUILayout.GetControlRect(false, 20);
        toolRect.width -= (otherButtonWidth + 2 * 2);

        toolIndex = GUI.Toolbar(toolRect, toolIndex, tabText);
        switch (toolIndex) {
            case 0: DrawSettingsGUI(); break;
            case 1: DrawBrushGUI(); break;
            case 2: DrawEraseGUI(); break;
        }

        toolRect.x += toolRect.width + 4;
        toolRect.width = otherButtonWidth;
        if (GUI.Button(toolRect, "")) {
            GenericMenu menu = new GenericMenu();
            menu.AddItem(new GUIContent("Clear all grass"), false, OtherCallback, 0);
            menu.ShowAsContext();
        }
        GUI.Label(toolRect, "•••", otherButtonTextStyle);

        GUI.enabled = true;

        if (GUI.changed) {
            system.OnEnable();
            EditorUtility.SetDirty(system);
        }
    }

    private void DrawSettingsGUI() {
        EditorGUILayout.Space(10);
        system.grassComputeShader = (ComputeShader)EditorGUILayout.ObjectField("Grass Compute Shader", system.grassComputeShader, typeof(ComputeShader), false);
        system.grassMaterial = (Material)EditorGUILayout.ObjectField("Grass Material", system.grassMaterial, typeof(Material), false);

        EditorGUILayout.Space(10);

        if (GUILayout.Button("Settings" + (settingsOpen ? " -" : " +"))) {
            settingsOpen = !settingsOpen;
        }
        if (settingsOpen) {
            system.grassWidth = EditorGUILayout.Slider("Grass Width", system.grassWidth, 0.1f, 10);
            system.grassHeight = EditorGUILayout.Slider("Grass Height", system.grassHeight, 0.1f, 10);
            system.grassCrossCount = EditorGUILayout.IntSlider("Grass Cross Count", system.grassCrossCount, 1, 5);
            EditorGUILayout.Space(5);
        }
        else EditorGUILayout.Space(1.5f);

        if (GUILayout.Button("Wind" + (windOpen ? " -" : " +"))) {
            windOpen = !windOpen;
        }
        if (windOpen) {
            system.windNoise = (Texture2D)EditorGUILayout.ObjectField("Wind Noise", system.windNoise, typeof(Texture2D), false);
            system.windPower = EditorGUILayout.FloatField("Wind Power", system.windPower);
            system.windScale = EditorGUILayout.FloatField("Wind Scale", system.windScale);
            system.windSpeed = EditorGUILayout.FloatField("Wind Speed", system.windSpeed);
            EditorGUILayout.Space(5);
        }
    }
    private void DrawBrushGUI() {
        if (system.HasUserIssues()) GUI.enabled = false;
            EditorGUILayout.Space(10);
        brushRadius = EditorGUILayout.Slider("Brush Radius", brushRadius, brushRadiusRange.x, brushRadiusRange.y);
        brushDensity = EditorGUILayout.Slider("Brush Density", brushDensity, brushDensityRange.x, brushDensityRange.y);
        brushSmoothness = EditorGUILayout.Slider("Brush Smoothness", brushSmoothness, brushSmoothnessRange.x, brushSmoothnessRange.y);
        EditorGUILayout.Space(10);
        GUI.enabled = false;
        EditorGUILayout.IntField("Detail Count", system.vertexPoints.Count);
        GUI.enabled = true;
    }
    private void DrawEraseGUI() {
        if (system.HasUserIssues()) GUI.enabled = false;
        EditorGUILayout.Space(10);
        brushRadius = EditorGUILayout.Slider("Brush Radius", brushRadius, brushRadiusRange.x, brushRadiusRange.y);
        brushDensity = EditorGUILayout.Slider("Brush Density", brushDensity, brushDensityRange.x, brushDensityRange.y);
        brushSmoothness = EditorGUILayout.Slider("Brush Smoothness", brushSmoothness, brushSmoothnessRange.x, brushSmoothnessRange.y);
        EditorGUILayout.Space(10);
        GUI.enabled = false;
        EditorGUILayout.IntField("Detail Count", system.vertexPoints.Count);
        GUI.enabled = true;
    }

    private void OtherCallback(object o) {
        int i = (int)o;
        CrossGrassManager.instance = system;
        switch (i) {
            case 0: if (EditorUtility.DisplayDialog("Clear", "Remove all grass!", "Confirm", "Cancel")) {
                system.ResetPoints();
            } break;
        }
    }

    public void OnSceneGUI(SceneView s) {
        switch (toolIndex) {
            case 0: DrawSettingsScene(); break;
            case 1: DrawBrushScene(); break;
            case 2: DrawEraseScene(); break;
        }
    }

    private void DrawSettingsScene() {
    }
    private void DrawBrushScene() {
        if (Application.isPlaying || system.HasUserIssues()) return;
        
        Event e = Event.current;
        HandleUtility.AddDefaultControl(GUIUtility.GetControlID(FocusType.Passive));

        Ray ray = HandleUtility.GUIPointToWorldRay(e.mousePosition);
        RaycastHit hit;
        if (Physics.Raycast(ray, out hit)) {
            paintColor.a = brushDensity * 0.8f;
            Handles.color = paintColor;
            Handles.DrawSolidDisc(hit.point, hit.normal, brushRadius);

            if (e.button == 0) {
                if (e.type == EventType.MouseDown || e.type == EventType.MouseDrag) {
                    List<CrossGrassManager.SourceVertex> toAdd = new List<CrossGrassManager.SourceVertex>();
                    Ray placementRay = new Ray();
                    RaycastHit placementHit;
                    int addCount = (int)Mathf.Max(1, brushRadius * (10 * brushDensity));

                    Vector3 origin = Vector3.zero;
                    float r = 0;
                    for (int i = 0; i < addCount; i++) {
                        origin = hit.point;
                        r = Random.Range(0, 1f);

                        if (Random.value < brushSmoothness * r) continue;

                        origin += (Quaternion.AngleAxis(Random.Range(0, 360), hit.normal) * Vector3.forward) * (brushRadius * r);
                        origin += hit.normal * (brushRadius / 2f) * (1 - r);

                        placementRay.origin = origin;
                        placementRay.direction = -hit.normal;

                        if (Physics.Raycast(placementRay, out placementHit, brushRadius)) {
                            toAdd.Add(new CrossGrassManager.SourceVertex() {
                                position = placementHit.point, 
                                normal = placementHit.normal 
                            });
                        }
                        // Debug.DrawRay(origin, -hit.normal * brushRadius, Color.red, 0.25f);
                    }
                    Undo.RegisterCompleteObjectUndo(system, "Add grass");
                    system.AddBlades(toAdd);
                }
                else if (e.shift && e.type == EventType.ScrollWheel) {
                    brushRadius = Mathf.Clamp(brushRadius + (e.delta.y * 3f), brushRadiusRange.x, brushRadiusRange.y);
                    e.Use();
                }
                else if (e.alt && e.type == EventType.ScrollWheel) {
                    brushDensity = Mathf.Clamp(brushDensity + (e.delta.y / 5f), brushDensityRange.x, brushDensityRange.y);
                    e.Use();
                }
            }
        }
    }
    private void DrawEraseScene() {
        if (Application.isPlaying || system.HasUserIssues()) return;
        
        Event e = Event.current;
        HandleUtility.AddDefaultControl(GUIUtility.GetControlID(FocusType.Passive));

        Ray ray = HandleUtility.GUIPointToWorldRay(e.mousePosition);
        RaycastHit hit;
        if (Physics.Raycast(ray, out hit)) {
            eraseColor.a = brushDensity * 0.8f;
            Handles.color = eraseColor;
            Handles.DrawSolidDisc(hit.point, hit.normal, brushRadius);

            if (e.button == 0) {
                if (e.type == EventType.MouseDown || e.type == EventType.MouseDrag) {
                    List<int> closeEnough = new List<int>();
                    List<Vector3> points = system.vertexPoints;
                    float d;
                    for (int i = 0; i < points.Count; i++) {
                        d = Vector3.Distance(points[i], hit.point);
                        if (d < brushRadius && Random.value < brushDensity && Random.value > (d / brushRadius) * brushSmoothness) {
                            closeEnough.Add(i);
                        }
                    }
                    if (closeEnough.Count >= 0) {
                        Undo.RegisterCompleteObjectUndo(system, "remove grass");
                        system.RemoveBlades(closeEnough);
                    }
                }
                else if (e.shift && e.type == EventType.ScrollWheel) {
                    brushRadius = Mathf.Clamp(brushRadius + (e.delta.y * 3f), brushRadiusRange.x, brushRadiusRange.y);
                    e.Use();
                }
                else if (e.alt && e.type == EventType.ScrollWheel) {
                    brushDensity = Mathf.Clamp(brushDensity + (e.delta.y / 5f), brushDensityRange.x, brushDensityRange.y);
                    e.Use();
                }
            }
        }
    }
}
#endif