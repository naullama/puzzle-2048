using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;

public class BuildScript
{
    public static void Build()
    {
        // GameBootstrap が RuntimeInitializeOnLoadMethod で起動するため
        // 空シーン（または既存の Game.unity）どちらでも動作する。
        string[] scenes = { "Assets/Scenes/Game.unity" };
        string outputPath = "docs/Build";

        // GitHub Pages は Content-Encoding: gzip を付与しないため圧縮無効
        PlayerSettings.WebGL.compressionFormat = WebGLCompressionFormat.Disabled;

        BuildPlayerOptions opt = new BuildPlayerOptions
        {
            scenes = scenes,
            locationPathName = outputPath,
            target = BuildTarget.WebGL,
            options = BuildOptions.None
        };

        Debug.Log($"Building WebGL to: {outputPath} (compression: disabled)");
        BuildReport report = BuildPipeline.BuildPlayer(opt);

        if (report.summary.result == BuildResult.Succeeded)
            Debug.Log($"Build succeeded: {report.summary.totalSize} bytes");
        else
        {
            Debug.LogError($"Build failed: {report.summary.result}");
            EditorApplication.Exit(1);
        }
    }
}
