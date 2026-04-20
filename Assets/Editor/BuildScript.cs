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

        // シャドウを品質設定でも無効化
        QualitySettings.shadows = ShadowQuality.Disable;

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
    static void PatchIndexHtml(string path)
    {
        if (!File.Exists(path)) { Debug.LogWarning("index.html not found: " + path); return; }

        string html = File.ReadAllText(path);

        // ① canvas を 480×700 に修正・display:block 明示
        html = html.Replace(
            "<canvas id=\"unity-canvas\" width=960 height=600 tabindex=\"-1\">",
            "<canvas id=\"unity-canvas\" width=\"480\" height=\"700\" tabindex=\"-1\" style=\"display: block;\">"
        );

        // ①-b <head> に html/body/canvas 表示 CSS を注入
        html = html.Replace(
            "</head>",
@"  <style>
      html, body { width: 100%; height: 100%; margin: 0; padding: 0; overflow: hidden; }
      canvas { display: block; }
    </style>
  </head>"
        );

        // ② Desktop 時の canvas CSS サイズも 480×700 に変更
        html = html.Replace(
            "canvas.style.width = \"960px\";",
            "canvas.style.width = \"480px\";"
        );
        html = html.Replace(
            "canvas.style.height = \"600px\";",
            "canvas.style.height = \"700px\";"
        );

        // ③ errorHandler のコメントアウトを解除
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

        // ③-b webglContextAttributes を config に追加
        // depth/stencil/alpha は省略して Unity デフォルトに任せる（false にすると黒画面になる）
        html = html.Replace(
            "showBanner: unityShowBanner,",
@"showBanner: unityShowBanner,
        webglContextAttributes: { failIfMajorPerformanceCaveat: false, powerPreference: ""default"" },"
        );

        // ③-c デバッグオーバーレイ + WebGL NullGfxDevice 根本修正 v3
        //
        // v3 変更点:
        //   1. writable:false → getter/setter 方式（strict mode での TypeError 回避）
        //   2. document.createElement → Document.prototype.createElement をパッチ
        const string debugUi = @"
    <div id='unity-dbg-overlay' style='position:fixed;top:0;left:0;right:0;
         max-height:40vh;overflow-y:auto;background:rgba(0,0,0,0.85);color:#39ff14;
         font:11px/1.4 monospace;padding:6px 8px;z-index:999999;pointer-events:none;
         white-space:pre-wrap;word-break:break-all;'></div>
    <script>
      var _origConsoleLog = console.log;
      var _dbgHasError = false;
      function dbg(msg, isError) {
        var el = document.getElementById('unity-dbg-overlay');
        if (!el) return;
        if (isError) { _dbgHasError = true; el.style.color = '#ff4444'; }
        var ts = (performance.now()/1000).toFixed(2);
        el.innerHTML += '[' + ts + 's] ' + msg + '\n';
        el.scrollTop = el.scrollHeight;
        _origConsoleLog('[DBG] ' + msg);
      }
      dbg('JS: ページ読み込み完了');

      // ── WebGL NullGfxDevice 根本修正 v3（getter/setter + Document.prototype） ──
      (function() {
        var _origGetCtx = HTMLCanvasElement.prototype.getContext;

        function lockCanvasGetContext(el) {
          try {
            var d = Object.getOwnPropertyDescriptor(el, 'getContext');
            if (d && !d.configurable) return;
            Object.defineProperty(el, 'getContext', {
              get: function() {
                return function(type, attrs) {
                  return _origGetCtx.call(el, type,
                    Object.assign({}, attrs || {}, { failIfMajorPerformanceCaveat: false }));
                };
              },
              set: function() {},
              configurable: false,
              enumerable: false
            });
            dbg('canvas locked id=' + (el.id || '<no-id>'));
          } catch(e) { dbg('canvas lock FAILED: ' + e, true); }
        }

        document.querySelectorAll('canvas').forEach(lockCanvasGetContext);

        var _origCreate = Document.prototype.createElement;
        Document.prototype.createElement = function(tag) {
          var el = _origCreate.apply(this, arguments);
          if (typeof tag === 'string' && tag.toLowerCase() === 'canvas')
            lockCanvasGetContext(el);
          return el;
        };

        dbg('canvas lock v3: getter/setter + Document.prototype.createElement');
      })();

      // ── WebGL 診断 ───────────────────────────────────────────────────────
      (function() {
        var tc = document.createElement('canvas');
        var gl2 = tc.getContext('webgl2');
        dbg(gl2 ? 'WebGL2 OK: ' + gl2.getParameter(gl2.RENDERER) : 'WebGL2 FAIL');
        dbg('UA: ' + navigator.userAgent.substring(0, 80));
      })();

      // ── Unity 内部ログのインターセプト ────────────────────────────────────
      (function() {
        var _log = console.log, _warn = console.warn, _err = console.error;
        function intercept(orig, prefix, isError) {
          return function() {
            var msg = Array.prototype.slice.call(arguments).join(' ');
            if (msg.indexOf('[DBG]') < 0 && /shader|renderer|webgl|null.?device/i.test(msg))
              dbg(prefix + msg.substring(0, 300), isError);
            orig.apply(console, arguments);
          };
        }
        console.log   = intercept(_log,  'LOG: ',  false);
        console.warn  = intercept(_warn, 'WARN: ', false);
        console.error = intercept(_err,  'ERR: ',  true);
      })();

      setTimeout(function() {
        if (!_dbgHasError) {
          var el = document.getElementById('unity-dbg-overlay');
          if (el) el.style.display = 'none';
        }
      }, 5000);

      window.addEventListener('error', function(e) {
        dbg('JS ERR: ' + e.message + ' (' + (e.filename||'') + ':' + e.lineno + ')', true);
      });
      window.addEventListener('unhandledrejection', function(e) {
        dbg('JS PROMISE ERR: ' + e.reason, true);
      });
    </script>
";
        html = html.Replace("</body>", debugUi + "</body>");

        File.WriteAllText(path, html);
        Debug.Log("index.html patched: canvas=480x700, overlay 3s auto-dismiss");
    }
}
