/*
 * SimpleGesture.cs
 * 
 * Description:
 * This script is the Base Class for Gesture Detection. 
 * See Unity.XR.Hands.Samples.Gestures.StaticHandGesture for more information.
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
using UnityEngine.XR.Hands.Gestures;
using UnityEngine.XR.Hands;

[Serializable]
public class SimpleGesture{

    [SerializeField]
    protected XRHandTrackingEvents m_HandTrackingEvents;

    [SerializeField]
    protected float m_MinimumHoldTime;

    [SerializeField]
    protected float m_GestureDetectionInterval;

    protected XRHandPose m_HandPose;
    protected XRHandShape m_HandShape;

    event Action m_OnTriggered;
    event Action m_GestureEnded;
    bool m_WasDetected;
    bool m_PerformedTriggered;

    float m_TimeOfLastConditionCheck;
    float m_HoldStartTime;

    public bool IsTriggered()
    {
        return m_PerformedTriggered;
    }
    public void SetHandShape(ScriptableObject handShapeOrPose) 
    {
        m_HandShape = handShapeOrPose as XRHandShape;
        m_HandPose = handShapeOrPose as XRHandPose;
    }

    public SimpleGesture(XRHandTrackingEvents handTrackingEvents, ScriptableObject handShapeOrPose, 
        float gestureDetectionInterval, float minimumHoldTime, 
        Action OnTriggered, Action GestureEnded)
    {
        m_HandTrackingEvents = handTrackingEvents;
        m_HandShape = handShapeOrPose as XRHandShape;
        m_HandPose = handShapeOrPose as XRHandPose;
        m_GestureDetectionInterval = gestureDetectionInterval;
        m_MinimumHoldTime = minimumHoldTime;
        m_OnTriggered = OnTriggered;
        m_GestureEnded = GestureEnded;
    }

    public void CheckForGesture()
    {
        m_HandTrackingEvents.jointsUpdated.AddListener(OnJointsUpdated);
    }

    public void StopCheckingForGesture()
    {
        m_HandTrackingEvents.jointsUpdated.RemoveListener(OnJointsUpdated);
    }

    protected virtual void OnJointsUpdated(XRHandJointsUpdatedEventArgs eventArgs)
    {
        if (Time.timeSinceLevelLoad < m_TimeOfLastConditionCheck + m_GestureDetectionInterval)
            return;

        var detected =
            m_HandTrackingEvents.handIsTracked &&
            m_HandShape != null && m_HandShape.CheckConditions(eventArgs) ||
            m_HandPose != null && m_HandPose.CheckConditions(eventArgs);

        if (!m_WasDetected && detected)
        {
            m_HoldStartTime = Time.timeSinceLevelLoad;
        }
        else if (m_WasDetected && !detected)
        {
            m_PerformedTriggered = false;
            m_HandTrackingEvents.jointsUpdated.RemoveListener(OnJointsUpdated);
            m_GestureEnded?.Invoke();
        }

        m_WasDetected = detected;
        if (!m_PerformedTriggered && detected)
        {
            var holdTimer = Time.timeSinceLevelLoad - m_HoldStartTime;
            if (holdTimer > m_MinimumHoldTime)
            {
                m_PerformedTriggered = true;
                m_OnTriggered?.Invoke();
            }
        }

        m_TimeOfLastConditionCheck = Time.timeSinceLevelLoad;
    }
}