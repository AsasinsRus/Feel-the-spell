using UnityEngine;
using UnityEngine.Events;

public class DestroyNotifier : MonoBehaviour
{
    public UnityEvent<GameObject> OnDestroy_;

    private void OnDestroy()
    {
        OnDestroy_?.Invoke(gameObject);
    }
}
