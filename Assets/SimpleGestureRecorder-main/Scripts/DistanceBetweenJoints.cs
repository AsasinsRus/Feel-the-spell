/*
 * DistanceBetweenJoints.cs
 * 
 * Description:
 * This script calculates the distance between two given XRHandJoints, using offsets.  
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
using UnityEngine.XR.Hands;

public class DistanceBetweenJoints : CenterBetweenJoints
{
    public float distance { get; private set; }

    const float RADIAL_OFFSET = 0.005f;

    public float GetLeftRadialOffset()
    {
        return GetRadialOffset(m_leftJointID);
    }
    public float GetRightRadialOffset()
    {
        return GetRadialOffset(m_rightJointID);
    }

    new void LateUpdate()
    {
        base.LateUpdate();
        CalculateDistance(leftPosition, rightPosition);
    }

    void CalculateDistance(Vector3 left, Vector3 right)
    {
        var rawDistance = Vector3.Distance(left, right);
        var totalOffset = GetRadialOffset(m_leftJointID) + GetRadialOffset(m_rightJointID);
        distance = rawDistance - totalOffset;
    }

    float GetRadialOffset(XRHandJointID jointID)
    {
        switch (jointID)
        {
            default:
                return RADIAL_OFFSET;
        }
    }
}
