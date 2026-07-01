/*
 * GestureRecorder.cs
 * 
 * Description:
 * This script records XRHandShapes to given resolution. 
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

using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR.Hands.Gestures;
using UnityEngine.XR.Hands;
using System.IO;
using System;
using System.Collections;

public class GestureRecorder : MonoBehaviour
{
    [Tooltip("The Camera which creates the screenshot for reference. Differs from main camera.")]
    [SerializeField]
    CaptureCamera m_ScreenCaptureCamera;

    [Tooltip("The duration in seconds how long poses are recorded.")]
    [SerializeField]
    float m_RecordingDuration = 3f;

    [Tooltip("The amount of pose snapshots created per second.")]
    [SerializeField]
    int m_StepsPerSecond = 30;

    [Tooltip("The event fired when recording ended.")]
    public event Action OnRecordEnded;

    public float recordingDuration { get { return m_RecordingDuration; } private set { } }

    string CURRENT_DIR_NAME;
    string CURRENT_FILE_NAME;

    const string WORKING_PATH = "Assets/SimpleGestureRecorder/Snapshots/";
    const string DIR_NAME = "Gesture ";
    const string FILE_NAME = "Gesture ";
    const string ASSET_EXTENSION = ".asset";
    const string IMAGE_EXTENSION = " Screenshot.png";
    const string LEFT = " LH Shape";
    const string RIGHT = " RH Shape";

    bool m_RecordingLeft, m_RecordingRight;
    HandData m_RightHand, m_LeftHand;
    static List<XRHandSubsystem> s_SubsystemsReuse = new List<XRHandSubsystem>();
    XRHandShape m_LeftHandShapeInstance, m_RightHandShapeInstance;

    public static XRHandSubsystem TryGetSubsystem()
    {
        SubsystemManager.GetSubsystems(s_SubsystemsReuse);
        return s_SubsystemsReuse.Count > 0 ? s_SubsystemsReuse[0] : null;
    }

    void Start()
    {
#if !UNITY_EDITOR
            Destroy(this);
#endif
        if(m_ScreenCaptureCamera == null)
            m_ScreenCaptureCamera = GameObject.Find("CaptureCamera").GetComponent<CaptureCamera>();

        m_LeftHand = new HandData((uint)(recordingDuration * m_StepsPerSecond));
        m_RightHand = new HandData((uint)(recordingDuration * m_StepsPerSecond));
    }

    public void StartRecording(bool recordingLeft, bool recordingRight)
    {
        m_RecordingLeft = recordingLeft;
        m_RecordingRight = recordingRight;

        StartCoroutine(Recorder());
    }
    public bool DoesHandShapeExist(Handedness handedness)
    {
        if (handedness == Handedness.Left && m_LeftHandShapeInstance != null)
            return true;

        if (handedness == Handedness.Right && m_RightHandShapeInstance != null)
            return true;

        return false;
    }
    public XRHandShape GetHandShape(Handedness handedness)
    {
        if (handedness == Handedness.Left && m_LeftHandShapeInstance != null)
        {
            return m_LeftHandShapeInstance;
        }

        if (handedness == Handedness.Right && m_RightHandShapeInstance != null)
        {
            return m_RightHandShapeInstance;
        }

        return null;
    }

    IEnumerator Recorder()
    {
        float stepDuration = 1f / m_StepsPerSecond;
        float timePassed = 0f;
        float halfTime = recordingDuration * 0.5f;
        bool captured = false;

        while(timePassed <= recordingDuration)
        {
            Record();
            yield return new WaitForSeconds(stepDuration);
            timePassed += stepDuration;

            if(!captured && timePassed >= halfTime)
            {
                CreateScreenshot();
                captured = true;
            }
        }

        EndRecording();
    }
    void Record()
    {
        var handSubsystem = TryGetSubsystem();
        if (handSubsystem == null)
            return;

        if (m_RecordingLeft)
            RecordHandData(handSubsystem.leftHand);
        if (m_RecordingRight)
            RecordHandData(handSubsystem.rightHand);
    }
    void RecordHandData(XRHand hand)
    {
        var handedness = hand.handedness;

        for (var fingerIndex = (int)XRHandFingerID.Thumb; fingerIndex <= (int)XRHandFingerID.Little; ++fingerIndex)
        {
            var fingerID = (XRHandFingerID)fingerIndex;
            var fingerShape = hand.CalculateFingerShape(fingerID, XRFingerShapeTypes.All);

            if (fingerShape.TryGetFullCurl(out var fullCurl))
                SetShapeData(handedness, fingerID, XRFingerShapeType.FullCurl, fullCurl);

            if (fingerShape.TryGetBaseCurl(out var baseCurl))
                SetShapeData(handedness, fingerID, XRFingerShapeType.BaseCurl, baseCurl);

            if (fingerShape.TryGetTipCurl(out var tipCurl))
                SetShapeData(handedness, fingerID, XRFingerShapeType.TipCurl, tipCurl);

            if (fingerShape.TryGetPinch(out var pinch))
                SetShapeData(handedness, fingerID, XRFingerShapeType.Pinch, pinch);

            if (fingerShape.TryGetSpread(out var spread))
                SetShapeData(handedness, fingerID, XRFingerShapeType.Spread, spread);
        }
    }
    void SetShapeData(Handedness handedness, XRHandFingerID fingerID, XRFingerShapeType shapeType, float value)
    {
        var val = Mathf.Clamp(value, 0f, 1f);

        if (handedness == Handedness.Left)
        {
            m_LeftHand.AddData(fingerID, shapeType, val);
        }
        else if (handedness == Handedness.Right)
        {
            m_RightHand.AddData(fingerID, shapeType, val);
        }
        else
        {
            Debug.LogError("Handedness not set.");
        }
    }

    void CreateScreenshot()
    {
        m_ScreenCaptureCamera?.TakeScreenshot();
    }

    void EndRecording()
    {
        CheckDirectoryName();
        CreateHandShapes();
        SaveAssets();
        ClearCache();
        OnRecordEnded?.Invoke();
    }
    void CheckDirectoryName()
    {
        if (!Directory.Exists(WORKING_PATH))
            Directory.CreateDirectory(WORKING_PATH);

        var dirCounter = (Directory.GetDirectories(WORKING_PATH).Length + 1).ToString();

        CURRENT_FILE_NAME = FILE_NAME + dirCounter;
        CURRENT_DIR_NAME = DIR_NAME + dirCounter + "/";

        if (!Directory.Exists(WORKING_PATH + CURRENT_DIR_NAME))
            Directory.CreateDirectory(WORKING_PATH + CURRENT_DIR_NAME);
    }
    void CreateHandShapes()
    {
        if(m_RecordingLeft)
        {
            var leftAssetPath = WORKING_PATH + CURRENT_DIR_NAME + CURRENT_FILE_NAME + LEFT + ASSET_EXTENSION;
            m_LeftHandShapeInstance = ScriptableObject.CreateInstance<XRHandShape>();
            m_LeftHand.ApplyDataToInstance(ref m_LeftHandShapeInstance);

#if UNITY_EDITOR
            UnityEditor.AssetDatabase.CreateAsset(m_LeftHandShapeInstance, leftAssetPath);
#endif
        }

        if (m_RecordingRight)
        {
            var rightAssetPath = WORKING_PATH + CURRENT_DIR_NAME + CURRENT_FILE_NAME + RIGHT + ASSET_EXTENSION;
            m_RightHandShapeInstance = ScriptableObject.CreateInstance<XRHandShape>();
            m_RightHand.ApplyDataToInstance(ref m_RightHandShapeInstance);

#if UNITY_EDITOR
            UnityEditor.AssetDatabase.CreateAsset(m_RightHandShapeInstance, rightAssetPath);
#endif
        }
    }
    void SaveAssets()
    {
#if UNITY_EDITOR
        SaveScreenshot();
        UnityEditor.AssetDatabase.SaveAssets();
        UnityEditor.AssetDatabase.Refresh();
#endif
    }
    void SaveScreenshot()
    {
        var fullImagePath = WORKING_PATH + CURRENT_DIR_NAME + CURRENT_FILE_NAME + IMAGE_EXTENSION;
        m_ScreenCaptureCamera?.SaveTexture(fullImagePath);
    }
    void ClearCache()
    {
        m_LeftHand.Reset();
        m_RightHand.Reset();
    }

    class FingerShapeData
    {
        Dictionary<XRFingerShapeType, float[]> _values;
        Dictionary<XRFingerShapeType, uint> _indexOf;
        
        XRHandFingerID _fingerID;
        float _defaultTolerance = 0.2f;
        uint _resolution;

        public FingerShapeData(XRHandFingerID fingerID, uint resolution)
        {
            _fingerID = fingerID;
            _resolution = resolution;

            Reset();
        }

        public bool TryAddEntry(XRFingerShapeType shapeType, float value)
        {
            if (!_indexOf.ContainsKey(shapeType) || !_values.ContainsKey(shapeType))
                return false;

            if (_indexOf[shapeType] >= _values[shapeType].Length)
                return false;

            _values[shapeType][_indexOf[shapeType]] = value;
            _indexOf[shapeType] = _indexOf[shapeType] + 1;

            return true;
        }
        public XRFingerShapeCondition GetShapeCondition()
        {
            var values = new Dictionary<XRFingerShapeType, float>();
            for (int i = 0; i < _values.Count; i++)
            {
                var shapeType = (XRFingerShapeType)i;
                var value = CalculateAverage(shapeType);

                if (value > 0f)
                {
                    values[shapeType] = value;
                }
            }

            var fingerShapeCondition = new XRFingerShapeCondition();
            fingerShapeCondition.fingerID = _fingerID;
            fingerShapeCondition.targets = new XRFingerShapeCondition.Target[values.Count];

            var index = 0;
            foreach (var entry in values)
            {
                var shapeType = entry.Key;

                fingerShapeCondition.targets[index].shapeType = shapeType;
                fingerShapeCondition.targets[index].desired = CalculateAverage(shapeType);
                fingerShapeCondition.targets[index].tolerance = CalculateTolerance(shapeType);

                index++;
            }

            return fingerShapeCondition;
        }
        public void Reset()
        {
            _values = new Dictionary<XRFingerShapeType, float[]>
            {
                [XRFingerShapeType.FullCurl]    = new float[_resolution],
                [XRFingerShapeType.BaseCurl]    = new float[_resolution],
                [XRFingerShapeType.TipCurl]     = new float[_resolution],
                [XRFingerShapeType.Pinch]       = new float[_resolution],
                [XRFingerShapeType.Spread]      = new float[_resolution]
            };
            _indexOf = new Dictionary<XRFingerShapeType, uint>
            {
                [XRFingerShapeType.FullCurl]    = 0,
                [XRFingerShapeType.BaseCurl]    = 0,
                [XRFingerShapeType.TipCurl]     = 0,
                [XRFingerShapeType.Pinch]       = 0,
                [XRFingerShapeType.Spread]      = 0
            };
        }

        float CalculateAverage(XRFingerShapeType shapeType)
        {
            if (!_values.ContainsKey(shapeType))
                return 0f;

            var sum = 0f;
            for (int i = 0; i < _values[shapeType].Length; i++)
            {
                sum += _values[shapeType][i];
            }

            var average = sum / _values[shapeType].Length;
            return average;
        }
        float CalculateTolerance(XRFingerShapeType shapeType)
        {
            var values = _values[shapeType];
            Array.Sort(values);

            var tolerance = Math.Abs(values[0] - values[values.Length - 1]);

            return (tolerance >= _defaultTolerance) ? tolerance : _defaultTolerance;
        }
    }
    class HandData
    {
        Dictionary<XRHandFingerID, FingerShapeData> fingers;

        uint _resolution;

        public HandData(uint resolution)
        {
            _resolution = resolution;
            Reset();
        }

        public void Reset()
        {
            fingers = new Dictionary<XRHandFingerID, FingerShapeData>
            {
                [XRHandFingerID.Thumb]  = new FingerShapeData(XRHandFingerID.Thumb,     _resolution),
                [XRHandFingerID.Index]  = new FingerShapeData(XRHandFingerID.Index,     _resolution),
                [XRHandFingerID.Middle] = new FingerShapeData(XRHandFingerID.Middle,    _resolution),
                [XRHandFingerID.Ring]   = new FingerShapeData(XRHandFingerID.Ring,      _resolution),
                [XRHandFingerID.Little] = new FingerShapeData(XRHandFingerID.Little,    _resolution)
            };
        }

        public void AddData(XRHandFingerID shape, XRFingerShapeType shapeType, float value)
        {
            fingers[shape].TryAddEntry(shapeType, value);
        }

        public void ApplyDataToInstance(ref XRHandShape instance)
        {
            for (int fingerIndex = (int)XRHandFingerID.Thumb; fingerIndex <= (int)XRHandFingerID.Little; fingerIndex++)
            {
                var fingerShapeCondition = fingers[(XRHandFingerID)fingerIndex].GetShapeCondition();
                instance.fingerShapeConditions.Add(fingerShapeCondition);
            }
        }
    }
}

