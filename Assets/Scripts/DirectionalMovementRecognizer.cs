using UnityEngine;

public class DirectionalMovementRecognizer : MonoBehaviour
{
    [SerializeField]
    private Movement[] movements;

    private void OnDrawGizmosSelected()
    {
        foreach(var movement in movements)
        {
            Vector3 endPoint = transform.position + GetDirectionalVector(movement.direction) * movement.distanceOrAngle;

            Gizmos.DrawLine(transform.position, endPoint);
            Gizmos.DrawSphere(endPoint, .3f);
        }
    }

    private Vector3 GetDirectionalVector(Direction direction)
    {
        switch(direction)
        {
            case Direction.X: return Vector3.right;
            case Direction.Y: return Vector3.up;
            case Direction.Z: return Vector3.forward;
        }

        return Vector3.zero;
    }
}


struct Movement
{
    public Direction direction;
    public MovementType type;

    public float distanceOrAngle;
    [Range(0f, 1f)]
    public float threshold;
}

enum Direction
{
    X, Y, Z
}

enum MovementType
{
    Rotation, PositionChange
}
