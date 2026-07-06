using System;
using NUnit.Framework;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.XR.Hands;
using UnityEngine.XR.Interaction.Toolkit.Interactables;
using UnityEngine.XR.Management;

public class XRI_FFBGraspClient : MonoBehaviour
{
    private XRI_FFBManager ffbManager;

    [SerializeField]
    private HandGraspDetector handGraspDetector;
    [SerializeField]
    private Handedness handedness;

    private XRHand CurrentHand 
        => handedness == Handedness.Left ? handSubsystem.leftHand : handSubsystem.rightHand;

    private XRGrabInteractable selected;
    private XRHandSubsystem handSubsystem;

    private void Awake()
    {
        ffbManager = FindFirstObjectByType<XRI_FFBManager>();
        handSubsystem = XRGeneralSettings.Instance?
            .Manager?
            .activeLoader?
            .GetLoadedSubsystem<XRHandSubsystem>();   
    }

    private void OnEnable()
    {
        handGraspDetector.OnGrab.AddListener(OnGrab);
        handGraspDetector.OnRelease.AddListener(OnRelease);

        handGraspDetector.AddOnTouch(_ApplyForceFeedback);
        handGraspDetector.AddOnUntouch(_ApplyForceFeedback);
    }

    private void OnDisable()
    {
        handGraspDetector.OnGrab.RemoveListener(OnGrab);
        handGraspDetector.OnRelease.RemoveListener(OnRelease);

        handGraspDetector.RemoveOnTouch(_ApplyForceFeedback);
        //handGraspDetector.RemoveOnUntouch(_ApplyForceFeedback);
    }

    private void OnGrab(XRGrabInteractable interactable)
    {
        selected = interactable;

        ApplyForceFeedback();
    }

    private void OnRelease(XRGrabInteractable interactable)
    {
        selected = null;

        ffbManager.RelaxForceFeedback(CurrentHand);
    }

    private void _ApplyForceFeedback(FingerContactEvent e)
    {
        ApplyForceFeedback();
    }
    private void ApplyForceFeedback()
    {
        if(handedness == Handedness.Invalid || handSubsystem == null || !selected)
            return;

        ffbManager.SetForceFeedbackFromSkeleton(CurrentHand, GetTouchedMask(selected));  
    }

    private bool[] GetTouchedMask(XRGrabInteractable interactable)
    {
        bool[] touched = new bool[5];
        
        int i = 0;
        var _touched = handGraspDetector.Touched(interactable);

        foreach (var finger in Enum.GetValues(typeof(XRHandFingerID)))
        {
            if(_touched.Contains((XRHandFingerID)finger))
                touched[i] = true;

            i++;
        }

        return touched;
    }
}
