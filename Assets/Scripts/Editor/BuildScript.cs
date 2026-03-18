using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;
using System.IO;

/// <summary>
/// Script de build para compilación headless (CI/CD).
/// Configura y ejecuta el build desde línea de comandos.
/// </summary>
public static class BuildScript
{
    /// <summary>
    /// Punto de entrada para build desde CLI:
    /// Unity -executeMethod BuildScript.BuildMacOS
    /// </summary>
    public static void BuildMacOS()
    {
        string buildPath = GetArg("-buildOutput") ?? "Build/DinoMax.app";

        // Asegurar que el directorio existe
        string dir = Path.GetDirectoryName(buildPath);
        if (!string.IsNullOrEmpty(dir))
            Directory.CreateDirectory(dir);

        // Obtener todas las escenas habilitadas
        string[] scenes = GetScenes();
        if (scenes.Length == 0)
        {
            Debug.LogWarning("[BuildScript] No scenes found — using MainScene by default.");
            scenes = new[] { "Assets/Scenes/MainScene.unity" };
        }

        Debug.Log($"[BuildScript] Building {scenes.Length} scene(s) → {buildPath}");

        var options = new BuildPlayerOptions
        {
            scenes           = scenes,
            locationPathName = buildPath,
            target           = BuildTarget.StandaloneOSX,
            options          = BuildOptions.None
        };

        BuildReport  report  = BuildPipeline.BuildPlayer(options);
        BuildSummary summary = report.summary;

        if (summary.result == BuildResult.Succeeded)
        {
            Debug.Log($"[BuildScript] ✅ Build succeeded! Size: {summary.totalSize / 1024 / 1024} MB");
            EditorApplication.Exit(0);
        }
        else
        {
            Debug.LogError($"[BuildScript] ❌ Build failed: {summary.result}");
            EditorApplication.Exit(1);
        }
    }

    // ─── Helpers ────────────────────────────────────────────────

    private static string[] GetScenes()
    {
        var scenes = new System.Collections.Generic.List<string>();
        foreach (var scene in EditorBuildSettings.scenes)
        {
            if (scene.enabled)
                scenes.Add(scene.path);
        }
        return scenes.ToArray();
    }

    private static string GetArg(string name)
    {
        string[] args = System.Environment.GetCommandLineArgs();
        for (int i = 0; i < args.Length - 1; i++)
            if (args[i] == name) return args[i + 1];
        return null;
    }
}
