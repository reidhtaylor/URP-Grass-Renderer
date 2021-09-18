using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ScreenShotter : MonoBehaviour
{
    void Update() {
        if (Input.GetKeyDown(KeyCode.Space)) {
            ScreenCapture.CaptureScreenshot("Picture_" + Random.Range(0, 1000).ToString(), 3);
        }
    }
}
