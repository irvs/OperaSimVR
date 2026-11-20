using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.XR.Management;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.XR.Management;
using Object = UnityEngine.Object;

/// <summary>
/// Opt-in Linux VR build settings. Use in a using block around BuildPipeline.BuildPlayer.
/// Existing Windows/Android XR assets are not edited.
/// </summary>
public sealed class OperaSimVROpenVRBuildScope : IDisposable
{
    const string OpenVRSettingsKey = "Unity.XR.OpenVR.Settings";
    const string LinuxGraphicsTarget = "LinuxStandaloneSupport";
    readonly XRGeneralSettingsPerBuildTarget originalTargets;
    readonly ScriptableObject originalOpenVR;
    readonly XRGeneralSettings originalInstance;
    readonly Object[] originalPreloaded;
    readonly GraphicsDeviceType[] originalGraphics;
    readonly bool originalDefaultGraphics;
    readonly bool hadLinuxGraphicsEntry;
    readonly StereoRenderingPath originalStereo;
    readonly string originalXRBootSettings;
    readonly bool hadTargets;
    readonly bool hadOpenVR;
    string temporaryAssetPath;
    bool disposed;

    public OperaSimVROpenVRBuildScope(bool vulkan = false)
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode)
            throw new BuildFailedException("Stop Play Mode before preparing Linux VR build settings.");
        Type loaderType = RequireType("Unity.XR.OpenVR.OpenVRLoader");
        Type settingsType = RequireType("Unity.XR.OpenVR.OpenVRSettings");
        // GetGraphicsAPIs returns effective defaults even when no serialized
        // Linux override exists. Capture that distinction before any graphics API.
        // Refuse an unfamiliar serialization layout before changing settings.
        using (var settings = SerializedPlayerSettings())
            hadLinuxGraphicsEntry = FindLinuxGraphicsEntry(GraphicsEntries(settings)) >= 0;
        hadTargets = EditorBuildSettings.TryGetConfigObject(XRGeneralSettings.k_SettingsKey, out originalTargets);
        hadOpenVR = EditorBuildSettings.TryGetConfigObject(OpenVRSettingsKey, out originalOpenVR);
        originalInstance = XRGeneralSettings.Instance;
        originalPreloaded = PlayerSettings.GetPreloadedAssets();
        originalGraphics = PlayerSettings.GetGraphicsAPIs(BuildTarget.StandaloneLinux64);
        originalDefaultGraphics = PlayerSettings.GetUseDefaultGraphicsAPIs(BuildTarget.StandaloneLinux64);
        originalStereo = PlayerSettings.stereoRenderingPath;
        originalXRBootSettings = EditorUserBuildSettings.GetPlatformSettings(
            BuildPipeline.GetBuildTargetName(BuildTarget.StandaloneLinux64), "xr-boot-settings");

        try
        {
            // Do not clone the original container: its OnEnable mutates the referenced
            // managers by filtering their loader lists. Build an independent Linux container.
            var targets = ScriptableObject.CreateInstance<XRGeneralSettingsPerBuildTarget>();
            targets.name = "OperaSimVR temporary Linux VR targets";
            temporaryAssetPath = "Assets/OperaSimVRLinuxVRBuild-" + Guid.NewGuid().ToString("N") + ".asset";
            AssetDatabase.CreateAsset(targets, temporaryAssetPath);

            var general = ScriptableObject.CreateInstance<XRGeneralSettings>();
            general.name = "OperaSimVR Linux VR settings";
            // The runtime bridge initializes OpenVR only for an explicit VR launch.
            general.InitManagerOnStart = false;
            var manager = ScriptableObject.CreateInstance<XRManagerSettings>();
            manager.name = "OperaSimVR Linux OpenVR providers";
            manager.automaticLoading = false;
            manager.automaticRunning = false;
            var loader = (XRLoader)ScriptableObject.CreateInstance(loaderType);
            loader.name = "OperaSimVR Linux OpenVR loader";
            if (!manager.TryAddLoader(loader))
                throw new BuildFailedException("Cannot register the temporary OpenVR loader.");
            general.Manager = manager;
            targets.SetSettingsForBuildTarget(BuildTargetGroup.Standalone, general);

            // Always use an independent settings object; upstream may dirty/save it.
            var openVR = ScriptableObject.CreateInstance(settingsType);
            if (originalOpenVR != null)
                EditorJsonUtility.FromJsonOverwrite(EditorJsonUtility.ToJson(originalOpenVR), openVR);
            openVR.name = "OperaSimVR Linux OpenVR settings";
            JsonUtility.FromJsonOverwrite("{\"StereoRenderingMode\":" + (vulkan ? "1" : "0")
                + ",\"InitializationType\":1,\"MirrorView\":2,\"PromptToUpgradePackage\":false,"
                + "\"PromptToUpgradePreviewPackages\":false}", openVR);
            foreach (Object asset in new Object[] { general, manager, loader, openVR })
                AssetDatabase.AddObjectToAsset(asset, targets);
            EditorUtility.SetDirty(targets);
            AssetDatabase.SaveAssetIfDirty(targets);
            EditorBuildSettings.AddConfigObject(XRGeneralSettings.k_SettingsKey, targets, true);
            EditorBuildSettings.AddConfigObject(OpenVRSettingsKey, openVR, true);

            // Valve documents Linux OpenGL+SPI and Linux Vulkan+MultiPass as crashes.
            // Include only the selected API so the built native settings cannot mismatch a fallback API.
            PlayerSettings.SetUseDefaultGraphicsAPIs(BuildTarget.StandaloneLinux64, false);
            PlayerSettings.SetGraphicsAPIs(BuildTarget.StandaloneLinux64,
                new[] { vulkan ? GraphicsDeviceType.Vulkan : GraphicsDeviceType.OpenGLCore });
            PlayerSettings.stereoRenderingPath = vulkan ? StereoRenderingPath.Instancing : StereoRenderingPath.MultiPass;
        }
        catch
        {
            Dispose();
            throw;
        }
    }

    static Type RequireType(string name)
    {
        Type type = AppDomain.CurrentDomain.GetAssemblies().Select(assembly => assembly.GetType(name, false))
            .FirstOrDefault(candidate => candidate != null);
        if (type == null)
            throw new BuildFailedException("Install the pinned OpenVR package before building Linux VR: " + name);
        return type;
    }

    static SerializedObject SerializedPlayerSettings()
    {
        Object asset = Unsupported.GetSerializedAssetInterfaceSingleton("PlayerSettings");
        if (asset == null)
            throw new BuildFailedException("Cannot inspect PlayerSettings before preparing Linux VR graphics settings.");
        return new SerializedObject(asset);
    }

    static SerializedProperty GraphicsEntries(SerializedObject settings)
    {
        SerializedProperty entries = settings.FindProperty("m_BuildTargetGraphicsAPIs");
        if (entries == null || !entries.isArray)
            throw new BuildFailedException("Unsupported PlayerSettings graphics serialization; Linux VR settings were not prepared.");
        return entries;
    }

    static int FindLinuxGraphicsEntry(SerializedProperty entries)
    {
        int index = -1;
        for (int i = 0; i < entries.arraySize; ++i)
        {
            SerializedProperty target = entries.GetArrayElementAtIndex(i).FindPropertyRelative("m_BuildTarget");
            if (target == null || target.propertyType != SerializedPropertyType.String)
                throw new BuildFailedException("Unsupported PlayerSettings graphics target serialization.");
            if (target.stringValue == LinuxGraphicsTarget)
                index = i;
        }
        return index;
    }

    void RemoveNewLinuxGraphicsEntry()
    {
        if (hadLinuxGraphicsEntry)
            return;
        using (var settings = SerializedPlayerSettings())
        {
            SerializedProperty entries = GraphicsEntries(settings);
            int index;
            while ((index = FindLinuxGraphicsEntry(entries)) >= 0)
                entries.DeleteArrayElementAtIndex(index);
            // Apply only the newly created Linux override removal. Other platform
            // entries and settings changed by build callbacks remain untouched.
            settings.ApplyModifiedPropertiesWithoutUndo();
        }
    }

    public static void VerifyBuildOutput(string executable, bool vulkan = false)
    {
        string fullPath = Path.GetFullPath(executable);
        string data = Path.Combine(Path.GetDirectoryName(fullPath), Path.GetFileNameWithoutExtension(fullPath) + "_Data");
        string settings = Path.Combine(data, "StreamingAssets", "SteamVR", "OpenVRSettings.asset");
        if (!File.Exists(settings) || !File.ReadAllLines(settings).Contains("StereoRenderingMode: " + (vulkan ? "1" : "0")))
            throw new BuildFailedException("The Linux VR build is missing matching OpenVR native settings: " + settings);
        foreach (string library in new[] { "libXRSDKOpenVR.so", "libopenvr_api.so" })
            if (!Directory.EnumerateFiles(data, library, SearchOption.AllDirectories).Any())
                throw new BuildFailedException("The Linux VR build did not include " + library);
    }

    public void Dispose()
    {
        if (disposed) return;
        disposed = true;
        try
        {
            PlayerSettings.SetPreloadedAssets(originalPreloaded);
            PlayerSettings.SetGraphicsAPIs(BuildTarget.StandaloneLinux64, originalGraphics);
            PlayerSettings.SetUseDefaultGraphicsAPIs(BuildTarget.StandaloneLinux64, originalDefaultGraphics);
            RemoveNewLinuxGraphicsEntry();
            PlayerSettings.stereoRenderingPath = originalStereo;
            EditorUserBuildSettings.SetPlatformSettings(
                BuildPipeline.GetBuildTargetName(BuildTarget.StandaloneLinux64), "xr-boot-settings", originalXRBootSettings);
        }
        finally
        {
            try
            {
                if (hadTargets) EditorBuildSettings.AddConfigObject(XRGeneralSettings.k_SettingsKey, originalTargets, true);
                else EditorBuildSettings.RemoveConfigObject(XRGeneralSettings.k_SettingsKey);
                if (hadOpenVR) EditorBuildSettings.AddConfigObject(OpenVRSettingsKey, originalOpenVR, true);
                else EditorBuildSettings.RemoveConfigObject(OpenVRSettingsKey);
            }
            finally
            {
                if (!string.IsNullOrEmpty(temporaryAssetPath)) AssetDatabase.DeleteAsset(temporaryAssetPath);
                XRGeneralSettings.Instance = originalInstance;
            }
        }
    }
}
