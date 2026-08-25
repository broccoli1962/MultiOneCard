using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace GoogleSpreadSheetLoader.Export
{
    /// <summary>
    /// Exports the distributable <c>GoogleSpreadSheetLoader.unitypackage</c> at the repo root.
    /// Batchmode: <c>Unity -batchmode -nographics -quit -projectPath ... -executeMethod GoogleSpreadSheetLoader.Export.GSSL_ExportUnityPackage.Export</c>
    /// </summary>
    public static class GSSL_ExportUnityPackage
    {
        public const string PackageAssetRoot = "Assets/GoogleSpreadSheetLoader";
        public const string OutputFileName = "GoogleSpreadSheetLoader.unitypackage";
        public const string ResultFileRelativePath = "Logs/gssl-export-unitypackage-result.json";

        private static readonly string[] ExcludedPathFragments =
        {
            "/Generated/",
            "/Generated.meta",
            "SettingData.asset",
        };

        [MenuItem("Tools/GSSL/Export Unity Package", priority = 50)]
        public static void ExportFromMenu()
        {
            if (!TryExport(out var error, out var outputAbsolutePath, out var assetCount))
            {
                EditorUtility.DisplayDialog("GSSL Export", error, "OK");
                Debug.LogError($"[GSSL_ExportUnityPackage] {error}");
                return;
            }

            EditorUtility.DisplayDialog(
                "GSSL Export",
                $"Exported {assetCount} assets to:\n{outputAbsolutePath}",
                "OK");
        }

        /// <summary>
        /// Entry point for <c>-executeMethod</c> (local script / CI).
        /// </summary>
        public static void Export()
        {
            var ok = TryExport(out var error, out var outputAbsolutePath, out var assetCount);
            WriteResultFile(ok, error, outputAbsolutePath, assetCount);

            if (!ok)
            {
                Debug.LogError($"[GSSL_ExportUnityPackage] {error}");
                if (Application.isBatchMode)
                    EditorApplication.Exit(1);
                return;
            }

            Debug.Log($"[GSSL_ExportUnityPackage] Exported {assetCount} assets → {outputAbsolutePath}");
            // 성공 시 Exit(0)하지 않음 — CLI의 -quit가 종료 코드를 담당.
        }

        public static bool TryExport(out string error, out string outputAbsolutePath, out int assetCount)
        {
            error = null;
            outputAbsolutePath = null;
            assetCount = 0;

            if (!AssetDatabase.IsValidFolder(PackageAssetRoot))
            {
                error = $"Missing folder: {PackageAssetRoot}";
                return false;
            }

            var projectRoot = Path.GetFullPath(Path.Combine(Application.dataPath, ".."));
            outputAbsolutePath = Path.Combine(projectRoot, OutputFileName);

            var assets = CollectExportAssetPaths();
            if (assets.Length == 0)
            {
                error = "No assets to export under Assets/GoogleSpreadSheetLoader (after exclusions).";
                return false;
            }

            assetCount = assets.Length;

            try
            {
                var logsDir = Path.Combine(projectRoot, "Logs");
                if (!Directory.Exists(logsDir))
                    Directory.CreateDirectory(logsDir);

                AssetDatabase.ExportPackage(assets, outputAbsolutePath, ExportPackageOptions.Default);
            }
            catch (Exception e)
            {
                error = $"ExportPackage failed: {e.Message}";
                return false;
            }

            if (!File.Exists(outputAbsolutePath) || new FileInfo(outputAbsolutePath).Length == 0)
            {
                error = $"Export finished but package missing or empty: {outputAbsolutePath}";
                return false;
            }

            return true;
        }

        private static string[] CollectExportAssetPaths()
        {
            var list = new List<string>();

            if (AssetDatabase.IsValidFolder(PackageAssetRoot))
                list.Add(PackageAssetRoot);

            var rootMeta = PackageAssetRoot + ".meta";
            if (File.Exists(Path.Combine(Application.dataPath, "..", rootMeta)))
                list.Add(rootMeta);

            foreach (var path in AssetDatabase.GetAllAssetPaths())
            {
                if (!path.StartsWith(PackageAssetRoot + "/", StringComparison.Ordinal))
                    continue;
                if (IsExcluded(path))
                    continue;
                list.Add(path);
            }

            return list.Distinct(StringComparer.Ordinal).OrderBy(p => p, StringComparer.Ordinal).ToArray();
        }

        private static bool IsExcluded(string assetPath)
        {
            foreach (var fragment in ExcludedPathFragments)
            {
                if (assetPath.IndexOf(fragment, StringComparison.OrdinalIgnoreCase) >= 0)
                    return true;
            }

            // Folder marker without trailing slash
            if (assetPath.Equals(PackageAssetRoot + "/Generated", StringComparison.OrdinalIgnoreCase))
                return true;

            return false;
        }

        private static void WriteResultFile(bool success, string error, string outputAbsolutePath, int assetCount)
        {
            try
            {
                var projectRoot = Path.GetFullPath(Path.Combine(Application.dataPath, ".."));
                var resultPath = Path.Combine(projectRoot, ResultFileRelativePath);
                var dir = Path.GetDirectoryName(resultPath);
                if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
                    Directory.CreateDirectory(dir);

                var json =
                    "{\n" +
                    $"  \"success\": {(success ? "true" : "false")},\n" +
                    $"  \"assetCount\": {assetCount},\n" +
                    $"  \"output\": \"{EscapeJson(outputAbsolutePath ?? string.Empty)}\",\n" +
                    $"  \"error\": \"{EscapeJson(error ?? string.Empty)}\",\n" +
                    $"  \"unityVersion\": \"{EscapeJson(Application.unityVersion)}\",\n" +
                    $"  \"timestampUtc\": \"{DateTime.UtcNow:o}\"\n" +
                    "}\n";

                File.WriteAllText(resultPath, json);
            }
            catch (Exception e)
            {
                Debug.LogWarning($"[GSSL_ExportUnityPackage] Failed to write result file: {e.Message}");
            }
        }

        private static string EscapeJson(string value) =>
            value.Replace("\\", "\\\\").Replace("\"", "\\\"").Replace("\n", "\\n").Replace("\r", "\\r");
    }
}
