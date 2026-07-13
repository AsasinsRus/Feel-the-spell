using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;

public class XRI_FFBClient : MonoBehaviour
{
    private XRI_FFBManager ffbManager;

    private void Awake()
    {
        ffbManager = FindFirstObjectByType<XRI_FFBManager>();
    }

    private void OnSelectEntered(SelectEnterEvent args)
    {
        Debug.Log("Received Interactable select event");
        
    }

    private void OnSelectExited(SelectExitEvent args)
    {
        
    }
}
