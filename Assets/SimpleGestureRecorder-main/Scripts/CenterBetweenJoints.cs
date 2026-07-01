/*
 * CenterBetweenJoints.cs
 * 
 * Description:
 * This script calculates the center between two given XRHandJoints, using offsets.  
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

public class CenterBetweenJoints : MonoBehaviour
{
    [SerializeField]
    protected XRHandJointID m_leftJointID = XRHandJointID.Palm;

    [SerializeField]
    protected XRHandJointID m_rightJointID = XRHandJointID.Palm;

    public Vector3 leftPosition { get; private set; }
    public Vector3 rightPosition { get; private set; }

    protected const float FINGERTIP_THICKNESS_IN_METER = 0.005f;
    protected const float HAND_THICKNESS_IN_METER = 0.015f;

    protected void LateUpdate()
    {
        var handSubsystem = GestureRecorder.TryGetSubsystem();
        if (handSubsystem == null)
            return;

        if (!handSubsystem.leftHand.GetJoint(m_leftJointID).TryGetPose(out Pose leftJoint) ||
            !handSubsystem.rightHand.GetJoint(m_rightJointID).TryGetPose(out Pose rightJoint))
            return;

        leftPosition = leftJoint.position + GetJointOffset(leftJoint, m_leftJointID);
        rightPosition = rightJoint.position + GetJointOffset(rightJoint, m_rightJointID);

        CenterAndOrientate();
    }

    protected void CenterAndOrientate()
    {
        Vector3 center = leftPosition + (rightPosition - leftPosition) * 0.5f;

        this.transform.position = center;

        this.transform.LookAt(leftPosition);
    }

    protected Vector3 GetJointOffset(Pose pose, XRHandJointID jointID)
    {
        var vector = new Vector3();

        switch (jointID)
        {
            case XRHandJointID.Palm:
                vector = -pose.up * HAND_THICKNESS_IN_METER;
                break;
            case XRHandJointID.IndexTip:
            case XRHandJointID.MiddleTip:
            case XRHandJointID.RingTip:
            case XRHandJointID.LittleTip:
                vector = -pose.up * FINGERTIP_THICKNESS_IN_METER;
                break;
            default:
                break;

        }
        return vector;
    }
}
