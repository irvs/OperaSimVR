using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.XR;

/// <summary>Desktop pointing for the original controller and terrain interactions.</summary>
[DefaultExecutionOrder(-10)]
public class OperaSimVRDesktopInput : MonoBehaviour
{
    ControllerManager manager;
    ControllerLaser laser;
    bool diagnostics;
    float nextDiagnosticTime;
    bool screenshotRequested;

    [Serializable]
    class RuntimeStatus
    {
        public int frame;
        public float gameTime;
        public string mode;
        public Vector3 zx200Position;
        public Vector3 swingEuler;
        public Vector3 boomEuler;
        public Vector3 armEuler;
        public Vector3 bucketEuler;
        public Vector3 cameraPosition;
        public Vector3 cameraEuler;
    }

    public static bool IsActive
    {
        get
        {
#if UNITY_EDITOR_LINUX || (UNITY_STANDALONE_LINUX && !UNITY_EDITOR)
            return !OperaSimVRLinuxXR.IsRunning && !XRSettings.isDeviceActive;
#else
            return false;
#endif
        }
    }

    public static bool TryGetPointerRay(out Ray ray)
    {
        Camera camera = Camera.main;
        if (IsActive && camera != null)
        {
            ray = camera.ScreenPointToRay(Input.mousePosition);
            return true;
        }
        ray = default;
        return false;
    }

    void Start()
    {
        manager = FindObjectOfType<ControllerManager>();
        laser = FindObjectOfType<ControllerLaser>();
        diagnostics = Environment.GetEnvironmentVariable("OPERASIMVR_DIAGNOSTICS") == "1";
    }

    void Update()
    {
        if (diagnostics) ObserveRuntime();
        if (IsActive && manager != null && Input.GetKeyDown(KeyCode.F7))
        {
            GameObject selected = string.IsNullOrEmpty(manager.Machine_name) ? null : GameObject.Find(manager.Machine_name);
            if (selected != null)
            {
                DrivingCommandPublisher driving = selected.GetComponent<DrivingCommandPublisher>();
                JointCommandPublisher joints = selected.GetComponent<JointCommandPublisher>();
                bool enable = !(driving != null ? driving.key : joints != null && joints.key);
                if (driving != null) driving.key = enable;
                if (joints != null) joints.key = enable;
                if (driving != null || joints != null)
                    Debug.Log("[OperaSimVR Ubuntu] Keyboard command input " + (enable ? "enabled" : "disabled") + " for " + selected.name);
            }
        }
        if (!IsActive || !Input.GetMouseButtonDown(0) || manager == null || laser == null
            || manager.DesignateVehicleFromInspector || manager.GetOnMachine != ControllerManager.RideOption.GetOff)
            return;
        if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject()) return;
        Ray ray;
        if (!TryGetPointerRay(out ray)) return;
        RaycastHit[] hits = Physics.RaycastAll(ray, 1000f, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore);
        Array.Sort(hits, (a, b) => a.distance.CompareTo(b.distance));
        foreach (RaycastHit hit in hits)
        {
            Transform root = hit.transform.root;
            if (root == transform.root) continue;
            if (root.GetComponent<ModelIdentifier>() != null || root.GetComponent<SensorCameraImageSubscriber>() != null
                || root.GetComponent<PillarCameraNamespace>() != null)
            {
                Transform child = hit.transform;
                while (child.parent != null && child.parent != root) child = child.parent;
                laser.parentObjectName = root.name;
                laser.GetOnVehicle = root.name;
                laser.OneBeforeRootObjectName = child.name;
            }
            // Do not select a machine through another solid object.
            break;
        }
    }

    void ObserveRuntime()
    {
        if (Time.unscaledTime < nextDiagnosticTime) return;
        nextDiagnosticTime = Time.unscaledTime + 10f;
        var status = new RuntimeStatus { frame = Time.frameCount, gameTime = Time.time };
        Camera camera = Camera.main;
        if (camera != null)
        {
            status.cameraPosition = camera.transform.position;
            status.cameraEuler = camera.transform.eulerAngles;
        }
        ModeSelector mode = FindObjectOfType<ModeSelector>();
        if (mode != null) status.mode = mode.WhichMode.ToString();
        GameObject machine = GameObject.Find("zx200");
        if (machine != null)
        {
            status.zx200Position = machine.transform.position;
            Transform swing = machine.transform.Find("base_link/body_link");
            Transform boom = machine.transform.Find("base_link/body_link/boom_link");
            Transform arm = machine.transform.Find("base_link/body_link/boom_link/arm_link");
            Transform bucket = machine.transform.Find("base_link/body_link/boom_link/arm_link/bucket_link");
            if (swing != null) status.swingEuler = swing.localEulerAngles;
            if (boom != null) status.boomEuler = boom.localEulerAngles;
            if (arm != null) status.armEuler = arm.localEulerAngles;
            if (bucket != null) status.bucketEuler = bucket.localEulerAngles;
        }
        Debug.Log("[OperaSimVR Ubuntu runtime] " + JsonUtility.ToJson(status));
        string screenshot = Environment.GetEnvironmentVariable("OPERASIMVR_SCREENSHOT");
        if (!screenshotRequested && Time.frameCount > 5 && !string.IsNullOrEmpty(screenshot))
        {
            ScreenCapture.CaptureScreenshot(screenshot);
            screenshotRequested = true;
        }
    }
}
