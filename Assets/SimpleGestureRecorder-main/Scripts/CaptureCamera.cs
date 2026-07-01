/*
 * GestureRecorderController.cs
 * 
 * Description:
 * This script controls the screencapture camera. 
 * Requires a Camera component.
 * 
 * Copyright (c) 2024 Nico Mahler
 * 
 * Permission is hereby granted, free of charge, to any person obtaining a copy
 * of this software and associated documentation files (the "Software"), to deal
 * in the Software without restriction, including without limitation the rights
 * to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
 * copies of the Software, and to permit persons to whom the Software is
 * furnished to do so, subject to the following conditions:
 * 
 * The above copyright notice and this permission notice shall be included in all
 * copies or substantial portions of the Software.
 * 
 * THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
 * IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
 * FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
 * AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
 * LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
 * OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
 * SOFTWARE.
 */

using System;
using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(Camera))]
public class CaptureCamera : MonoBehaviour
{
    public Image gestureImage;

    [SerializeField]
    GameObject m_MainCamera;

    const int DISPLAY_1 = 0;
    const int DISPLAY_2 = 1;

    Camera m_Cam;
    Texture2D m_Screenshot;

    event Action OnPostRenderEvent;

    public void TakeScreenshot()
    {
        m_Cam.targetDisplay = DISPLAY_1;
        OnPostRenderEvent += CreateTexture;
    }
    public void SaveTexture(string dirPath)
    {
#if UNITY_EDITOR
        byte[] bytes = m_Screenshot.EncodeToPNG();
        System.IO.File.WriteAllBytes(dirPath, bytes);
#endif
    }

    void Start()
    {
#if !UNITY_EDITOR
            Destroy(this);
#endif
        m_MainCamera = GameObject.Find("Main Camera");
        if (!m_MainCamera)
            m_MainCamera = GameObject.FindGameObjectWithTag("MainCamera");

        m_Cam = GetComponent<Camera>();
    }

    void Update()
    {
        if(m_MainCamera != null)
        {
            transform.position = m_MainCamera.transform.position;
            transform.rotation = m_MainCamera.transform.rotation;
            transform.localScale = m_MainCamera.transform.localScale;
        }
    }

    void OnPostRender()
    {
        OnPostRenderEvent?.Invoke();
    }

    void CreateTexture()
    {
        int screenShotWidth = Screen.width;
        int screenShotHeight = Screen.height;

        m_Screenshot = new Texture2D(screenShotWidth, screenShotHeight, TextureFormat.RGB24, false);
        var rect = new Rect(0, 0, screenShotWidth, screenShotHeight);
        m_Screenshot.ReadPixels(rect, 0, 0);
        m_Screenshot.Apply();

        gestureImage.sprite = Sprite.Create(m_Screenshot, rect, new Vector2(0.5f, 0.5f));
        gestureImage.enabled = true;

        m_Cam.targetDisplay = DISPLAY_2;

        OnPostRenderEvent -= CreateTexture;
    }
}