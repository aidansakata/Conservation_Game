// WebGLBuild.cs -- pass 5 (reconstruction_5_plan.md, B-4/B-5).
// Editor-only build tooling. Ships in no build and touches no gameplay code.
// Exists because Player Settings and BuildPipeline are only reachable from the
// Editor GUI or from editor script; mcp-unity can invoke menu items but cannot
// complete the Build dialog. Authorised in chat as the single exception to the
// pass-5 constraint that limits C# changes to LevelJsonLoader.cs and ApiConfig.cs.

#if UNITY_EDITOR
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;

public static class WebGLBuild
{
    const string OUT = "Build/WebGL";

    static string Settings()
    {
        var sb = new StringBuilder();
        sb.AppendLine("  compressionFormat      = " + PlayerSettings.WebGL.compressionFormat);
        sb.AppendLine("  decompressionFallback  = " + PlayerSettings.WebGL.decompressionFallback);
        sb.AppendLine("  exceptionSupport       = " + PlayerSettings.WebGL.exceptionSupport);
        sb.AppendLine("  defaultWebScreenWidth  = " + PlayerSettings.defaultWebScreenWidth);
        sb.AppendLine("  defaultWebScreenHeight = " + PlayerSettings.defaultWebScreenHeight);
        sb.AppendLine("  runInBackground        = " + PlayerSettings.runInBackground);
        sb.AppendLine("  dataCaching            = " + PlayerSettings.WebGL.dataCaching);
        sb.AppendLine("  activeBuildTarget      = " + EditorUserBuildSettings.activeBuildTarget);
        return sb.ToString();
    }

    [MenuItem("Tools/Build5/B-4 Report Settings")]
    public static void Report() => Debug.Log("[B-4] settings\n" + Settings());

    [MenuItem("Tools/Build5/B-4 Apply Settings")]
    public static void Apply()
    {
        string before = Settings();

        // Gzip + decompression fallback rather than Unity's default Brotli: itch.io does not
        // send the Content-Encoding headers Brotli needs, and the symptom is a blank screen
        // with nothing in the Editor to warn you. The fallback decompresses in JS instead,
        // so no server header is required at all.
        PlayerSettings.WebGL.compressionFormat = WebGLCompressionFormat.Gzip;
        PlayerSettings.WebGL.decompressionFallback = true;
        PlayerSettings.WebGL.exceptionSupport = WebGLExceptionSupport.ExplicitlyThrownExceptionsOnly;
        PlayerSettings.defaultWebScreenWidth = 1920;
        PlayerSettings.defaultWebScreenHeight = 1080;
        PlayerSettings.runInBackground = true;

        AssetDatabase.SaveAssets();
        Debug.Log("[B-4] BEFORE\n" + before + "\n[B-4] AFTER\n" + Settings());
    }

    [MenuItem("Tools/Build5/B-5 Switch To WebGL")]
    public static void Switch()
    {
        if (EditorUserBuildSettings.activeBuildTarget == BuildTarget.WebGL)
        { Debug.Log("[B-5] already on WebGL"); return; }
        Debug.Log("[B-5] switching to WebGL (reimports assets, expect several minutes)");
        EditorUserBuildSettings.SwitchActiveBuildTarget(BuildTargetGroup.WebGL, BuildTarget.WebGL);
    }

    [MenuItem("Tools/Build5/B-5 Build")]
    public static void Build()
    {
        var scenes = EditorBuildSettings.scenes.Where(s => s.enabled).Select(s => s.path).ToArray();
        Debug.Log("[B-5] building " + scenes.Length + " scenes -> " + OUT + "\n  " + string.Join("\n  ", scenes));

        var report = BuildPipeline.BuildPlayer(new BuildPlayerOptions
        {
            scenes = scenes,
            locationPathName = OUT,
            target = BuildTarget.WebGL,
            options = BuildOptions.None
        });

        var s = report.summary;
        var sb = new StringBuilder();
        sb.AppendLine("[B-5] result   = " + s.result);
        sb.AppendLine("[B-5] time     = " + s.totalTime);
        sb.AppendLine("[B-5] size     = " + (s.totalSize / 1048576f).ToString("F1") + " MB");
        sb.AppendLine("[B-5] errors   = " + s.totalErrors + "   warnings = " + s.totalWarnings);
        foreach (var step in report.steps)
            foreach (var m in step.messages)
                if (m.type == LogType.Error || m.type == LogType.Warning)
                    sb.AppendLine("[B-5] " + m.type + ": " + m.content.Replace("\n", " ").Trim());
        Debug.Log(sb.ToString());
    }
}
#endif
