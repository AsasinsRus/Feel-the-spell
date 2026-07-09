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
    private static readonly XRHandFingerID[] FingerOrder =
    {
        XRHandFingerID.Thumb,
        XRHandFingerID.Index,
        XRHandFingerID.Middle,
        XRHandFingerID.Ring,
        XRHandFingerID.Little
    };

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
        if(handGraspDetector == null)
            return;

        handGraspDetector.OnGrab.AddListener(OnGrab);
        handGraspDetector.OnRelease.AddListener(OnRelease);

        handGraspDetector.AddOnTouch(_ApplyForceFeedback);
        //handGraspDetector.AddOnUntouch(_ApplyForceFeedback);
    }

    private void OnDisable()
    {
        handGraspDetector.OnGrab.RemoveListener(OnGrab);
        handGraspDetector.OnRelease.RemoveListener(OnRelease);

        handGraspDetector.RemoveOnTouch(_ApplyForceFeedback);
        //andGraspDetector.RemoveOnUntouch(_ApplyForceFeedback);
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
        if(handedness == Handedness.Invalid || handSubsystem == null || !selected || !CurrentHand.isTracked)
        {
            if(ffbManager != null && handSubsystem != null)
            ffbManager.RelaxForceFeedback(CurrentHand);

            return;
        }
        ffbManager.SetForceFeedbackFromXRHand(CurrentHand, GetTouchedMask(selected));  
    }

    private bool[] GetTouchedMask(XRGrabInteractable interactable)
    {
        bool[] touched = new bool[5];
        
        int i = 0;
        var _touched = handGraspDetector.Touched(interactable);

        foreach (var finger in FingerOrder)
        {
            if(_touched.Contains(finger))
                touched[i] = true;

            i++;
        }

        return touched;
    }

    
}
