/*
 * GestureRecorderController.cs
 * 
 * Description:
 * This script controls the Gesture Recording. 
 * Requires the GestureRecorder component.
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

using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using UnityEngine.UI;
using UnityEngine.XR.Hands;
using System;

[RequireComponent(typeof(GestureRecorder))]
public class GestureRecorderController : MonoBehaviour
{
    enum PoseStatus
    {
        NONE,
        LEFT,
        BOTH,
        RIGHT
    }

    public Image recordLeftIndicatorBar, recordBothIndicatorBar, recordRightIndicatorBar, recordingFrame, recordingStatusCircle;
    public TMP_Text countdown;
    public int countdownDuration = 3;
    public float poseDuration = 2f;
    public List<GameObject> menu;
    public DebugHandGesture leftCustomGesture, rightCustomGesture;

    GestureRecorder m_Recorder;
    bool m_RightThumbUp, m_LeftThumbUp, m_RightThumbDown, m_LeftThumbDown, m_CountDownStarted, m_RecordLeft, m_RecordRight, m_Recording;
    PoseStatus m_CurrentPose = PoseStatus.NONE;
    float m_PoseTime = 0f;

    public void SetRightThumbUp(bool value)
    {
        m_RightThumbUp = value;
    }
    public void SetLeftThumbUp(bool value)
    {
        m_LeftThumbUp = value;
    }
    public void SetRightThumbDown(bool value)
    {
        m_RightThumbDown = value;
    }
    public void SetLeftThumbDown(bool value)
    {
        m_LeftThumbDown = value;
    }

    void Start()
    {
        m_Recorder = GetComponent<GestureRecorder>();
        m_Recorder.OnRecordEnded += StopRecording;
        ResetUI();
    }

    void Update()
    {
        if (m_Recording)
        {
            UpdateVisualRecordingStatus();
        }

        if (m_CountDownStarted)
            return;

        CheckPose();
        UpdateVisualPoseStatus();
    }

    void UpdateVisualRecordingStatus()
    {
        recordingStatusCircle.fillAmount += Time.smoothDeltaTime * (1 / m_Recorder.recordingDuration);
    }

    void CheckPose()
    {
        var lastPose = m_CurrentPose;
        ResetPose();

        if (m_LeftThumbUp && m_RightThumbDown)
        {
            m_CurrentPose = PoseStatus.LEFT;
        }

        if (m_LeftThumbUp && m_RightThumbUp)
        {
            m_CurrentPose = PoseStatus.BOTH;
        }

        if (m_LeftThumbDown && m_RightThumbUp)
        {
            m_CurrentPose = PoseStatus.RIGHT;
        }

        if(m_CurrentPose == PoseStatus.NONE)
        {
            ResetPoseTime();
        }
        else if(lastPose == m_CurrentPose)
        {
            UpdatePoseTime();
        }

        CheckPoseTime();
    }
    void ResetPose()
    {
        m_CurrentPose = PoseStatus.NONE;
    }
    void UpdatePoseTime()
    {
        m_PoseTime = Math.Clamp((m_PoseTime + Time.smoothDeltaTime), 0f, poseDuration);
    }
    void ResetPoseTime()
    {
        m_PoseTime = 0f;
    }
    void CheckPoseTime()
    {
        if(m_PoseTime == poseDuration)
        {
            StartCountdown();
        }
    }

   
    void UpdateVisualPoseStatus()
    {
        ResetIndicatorBars();

        switch (m_CurrentPose)
        {
            case PoseStatus.NONE: 
                break;
            case PoseStatus.LEFT: recordLeftIndicatorBar.fillAmount = m_PoseTime/ poseDuration; 
                break;
            case PoseStatus.RIGHT: recordRightIndicatorBar.fillAmount = m_PoseTime/ poseDuration; 
                break;
            case PoseStatus.BOTH: recordBothIndicatorBar.fillAmount = m_PoseTime/ poseDuration; 
                break;
        }
    }
    void ResetIndicatorBars()
    {
        recordLeftIndicatorBar.fillAmount = 0;
        recordBothIndicatorBar.fillAmount = 0;
        recordRightIndicatorBar.fillAmount = 0;
    }
    void StartCountdown() 
    {
        SetRecordingSettings();

        m_CountDownStarted = true;

        HideUI();

        countdown.enabled = true;
        recordingFrame.enabled = true;
        StartCoroutine(CountDown(countdown,countdownDuration));
    }
    void SetRecordingSettings()
    {
        m_RecordLeft = (m_CurrentPose == PoseStatus.LEFT || m_CurrentPose == PoseStatus.BOTH) ? true : false;
        m_RecordRight = (m_CurrentPose == PoseStatus.RIGHT || m_CurrentPose == PoseStatus.BOTH) ? true : false;
    }
    IEnumerator CountDown(TMP_Text text, int duration)
    {
        recordingFrame.enabled = true;
        recordingFrame.color = Color.white;

        for (int i = duration; i > 0; i--)
        {
            string secondsRemaining = i.ToString();
            text.text = secondsRemaining;
            yield return new WaitForSeconds(1);
        }

        recordingFrame.color = Color.red;
        text.enabled = false;

        m_Recorder.StartRecording(m_RecordLeft, m_RecordRight);
        m_Recording = true;

        yield return null;
    }
    void HideUI()
    {
        foreach (GameObject obj in menu)
        {
            obj.SetActive(false);
        }
    }
    void ResetUI()
    {
        foreach (GameObject obj in menu)
        {
            obj.SetActive(true);
        }

        countdown.enabled = false;
        recordingFrame.enabled = false;
        recordingStatusCircle.fillAmount = 0;
        countdown.text = countdownDuration.ToString();
    }

    void StopRecording()
    {
        m_Recording = false;
        m_CountDownStarted = false;

        ResetUI();
        SetCustomGesture();
    }
    void SetCustomGesture()
    {
        if (m_Recorder.DoesHandShapeExist(Handedness.Left))
        {
            leftCustomGesture.SetHandShape(m_Recorder.GetHandShape(Handedness.Left));
        }

        if (m_Recorder.DoesHandShapeExist(Handedness.Right))
        {
            rightCustomGesture.SetHandShape(m_Recorder.GetHandShape(Handedness.Right));
        }
    }
}
