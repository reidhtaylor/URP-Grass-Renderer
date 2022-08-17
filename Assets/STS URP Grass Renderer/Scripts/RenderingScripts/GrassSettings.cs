using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class GrassSettings
{
    public void TrySetDefault() {
        windNoise = Resources.Load<Texture2D>("DefaultNoise");
    }
    
    [Header("Form")]
    public int maxSegments = 8;
    public float maxBendAngle = 0.1f;
    public float bladeCurvature = 1;
    public float bladeHeight = 2;
    public float bladeHeightVariance = 0.3f;
    public float bladeWidth = 0.4f;
    public float bladeWidthVariance = 0.1f;

    [Header("Wind")]
    public Texture2D windNoise;
    public float windScale = 0.01f;
    public float windSpeed = 0.05f;
    public float windAmount = 0.5f;

    [Header("LOD")]
    public Camera overrideLODCam;
    public float lodDistance = 15;
    public float clipDistance = 100;
    public float clipOffset = 3;

    [Header("Flatten")]
    public int maxFlattenCalculations = 25;
}
