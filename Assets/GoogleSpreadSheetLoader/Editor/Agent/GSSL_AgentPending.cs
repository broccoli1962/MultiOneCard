using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Newtonsoft.Json;
using UnityEngine;

namespace GoogleSpreadSheetLoader.Agent
{
    /// <summary>
    /// Agent bridge files for pending/result JSON under .cursor/.
    /// Cache under Generated/Cache/ must only be written by GSSL — never hand-edited in real usage.
    /// </summary>
    public static class GSSL_AgentPending
    {
        public const string ModeUpdate = "update";
        public const string ModeRegenerate = "regenerate";

        public const string StatusRunning = "running";
        public const string StatusSuccess = "success";
        public const string StatusError = "error";
        public const string StatusBusy = "busy";

        private static string ProjectRoot =>
            Directory.GetParent(Application.dataPath)?.FullName ?? Application.dataPath;

        public static string PendingFilePath =>
            Path.Combine(ProjectRoot, ".cursor", "gssl-pending.json");

        public static string ResultFilePath =>
            Path.Combine(ProjectRoot, ".cursor", "gssl-result.json");

        [Serializable]
        public class PendingRequest
        {
            public string mode = ModeUpdate;
            public List<string> sheets = new();
        }

        [Serializable]
        public class AgentResult
        {
            public string status;
            public string mode;
            public List<string> sheets = new();
            public string message;
            public string finishedAt;
        }

        public static PendingRequest LoadPending()
        {
            if (!File.Exists(PendingFilePath))
                return null;

            var json = File.ReadAllText(PendingFilePath);
            if (string.IsNullOrWhiteSpace(json))
                return null;

            return JsonConvert.DeserializeObject<PendingRequest>(json);
        }

        public static void ClearPending()
        {
            if (!File.Exists(PendingFilePath))
                return;

            File.Delete(PendingFilePath);
        }

        public static void WriteResult(AgentResult result)
        {
            EnsureCursorDirectory();

            result.finishedAt = DateTime.Now.ToString("o");
            var json = JsonConvert.SerializeObject(result, Formatting.Indented);
            File.WriteAllText(ResultFilePath, json);
        }

        public static void WriteRunning(string mode, List<string> sheets)
        {
            WriteResult(new AgentResult
            {
                status = StatusRunning,
                mode = mode,
                sheets = sheets ?? new List<string>(),
                message = "GSSL agent process is running.",
            });
        }

        public static bool TryValidatePending(PendingRequest pending, out string errorMessage)
        {
            errorMessage = null;

            if (pending == null)
            {
                errorMessage = $"Pending file not found: {PendingFilePath}";
                return false;
            }

            if (pending.sheets == null || pending.sheets.Count == 0)
            {
                errorMessage = "Pending sheets list is empty.";
                return false;
            }

            if (pending.mode != ModeUpdate && pending.mode != ModeRegenerate)
            {
                errorMessage = $"Unsupported mode: {pending.mode}. Use '{ModeUpdate}' or '{ModeRegenerate}'.";
                return false;
            }

            var cachedSheetNames = GSSL_CacheManager.GetAllCachedSheets()
                .Select(info => info.sheetName)
                .ToHashSet();

            var missingSheets = pending.sheets
                .Where(sheetName => !string.IsNullOrWhiteSpace(sheetName) && !cachedSheetNames.Contains(sheetName))
                .Distinct()
                .ToList();

            if (missingSheets.Count > 0)
            {
                errorMessage =
                    "Sheets are not registered in cache_index.json: " +
                    string.Join(", ", missingSheets) +
                    ". Run spreadsheet-level sync in GSSL first.";
                return false;
            }

            return true;
        }

        private static void EnsureCursorDirectory()
        {
            var cursorDirectory = Path.Combine(ProjectRoot, ".cursor");
            if (!Directory.Exists(cursorDirectory))
                Directory.CreateDirectory(cursorDirectory);
        }
    }
}
