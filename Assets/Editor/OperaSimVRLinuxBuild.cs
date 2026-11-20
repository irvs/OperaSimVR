using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;

public static class OperaSimVRLinuxBuild
{
    [MenuItem("OperaSimVR/Ubuntu/Build Linux x86_64 player")]
    public static void Build()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode)
            throw new BuildFailedException("Stop Play Mode before building OperaSimVR.");
        string xr = Environment.GetEnvironmentVariable("OPERASIMVR_XR");
        if (!string.IsNullOrEmpty(xr) && xr != "desktop" && xr != "steamvr")
            throw new BuildFailedException("Unknown OPERASIMVR_XR value: " + xr);
        bool vr = xr == "steamvr";
        string output = Environment.GetEnvironmentVariable("OPERASIMVR_BUILD_PATH");
        if (string.IsNullOrEmpty(output))
            output = vr ? "Build/LinuxVR/OperaSimVR.x86_64" : "Build/Linux/OperaSimVR.x86_64";
        string[] scenes = EditorBuildSettings.scenes.Where(scene => scene.enabled && File.Exists(scene.path))
            .Select(scene => scene.path).ToArray();
        foreach (var scene in EditorBuildSettings.scenes.Where(scene => scene.enabled && !File.Exists(scene.path)))
            Debug.LogWarning("[OperaSimVR Ubuntu] Build Settings refers to an absent scene: " + scene.path);
        if (!scenes.Contains("Assets/Scenes/SampleScene.unity"))
            throw new BuildFailedException("Enable the original SampleScene in Build Settings before building.");
        Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(output)));
        // SDK build callbacks may modify preloaded assets. Keep these changes
        // within the build, including the ordinary desktop build.
        UnityEngine.Object[] preloaded = PlayerSettings.GetPreloadedAssets();
        try
        {
            using (var settings = vr ? new OperaSimVROpenVRBuildScope(vulkan: true) : null)
            {
                BuildReport report = BuildPipeline.BuildPlayer(new BuildPlayerOptions
                {
                    scenes = scenes,
                    locationPathName = output,
                    target = BuildTarget.StandaloneLinux64,
                    options = BuildOptions.None
                });
                if (report.summary.result != BuildResult.Succeeded)
                    throw new BuildFailedException("OperaSimVR Linux build failed: " + report.summary.result);
                if (vr) OperaSimVROpenVRBuildScope.VerifyBuildOutput(output, vulkan: true);
                Debug.Log("[OperaSimVR Ubuntu] Linux player built: " + report.summary.outputPath);
            }
        }
        finally
        {
            PlayerSettings.SetPreloadedAssets(preloaded);
        }
    }
}
