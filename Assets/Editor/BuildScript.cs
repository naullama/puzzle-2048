using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

public class BuildScript
{
    public static void Build()
    {
        // シーンをコードで生成してGUID不一致を完全回避
        CreateScene();

        PlayerSettings.WebGL.compressionFormat = WebGLCompressionFormat.Disabled;
        PlayerSettings.SetManagedStrippingLevel(BuildTargetGroup.WebGL, ManagedStrippingLevel.Disabled);
        PlayerSettings.WebGL.exceptionSupport = WebGLExceptionSupport.FullWithStacktrace;

        string[] scenes = { "Assets/Scenes/Game.unity" };
        BuildPlayerOptions opt = new BuildPlayerOptions
        {
            scenes = scenes,
            locationPathName = "docs",
            target = BuildTarget.WebGL,
            options = BuildOptions.None
        };

        Debug.Log("Building WebGL (compression: disabled)");
        BuildReport report = BuildPipeline.BuildPlayer(opt);

        if (report.summary.result == BuildResult.Succeeded)
            Debug.Log($"Build succeeded: {report.summary.totalSize} bytes");
        else
        {
            Debug.LogError($"Build failed: {report.summary.result}");
            EditorApplication.Exit(1);
        }
    }

    static void CreateScene()
    {
        var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

        // GameBootstrap を持つ GameObject を作成
        var go = new GameObject("GameBootstrap");
        go.AddComponent<GameBootstrap>();

        EditorSceneManager.SaveScene(scene, "Assets/Scenes/Game.unity");
        Debug.Log("Scene created: Assets/Scenes/Game.unity with GameBootstrap attached");
    }
}
