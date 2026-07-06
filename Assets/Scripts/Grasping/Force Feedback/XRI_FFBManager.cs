using System;
using System.IO.Pipes;
using System.Runtime.InteropServices;
using Unity.VisualScripting;
using UnityEditor.EditorTools;
using UnityEngine;
using UnityEngine.XR;
using UnityEngine.XR.Hands;
using UnityEngine.XR.Hands.Gestures;
using UnityEngine.XR.Interaction.Toolkit.Interactables;
using UnityEngine.XR.Interaction.Toolkit.Interactors;

public class XRI_FFBManager : MonoBehaviour
{
    private XRI_FFBProvider ffbProviderLeft;
    private XRI_FFBProvider ffbProviderRight;

    [Tooltip("Whether to inject the FFBProvider script into all interactable game objects")]
    public bool injectFfbProvider = true;

    private void Awake()
    {
        ffbProviderLeft = new XRI_FFBProvider(Handedness.Left);
        ffbProviderRight = new XRI_FFBProvider(Handedness.Right);

        if(injectFfbProvider)
        {
            var interactables = FindObjectsByType<XRGrabInteractable>(FindObjectsSortMode.None);

            foreach(var interactable in interactables)
            {
                if(!interactable.TryGetComponent<XRI_FFBClient>(out _))
                    interactable.AddComponent<XRI_FFBClient>();
            }

            Debug.Log("Found: " + interactables.Length + " interactables with FFB");
        }
    }

    private void _SetForceFeedback(XRHand hand, XRI_VRFFBInput input)
    {
        if(hand.handedness == Handedness.Left)
        {
            ffbProviderLeft.SetFFB(input);
        }
        else
        {
            ffbProviderRight.SetFFB(input);
        }
    }

    public void SetForceFeedbackFromSkeleton(XRHand hand, bool[] trackFinger = null)
    {
        if(!hand.isTracked)
            return;

        short thumb, index, middle, ring, little;

        if(trackFinger == null || trackFinger.Length != 5)
        {
            thumb = CurlToForce(GetFingerCurl(hand, XRHandFingerID.Thumb));
            index = CurlToForce(GetFingerCurl(hand, XRHandFingerID.Index));
            middle = CurlToForce(GetFingerCurl(hand, XRHandFingerID.Middle));
            ring = CurlToForce(GetFingerCurl(hand, XRHandFingerID.Ring));
            little = CurlToForce(GetFingerCurl(hand, XRHandFingerID.Little));
        }
        else
        {
            thumb = trackFinger[0] ? CurlToForce(GetFingerCurl(hand, XRHandFingerID.Thumb)) : (short)0;
            index = trackFinger[1] ? CurlToForce(GetFingerCurl(hand, XRHandFingerID.Index)) : (short)0;
            middle = trackFinger[2] ? CurlToForce(GetFingerCurl(hand, XRHandFingerID.Middle)) : (short)0;
            ring = trackFinger[3] ? CurlToForce(GetFingerCurl(hand, XRHandFingerID.Ring)) : (short)0;
            little = trackFinger[4] ? CurlToForce(GetFingerCurl(hand, XRHandFingerID.Little)) : (short)0;
        }

        
        var input = new XRI_VRFFBInput(thumb, index, middle, ring, little);

        _SetForceFeedback(hand, input);
    }

    private float GetFingerCurl(XRHand hand, XRHandFingerID fingerID)
    {
        var shape = XRFingerShapeMath.CalculateFingerShape(
            hand,
            fingerID,
            XRFingerShapeTypes.FullCurl
        );

        return shape.TryGetFullCurl(out float curl) ? Mathf.Clamp01(curl) : 0f;
    }

    private short CurlToForce(float curl)
        => (short)Mathf.Clamp(Mathf.RoundToInt((1f - curl) * 1000), 0, 1000);
    public void RelaxForceFeedback(XRHand hand)
    {
        XRI_VRFFBInput input = new XRI_VRFFBInput(0, 0, 0, 0, 0);

        _SetForceFeedback(hand, input);
    }

    public void SetForceFeedbackByCurl(XRHand hand, XRI_VRFFBInput input)
    {
        _SetForceFeedback(hand, input);
    }

    private void Stop()
    {
        ffbProviderLeft.Close();
        ffbProviderRight.Close();
    }
    private void OnApplicationQuit()
    {
        Stop();
    }

    private void OnDestroy()
    {
        Stop();
    }
}

public struct XRI_VRFFBInput
{
    //Curl goes between 0-1000
    public XRI_VRFFBInput(short thumbCurl, short indexCurl, short middleCurl, short ringCurl, short pinkyCurl)
    {
        this.thumbCurl = thumbCurl;
        this.indexCurl = indexCurl;
        this.middleCurl = middleCurl;
        this.ringCurl = ringCurl;
        this.pinkyCurl = pinkyCurl;
    }
    public short thumbCurl;
    public short indexCurl;
    public short middleCurl;
    public short ringCurl;
    public short pinkyCurl;
};

class XRI_FFBProvider
{
    private XRI_NamedPipesProvider _namedPipeProvider;
    public Handedness controllerRole;
    
    public XRI_FFBProvider(Handedness controllerRole)
    {
        this.controllerRole = controllerRole;
        _namedPipeProvider = new XRI_NamedPipesProvider(controllerRole);
        
        _namedPipeProvider.Connect();
    }
   
    public bool SetFFB(XRI_VRFFBInput input)
    {
         return _namedPipeProvider.Send(input);
    }

    public void Close()
    {
        _namedPipeProvider.Disconnect();
    }
}

class XRI_NamedPipesProvider
{
    private NamedPipeClientStream _pipe;
    public XRI_NamedPipesProvider(Handedness handedness)
    {
        if(handedness == Handedness.Invalid)
            throw new Exception("Invalid hand");

        _pipe = new NamedPipeClientStream("vrapplication/ffb/curl/" + (handedness == Handedness.Right ? "right" : "left"));
    }

    public void Connect()
    {
        try
        {
            Debug.Log("Connecting to pipe");
            _pipe.Connect();
            Debug.Log("Successfully connected to pipe");
        }
        catch
        {
            Debug.Log("Unable to connect to pipe... Assuming that hand is inactive.");
        }
        
    }

    public void Disconnect()
    {
        if (_pipe.IsConnected)
        {
            _pipe.Dispose();
        }
    }

    public bool Send(XRI_VRFFBInput input)
    {
        if (_pipe.IsConnected)
        {
            Debug.Log("running task");
            int size = Marshal.SizeOf(input);
            byte[] arr = new byte[size];

            IntPtr ptr = Marshal.AllocHGlobal(size);
            Marshal.StructureToPtr(input, ptr, true);
            Marshal.Copy(ptr, arr, 0, size);
            Marshal.FreeHGlobal(ptr);

            _pipe.Write(arr, 0, size);

            Debug.Log("Sent force feedback message.");

            return true;
        }

        return false;
    }
}
