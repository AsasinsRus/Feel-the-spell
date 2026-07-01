using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using PDollarGestureRecognizer;
using System.IO;
using UnityEngine.Events;
using System;

public class MovementRecognizer : MonoBehaviour
{
    private const int MIN_POINTS_NUM_FOR_RECOGNITION = 2;
    [SerializeField]
    private Transform anchor;

    [SerializeField]
    private float newPositionThresholdDistance = 0.1f;
    [SerializeField, Range(.0f, 1f)]
    private float recognitionThreashold = 0.9f;
    public UnityEvent<string, Vector3[]> OnRecognition;
    public event Action<Vector3> OnAddPoint;
    public event Action OnRecognitionEnd;
    public event Action OnRecognitionStart;

    [SerializeField]
    private bool creationMode;
    [SerializeField]
    private string newGestureName = "Blank";
    [SerializeField]
    private List<Gesture> trainingSet = new();

    private List<Vector3> positionsList = new();

    private bool isRecognizing;

    private void Start()
    {
        string[] gestureFiles = Directory.GetFiles(Application.dataPath + "/Gestures/Movement Gestures/", "*.xml");

        foreach (string gestureFile in gestureFiles)
        {
            trainingSet.Add(GestureIO.ReadGestureFromFile(gestureFile));
        }
    }

    public void StartRecognition()
    {
        Debug.Log("Movement recognition started");

        isRecognizing = true;
        AddCurrentPoint();
        OnRecognitionStart?.Invoke();

        StartCoroutine(Recognition());
    }

    private void AddCurrentPoint()
    {
        positionsList.Add(anchor.position);
        OnAddPoint?.Invoke(anchor.position);
    }

    public void StopRecognition()
    {
        Debug.Log("Movement recognition stopped");

        isRecognizing = false;
        
        RecognizeGesture();
        OnRecognitionEnd?.Invoke();

        positionsList.Clear();
    }

    private void RecognizeGesture()
    {
        if (positionsList.Count < MIN_POINTS_NUM_FOR_RECOGNITION)
        {
            Debug.LogWarning("Not enough points to recognize gesture");
            return;
        }

        Point[] points = new Point[positionsList.Count];

        for (int i = 0; i < points.Length; i++)
        {
            Vector2 point = Camera.main.WorldToScreenPoint(positionsList[i]);

            points[i] = new Point(point.x, point.y, 0);
        }

        Gesture gesture = new Gesture(points);

        if (creationMode)
        {
            CreateGesture(points, gesture);
        }
        else
        {
            Result result = PointCloudRecognizer.Classify(gesture, trainingSet.ToArray());
            Debug.Log(result.GestureClass + " " + result.Score);

            if (result.Score >= recognitionThreashold)
                OnRecognition?.Invoke(result.GestureClass, positionsList.ToArray());
        }
    }

    private void CreateGesture(Point[] points, Gesture gesture)
    {
        gesture.Name = newGestureName;
        trainingSet.Add(gesture);

        string fileName = Application.dataPath + "/Gestures/Movement Gestures/" + newGestureName + ".xml";
        GestureIO.WriteGesture(points, newGestureName, fileName);
    }

    private IEnumerator Recognition()
    {
        Vector3 oldPos = anchor.position;

        while(isRecognizing)
        {
            if (Vector3.Distance(anchor.position, oldPos) >= newPositionThresholdDistance)
            {
                oldPos = anchor.position;
                AddCurrentPoint();
            }

            yield return null;
        }
    }
}
