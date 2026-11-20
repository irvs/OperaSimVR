using System;
using System.IO;
using RosMessageTypes.Geometry;
using RosMessageTypes.Sensor;
using Unity.Robotics.ROSTCPConnector;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

/// <summary>Editor-only setup and observation of the original OperaSimVR scene.</summary>
[InitializeOnLoad]
public static class OperaSimVRUbuntu
{
    const string ScenePath = "Assets/Scenes/SampleScene.unity";
    const string PoseTopic = "/zx200/base_link/pose";
    const string JointTopic = "/zx200/joint_state";
    const string ActiveKey = "OperaSimVRUbuntu.Active";
    const string StateKey = "OperaSimVRUbuntu.State";
    const double StatusInterval = 10.0;

    [Serializable]
    class ViewerStatus
    {
        public string utc;
        public bool isPlaying;
        public bool isPaused;
        public string mode;
        public string scene;
        public int frameCount;
        public float gameTime;
        public double observedFramesPerSecond;
        public string endpoint;
        public string poseTopic = PoseTopic;
        public string jointTopic = JointTopic;
        public long poseMessages;
        public long jointMessages;
        public string lastPoseUtc;
        public string lastJointUtc;
        public double lastPoseAgeSeconds = -1;
        public double lastJointAgeSeconds = -1;
        public Vector3 latestRosPosition;
        public Quaternion latestRosOrientation;
        public string[] latestJointNames = new string[0];
        public double[] latestRosJointPositions = new double[0];
        public bool zx200Found;
        public Vector3 unityZX200Position;
        public Quaternion unityZX200Rotation;
        public Vector3 unityZX200Euler;
        public bool fourJointTransformsFound;
        public Vector3 swingLocalEuler;
        public Vector3 boomLocalEuler;
        public Vector3 armLocalEuler;
        public Vector3 bucketLocalEuler;
        public double[] JointPositions = new double[0];
        public bool initialPoseFramed;
        public bool originalCameraFound;
        public Vector3 originalCameraPosition;
        public Vector3 originalCameraEuler;
        public string graphicsDeviceName;
        public string graphicsDeviceType;
        public bool supportsComputeShaders;
        public int terrainHeightmapResolution;
        public int terrainTiles;
        public int errorCount;
        public string lastError;
        public string screenshotRequestedUtc;
    }

    static ViewerStatus s_Status = new ViewerStatus();
    static ROSConnection s_ObservedConnection;
    static bool s_CallbacksRegistered;
    static double s_NextStatusTime;
    static double s_LastStatusTime;
    static int s_LastStatusFrame;
    static int s_ReframeAfterFrame = -1;

    static string ProjectRoot { get { return Path.GetDirectoryName(Application.dataPath); } }
    static string CommandPath { get { return Path.Combine(ProjectRoot, "Temp", "OperaSimVRUbuntu.command"); } }
    static string StatusPath { get { return Path.Combine(ProjectRoot, "Logs", "ubuntu-unity-status.json"); } }

    static OperaSimVRUbuntu()
    {
        string saved = SessionState.GetString(StateKey, "");
        if (!string.IsNullOrEmpty(saved))
        {
            try { s_Status = JsonUtility.FromJson<ViewerStatus>(saved) ?? new ViewerStatus(); }
            catch (ArgumentException) { s_Status = new ViewerStatus(); }
        }
        EditorApplication.update += Update;
        EditorApplication.playModeStateChanged += OnPlayModeChanged;
        AssemblyReloadEvents.beforeAssemblyReload += SaveSession;
        Application.logMessageReceived += CountError;
        s_NextStatusTime = EditorApplication.timeSinceStartup;
    }

    [MenuItem("OperaSimVR/Ubuntu/Start scene with current settings")]
    public static void StartConfigured()
    {
        StartScene(OperaSimVRLaunchSettings.FromEnvironment());
    }

    [MenuItem("OperaSimVR/Ubuntu/Start ZX200 viewer")]
    public static void StartViewer()
    {
        StartScene(new OperaSimVRLaunchSettings { mode = "viewer" });
    }

    static void StartScene(OperaSimVRLaunchSettings settings)
    {
        settings.Validate();
        if (EditorApplication.isPlayingOrWillChangePlaymode)
        {
            Debug.LogWarning("[OperaSimVR Ubuntu] Stop Play Mode before starting the viewer again.");
            return;
        }
        if (EditorApplication.isCompiling || EditorApplication.isUpdating)
        {
            Debug.LogWarning("[OperaSimVR Ubuntu] Wait for compilation and import before starting the viewer.");
            return;
        }

        if (UnityEngine.SceneManagement.SceneManager.GetActiveScene().path != ScenePath)
        {
            if (!Application.isBatchMode && !EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
                return;
            EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
        }
        SessionState.SetString(OperaSimVRLaunchSettings.SessionKey, JsonUtility.ToJson(settings));

        s_Status = new ViewerStatus();
        s_ObservedConnection = null;
        s_CallbacksRegistered = false;
        s_ReframeAfterFrame = -1;
        s_LastStatusTime = 0;
        s_LastStatusFrame = 0;
        SessionState.SetBool(ActiveKey, true);
        SaveSession();
        FocusGameView();
        Debug.Log("[OperaSimVR Ubuntu] Starting the original SampleScene; scene assets have not been saved.");
        EditorApplication.isPlaying = true;
    }

    static void Update()
    {
        if (EditorApplication.isCompiling || EditorApplication.isUpdating)
            return;
        ConsumeCommand();
        if (!SessionState.GetBool(ActiveKey, false) || !EditorApplication.isPlaying)
            return;

        RegisterObservationCallbacks();
        if (IsViewer() && !s_Status.initialPoseFramed && s_Status.poseMessages > 0)
        {
            if (s_ReframeAfterFrame < 0)
                s_ReframeAfterFrame = Time.frameCount + 3;
            if (Time.frameCount >= s_ReframeAfterFrame)
                FrameOriginalCamera();
        }
        if (EditorApplication.timeSinceStartup >= s_NextStatusTime)
        {
            s_NextStatusTime = EditorApplication.timeSinceStartup + StatusInterval;
            WriteStatus();
        }
    }

    static void ConsumeCommand()
    {
        if (!File.Exists(CommandPath))
            return;
        try
        {
            string command = File.ReadAllText(CommandPath).Trim();
            File.Delete(CommandPath);
            if (command.StartsWith("{"))
            {
                StartScene(JsonUtility.FromJson<OperaSimVRLaunchSettings>(command));
                return;
            }
            switch (command.ToLowerInvariant())
            {
                case "start": StartConfigured(); break;
                case "snapshot": CaptureSnapshot(); break;
                case "stop":
                    WriteStatus();
                    EditorApplication.isPlaying = false;
                    break;
                case "quit": EditorApplication.Exit(0); break;
                default: Debug.LogWarning("[OperaSimVR Ubuntu] Unknown command: " + command); break;
            }
        }
        catch (Exception exception)
        {
            Debug.LogException(exception);
        }
    }

    static void OnPlayModeChanged(PlayModeStateChange state)
    {
        if (state == PlayModeStateChange.EnteredPlayMode && SessionState.GetBool(ActiveKey, false))
        {
            s_NextStatusTime = EditorApplication.timeSinceStartup;
            s_LastStatusTime = 0;
            s_LastStatusFrame = Time.frameCount;
            RegisterObservationCallbacks();
            FocusGameView();
        }
        else if (state == PlayModeStateChange.ExitingPlayMode && SessionState.GetBool(ActiveKey, false))
        {
            WriteStatus();
            SaveSession();
        }
        else if (state == PlayModeStateChange.EnteredEditMode)
        {
            bool wasActive = SessionState.GetBool(ActiveKey, false);
            SessionState.SetBool(ActiveKey, false);
            SessionState.EraseString(OperaSimVRLaunchSettings.SessionKey);
            s_ObservedConnection = null;
            s_CallbacksRegistered = false;
            if (wasActive)
                WriteStatus();
        }
    }

    static void RegisterObservationCallbacks()
    {
        if (!IsViewer()) return;
        ROSConnection ros = ROSConnection.GetOrCreateInstance();
        if (s_CallbacksRegistered && s_ObservedConnection == ros)
            return;
        ros.Subscribe<PoseStampedMsg>(PoseTopic, ObservePose);
        ros.Subscribe<JointStateMsg>(JointTopic, ObserveJoints);
        s_ObservedConnection = ros;
        s_CallbacksRegistered = true;
    }

    static bool IsViewer()
    {
        string json = SessionState.GetString(OperaSimVRLaunchSettings.SessionKey, "");
        return !string.IsNullOrEmpty(json) && JsonUtility.FromJson<OperaSimVRLaunchSettings>(json).mode == "viewer";
    }

    static void ObservePose(PoseStampedMsg message)
    {
        s_Status.poseMessages++;
        s_Status.lastPoseUtc = DateTime.UtcNow.ToString("O");
        s_Status.latestRosPosition = new Vector3((float)message.pose.position.x, (float)message.pose.position.y, (float)message.pose.position.z);
        s_Status.latestRosOrientation = new Quaternion((float)message.pose.orientation.x, (float)message.pose.orientation.y, (float)message.pose.orientation.z, (float)message.pose.orientation.w);
    }

    static void ObserveJoints(JointStateMsg message)
    {
        s_Status.jointMessages++;
        s_Status.lastJointUtc = DateTime.UtcNow.ToString("O");
        s_Status.latestJointNames = message.name == null ? new string[0] : (string[])message.name.Clone();
        s_Status.latestRosJointPositions = message.position == null ? new double[0] : (double[])message.position.Clone();
    }

    static Camera OriginalCamera(GameObject player, out OVRCameraRig rig)
    {
        rig = player == null ? null : player.GetComponentInChildren<OVRCameraRig>(true);
        if (rig != null && rig.centerEyeAnchor != null)
            return rig.centerEyeAnchor.GetComponent<Camera>();
        return player == null ? null : player.GetComponentInChildren<Camera>(true);
    }

    static void FrameOriginalCamera()
    {
        GameObject zx200 = GameObject.Find("zx200");
        GameObject player = GameObject.Find("OVRPlayerController");
        OVRCameraRig rig;
        Camera camera = OriginalCamera(player, out rig);
        if (zx200 == null || player == null || rig == null || camera == null)
            return;

        Vector3 target = zx200.transform.position + Vector3.up * 2.0f;
        Vector3 viewpoint = zx200.transform.position + new Vector3(12, 7, 16);
        Vector3 direction = target - viewpoint;
        float yaw = Mathf.Atan2(direction.x, direction.z) * Mathf.Rad2Deg;
        float pitch = -Mathf.Atan2(direction.y, new Vector2(direction.x, direction.z).magnitude) * Mathf.Rad2Deg;
        player.transform.rotation = Quaternion.Euler(0, yaw, 0);
        rig.transform.localRotation = Quaternion.Euler(pitch, 0, 0);
        player.transform.position += viewpoint - camera.transform.position;
        s_Status.initialPoseFramed = true;
        Debug.Log("[OperaSimVR Ubuntu] Positioned the original OVR camera beside the received ZX200 pose.");
        SaveSession();
    }

    static void CaptureSnapshot()
    {
        Directory.CreateDirectory(Path.GetDirectoryName(StatusPath));
        if (EditorApplication.isPlaying)
        {
            FocusGameView();
            ScreenCapture.CaptureScreenshot(Path.Combine(ProjectRoot, "Logs", "ubuntu-unity-game.png"));
            s_Status.screenshotRequestedUtc = DateTime.UtcNow.ToString("O");
        }
        WriteStatus();
    }

    static void WriteStatus()
    {
        try
        {
            s_Status.utc = DateTime.UtcNow.ToString("O");
            s_Status.isPlaying = EditorApplication.isPlaying;
            s_Status.isPaused = EditorApplication.isPaused;
            s_Status.scene = UnityEngine.SceneManagement.SceneManager.GetActiveScene().path;
            ModeSelector mode = UnityEngine.Object.FindObjectOfType<ModeSelector>();
            s_Status.mode = mode == null ? "" : mode.WhichMode.ToString();
            ROSConnection connection = UnityEngine.Object.FindObjectOfType<ROSConnection>();
            s_Status.endpoint = connection == null ? "" : connection.RosIPAddress + ":" + connection.RosPort;
            s_Status.frameCount = Time.frameCount;
            s_Status.gameTime = Time.time;
            double now = EditorApplication.timeSinceStartup;
            s_Status.observedFramesPerSecond = s_LastStatusTime > 0 && now > s_LastStatusTime
                ? (Time.frameCount - s_LastStatusFrame) / (now - s_LastStatusTime) : 0;
            s_LastStatusTime = now;
            s_LastStatusFrame = Time.frameCount;
            s_Status.lastPoseAgeSeconds = AgeSeconds(s_Status.lastPoseUtc);
            s_Status.lastJointAgeSeconds = AgeSeconds(s_Status.lastJointUtc);
            s_Status.graphicsDeviceName = SystemInfo.graphicsDeviceName;
            s_Status.graphicsDeviceType = SystemInfo.graphicsDeviceType.ToString();
            s_Status.supportsComputeShaders = SystemInfo.supportsComputeShaders;
            FieldMainManager field = UnityEngine.Object.FindObjectOfType<FieldMainManager>();
            if (field != null && field.terrain != null)
            {
                s_Status.terrainHeightmapResolution = field.terrain.terrainData.heightmapResolution;
                TerrainTiler tiler = field.terrain.GetComponent<TerrainTiler>();
                s_Status.terrainTiles = tiler != null && tiler.terrains != null ? tiler.terrains.Count : 0;
            }

            GameObject zx200 = GameObject.Find("zx200");
            s_Status.zx200Found = zx200 != null;
            if (zx200 != null)
            {
                s_Status.unityZX200Position = zx200.transform.position;
                s_Status.unityZX200Rotation = zx200.transform.rotation;
                s_Status.unityZX200Euler = zx200.transform.eulerAngles;
                Transform swing = zx200.transform.Find("base_link/body_link");
                Transform boom = zx200.transform.Find("base_link/body_link/boom_link");
                Transform arm = zx200.transform.Find("base_link/body_link/boom_link/arm_link");
                Transform bucket = zx200.transform.Find("base_link/body_link/boom_link/arm_link/bucket_link");
                s_Status.fourJointTransformsFound = swing != null && boom != null && arm != null && bucket != null;
                if (swing != null) s_Status.swingLocalEuler = swing.localEulerAngles;
                if (boom != null) s_Status.boomLocalEuler = boom.localEulerAngles;
                if (arm != null) s_Status.armLocalEuler = arm.localEulerAngles;
                if (bucket != null) s_Status.bucketLocalEuler = bucket.localEulerAngles;
                JointSubscriber subscriber = zx200.GetComponent<JointSubscriber>();
                s_Status.JointPositions = subscriber != null && subscriber.JointPositions != null
                    ? subscriber.JointPositions.ToArray() : new double[0];
            }

            OVRCameraRig rig;
            Camera camera = OriginalCamera(GameObject.Find("OVRPlayerController"), out rig);
            s_Status.originalCameraFound = camera != null;
            if (camera != null)
            {
                s_Status.originalCameraPosition = camera.transform.position;
                s_Status.originalCameraEuler = camera.transform.eulerAngles;
            }
            Directory.CreateDirectory(Path.GetDirectoryName(StatusPath));
            string temporaryPath = StatusPath + ".tmp";
            File.WriteAllText(temporaryPath, JsonUtility.ToJson(s_Status, true));
            if (File.Exists(StatusPath)) File.Delete(StatusPath);
            File.Move(temporaryPath, StatusPath);
            SaveSession();
        }
        catch (Exception exception)
        {
            Debug.LogException(exception);
        }
    }

    static double AgeSeconds(string utc)
    {
        DateTime received;
        return DateTime.TryParse(utc, null, System.Globalization.DateTimeStyles.RoundtripKind, out received)
            ? Math.Max(0, (DateTime.UtcNow - received.ToUniversalTime()).TotalSeconds) : -1;
    }

    static void CountError(string condition, string stackTrace, LogType type)
    {
        if (!SessionState.GetBool(ActiveKey, false))
            return;
        if (type == LogType.Error || type == LogType.Exception || type == LogType.Assert)
        {
            s_Status.errorCount++;
            s_Status.lastError = condition;
        }
    }

    static void SaveSession()
    {
        SessionState.SetString(StateKey, JsonUtility.ToJson(s_Status));
    }

    static void FocusGameView()
    {
        Type type = typeof(EditorWindow).Assembly.GetType("UnityEditor.GameView");
        if (type != null)
            EditorWindow.GetWindow(type).Focus();
    }
}
