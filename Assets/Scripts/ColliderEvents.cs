using System;
using UnityEngine;

public class ColliderEvents : MonoBehaviour
{
    public event Action<Collider, GameObject> OnTriggerEnter_;
    public event Action<Collider, GameObject> OnTriggerExit_;

    public event Action<Collision, GameObject> OnCollisionEnter_;
    public event Action<Collision, GameObject> OnCollisionExit_;

    void OnTriggerEnter(Collider other) => OnTriggerEnter_?.Invoke(other, gameObject);
    void OnTriggerExit(Collider other) => OnTriggerExit_?.Invoke(other, gameObject);

    void OnCollisionEnter(Collision other) => OnCollisionEnter_?.Invoke(other, gameObject);
    void OnCollisionExit(Collision other) => OnCollisionExit_?.Invoke(other, gameObject);
}
