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
using static Unity.VisualScripting.Member;

public class HandGraspDetector : MonoBehaviour
{
    private const float ANGLE_TO_THE_PALM =  45f;
    
    private const int GRASP_SCORE_THRESHOLD = 10;
    private const int RELEASE_SCORE_THRESHOLD = 7;

    private const int CURL_SCORE = 1;

    [Header ("Hand info")]
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

    //private List<FingerTouchData> fingers = new();
    private Dictionary<GameObject, FingerTouchData> objToFingersData = new();

    [SerializeField]
    private Collider palm;

    [Header("Interactions")]
    [SerializeField]
    private XRDirectInteractor interactor;

    [SerializeField]
    public UnityEvent<XRGrabInteractable> OnGrap;
    [SerializeField]
    public UnityEvent<XRGrabInteractable> OnRelease;

    private XRGrabInteractable selectedInteractable;
    private XRGrabInteractable currentCandidate;

    private Dictionary<XRGrabInteractable, int> interactableScorings = new();
    private Dictionary<FingerTouchData, HashSet<XRGrabInteractable>> touchedByFinger = new();

    XRHandSubsystem handSubsystem;

    private void Awake()
    {
        if(thumbTip.fingerTipCollider == null)
        {
            Debug.LogError("Thumb was not added!");
            return;
        }

        //fingers.Add(thumbTip);
        //fingers.Add(indexTip);
        //fingers.Add(middleTip);
        //fingers.Add(ringTip);
        //fingers.Add(pinkyTip);

        objToFingersData.Add(thumbTip.fingerTipCollider.gameObject, thumbTip);
        objToFingersData.Add(indexTip.fingerTipCollider.gameObject, indexTip);
        objToFingersData.Add(middleTip.fingerTipCollider.gameObject, middleTip);
        objToFingersData.Add(ringTip.fingerTipCollider.gameObject, ringTip);
        objToFingersData.Add(pinkyTip.fingerTipCollider.gameObject, pinkyTip);

        if (objToFingersData.Values.Skip(1).All(m => m == null))
        {
            Debug.LogError("No fingers were detected!");
            return;
        }

        thumbTip.fingerTipCollider.GetOrAddComponent<ColliderEvents>();
        foreach(var finger in objToFingersData.Values)
        {
            if (finger == null) continue;

            var colliderEvents = finger.fingerTipCollider.GetOrAddComponent<ColliderEvents>();

            colliderEvents.OnTriggerEnter_ += OnInteractableFingerTouch;
            colliderEvents.OnTriggerExit_ += OnInteractableFingerUntouch;
        }

        handSubsystem = XRGeneralSettings.Instance?.Manager?.activeLoader?.GetLoadedSubsystem<XRHandSubsystem>();
    }

    private void Update()
    {
        if (handSubsystem == null) return;
        if(!CurrentHand.isTracked)
        {
            if(selectedInteractable != null)
                ReleaseSelected();

            return;
        }

        foreach(var fingerData in objToFingersData.Values)
        {
            if(IsFingerCurledEnough(fingerData) && !fingerData.isCurledEnough)
            {
                fingerData.isCurledEnough = true;
                foreach(var interactable in touchedByFinger[fingerData])
                {
                    AddScore(interactable, CURL_SCORE);
                }
            }
            else if(!IsFingerCurledEnough(fingerData) && fingerData.isCurledEnough)
            {
                fingerData.isCurledEnough = false;
                foreach (var interactable in touchedByFinger[fingerData])
                {
                    RemoveScore(interactable, CURL_SCORE);
                }
            }
        }
    }

    private bool IsFingerCurledEnough(FingerTouchData fingerTouchData)
    {
        if (HandCurlProvider.TryGetFingerCurl(CurrentHand, fingerTouchData.handFingerID, out float curl))
            return curl >= fingerTouchData.fingerCurlThreshold;

        return false;
    }

    private void OnInteractableFingerTouch(Collider other, GameObject source)
    {
        if (other.TryGetComponent<XRGrabInteractable>(out var interactable))
        {
            if (DirectedIntoTheHand(source, other))
            {
                touchedByFinger[objToFingersData[source]].Add(interactable);
                AddScore(interactable, objToFingersData[source].touchScore);
            }
        }
    }

    private bool DirectedIntoTheHand(GameObject source, Collider other)
    {
        Vector3 fingerPos = objToFingersData[source].fingerTipCollider.transform.position;

        Vector3 toObject = other.transform.position - fingerPos;
        Vector3 inwardDirection = objToFingersData[source].fingerTipCollider.transform.TransformDirection(objToFingersData[source].insidePalmDirection);

        return Vector3.Angle(inwardDirection, toObject) <= ANGLE_TO_THE_PALM;
    }

    private void OnInteractableFingerUntouch(Collider other, GameObject source)
    {
        if (other.TryGetComponent<XRGrabInteractable>(out var interactable))
        {
            if (DirectedIntoTheHand(source, other))
            {
                touchedByFinger[objToFingersData[source]].Remove(interactable);
                RemoveScore(interactable, objToFingersData[source].touchScore);
            }
        }
    }

    private void AddScore(XRGrabInteractable interactable, int score)
    {
        if (!interactableScorings.ContainsKey(interactable))
            interactableScorings[interactable] = 0;

        interactableScorings[interactable] += score;

        EstimateCandidates();
        TryGrasp();
    }

    private void RemoveScore(XRGrabInteractable interactable, int score)
    {
        if (!interactableScorings.ContainsKey(interactable))
            return;

        interactableScorings[interactable] -= score;

        if (interactableScorings[interactable] <= 0)
            interactableScorings.Remove(interactable);

        EstimateCandidates();
        TryGrasp();
    }

    private void EstimateCandidates()
    {
        if (interactableScorings.Count == 0) return;

        var maxScoring = interactableScorings.Max();

        currentCandidate = maxScoring.Key;
    }

    private bool TryGrasp()
    {
        if (currentCandidate != null && interactableScorings[currentCandidate] >= GRASP_SCORE_THRESHOLD)
        {
            GraspCurrentCandidate();

            return true;
        }
        else if(selectedInteractable != null && interactableScorings[selectedInteractable] < RELEASE_SCORE_THRESHOLD)
        {
            ReleaseSelected();
        }

        return false;
    }

    private void GraspCurrentCandidate()
    {
        OnGrap?.Invoke(currentCandidate);
        selectedInteractable = currentCandidate;
        currentCandidate = null;
    }

    private void ReleaseSelected()
    {
        OnRelease?.Invoke(selectedInteractable);
        selectedInteractable = null;
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
