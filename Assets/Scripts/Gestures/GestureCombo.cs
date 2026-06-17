/*
 * GestureCombo.cs
 * 
 * Description:
 * This script handles the recognition of multiple gestures as a combo. 
 * Each gesture in a combo fires an event, which triggers the next gesture detection.
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
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.XR.Hands;

public class GestureCombo : MonoBehaviour
{
    [SerializeField]
    XRHandTrackingEvents m_HandTrackingEvents;

    [SerializeField]
    ScriptableObject[] m_Gestures;

    [SerializeField]
    UnityEvent m_ComboStarted;

    [SerializeField]
    UnityEvent m_ComboContinued;

    [SerializeField]
    UnityEvent m_ComboResetted;

    [SerializeField]
    UnityEvent m_ComboEnded;

    [SerializeField]
    float m_MinimumHoldTime = 0.2f;
    [SerializeField]
    float m_GestureDetectionInterval = 0.1f;

    [SerializeField]
    float m_TimeUntilGestureResetsInSeconds = 1.0f;

    SimpleGesture[] simpleGestures;
    //bool comboStarted = false;
    int comboIndex = 0;
    IEnumerator currentComboTimer;
    

    void Awake()
    {
        if (m_Gestures == null || m_Gestures.Length == 0)
            return;
       
        simpleGestures = new SimpleGesture[m_Gestures.Length];

        for (int i = 0; i < m_Gestures.Length; i++)
        {
            if (i == 0)
                simpleGestures[i] = new SimpleGesture(m_HandTrackingEvents, m_Gestures[i], m_GestureDetectionInterval, m_MinimumHoldTime, ComboStarted, Next);

            else if (i == m_Gestures.Length - 1)
                simpleGestures[i] = new SimpleGesture(m_HandTrackingEvents, m_Gestures[i], m_GestureDetectionInterval, m_MinimumHoldTime, ComboEnded, null);

            else
                simpleGestures[i] = new SimpleGesture(m_HandTrackingEvents, m_Gestures[i], m_GestureDetectionInterval, m_MinimumHoldTime, ComboContinued, Next);
        }

        ResetCombo();
    }

    void ComboStarted()
    {
        m_ComboStarted?.Invoke();
        //comboStarted = true;
    }
    void ComboContinued()
    {
        m_ComboContinued?.Invoke();
    }
    void ComboEnded()
    {
        m_ComboEnded?.Invoke();
    }

    void Next()
    {
        StartComboTimer();
        comboIndex++;

        if (comboIndex >= m_Gestures.Length)
            return;

        simpleGestures[comboIndex].CheckForGesture();
    }

    void StartComboTimer()
    {
        if(currentComboTimer != null)
            StopCoroutine(currentComboTimer);

        currentComboTimer = ComboTimer(m_TimeUntilGestureResetsInSeconds);
        StartCoroutine(currentComboTimer);
    }

    IEnumerator ComboTimer(float seconds)
    {
        var step = 0.1f;
        var timePassed = 0f;

        while (timePassed < seconds)
        {
            timePassed += step;
            yield return new WaitForSeconds(step);
        }

        ResetCombo();
    }

    void ResetCombo()
    {
        simpleGestures[comboIndex].StopCheckingForGesture();

        comboIndex = 0;
        //comboStarted = false;
        m_ComboResetted.Invoke();

        simpleGestures[comboIndex].CheckForGesture();
    }
}
