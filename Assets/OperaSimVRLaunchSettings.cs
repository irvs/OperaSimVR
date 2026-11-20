using System;
using Unity.Robotics.ROSTCPConnector;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>Optional launch overrides; an ordinary launch keeps the scene's settings.</summary>
[Serializable]
public class OperaSimVRLaunchSettings
{
    public const string SessionKey = "OperaSimVRUbuntu.LaunchSettings";
    public string mode = "scene";
    public string rosIp = "";
    public int rosPort;

    public static OperaSimVRLaunchSettings FromEnvironment()
    {
        var settings = new OperaSimVRLaunchSettings();
        settings.mode = Environment.GetEnvironmentVariable("OPERASIMVR_MODE") ?? "scene";
        settings.rosIp = Environment.GetEnvironmentVariable("OPERASIMVR_ROS_IP") ?? "";
        string port = Environment.GetEnvironmentVariable("OPERASIMVR_ROS_PORT");
        if (!string.IsNullOrEmpty(port) && !int.TryParse(port, out settings.rosPort))
            throw new ArgumentException("OPERASIMVR_ROS_PORT must be an integer between 1 and 65535.");
        settings.Validate();
        if (!string.IsNullOrEmpty(port) && settings.rosPort == 0)
            throw new ArgumentException("OPERASIMVR_ROS_PORT must be between 1 and 65535.");
        return settings;
    }

    public void Validate()
    {
        switch (mode)
        {
            case "scene": case "viewer": case "simulator": case "preview":
            case "preview-and-play": case "preview-ar": break;
            default: throw new ArgumentException("Unknown OperaSimVR mode: " + mode);
        }
        if (rosPort < 0 || rosPort > 65535)
            throw new ArgumentException("ROS TCP port must be between 1 and 65535.");
    }

    public void Apply()
    {
        Validate();
        ModeSelector selector = UnityEngine.Object.FindObjectOfType<ModeSelector>();
        if (mode != "scene")
        {
            if (selector == null)
                throw new InvalidOperationException("This scene has no ModeSelector.");
            switch (mode)
            {
                case "viewer": selector.WhichMode = ModeSelector.ModeOption.PlayMode; break;
                case "simulator": selector.WhichMode = ModeSelector.ModeOption.NormalModeSimulator; break;
                case "preview": selector.WhichMode = ModeSelector.ModeOption.PreviewModeForTeleop; break;
                case "preview-and-play": selector.WhichMode = ModeSelector.ModeOption.PreviewAndPlay; break;
                case "preview-ar": selector.WhichMode = ModeSelector.ModeOption.PreviewAR; break;
            }
        }
        if (mode == "viewer")
        {
            GameObject zx200 = GameObject.Find("zx200");
            PoseSubscriber pose = zx200 == null ? null : zx200.GetComponent<PoseSubscriber>();
            JointSubscriber joints = zx200 == null ? null : zx200.GetComponent<JointSubscriber>();
            if (pose == null || joints == null)
                throw new InvalidOperationException("The ZX200 viewer requires its original pose and joint subscribers.");
            pose.PoseSubscribeTopicName = "/zx200/base_link/pose";
            pose.ChengePositionSW = true;
            pose.ViaDB = false;
            pose.PoseMsgType = PoseSubscriber.PoseMessageType.PoseStampedMsg;
            pose.Coordinate = PoseSubscriber.CoordinateSelect.ROS;
            joints.JointSubscribeTopicName = "/zx200/joint_state";
            joints.JointChengeSw = true;
            joints.ViaDB = false;
        }
        if (mode == "viewer" || !string.IsNullOrEmpty(rosIp) || rosPort != 0)
        {
            ROSConnection ros = ROSConnection.GetOrCreateInstance();
            if (!string.IsNullOrEmpty(rosIp)) ros.RosIPAddress = rosIp;
            else if (mode == "viewer") ros.RosIPAddress = "127.0.0.1";
            if (rosPort != 0) ros.RosPort = rosPort;
            else if (mode == "viewer") ros.RosPort = 10001;
            ros.ConnectOnStart = true;
        }
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    static void Initialize()
    {
#if UNITY_EDITOR_LINUX || (UNITY_STANDALONE_LINUX && !UNITY_EDITOR)
        SceneManager.sceneLoaded -= ConfigureScene;
        SceneManager.sceneLoaded += ConfigureScene;
#endif
    }

    static void ConfigureScene(Scene scene, LoadSceneMode loadMode)
    {
        // Only the main simulation scene has these modes and machine subscribers.
        if (scene.path != "Assets/Scenes/SampleScene.unity") return;
        OperaSimVRLaunchSettings settings = null;
#if UNITY_EDITOR
        string json = UnityEditor.SessionState.GetString(SessionKey, "");
        if (!string.IsNullOrEmpty(json)) settings = JsonUtility.FromJson<OperaSimVRLaunchSettings>(json);
#endif
        settings = settings ?? FromEnvironment();
        settings.Apply();
        GameObject player = GameObject.Find("OVRPlayerController");
        if (player != null && player.GetComponent<OperaSimVRDesktopInput>() == null)
            player.AddComponent<OperaSimVRDesktopInput>();
#if UNITY_STANDALONE_LINUX && !UNITY_EDITOR
        if (settings.mode == "viewer" && player != null && player.GetComponent<OperaSimVRViewerCamera>() == null)
            player.AddComponent<OperaSimVRViewerCamera>();
#endif
        Debug.Log("[OperaSimVR Ubuntu] Graphics: " + SystemInfo.graphicsDeviceName + "; "
            + SystemInfo.graphicsDeviceVersion + "; Compute shaders: " + SystemInfo.supportsComputeShaders);
        if (!SystemInfo.supportsComputeShaders && SystemInfo.graphicsDeviceType != UnityEngine.Rendering.GraphicsDeviceType.Null)
            Debug.LogError("[OperaSimVR Ubuntu] Terrain excavation and erosion require Compute Shader support. "
                + "Use a compatible GPU driver or relaunch with --software-rendering.");
    }
}
