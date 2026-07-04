using System;
using System.Collections.Generic;
using System.Linq;
using Unity.VisualScripting;
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
    private const float THUMB_OPPOSITION_ANGLE = 70f;

    private const int GRASP_SCORE_THRESHOLD = 10;
    private const int RELEASE_SCORE_THRESHOLD = 8;

    private const int CURL_SCORE = 1;

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

    [Header("Events")]
    [SerializeField]
    public UnityEvent<XRGrabInteractable> OnGrap;
    [SerializeField]
    public UnityEvent<XRGrabInteractable> OnRelease;

    private XRHandSubsystem handSubsystem;

    private FingerContactRegistry contactRegistry;
    private GraspScoreState scoreState;
    private XRIHandGrabber grabber;

    [SerializeField]
    private bool log = false;

    private void Awake()
    {
        if (!FingersSetup()) return;

        handSubsystem = XRGeneralSettings.Instance?.Manager?.activeLoader?.GetLoadedSubsystem<XRHandSubsystem>();

        scoreState = new GraspScoreState();
        grabber = new XRIHandGrabber(interactor, hand);
    }

    private bool FingersSetup()
    {
        var fingers = new[] { thumbTip, indexTip, middleTip, ringTip, pinkyTip };

        if (thumbTip.fingerTipCollider == null)
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
    }

    private void Update()
    {
        if (handSubsystem == null) return;
        if (!CurrentHand.isTracked)
        {
            grabber.Release();
            return;
        }

        CurlCheck();
        PalmCheck();
        ThumbOpposedToFingerCheck();

        TryGrasp();
    }

    private void CurlCheck()
    {
        foreach (var finger in contactRegistry.Fingers)
        {
            if (finger == null) continue;

            bool isCurledNow = IsFingerCurledEnough(finger);

            if (finger.isCurledEnough == isCurledNow)
                continue;

            finger.isCurledEnough = isCurledNow;

            foreach(var interactable in contactRegistry.GetTouches(finger))
            {
                if (isCurledNow)
                {
                    scoreState.AddScore(interactable, CURL_SCORE);
                    if (log)
                        Debug.Log(finger.handFingerID + " is curled enough");
                }
                else
                    scoreState.RemoveScore(interactable, CURL_SCORE);
            }
        }
    }

    private bool IsFingerCurledEnough(FingerTouchData fingerTouchData)
    {
        HandCurlProvider.TryGetFingerCurl(CurrentHand, fingerTouchData.handFingerID, out float curl);

        return curl >= fingerTouchData.fingerCurlThreshold;
    }

    private void PalmCheck()
    {
        foreach (var interactable in contactRegistry.GetAllTouches())
        {
            scoreState.SetPalmState(interactable, IsNearPalm(interactable));
        }
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

    private void ThumbOpposedToFingerCheck()
    {
        if (thumbTip == null || thumbTip.fingerTipCollider == null) return;

        foreach (var interactable in contactRegistry.GetTouches(thumbTip))
        {
            scoreState.SetThumbOppositionState(interactable, HasThumbOpposition(interactable));
        }
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

    private void OnInteractableFingerTouch(FingerContactEvent fingerContactEvent)
    {
        if (!fingerContactEvent.Interactable) return;
        if (!DirectedIntoTheHand(fingerContactEvent)) return;

        scoreState.AddScore(fingerContactEvent.Interactable, fingerContactEvent.Finger.touchScore);

        if (log) Debug.Log(fingerContactEvent.Finger.handFingerID + " is touching " + fingerContactEvent.OtherCollider.name + " from the correct side");
    }

    private void OnInteractableDestroy(GameObject gameObject)
    {
        var interactable = gameObject.GetComponent<XRGrabInteractable>();

        if (interactable == null) return;

        if (interactable == grabber.SelectedInteractable)
            grabber.Release();

        scoreState.RemoveInteractable(interactable);
        contactRegistry.RemoveInteractableEverywhere(interactable);
    }

    private bool DirectedIntoTheHand(FingerContactEvent fingerContactEvent)
    {
        Vector3 fingerPos = fingerContactEvent.Finger.fingerTipCollider.transform.position;

        Vector3 toObject = fingerContactEvent.OtherCollider.bounds.center - fingerPos;
        Vector3 inwardDirection = fingerContactEvent.Finger.fingerTipCollider.transform
            .TransformDirection(fingerContactEvent.Finger.insidePalmDirection);

        return Vector3.Angle(inwardDirection, toObject) <= ANGLE_TO_THE_PALM;
    }

    private void OnInteractableFingerUntouch(FingerContactEvent fingerContactEvent)
    {
        if (!fingerContactEvent.Interactable) return;
        if (!DirectedIntoTheHand(fingerContactEvent)) return;

        scoreState.RemoveScore(fingerContactEvent.Interactable, fingerContactEvent.Finger.touchScore);
    }

    private bool TryGrasp()
    {
        var canditdate = scoreState.CurrentCandidate;

        if(canditdate != null && scoreState.TryGetScore(canditdate, out var candidateScore) 
            && candidateScore >= GRASP_SCORE_THRESHOLD)
        {
            if(grabber.TryGrab(canditdate))
            {
                OnGrap?.Invoke(canditdate);
                if (log) Debug.Log("Grasp condition was achived");
                return true;
            }
        }

        var selected = grabber.SelectedInteractable;

        if(selected != null)
        {
            if(!scoreState.TryGetScore(selected, out var selectedScore) || selectedScore < RELEASE_SCORE_THRESHOLD)
            {
                OnRelease?.Invoke(selected);
                grabber.Release();
            }
        }

        return false;
    }

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
}

[Serializable]
public class FingerTouchData
{
    public Collider fingerTipCollider;
    public float fingerCurlThreshold;

    public XRHandFingerID handFingerID;

    public int touchScore;

    public Vector3 insidePalmDirection;

    public bool isCurledEnough;
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
