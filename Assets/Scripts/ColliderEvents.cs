using System;
using UnityEngine;

public class ColliderEvents : MonoBehaviour
{
    public event Action<Collider> OnEnter;
    public event Action<Collider> OnExit;

    void OnTriggerEnter(Collider other) => OnEnter?.Invoke(other);
    void OnTriggerExit(Collider other) => OnExit?.Invoke(other);
}
