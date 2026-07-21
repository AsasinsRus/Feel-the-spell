using UnityEngine;

public class MicrophoneSelectorUI : MonoBehaviour {
    [Header("Hotkey")]
    [Tooltip("Held down together with Toggle Key to open/close the panel.")]
    [SerializeField] private KeyCode modifierKey = KeyCode.LeftControl;
    [Tooltip("Pressed while Modifier Key is held to open/close the panel.")]
    [SerializeField] private KeyCode toggleKey = KeyCode.M;

    [Header("UI Style")]
    [SerializeField] private int windowWidth = 340;
    [SerializeField] private int windowPadding = 20;
    [SerializeField] private int listHeight = 220;

    private bool isVisible;
    private string[] microphones = System.Array.Empty<string>();
    private Vector2 scrollPos;
    private Rect windowRect;
    private string statusMessage = "";
    private int windowId;

    private void Awake() {
        windowId = GetInstanceID();
        windowRect = new Rect(windowPadding, windowPadding, windowWidth, 360);
    }

    private void Update() {
        if (Input.GetKey(modifierKey) && Input.GetKeyDown(toggleKey))
            ToggleUI();

        // Also allow closing with Escape while open.
        if (isVisible && Input.GetKeyDown(KeyCode.Escape))
            isVisible = false;
    }

    private void ToggleUI() {
        isVisible = !isVisible;
        if (isVisible) {
            RefreshDeviceList();
            statusMessage = "";
        }
    }

    private void RefreshDeviceList() {
        microphones = PythonMicFinder.GetDevicesRawList();
    }

    private void OnGUI() {
        if (!isVisible) return;

        windowRect = GUI.Window(windowId, windowRect, DrawWindow, "Select Microphone");
    }

    private void DrawWindow(int id) {
        var client = AudioClient.Instance;
        string current = client != null ? client.DeviceName : "(AudioClient not found)";
        bool switching = client != null && client.IsSwitchingDevice;

        GUILayout.Space(5);
        GUILayout.Label($"Current device: {current}");
        if (switching)
            GUILayout.Label("Switching microphone, please wait...");

        GUILayout.Space(5);

        GUI.enabled = !switching;
        if (GUILayout.Button("Refresh list"))
            RefreshDeviceList();
        GUI.enabled = true;

        GUILayout.Space(5);

        scrollPos = GUILayout.BeginScrollView(scrollPos, GUILayout.Height(listHeight));

        if (microphones.Length == 0) {
            GUILayout.Label("No microphones found.");
        } else {
            foreach (var mic in microphones) {
                bool isCurrent = mic == current;
                GUI.enabled = !switching && !isCurrent;

                if (GUILayout.Button(isCurrent ? $"> {mic}  (active)" : mic))
                    SelectMicrophone(mic);

                GUI.enabled = true;
            }
        }

        GUILayout.EndScrollView();

        GUILayout.Space(5);

        if (!string.IsNullOrEmpty(statusMessage))
            GUILayout.Label(statusMessage);

        GUILayout.Space(5);

        if (GUILayout.Button("Close"))
            isVisible = false;

        GUI.DragWindow(new Rect(0, 0, windowWidth, 20));
    }

    private async void SelectMicrophone(string micName) {
        var client = AudioClient.Instance;
        if (client == null) {
            statusMessage = "AudioClient instance not found.";
            return;
        }

        statusMessage = $"Switching to {micName}...";

        try {
            await client.ChangeDeviceAsync(micName);
            statusMessage = $"Now using {micName}.";
        } catch (System.Exception e) {
            statusMessage = $"Failed to switch: {e.Message}";
            Debug.LogError(e);
        }
    }
}