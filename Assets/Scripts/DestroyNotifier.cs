using UnityEngine;
using UnityEngine.Events;

public class DestroyNotifier : MonoBehaviour
{
    public UnityEvent<GameObject> OnDestroy_ = new();

    private void OnDestroy()
    {
        OnDestroy_?.Invoke(gameObject);
    }
}
