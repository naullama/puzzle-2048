using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;

public class BuildScript
{
    public static void Build()
    {
        string[] scenes = { "Assets/Scenes/Game.unity" };
        string outputPath = "docs/Build";

        BuildPlayerOptions buildPlayerOptions = new BuildPlayerOptions();
        buildPlayerOptions.scenes = scenes;
        buildPlayerOptions.locationPathName = outputPath;
        buildPlayerOptions.target = BuildTarget.WebGL;
        buildPlayerOptions.options = BuildOptions.None;

        // GitHub Pages does not serve Content-Encoding: gzip headers,
        // so disable compression to serve plain files directly.
        PlayerSettings.WebGL.compressionFormat = WebGLCompressionFormat.Disabled;
        Debug.Log($"Building WebGL to: {outputPath} (compression: disabled)");
        BuildReport report = BuildPipeline.BuildPlayer(buildPlayerOptions);

        if (report.summary.result == BuildResult.Succeeded)
        {
            Debug.Log($"Build succeeded: {report.summary.totalSize} bytes");
        }
        else
        {
            Debug.LogError($"Build failed: {report.summary.result}");
            EditorApplication.Exit(1);
        }
    }
}
