using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Trampler : MonoBehaviour
{
    [Header("Trample Data")]
    public Transform overrideTransform;
    public float radius = 2.3f;
    [Range(0, 1)]
    public float weight = 1;
    [Range(0, 1)]
    public float impression = 0.4f;

    public virtual void Update() {
        GrassManager.RegisterFlatten(overrideTransform == null ? transform : overrideTransform, radius, weight, impression * 10);    
    }
}
