using System.IO;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEditor.SceneManagement;
using UnityEngine;

public class BuildScript
{
    public static void Build()
    {
        // ── 1. スクリプト変更をエディタに確実に反映 ──
        AssetDatabase.Refresh();

        // ── 2. シーン生成 ──
        CreateScene();

        // ── 3. WebGL 設定（メモリ・圧縮・ストリッピング・例外） ──
        PlayerSettings.WebGL.compressionFormat       = WebGLCompressionFormat.Disabled;
        PlayerSettings.WebGL.exceptionSupport        = WebGLExceptionSupport.FullWithStacktrace;
        PlayerSettings.WebGL.debugSymbolMode         = WebGLDebugSymbolMode.External;
        PlayerSettings.SetManagedStrippingLevel(
            UnityEditor.Build.NamedBuildTarget.WebGL, ManagedStrippingLevel.Disabled);

        // 初期メモリを 256MB に引き上げ（デフォルト 32MB は不足する場合あり）
        PlayerSettings.WebGL.initialMemorySize = 256;

        // 設定を保存
        AssetDatabase.SaveAssets();

        // ── 4. ビルド実行 ──
        string[] scenes = { "Assets/Scenes/Game.unity" };
        var opt = new BuildPlayerOptions
        {
            scenes           = scenes,
            locationPathName = "docs",
            target           = BuildTarget.WebGL,
            options          = BuildOptions.None
        };

        Debug.Log("Building WebGL: stripping=Disabled, compression=Disabled, memory=256MB");
        BuildReport report = BuildPipeline.BuildPlayer(opt);

        if (report.summary.result == BuildResult.Succeeded)
        {
            Debug.Log($"Build succeeded: {report.summary.totalSize} bytes");
            PatchIndexHtml("docs/index.html");
        }
        else
        {
            Debug.LogError($"Build failed: {report.summary.result}");
            EditorApplication.Exit(1);
        }
    }

    // ── シーン生成 ────────────────────────────────────────
    static void CreateScene()
    {
        var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        var go = new GameObject("GameBootstrap");
        go.AddComponent<GameBootstrap>();
        EditorSceneManager.SaveScene(scene, "Assets/Scenes/Game.unity");
        Debug.Log("Scene created: Assets/Scenes/Game.unity");
    }

    // ── index.html のパッチ ───────────────────────────────
    // ビルド後に生成される index.html を書き換えて
    // ① errorHandler を有効化（JS エラーを alert で表示）
    // ② デバッグオーバーレイの初期化コードを追加
    static void PatchIndexHtml(string path)
    {
        if (!File.Exists(path)) { Debug.LogWarning("index.html not found: " + path); return; }

        string html = File.ReadAllText(path);

        // errorHandler のコメントアウトを解除
        html = html.Replace(
            "// errorHandler: function(err, url, line) {",
            "errorHandler: function(err, url, line) {"
        );
        html = html.Replace(
            "//    alert(\"error \" + err + \" occurred at line \" + line);",
            "    console.error('[Unity] ' + err + ' at ' + url + ':' + line);"
        );
        html = html.Replace(
            "//    // Return 'true' if you handled this error and don't want Unity",
            "    // Return 'true' if you handled this error and don't want Unity"
        );
        html = html.Replace(
            "//    // to process it further, 'false' otherwise.",
            "    // to process it further, 'false' otherwise."
        );
        html = html.Replace(
            "//    return true;",
            "    return false;"
        );
        html = html.Replace(
            "// },",
            "},"
        );

        // </body> の直前にデバッグ UI を追加
        const string debugUi = @"
    <div id='unity-dbg-overlay' style='display:none;position:fixed;top:0;left:0;right:0;
         max-height:45vh;overflow-y:auto;background:rgba(0,0,0,0.85);color:#39ff14;
         font:12px/1.4 monospace;padding:6px 8px;z-index:999999;pointer-events:none;
         white-space:pre-wrap;word-break:break-all;'></div>
    <script>
      // Unity ローダーのエラーをオーバーレイに表示
      window.addEventListener('error', function(e) {
        var el = document.getElementById('unity-dbg-overlay');
        if (el) { el.style.display='block'; el.innerHTML += '[JS ERR] ' + e.message + '\n'; }
      });
    </script>
";
        html = html.Replace("</body>", debugUi + "</body>");

        File.WriteAllText(path, html);
        Debug.Log("index.html patched: errorHandler enabled, debug overlay added");
    }
}
