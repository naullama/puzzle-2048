using System.Runtime.InteropServices;
using UnityEngine;
using UnityEngine.Scripting;

/// <summary>
/// GameBootstrap とは独立した診断クラス。
/// RuntimeInitializeOnLoadMethod により MonoBehaviour/シーンと無関係に実行される。
/// </summary>
[Preserve]
public static class GameDiagnostics
{
#if UNITY_WEBGL && !UNITY_EDITOR
    [DllImport("__Internal")] static extern void JsLog(string msg);
    [DllImport("__Internal")] static extern void JsAlert(string msg);
#else
    static void JsLog(string msg)   => Debug.Log("[DIAG] " + msg);
    static void JsAlert(string msg) => Debug.LogError("[DIAG ALERT] " + msg);
#endif

    static void Log(string msg)
    {
        Debug.Log("[DIAG] " + msg);
#if UNITY_WEBGL && !UNITY_EDITOR
        try { JsLog(msg); } catch { }
#endif
    }

    // ── ライフサイクル 5段階 ──────────────────────────────

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    [Preserve]
    static void OnSubsystem()
    {
        Log("1/5 SubsystemRegistration - Unity ランタイム起動");
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterAssembliesLoaded)]
    [Preserve]
    static void OnAssemblies()
    {
        Log("2/5 AfterAssembliesLoaded - アセンブリ読み込み完了");
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSplashScreen)]
    [Preserve]
    static void OnBeforeSplash()
    {
        Log("3/5 BeforeSplashScreen - スプラッシュ前");
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    [Preserve]
    static void OnBeforeScene()
    {
        Log("4/5 BeforeSceneLoad - シーン読み込み前");
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    [Preserve]
    static void OnAfterScene()
    {
        Log("5/5 AfterSceneLoad - シーン読み込み完了");

        var gb = Object.FindAnyObjectByType<GameBootstrap>();
        if (gb != null)
        {
            Log("GameBootstrap: 検出 OK (シーンに存在)");
        }
        else
        {
            Log("GameBootstrap: シーンに存在しない → 動的生成");
#if UNITY_WEBGL && !UNITY_EDITOR
            try { JsAlert("GameBootstrap がシーンにいません。動的生成します。"); } catch { }
#endif
            var go = new GameObject("GameBootstrap_Auto");
            go.AddComponent<GameBootstrap>();
        }
    }
}
