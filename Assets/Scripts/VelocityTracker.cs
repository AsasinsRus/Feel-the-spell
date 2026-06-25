using UnityEngine;

public class VelocityTracker : MonoBehaviour
{
    public Vector3 Velocity { get; private set; }
    public Vector3 AngularVelocity { get; private set; }

    private Vector3 prevPos;
    private Quaternion prevRot;

    private void Update()
    {
        Velocity = (transform.position - prevPos) / Time.deltaTime;

        Quaternion deltaRot = transform.rotation * Quaternion.Inverse(prevRot);
        deltaRot.ToAngleAxis(out float angle, out Vector3 axis);
        AngularVelocity = axis * (angle * Mathf.Deg2Rad / Time.deltaTime);

        prevPos = transform.position;
        prevRot = transform.rotation;
    }
}
