using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;
using System.IO;

public static class AutoBuild
{
    private const string BuildPath = "Build/Cornucopia.apk";

    [MenuItem("Cornucopia/Build and Run Android")]
    public static void BuildAndRun()
    {
        string[] scenes = new string[]
        {
            "Assets/Scenes/FirebaseLogin.unity",
            "Assets/Scenes/user/Home.unity",
            "Assets/Scenes/user/Collectibles.unity",
            "Assets/Scenes/user/Notification.unity",
            "Assets/Scenes/user/NotifyModel.unity",
            "Assets/Scenes/user/Profile.unity",
            "Assets/Scenes/user/UserModelDetails.unity",
            "Assets/UX/Scenes/UXManagerScene.unity"
        };

        Directory.CreateDirectory("Build");

        BuildPlayerOptions options = new BuildPlayerOptions
        {
            scenes = scenes,
            locationPathName = BuildPath,
            target = BuildTarget.Android,
            options = BuildOptions.AutoRunPlayer
        };

        Debug.Log("[AutoBuild] Starting build to: " + BuildPath);
        BuildReport report = BuildPipeline.BuildPlayer(options);

        if (report.summary.result == BuildResult.Succeeded)
            Debug.Log("[AutoBuild] Build succeeded in " + report.summary.totalTime.TotalSeconds.ToString("F1") + "s");
        else
            Debug.LogError("[AutoBuild] Build FAILED: " + report.summary.result);
    }
}
