using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Events;

public class DirectionalMovementRecognizer : MonoBehaviour
{
    [SerializeField]
    public Movement[] movements;

    [SerializeField]
    public Transform objectToCheckOnMovement;

    [SerializeField]
    public Transform relativeTo;

    [SerializeField]
    public float recognitionTime;

    public bool recognize;

    [SerializeField]
    public UnityEvent OnMovementsRecognition;
    [SerializeField]
    public UnityEvent WhileMovementRecognition;

    private VelocityTracker velocityTracker;
    
    private Dictionary<Movement, Coroutine> activeMovementRecognitions = new();

    private void Awake()
    {
        objectToCheckOnMovement.TryGetComponent(out velocityTracker);
    }

    private void Update()
    {
        if (!recognize)
        {
            RecognitionReset();
            return;
        }

        bool anyInProgress = movements.Any(m => m.status == RecognitionStatus.IN_PROGRESS);

        if(!anyInProgress)
        {
            bool anyAborted = movements.Any(m => m.status == RecognitionStatus.ABORTED);
            bool allEnded = movements.All(m => m.status == RecognitionStatus.ENDED);

            if(allEnded && movements.Length > 0)
                OnMovementsRecognition?.Invoke();

            if(anyAborted || allEnded)
            {
                RecognitionReset();
            }
        }

        if (movements.Any(m => m.status != RecognitionStatus.NOT_STARTED)) return;

        foreach (var movement in movements)
        {
            switch(movement.type)
            {
                case MovementType.POSITION_CHANGE:
                    TryStartRecognition(movement, velocityTracker.Velocity.normalized);

                    break;
                case MovementType.ROTATION:
                    TryStartRecognition(movement, velocityTracker.AngularVelocity.normalized);

                    break;
            }
        }

    }

    private void RecognitionReset()
    {
        foreach (var activeRecognition in activeMovementRecognitions.Keys.ToList())
        {
            StopCoroutine(activeMovementRecognitions[activeRecognition]);
            activeMovementRecognitions.Remove(activeRecognition);
        }

        for (int i = 0; i < movements.Length; i++)
            movements[i].status = RecognitionStatus.NOT_STARTED;
    }

    private bool TryStartRecognition(Movement movement, Vector3 activeDirection)
    {
        if (SimilarDirection(movement, activeDirection))
        {
            if (!activeMovementRecognitions.ContainsKey(movement))
            {
                activeMovementRecognitions.Add(movement, StartCoroutine(Recognize(movement, velocityTracker)));

                return true;
            }
        }

        return false;
    }

    public bool SimilarDirection(Movement movement, Vector3 actualDirection)
    {
        if(actualDirection == Vector3.zero) return false;

        Vector3 expected = relativeTo != null
            ? relativeTo.TransformDirection(movement.direction.normalized)
            : movement.direction.normalized;

        float angle = Vector3.Angle(actualDirection, expected);
        float maxAngle = movement.threshold * 90f;

        return angle < maxAngle;
    }

    private IEnumerator Recognize(Movement movement, VelocityTracker velocityTracker)
    {
        Vector3 startingPos = objectToCheckOnMovement.transform.position;
        Quaternion startingRot = objectToCheckOnMovement.transform.rotation;

        movement.status = RecognitionStatus.IN_PROGRESS;

        float elapsed = 0f;

        while(elapsed <= recognitionTime)
        {
            elapsed += Time.deltaTime;

            switch (movement.type)
            {
                case MovementType.POSITION_CHANGE:
                    if (SimilarDirection(movement, velocityTracker.Velocity.normalized))
                    {
                        if (TryRecognize(movement, Vector3.Distance(startingPos, objectToCheckOnMovement.position)))
                        {
                            activeMovementRecognitions.Remove(movement);
                            yield break;
                        }
                    }
                    else
                    {
                        activeMovementRecognitions.Remove(movement);
                        movement.status = RecognitionStatus.ABORTED;
                        yield break;
                    }

                    break;
                case MovementType.ROTATION:
                    if (SimilarDirection(movement, velocityTracker.AngularVelocity.normalized))
                    {
                        if(TryRecognize(movement, Quaternion.Angle(startingRot, objectToCheckOnMovement.rotation)))
                        {
                            activeMovementRecognitions.Remove(movement);
                            yield break;
                        }
                    }
                    else
                    {
                        activeMovementRecognitions.Remove(movement);
                        movement.status = RecognitionStatus.ABORTED;
                        yield break;
                    }

                    break;
            }

            WhileMovementRecognition?.Invoke();

            yield return null;
        }

        activeMovementRecognitions.Remove(movement);
        movement.status = RecognitionStatus.ABORTED;
    }

    private bool TryRecognize(Movement movement, float currentValue)
    {
        if (currentValue >= Mathf.Abs(movement.distanceOrAngle))
        {
            movement.OnMovement?.Invoke();
            movement.status = RecognitionStatus.ENDED;

            return true;
        }

        return false;
    }

    private void OnDrawGizmosSelected()
    {
        if (relativeTo == null) return;

        foreach (var movement in movements)
        {
            switch(movement.type)
            {
                case MovementType.POSITION_CHANGE:
                    DrawPositionGizmo(movement);
                    break;
                case MovementType.ROTATION:
                    DrawRotationGizmo(movement);
                    break;
            }
        }
    }

    private void DrawPositionGizmo(Movement movement)
    {
        Vector3 origin = relativeTo.position;
        Vector3 expected = relativeTo.TransformDirection(movement.direction.normalized);
        float maxAngle = movement.threshold * 90f;
        float lineLength = Mathf.Abs(movement.distanceOrAngle);

        // center direction line
        Gizmos.color = Color.blue;
        Gizmos.DrawLine(origin, origin + expected * lineLength);
        Gizmos.DrawSphere(origin + expected * lineLength, 0.03f);

        // draw cone edges — rotate the direction around multiple axes to show the cone
        int segments = 12;

        Gizmos.color = new Color(1f, 1f, 0f, 0.4f);
        for (int i = 0; i < segments; i++)
        {
            float azimuth = (360f / segments) * i;

            // find a perpendicular to rotate around
            Vector3 perp = Vector3.Cross(expected, Vector3.up);
            if (perp.magnitude < 0.01f)
                perp = Vector3.Cross(expected, Vector3.right);

            // rotate perp around expected to get cone edge direction
            Vector3 edge = Quaternion.AngleAxis(azimuth, expected) *
                           Quaternion.AngleAxis(maxAngle, perp.normalized) * expected;

            Gizmos.DrawLine(origin, origin + edge * lineLength);
        }

        // draw a circle at the end of the cone
        Gizmos.color = Color.yellow;
        Vector3 circleCenter = origin + expected * lineLength;
        float circleRadius = lineLength * Mathf.Tan(maxAngle * Mathf.Deg2Rad);

        Vector3 right = Vector3.Cross(expected, Vector3.up).normalized;
        if (right.magnitude < 0.01f) right = Vector3.Cross(expected, Vector3.right).normalized;
        Vector3 up = Vector3.Cross(expected, right);
        DrawCircle(segments, circleCenter, circleRadius, right, up);
    }

    private static void DrawCircle(int segments, Vector3 circleCenter, float circleRadius, Vector3 right, Vector3 up)
    {
        for (int i = 0; i < segments; i++)
        {
            float a1 = (360f / segments) * i * Mathf.Deg2Rad;
            float a2 = (360f / segments) * (i + 1) * Mathf.Deg2Rad;

            Vector3 p1 = circleCenter + (right * Mathf.Cos(a1) + up * Mathf.Sin(a1)) * circleRadius;
            Vector3 p2 = circleCenter + (right * Mathf.Cos(a2) + up * Mathf.Sin(a2)) * circleRadius;

            Gizmos.DrawLine(p1, p2);
        }
    }

    private void DrawRotationGizmo(Movement movement)
    {
        Vector3 origin = relativeTo.position;
        Vector3 axis = relativeTo.TransformDirection(movement.direction.normalized);
        float maxAngle = movement.threshold * 90f;
        float requiredDegrees = Mathf.Abs(movement.distanceOrAngle);
        float axisLength = 0.3f;

        // spin axis line
        Gizmos.color = Color.blue;
        Gizmos.DrawLine(origin - axis * axisLength, origin + axis * axisLength);

        // deviation cone around the axis
        Gizmos.color = new Color(1f, 1f, 0f, 0.4f);
        int segments = 12;
        Vector3 perp = Vector3.Cross(axis, Vector3.up);
        if (perp.magnitude < 0.01f) perp = Vector3.Cross(axis, Vector3.right);

        for (int i = 0; i < segments; i++)
        {
            float azimuth = (360f / segments) * i;
            Vector3 edge = Quaternion.AngleAxis(azimuth, axis) *
                           Quaternion.AngleAxis(maxAngle, perp.normalized) * axis;
            Gizmos.DrawLine(origin, origin + edge * axisLength);
        }

        // arc showing required rotation amount
        // draw a circle perpendicular to the axis, then show the arc slice
        Gizmos.color = Color.green;
        float arcRadius = 0.15f;
        Vector3 arcRight = perp.normalized;
        Vector3 arcUp = Vector3.Cross(axis, arcRight);

        int arcSegments = 32;

        // arrow at end of arc
        Vector3 arcEnd = origin + (arcRight * Mathf.Cos(requiredDegrees * Mathf.Deg2Rad)
                                 + arcUp * Mathf.Sin(requiredDegrees * Mathf.Deg2Rad)) * arcRadius;
        Gizmos.DrawSphere(arcEnd, 0.015f);

        for (int i = 0; i < arcSegments; i++)
        {
            float a1 = (360f / arcSegments) * i * Mathf.Deg2Rad;
            float a2 = (360f / arcSegments) * (i + 1) * Mathf.Deg2Rad;

            Vector3 p1 = origin + (arcRight * Mathf.Cos(a1) + arcUp * Mathf.Sin(a1)) * arcRadius;
            Vector3 p2 = origin + (arcRight * Mathf.Cos(a2) + arcUp * Mathf.Sin(a2)) * arcRadius;

            Gizmos.DrawLine(p1, p2);

            if (Vector3.Distance(p2, arcEnd) <= Vector3.Distance(p1, p2))
            {
                Gizmos.DrawLine(p2, arcEnd);
                break;
            }
        }

        
    }
}

[Serializable]
public class Movement
{
    public Vector3 direction;
    public MovementType type;

    public float distanceOrAngle;
    [Range(0f, 1f)]
    public float threshold;

    public UnityEvent OnMovement;

    public RecognitionStatus status;
}

[Serializable]
public enum MovementType
{
    ROTATION, POSITION_CHANGE
}

[Serializable]
public enum RecognitionStatus
{
    NOT_STARTED, IN_PROGRESS, ENDED, ABORTED
}