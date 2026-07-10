using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit.Inputs;
using UnityEngine.XR.OpenXR;

[RequireComponent(typeof(XRInputModalityManager))]
public class SteamVRInputModalityManager : MonoBehaviour
{
    private const string STEAMVR = "SteamVR/OpenXR";

    XRInputModalityManager modalityManager;

    void Start()
    {
        modalityManager = GetComponent<XRInputModalityManager>();
        UpdateHandsActive();
    }

    private void UpdateHandsActive()
    {
        if (OpenXRRuntime.name == STEAMVR)
        {
            modalityManager.enabled = false;

            modalityManager.rightHand.SetActive(true);
            modalityManager.leftHand.SetActive(true);

            modalityManager.rightController.SetActive(false);
            modalityManager.leftController.SetActive(false);
        }
        else
        {
            modalityManager.enabled = true;
        }
    }
}
