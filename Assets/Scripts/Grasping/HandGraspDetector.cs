using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.XR.Hands;
using UnityEngine.XR.Hands.Gestures;
using UnityEngine.XR.Interaction.Toolkit.Interactables;
using UnityEngine.XR.Interaction.Toolkit.Interactors;
using UnityEngine.XR.Management;

public class HandGraspDetector : MonoBehaviour
{
    private const float ANGLE_TO_THE_PALM = 45f;
    private const float THUMB_OPPOSITION_ANGLE = 50f;

    [Header("Hand info")]
    [SerializeField]
    private FingerTouchData thumbTip;
    [SerializeField]
    private FingerTouchData indexTip;
    [SerializeField]
    private FingerTouchData middleTip;
    [SerializeField]
    private FingerTouchData ringTip;
    [SerializeField]
    private FingerTouchData pinkyTip;
    [SerializeField]
    private Handedness handedness;
    private XRHand CurrentHand => handedness == Handedness.Left ? handSubsystem.leftHand : handSubsystem.rightHand;
    [SerializeField]
    private Transform hand;
    [SerializeField]
    private Collider palm;
    [SerializeField]
    private float closeToPalmThreshold;

    [Header("Interactions")]
    [SerializeField]
    private XRDirectInteractor interactor;

    [SerializeField]
    private float releaseGraceTime = .1f;
    [SerializeField]
    private float pinchHoldDistanceMultiplier = 1.6f;
    [SerializeField]
    private float powerHoldDistanceMultiplier = 1.8f;
    [SerializeField]
    private float releaseCurlDifference = .1f;

    [Header("Events")]
    [SerializeField]
    public UnityEvent<XRGrabInteractable> OnGrab;
    [SerializeField]
    public UnityEvent<XRGrabInteractable> OnRelease;

    private XRHandSubsystem handSubsystem;

    private FingerContactRegistry contactRegistry;
    private XRIHandGrabber grabber;

    [SerializeField]
    private bool log = false;

    private HeldGraspState heldState;
    private bool hasHeldState;

    private const int FRAMES_UNITL_NEXT_GRAB = 10;
    private int framesFromGrab = 0;

    private void Awake()
    {
        if (!FingersSetup()) return;

        handSubsystem = XRGeneralSettings.Instance?.Manager?.activeLoader?.GetLoadedSubsystem<XRHandSubsystem>();

        grabber = new XRIHandGrabber(interactor, hand);
    }

    private bool FingersSetup()
    {
        var fingers = new[] { thumbTip, indexTip, middleTip, ringTip, pinkyTip };

        if (thumbTip == null || thumbTip.fingerTipCollider == null)
        {
            Debug.LogError("Thumb was not added!");
            return false;
        }

        if (fingers.Skip(1).All(m => m == null || m.fingerTipCollider == null))
        {
            Debug.LogError("No fingers were detected!");
            return false;
        }

        contactRegistry = new FingerContactRegistry(
            fingers,
            OnInteractableFingerTouch,
            OnInteractableFingerUntouch,
            OnInteractableDestroy
            );

        contactRegistry.Bind();

        return true;
    }

    private void OnDestroy()
    {
        contactRegistry?.Unbind();
        grabber?.UnsubscribeInteractor();
    }

    private void Update()
    {
        framesFromGrab++;

        if (handSubsystem == null) return;
        if (!CurrentHand.isTracked)
        {
            ForceRelease();
            return;
        }
        
        if (grabber.SelectedInteractable)
        {
            EvaluateHold();
            return;
        }

        // BUG: still taking items from inventory even if something is selected 
        if (framesFromGrab >= FRAMES_UNITL_NEXT_GRAB)
            TryAcquireGrab();
    }

    private void TryAcquireGrab()
    {
        foreach (var interactable in contactRegistry.GetAllTouches())
        {
            if (TryStartGrasp(interactable))
            {
                framesFromGrab = 0;
                return;
            }
        }
    }

    #region Checks
    private bool IsFingerCurledEnough(FingerTouchData fingerTouchData)
    {
        HandCurlProvider.TryGetFingerCurl(CurrentHand, fingerTouchData.handFingerID, out float curl);

        return curl >= fingerTouchData.fingerCurlThreshold;
    }

    private bool IsNearPalm(XRGrabInteractable interactable)
    {
        if (!TryGetInteractableCenter(interactable, out var interactableCenter))
            return false;

        Vector3 palmCenter = palm.bounds.center;
        float distance = Vector3.Distance(interactableCenter, palmCenter);

        return distance <= closeToPalmThreshold;
    }

    private bool TryGetInteractableCenter(XRGrabInteractable interactable, out Vector3 center)
    {
        center = default;

        if (!interactable) return false;

        var col = interactable.GetComponentInChildren<Collider>();
        if (!col) return false;

        center = col.bounds.center;

        return true;
    }

    private bool HasThumbOpposition(XRGrabInteractable interactable)
    {
        if (!TryGetInteractableCenter(interactable, out var center))
            return false;

        Vector3 interactableToThumb = thumbTip.fingerTipCollider.bounds.center - center;

        foreach(var finger in contactRegistry.Fingers)
        {
            if(finger == thumbTip)
                continue;

            if (!contactRegistry.IsTouching(finger, interactable))
                continue;

            Vector3 interactableToFinger = finger.fingerTipCollider.bounds.center - center;
            float angle = Vector3.Angle(interactableToThumb, interactableToFinger);

            if (angle >= THUMB_OPPOSITION_ANGLE)
            {
                if (log) Debug.Log("Thumb is opposed enough to " + finger.handFingerID + " for " + interactable.name);
                return true;
            }
        }

        return false;
    }
    private bool DirectedIntoTheHand(FingerContactEvent fingerContactEvent)
    {
        Vector3 fingerPos = fingerContactEvent.Finger.fingerTipCollider.transform.position;

        Vector3 toObject = fingerContactEvent.OtherCollider.bounds.center - fingerPos;
        Vector3 inwardDirection = fingerContactEvent.Finger.fingerTipCollider.transform
            .TransformDirection(fingerContactEvent.Finger.insidePalmDirection);

        return Vector3.Angle(inwardDirection, toObject) <= ANGLE_TO_THE_PALM;
    }

    #endregion

    #region Events
    private void OnInteractableFingerTouch(FingerContactEvent fingerContactEvent)
    {
        if (!fingerContactEvent.Interactable) return;
        if (!DirectedIntoTheHand(fingerContactEvent)) return;

        TryStartGrasp(fingerContactEvent.Interactable);

        if (log) Debug.Log(fingerContactEvent.Finger.handFingerID + " is touching " + fingerContactEvent.OtherCollider.name + " from the correct side");
    }

    private void OnInteractableDestroy(GameObject gameObject)
    {
        var interactable = gameObject.GetComponent<XRGrabInteractable>();

        if (interactable == null) return;

        if (interactable == grabber.SelectedInteractable)
        {
            OnRelease?.Invoke(interactable);
            grabber.Release();
        }

        contactRegistry.RemoveInteractableEverywhere(interactable);
    }

    private void OnInteractableFingerUntouch(FingerContactEvent fingerContactEvent)
    {
        if (!fingerContactEvent.Interactable) return;
        if (!DirectedIntoTheHand(fingerContactEvent)) return;

        TryStartGrasp(fingerContactEvent.Interactable);
    }
    #endregion

    #region Grabbing
    private bool TryStartGrasp(XRGrabInteractable interactable)
    {
        if (!CanStartGrab(interactable))
            return false;

        var evidance = GetGraspEvidence(interactable);
        var mode = DetermineGrabMode(evidance);
        var primaryFinger = DeterminePrimarySupportFinger(interactable);

        if (!HandCurlProvider.TryGetFingerCurl(CurrentHand, primaryFinger.handFingerID, out var primaryFingerCurl))
            return false;
        if (!HandCurlProvider.TryGetFingerCurl(CurrentHand, thumbTip.handFingerID, out var thumbCurl))
            return false;

        if (!grabber.TryGrab(interactable))
            return false;

        heldState = new HeldGraspState
        {
            Interactable = interactable,
            Mode = mode,
            ThumbCurl = thumbCurl,
            PrimarySupportFinger = primaryFinger,
            PrimarySupportFingerCurl = primaryFingerCurl,
            InvalidSince = -1
        };

        hasHeldState = true;
        OnGrab?.Invoke(interactable);

        if (log) Debug.Log("Grasp condition was achived" + "\nGrasp Mode: " + heldState.Mode);
        return true;
    }

    private void EvaluateHold()
    {
        if(!hasHeldState || heldState.Interactable != grabber.SelectedInteractable)
        {
            ForceRelease();
            return;
        }

        if(CanKeepHolding(heldState))
        {
            heldState.InvalidSince = -1;
            return;
        }

        if(heldState.InvalidSince < 0f)
            heldState.InvalidSince = Time.time;

        if (Time.time - heldState.InvalidSince >= releaseGraceTime)
        {
            ForceRelease();
            heldState.InvalidSince = -1;
        }
    }

    private bool CanKeepHolding(HeldGraspState held)
    {
        if (held.Interactable == null)
            return false;

        if (!TryGetInteractableCenter(held.Interactable, out var center))
            return false;

        float palmDistance = Vector3.Distance(palm.bounds.center, center);

        if (!HandCurlProvider.TryGetFingerCurl(CurrentHand, held.PrimarySupportFinger.handFingerID, out var currentPrimaryFingerCurl))
            return false;
        if (!HandCurlProvider.TryGetFingerCurl(CurrentHand, thumbTip.handFingerID, out var currentThumbCurl))
            return false;

        bool thumbStillCurledEnough = held.ThumbCurl - releaseCurlDifference <= currentThumbCurl;
        bool supportStillCurledEnough = held.PrimarySupportFingerCurl - releaseCurlDifference <= currentPrimaryFingerCurl;

        return held.Mode switch
        {
            GrabMode.PINCH =>
                thumbStillCurledEnough &&
                supportStillCurledEnough,
            GrabMode.POWER =>
                supportStillCurledEnough,
            _ => false
        };
    }

    private GrabMode DetermineGrabMode(GraspEvidence e)
    {
        // works with bugs needs thinking
        bool pinchGrab =
            e.ThumbTouch &&
            e.SupportingFingerTouches >= 1 &&
            e.CurledSupportingFingers >= 1 &&
            e.ThumbOpposed;

        bool powerGrab =
            e.NearPalm &&
            (e.SupportingFingerTouches >= 1 && e.CurledSupportingFingers >= 1 ||
            e.ThumbTouch && e.ThumbCurledEnough);

        if (pinchGrab)
            return GrabMode.PINCH;
        if (powerGrab)
            return GrabMode.POWER;

        return GrabMode.NONE;
    }

    private FingerTouchData DeterminePrimarySupportFinger(XRGrabInteractable interactable)
    {
        FingerTouchData[] order = { indexTip, middleTip, ringTip, pinkyTip };

        foreach(var finger in order)
        {
            if (finger == null)
                continue;

            if (contactRegistry.IsTouching(finger, interactable))
                return finger;
        }

        return null;
    }

    private bool CanStartGrab(XRGrabInteractable interactable)
    {
        if (interactable == null)
            return false;

        if (grabber.SelectedInteractable != null)
            return false;

        var e = GetGraspEvidence(interactable);

        return DetermineGrabMode(e) != GrabMode.NONE;
    }

    private void ForceRelease()
    {
        var selected = grabber.SelectedInteractable;
        if(selected)
        {
            OnRelease?.Invoke(selected);
            grabber.Release();
        }

        hasHeldState = false;
    }

    private GraspEvidence GetGraspEvidence(XRGrabInteractable interactable)
    {
        int curledSupportingFingers = 0;
        int supportingFingerTouches = 0;

        foreach(var finger in contactRegistry.Fingers)
        {
            if (finger == null || finger == thumbTip)
                continue;

            if (!contactRegistry.IsTouching(finger, interactable))
                continue;

            supportingFingerTouches++;

            if (IsFingerCurledEnough(finger))
                curledSupportingFingers++;
        }

        return new GraspEvidence
        {
            ThumbTouch = thumbTip != null && contactRegistry.IsTouching(thumbTip, interactable),
            ThumbCurledEnough = IsFingerCurledEnough(thumbTip),

            SupportingFingerTouches = supportingFingerTouches,
            CurledSupportingFingers = curledSupportingFingers,

            NearPalm = IsNearPalm(interactable),
            ThumbOpposed = HasThumbOpposition(interactable),

            Interactable = interactable
        };
    }

    #endregion

    #region Helpers

    public HashSet<XRHandFingerID> Touched(XRGrabInteractable interactable)
    {
        HashSet<XRHandFingerID> touchedBy = new();

        foreach(var finger in contactRegistry.Fingers)
        {
            if(contactRegistry.IsTouching(finger, interactable))
                touchedBy.Add(finger.handFingerID);
        }

        return touchedBy;
    }

    public void AddOnTouch(Action<FingerContactEvent> onTouch)
    {
        contactRegistry.onFingerTouch += onTouch;
    }

    public void AddOnUntouch(Action<FingerContactEvent> onUntouch)
    {
        contactRegistry.onFingerUntouch += onUntouch;
    }

    public void RemoveOnTouch(Action<FingerContactEvent> onTouch)
    {
        contactRegistry.onFingerTouch -= onTouch;
    }

    public void RemoveOnUntouch(Action<FingerContactEvent> onUntouch)
    {
        contactRegistry.onFingerUntouch -= onUntouch;
    }

    #endregion

    #region Gizmos
    private void OnDrawGizmosSelected()
    {
        DrawFingerGizmos(thumbTip);
        DrawFingerGizmos(indexTip);
        DrawFingerGizmos(middleTip);
        DrawFingerGizmos(ringTip);
        DrawFingerGizmos(pinkyTip);
    }

    private void DrawFingerGizmos(FingerTouchData finger)
    {
        if (finger != null && finger.fingerTipCollider != null)
        {
            Gizmos.color = Color.white;

            Vector3 startingPoint = finger.fingerTipCollider.bounds.center;

            Gizmos.DrawLine(startingPoint, startingPoint + finger.insidePalmDirection.normalized * .03f);
        }
    }

    #endregion
}

[Serializable]
public class FingerTouchData
{
    public Collider fingerTipCollider;
    public float fingerCurlThreshold;

    public XRHandFingerID handFingerID;

    public int touchScore;

    public Vector3 insidePalmDirection;
}


public struct GraspEvidence
{
    public bool ThumbTouch;
    public bool ThumbCurledEnough;
    public int SupportingFingerTouches;
    public bool NearPalm;
    public bool ThumbOpposed;
    public int CurledSupportingFingers;

    public XRGrabInteractable Interactable;
}

public enum GrabMode
{
    NONE,
    PINCH,
    POWER
}
public struct HeldGraspState
{
    public XRGrabInteractable Interactable;
    public GrabMode Mode;

    public float ThumbCurl;

    public FingerTouchData PrimarySupportFinger;
    public float PrimarySupportFingerCurl;

    public float InvalidSince;
}

public static class HandCurlProvider
{ 
    public static bool TryGetFingerCurl(XRHand hand, XRHandFingerID fingerID, out float curl)
    {
        curl = 0f;

        if(!hand.isTracked) return false;

        var fingerShape = XRFingerShapeMath.CalculateFingerShape(
            hand,
            fingerID,
            XRFingerShapeTypes.FullCurl
            );

        return fingerShape.TryGetFullCurl(out curl);
    }
}
