using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;
using UnityEngine.XR;
using UnityEngine.XR.Management;

// Linux VR bridge; Quest 3 hardware behavior requires device validation.
// No settings assets are saved, and no runtime is started without explicit opt-in.
[DefaultExecutionOrder(-10000)]
public sealed class OperaSimVRLinuxXR : MonoBehaviour
{
    public static bool IsRunning { get; private set; }

#if UNITY_EDITOR_LINUX || (UNITY_STANDALONE_LINUX && !UNITY_EDITOR)
    sealed class RigBinding
    {
        public OVRCameraRig rig;
        public OVRManager manager;
        public OVRPlayerController player;
        public CharacterController character;
        public bool rigEnabled, managerEnabled, useProfileData;
        public Vector3 rigPosition;
        public OVRManager.TrackingOrigin origin;
    }

    static OperaSimVRLinuxXR instance;
    readonly List<RigBinding> bindings = new List<RigBinding>();
    readonly List<XRNodeState> nodes = new List<XRNodeState>();
    XRLoader loader;
    XRDisplaySubsystem display;
    XRInputSubsystem input;
    bool initialized, started, ready, warnedTracking, hadTracking;
    bool originConfigured;
    OVRManager.TrackingOrigin configuredOrigin;
    OVRManager.XRDevice previousDevice;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    static void ResetState()
    {
        IsRunning = false;
        instance = null;
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    static void Bootstrap()
    {
        string selection = Environment.GetEnvironmentVariable("OPERASIMVR_XR");
        if (string.IsNullOrEmpty(selection) || selection.Equals("desktop", StringComparison.OrdinalIgnoreCase))
            return;
        if (!selection.Equals("steamvr", StringComparison.OrdinalIgnoreCase))
        {
            Debug.LogError("[OperaSimVR XR] Unknown OPERASIMVR_XR value: " + selection);
            return;
        }
        var owner = new GameObject("OperaSimVR Linux XR");
        DontDestroyOnLoad(owner);
        instance = owner.AddComponent<OperaSimVRLinuxXR>();
        instance.Begin();
    }

    void Begin()
    {
        try
        {
            if (Application.isBatchMode || SystemInfo.graphicsDeviceType == GraphicsDeviceType.Null)
                throw new InvalidOperationException("A graphical session is required.");
            if (SystemInfo.graphicsDeviceType != GraphicsDeviceType.Vulkan)
                throw new InvalidOperationException("This VR configuration requires Vulkan. Launch with -force-vulkan.");
            var displays = new List<XRDisplaySubsystem>();
            SubsystemManager.GetInstances(displays);
            foreach (var other in displays)
                if (other.running)
                    throw new InvalidOperationException("An XR display is already running; refusing a second loader.");

            // The Linux VR build includes the fixed Valve v1.2.3 package and its
            // native libraries via a temporary build-time loader configuration.
            // Reflection keeps an ordinary desktop build independent of this opt-in package.
            Type loaderType = Type.GetType("Unity.XR.OpenVR.OpenVRLoader, Unity.XR.OpenVR", false);
            if (loaderType == null)
                throw new InvalidOperationException("OpenVR is not included. Install the pinned package and make the Linux VR build.");
            if (OVRInput.controllers.Count == 0)
                throw new InvalidOperationException("The verified Meta XR 72 Linux input patch has not been applied.");
            loader = ScriptableObject.CreateInstance(loaderType) as XRLoader;
            if (loader == null)
                throw new InvalidOperationException("The OpenVR loader type could not be created.");
            initialized = true; // Deinitialize also cleans up a partial/failed Initialize.
            if (!loader.Initialize())
                throw new InvalidOperationException("OpenVR initialization failed. Connect Quest 3 to SteamVR first.");
            if (!loader.Start())
                throw new InvalidOperationException("OpenVR subsystems could not be started.");
            started = true;
            display = loader.GetLoadedSubsystem<XRDisplaySubsystem>();
            input = loader.GetLoadedSubsystem<XRInputSubsystem>();
            if (display == null || !display.running || input == null)
                throw new InvalidOperationException("OpenVR did not expose its display and pose input subsystems.");

            // Native OpenVR's provider owns VR_Init and VR_Shutdown. The bundled
            // Meta managed wrapper only acquires an interface from that same .so.
            // Calling OpenVR.Init/Shutdown here would compete with loader ownership.
            if (!OVR.OpenVR.OpenVR.IsInterfaceVersionValid(OVR.OpenVR.OpenVR.IVRSystem_Version) ||
                OVR.OpenVR.OpenVR.System == null)
                throw new InvalidOperationException("SteamVR does not expose the required IVRSystem_019 interface.");

            previousDevice = OVRManager.loadedXRDevice;
            OVRManager.loadedXRDevice = OVRManager.XRDevice.OpenVR;
            ready = true;
            SceneManager.sceneLoaded += SceneLoaded;
            Application.onBeforeRender += BeforeRender;
            BindRigs();
            Debug.Log("[OperaSimVR XR] OpenVR started. Quest button/axis input uses the Meta SDK's Linux-patched legacy OpenVR route. Hardware validation pending.");
        }
        catch (Exception error)
        {
            Debug.LogError("[OperaSimVR XR] VR startup failed (graphics API: " +
                SystemInfo.graphicsDeviceType + "): " + error.Message);
            try
            {
                End();
            }
            finally
            {
                enabled = false;
#if !UNITY_EDITOR
                Application.Quit(2);
#endif
            }
        }
    }

    void SceneLoaded(Scene scene, LoadSceneMode mode)
    {
        BindRigs();
    }

    void BindRigs()
    {
        foreach (var rig in FindObjectsOfType<OVRCameraRig>(true))
        {
            if (!rig.gameObject.scene.IsValid() || bindings.Exists(item => item.rig == rig))
                continue;
            var manager = rig.GetComponent<OVRManager>();
            var player = rig.GetComponentInParent<OVRPlayerController>();
            var binding = new RigBinding
            {
                rig = rig, manager = manager, player = player,
                character = player == null ? null : player.GetComponent<CharacterController>(),
                rigEnabled = rig.enabled,
                managerEnabled = manager != null && manager.enabled,
                useProfileData = player != null && player.useProfileData,
                rigPosition = rig.transform.localPosition,
                origin = ReadSavedOrigin(manager)
            };
            bindings.Add(binding);
            // Preserve the original hierarchy, serialized anchors and interaction
            // components. Only one source updates anchors/input during Linux VR.
            rig.enabled = false;
            if (manager != null)
                manager.enabled = false;
            if (player != null)
                player.useProfileData = false; // Native Oculus profile data is unavailable.
            ConfigureOrigin(binding);
        }
    }

    static OVRManager.TrackingOrigin ReadSavedOrigin(OVRManager manager)
    {
        if (manager == null)
            return OVRManager.TrackingOrigin.FloorLevel;
        // Once a non-Oculus XR display runs, the public getter reads the native
        // Oculus stub instead of the serialized setting. SDK 72 serializes this
        // backing field; reading it preserves the existing scene choice.
        var field = typeof(OVRManager).GetField("_trackingOriginType", BindingFlags.Instance | BindingFlags.NonPublic);
        if (field == null)
            throw new InvalidOperationException("The expected Meta XR 72 tracking-origin field is absent.");
        return (OVRManager.TrackingOrigin)field.GetValue(manager);
    }

    void ConfigureOrigin(RigBinding binding)
    {
        if (originConfigured && configuredOrigin != binding.origin)
        {
            Debug.LogWarning("[OperaSimVR XR] Multiple rigs request different origins. The first rig's origin is retained.");
            return;
        }
        var mode = binding.origin == OVRManager.TrackingOrigin.EyeLevel
            ? TrackingOriginModeFlags.Device : TrackingOriginModeFlags.Floor;
        if (!originConfigured)
        {
            if (!input.TrySetTrackingOriginMode(mode))
            {
                Debug.LogWarning("[OperaSimVR XR] The scene's tracking origin could not be selected. Original rig offsets are retained; verify camera height.");
                return;
            }
            configuredOrigin = binding.origin;
            originConfigured = true;
            if (binding.origin == OVRManager.TrackingOrigin.Stage)
                Debug.LogWarning("[OperaSimVR XR] Stage uses SteamVR's standing/floor origin. Stage bounds require hardware validation.");
        }
        if (!binding.useProfileData || binding.character == null)
            return;
        var position = binding.rig.transform.localPosition;
        if (mode == TrackingOriginModeFlags.Device)
        {
            float eyeHeight = OVR.OpenVR.OpenVR.System.GetSeatedZeroPoseToStandingAbsoluteTrackingPose().m7;
            if (eyeHeight <= 0f || float.IsNaN(eyeHeight) || float.IsInfinity(eyeHeight))
            {
                Debug.LogWarning("[OperaSimVR XR] SteamVR did not provide a seated-origin height. Original rig offset is retained; verify EyeLevel camera height.");
                return;
            }
            position.y = eyeHeight - 0.5f * binding.character.height + binding.character.center.y;
        }
        else
            position.y = -0.5f * binding.character.height + binding.character.center.y;
        binding.rig.transform.localPosition = position;
    }

    void Update()
    {
        if (!ready)
            return;
        if (display == null || !display.running)
        {
            Debug.LogWarning("[OperaSimVR XR] The OpenVR display stopped. Restart the application after SteamVR reconnects.");
            End();
            enabled = false;
            return;
        }

        var system = OVR.OpenVR.OpenVR.System;
        bool connected = system != null && system.IsTrackedDeviceConnected(OVR.OpenVR.OpenVR.k_unTrackedDeviceIndex_Hmd);
        IsRunning = connected;
        OVRManager.loadedXRDevice = OVRManager.XRDevice.OpenVR;
        ReadNodes();
        Vector3 position;
        Quaternion rotation;
        bool headTracked = TryPose(XRNode.CenterEye, out position, out rotation) || TryPose(XRNode.Head, out position, out rotation);

        if (!connected || !headTracked)
        {
            ClearInput();
            hadTracking = false;
            if (!warnedTracking)
            {
                Debug.LogWarning("[OperaSimVR XR] Headset tracking is unavailable. Controller input is cleared until tracking returns.");
                warnedTracking = true;
            }
            return;
        }
        warnedTracking = false;

        // SteamVR's dashboard or another runtime UI may consume input while
        // tracking remains active. Keep poses current without driving controls.
        if (!system.IsInputAvailable())
        {
            ClearInput();
            hadTracking = false;
            ApplyAnchors(false);
            return;
        }

        // Exactly one input transition per frame; UI GetDown/GetUp must not be
        // consumed again by onBeforeRender or by the disabled OVRManager.
        var oldConnected = OVRInput.connectedControllerTypes;
        OVRInput.Update();
        foreach (var controller in OVRInput.controllers)
            if (!hadTracking || (controller.currentState.ConnectedControllers & ~(uint)oldConnected) != 0)
                controller.previousState = controller.currentState;
        hadTracking = true;
        ApplyAnchors(true);
    }

    void BeforeRender()
    {
        if (!ready || !IsRunning)
            return;
        ReadNodes();
        ApplyAnchors(false);
    }

    void ReadNodes()
    {
        nodes.Clear();
        InputTracking.GetNodeStates(nodes);
    }

    bool TryPose(XRNode kind, out Vector3 position, out Quaternion rotation)
    {
        foreach (var node in nodes)
        {
            if (node.nodeType == kind && node.tracked && node.TryGetPosition(out position) && node.TryGetRotation(out rotation))
                return true;
        }
        position = Vector3.zero;
        rotation = Quaternion.identity;
        return false;
    }

    void ApplyAnchors(bool movePlayer)
    {
        foreach (var binding in bindings)
        {
            var rig = binding.rig;
            if (rig == null || !rig.gameObject.activeInHierarchy || rig.trackingSpace == null || rig.centerEyeAnchor == null)
                continue;
            SetAnchor(rig.centerEyeAnchor, XRNode.CenterEye, XRNode.Head);
            SetAnchor(rig.leftEyeAnchor, XRNode.LeftEye, XRNode.CenterEye);
            SetAnchor(rig.rightEyeAnchor, XRNode.RightEye, XRNode.CenterEye);
            SetAnchor(rig.leftHandAnchor, XRNode.LeftHand, XRNode.LeftHand);
            SetAnchor(rig.rightHandAnchor, XRNode.RightHand, XRNode.RightHand);
            ApplyControllerOffset(rig.leftControllerAnchor, XRNode.LeftHand);
            ApplyControllerOffset(rig.rightControllerAnchor, XRNode.RightHand);

            if (rig.leftControllerAnchor != null && rig.rightControllerAnchor != null)
                OVRInput.SetOpenVRLocalPose(
                    rig.trackingSpace.InverseTransformPoint(rig.leftControllerAnchor.position),
                    rig.trackingSpace.InverseTransformPoint(rig.rightControllerAnchor.position),
                    Quaternion.Inverse(rig.trackingSpace.rotation) * rig.leftControllerAnchor.rotation,
                    Quaternion.Inverse(rig.trackingSpace.rotation) * rig.rightControllerAnchor.rotation);

            // Reuse original acceleration, gravity, snap turning and all public
            // locomotion controls. Boarding/bird's-eye mode disable the CharacterController.
            if (movePlayer && binding.player != null && binding.player.isActiveAndEnabled &&
                binding.character != null && binding.character.enabled)
                binding.player.UpdateTransform(rig);
        }
    }

    void SetAnchor(Transform target, XRNode kind, XRNode fallback)
    {
        Vector3 position;
        Quaternion rotation;
        if (target != null && (TryPose(kind, out position, out rotation) || TryPose(fallback, out position, out rotation)))
            target.SetLocalPositionAndRotation(position, rotation);
    }

    static void ApplyControllerOffset(Transform target, XRNode hand)
    {
        if (target == null)
            return;
        var offset = OVRManager.GetOpenVRControllerOffset(hand);
        target.SetLocalPositionAndRotation(offset.position, offset.orientation);
    }

    static void ClearInput()
    {
        OVRInput.SetControllerVibration(0f, 0f, OVRInput.Controller.LTouch);
        OVRInput.SetControllerVibration(0f, 0f, OVRInput.Controller.RTouch);
        bool releasePending = false;
        foreach (var controller in OVRInput.controllers)
        {
            releasePending |= controller.currentState.Buttons != 0 || controller.currentState.Touches != 0;
            controller.previousState = controller.currentState;
            controller.currentState = default(OVRPlugin.ControllerState6);
        }
        OVRInput.connectedControllerTypes = OVRInput.Controller.None;
        // Retain the preceding active mask for the first cleared frame so SDK UI
        // GetUp can release a pressed pointer. All analog/current button values
        // are already zero; the next frame removes the mask as well.
        if (!releasePending)
            OVRInput.activeControllerType = OVRInput.Controller.None;
    }

    void End()
    {
        bool ownedInput = ready;
        SceneManager.sceneLoaded -= SceneLoaded;
        Application.onBeforeRender -= BeforeRender;
        if (ownedInput)
        {
            ClearInput();
            OVRManager.loadedXRDevice = previousDevice;
        }
        ready = false;
        IsRunning = false;
        // No input tick runs after shutdown. Prevent a retained previous state
        // from producing GetUp on every later desktop frame.
        if (ownedInput)
        {
            foreach (var controller in OVRInput.controllers)
                controller.previousState = controller.currentState = default(OVRPlugin.ControllerState6);
            OVRInput.activeControllerType = OVRInput.Controller.None;
            OVRInput.connectedControllerTypes = OVRInput.Controller.None;
        }
        foreach (var binding in bindings)
        {
            if (binding.rig != null)
            {
                binding.rig.enabled = binding.rigEnabled;
                binding.rig.transform.localPosition = binding.rigPosition;
            }
            if (binding.manager != null)
                binding.manager.enabled = binding.managerEnabled;
            if (binding.player != null)
                binding.player.useProfileData = binding.useProfileData;
        }
        bindings.Clear();
        if (loader != null)
        {
            try
            {
                if (started)
                    loader.Stop();
            }
            catch (Exception error)
            {
                Debug.LogWarning("[OperaSimVR XR] OpenVR shutdown: " + error.Message);
            }
            try
            {
                if (initialized)
                    loader.Deinitialize();
            }
            catch (Exception error)
            {
                Debug.LogWarning("[OperaSimVR XR] OpenVR cleanup: " + error.Message);
            }
            Destroy(loader);
        }
        loader = null;
        initialized = started = false;
        originConfigured = false;
    }

    void OnDestroy()
    {
        End();
        if (instance == this)
            instance = null;
    }
#endif
}
