/*
 * VisualDistanceDebugger.cs
 * 
 * Description:
 * This script handles the visual representation of the distance between given joints. 
 * Requires the DistanceBetweenJoints component.
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

using UnityEngine;

[RequireComponent(typeof(DistanceBetweenJoints))]
public class VisualDistanceDebugger : MonoBehaviour
{
    [SerializeField]
    Transform m_LeftArrow, m_Tube, m_RightArrow;

    DistanceBetweenJoints m_DistanceBetweenJoints;

    const float CONE_HEIGHT_IN_METER = 0.02f;

    void Awake()
    {
        m_DistanceBetweenJoints = GetComponent<DistanceBetweenJoints>();
    }

    void LateUpdate()
    {
        ScaleToDistance();
        SetConesToJoints();
    }

    void ScaleToDistance()
    {
        if (!m_Tube)
            return;

        var visualDistance = m_DistanceBetweenJoints.distance - 2f * CONE_HEIGHT_IN_METER;
        visualDistance = visualDistance < 0 ? 0 : visualDistance;

        m_Tube.localScale = new Vector3(this.transform.localScale.x, this.transform.localScale.y, visualDistance);
    }
    void SetConesToJoints()
    {

        var left = m_DistanceBetweenJoints.leftPosition;
        var right = m_DistanceBetweenJoints.rightPosition;

        if (!m_LeftArrow)
            return;

        var leftOffset = (left - right).normalized * (CONE_HEIGHT_IN_METER + m_DistanceBetweenJoints.GetLeftRadialOffset());
        m_LeftArrow.position = left - leftOffset;

        if (m_LeftArrow.localPosition.z < 0)
        {
            m_LeftArrow.localPosition = new Vector3(m_LeftArrow.localPosition.x, m_LeftArrow.localPosition.y, 0);
        }

        if (!m_RightArrow)
            return;

        var rightOffset = (right - left).normalized * (CONE_HEIGHT_IN_METER + m_DistanceBetweenJoints.GetRightRadialOffset());
        m_RightArrow.position = right - rightOffset;

        if (m_RightArrow.localPosition.z > 0)
        {
            m_RightArrow.localPosition = new Vector3(m_RightArrow.localPosition.x, m_RightArrow.localPosition.y, 0);
        }
    }
}
