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
    private const int RELEASE_SCORE_THRESHOLD = 7;

    private const int CURL_SCORE = 1;
    private const int PALM_SCORE = 2;
    private const int THUMB_OPPOSED_SCORE = 2;

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
    private Transform attachPoint;

    //private List<FingerTouchData> fingers = new();
    private Dictionary<GameObject, FingerTouchData> objToFingersData = new();

    [SerializeField]
    private Collider palm;
    [SerializeField]
    private float closeToPalmThreshold;
    private HashSet<XRGrabInteractable> closeToPalm = new();

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

    private XRHandSubsystem handSubsystem;

    private HashSet<XRGrabInteractable> thumbOpposedFinger = new();
    private HashSet<XRGrabInteractable> registeredOnDestruction = new();

    [SerializeField]
    private bool log = false;

    private void Awake()
    {
        if (!FingersSetup()) return;

        attachPoint = Instantiate((new GameObject()).transform, hand);
        attachPoint.name = "Attach point";

        interactor.attachTransform = attachPoint;

        handSubsystem = XRGeneralSettings.Instance?.Manager?.activeLoader?.GetLoadedSubsystem<XRHandSubsystem>();
    }

    private bool FingersSetup()
    {
        if (thumbTip.fingerTipCollider == null)
        {
            Debug.LogError("Thumb was not added!");
            return false;
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

        if (objToFingersData.Values.Skip(1).All(m => m == null || m.fingerTipCollider == null))
        {
            Debug.LogError("No fingers were detected!");
            return false;
        }

        thumbTip.fingerTipCollider.GetOrAddComponent<ColliderEvents>();
        foreach (var finger in objToFingersData.Values)
        {
            if (finger == null) continue;

            var colliderEvents = finger.fingerTipCollider.GetOrAddComponent<ColliderEvents>();

            colliderEvents.OnTriggerEnter_ += OnInteractableFingerTouch;
            colliderEvents.OnTriggerExit_ += OnInteractableFingerUntouch;

            touchedByFinger.Add(finger, new());
        }

        return true;
    }

    private void Update()
    {
        if (handSubsystem == null) return;
        if (!CurrentHand.isTracked)
        {
            if (selectedInteractable != null)
                ReleaseSelected();

            return;
        }

        CurlCheck();
        PalmCheck();
        ThumbOpposedToFingerCheck();
    }

    private void CurlCheck()
    {
        foreach (var fingerData in objToFingersData.Values)
        {
            if (fingerData == null) continue;

            if (IsFingerCurledEnough(fingerData) && !fingerData.isCurledEnough)
            {
                fingerData.isCurledEnough = true;
                foreach (var interactable in touchedByFinger[fingerData])
                {
                    if (log) Debug.Log(fingerData.handFingerID + " is curled enough");
                    
                    AddScore(interactable, CURL_SCORE);
                }
            }
            else if (!IsFingerCurledEnough(fingerData) && fingerData.isCurledEnough)
            {
                fingerData.isCurledEnough = false;
                foreach (var interactable in touchedByFinger[fingerData])
                {
                    RemoveScore(interactable, CURL_SCORE);
                }
            }
        }
    }

    private void PalmCheck()
    {
        foreach (var interactable in touchedByFinger[thumbTip])
        {
            if (!TryGetInteractableCenter(interactable, out var interactableCenter))
            {
                closeToPalm.Remove(interactable);
                continue;
            }

            Vector3 palmCenter = palm.bounds.center;

            if (Vector3.Distance(interactableCenter, palmCenter) <= closeToPalmThreshold
                && !closeToPalm.Contains(interactable))
            {
                if (log) Debug.Log("Palm is close enough to " + interactable.name);

                closeToPalm.Add(interactable);
                AddScore(interactable, PALM_SCORE);
            }
            else if (Vector3.Distance(interactableCenter, palmCenter) > closeToPalmThreshold
                && closeToPalm.Contains(interactable))
            {
                closeToPalm.Remove(interactable);
                RemoveScore(interactable, PALM_SCORE);
            }
        }
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
        if (thumbTip == null) return;

        foreach (var interactable in touchedByFinger[thumbTip])
        {
            if(!TryGetInteractableCenter(interactable, out var interactableCenter))
            {
                thumbOpposedFinger.Remove(interactable);
                continue;
            }

            foreach (var finger in objToFingersData.Values.Skip(1))
            {
                if (finger == null) continue;

                foreach (var fingerInteractable in touchedByFinger[finger])
                {
                    if (interactable == fingerInteractable)
                    {
                        Vector3 interactableToThumb = interactableCenter - interactable.GetComponentInChildren<Collider>().bounds.center;
                        Vector3 interactableToFinger = finger.fingerTipCollider.bounds.center - interactable.GetComponentInChildren<Collider>().bounds.center;

                        if (Vector3.Angle(interactableToThumb, interactableToFinger) >= THUMB_OPPOSITION_ANGLE
                            && !thumbOpposedFinger.Contains(interactable))
                        {
                            if (log) Debug.Log("Thumb is opposed enough to " + finger.handFingerID + " for " + interactable.name);

                            thumbOpposedFinger.Add(interactable);
                            AddScore(interactable, THUMB_OPPOSED_SCORE);
                        }
                        else if (Vector3.Angle(interactableToThumb, interactableToFinger) < THUMB_OPPOSITION_ANGLE
                            && thumbOpposedFinger.Contains(interactable))
                        {
                            thumbOpposedFinger.Remove(interactable);
                            RemoveScore(interactable, THUMB_OPPOSED_SCORE);
                        }
                    }
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
        var interactable = other.GetComponentInParent<XRGrabInteractable>();

        if (!interactable) return;

        if (DirectedIntoTheHand(source, other))
        {
            if (!objToFingersData.ContainsKey(source)) return;

            if (log) Debug.Log(objToFingersData[source].handFingerID + " is touching " + other.name + " from the correct side");

            var finger = objToFingersData[source];

            if (touchedByFinger[finger].Add(interactable))
            {
                AddScore(interactable, objToFingersData[source].touchScore);

                if (registeredOnDestruction.Add(interactable))
                {
                    if (!interactable.TryGetComponent<DestroyNotifier>(out var destroyNotifier))
                    {
                        destroyNotifier = interactable.AddComponent<DestroyNotifier>();
                    }
                    destroyNotifier.OnDestroy_.RemoveListener(OnInteractableDestroy);
                    destroyNotifier.OnDestroy_.AddListener(OnInteractableDestroy);
                }
            }
        }
    }

    private void OnInteractableDestroy(GameObject gameObject)
    {
        var interactable = gameObject.GetComponent<XRGrabInteractable>();

        if (interactable == null) return;

        registeredOnDestruction.Remove(interactable);

        if (selectedInteractable == interactable)
        {
            if (interactor)
                interactor.EndManualInteraction();

            selectedInteractable = null;
        }
        if (currentCandidate == interactable) currentCandidate = null;
        
        interactableScorings.Remove(interactable);
        closeToPalm.Remove(interactable);
        thumbOpposedFinger.Remove(interactable);

        foreach (var finger in touchedByFinger.Keys.ToList())
            touchedByFinger[finger].Remove(interactable);
    }

    private bool DirectedIntoTheHand(GameObject source, Collider other)
    {
        Vector3 fingerPos = objToFingersData[source].fingerTipCollider.transform.position;

        Vector3 toObject = other.bounds.center - fingerPos;
        Vector3 inwardDirection = objToFingersData[source].fingerTipCollider.transform.TransformDirection(objToFingersData[source].insidePalmDirection);

        return Vector3.Angle(inwardDirection, toObject) <= ANGLE_TO_THE_PALM;
    }

    private void OnInteractableFingerUntouch(Collider other, GameObject source)
    {
        var interactable = other.GetComponentInParent<XRGrabInteractable>();

        if (!interactable) return;

        if (DirectedIntoTheHand(source, other))
        {
            var finger = objToFingersData[source];

            if (touchedByFinger[finger].Remove(interactable))
            {
                RemoveScore(interactable, objToFingersData[source].touchScore);

                if (interactable.TryGetComponent<DestroyNotifier>(out var destroyNotifier)
                    && touchedByFinger.All(m => !m.Value.Contains(interactable)))
                {
                    destroyNotifier.OnDestroy_.RemoveListener(OnInteractableDestroy);
                }
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
        if (interactableScorings.Count == 0)
        {
            currentCandidate = null;
            return;
        }

        var maxScoring = interactableScorings.Aggregate(
            (best, next) =>
                next.Value > best.Value
                ? next
                : best
            );  

        currentCandidate = maxScoring.Key;

        if (log) Debug.Log("Current candidate : " + currentCandidate.name + " has score of " + maxScoring.Value);
    }

    private bool TryGrasp()
    {
        if (currentCandidate != null && currentCandidate != selectedInteractable
            && interactableScorings.TryGetValue(currentCandidate, out var currentScore) 
            && currentScore >= GRASP_SCORE_THRESHOLD)
        {
            if (log) Debug.Log("Grasp condition was achived");

            GraspCurrentCandidate();

            return true;
        }
        else if (selectedInteractable != null)
        {
            if(!interactableScorings.TryGetValue(selectedInteractable, out var selectedScore) 
                || selectedScore < RELEASE_SCORE_THRESHOLD)
            {
                ReleaseSelected();
            }
        }

        return false;
    }

    private void GraspCurrentCandidate()
    {
        if (!interactor || !currentCandidate) return;

        if (selectedInteractable) ReleaseSelected();

        attachPoint.position = currentCandidate.transform.position;
        interactor.StartManualInteraction((IXRSelectInteractable)currentCandidate);

        OnGrap?.Invoke(currentCandidate);
        selectedInteractable = currentCandidate;
        currentCandidate = null;
    }

    private void ReleaseSelected()
    {
        if (!interactor || !selectedInteractable) return;

        OnRelease?.Invoke(selectedInteractable);
        interactor.EndManualInteraction();
        selectedInteractable = null;
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
