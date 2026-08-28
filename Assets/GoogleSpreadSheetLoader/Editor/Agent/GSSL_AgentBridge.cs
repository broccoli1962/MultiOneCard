using System;
using System.Collections.Generic;
using GoogleSpreadSheetLoader.OneButton;
using GoogleSpreadSheetLoader.Setting;
using UnityEditor;
using UnityEngine;

namespace GoogleSpreadSheetLoader.Agent
{
    /// <summary>
    /// Exposes GSSL sync entry points via MenuItem for Unity Skills.
    /// Real usage must go through Google Sheets + mode:update sync; do not hand-edit Generated/Cache.
    /// </summary>
    public static class GSSL_AgentBridge
    {
        private static bool _isRunning;

        [MenuItem("Tools/GSSL/Sync Pending Sheets", priority = 20)]
        public static void SyncPendingSheets()
        {
            ExecutePending(GSSL_AgentPending.ModeUpdate);
        }

        [MenuItem("Tools/GSSL/Regenerate Pending Sheets", priority = 21)]
        public static void RegeneratePendingSheets()
        {
            ExecutePending(GSSL_AgentPending.ModeRegenerate);
        }

        [MenuItem("Tools/GSSL/Reconnect Table Linker", priority = 22)]
        public static void ReconnectTableLinkerMenu()
        {
            if (!EnsureSettingReady() || TryRejectWhenBusy())
                return;

            var sheets = new List<string>();
            GSSL_AgentPending.WriteRunning("reconnect_table_linker", sheets);
            BeginAwaitable(
                GSSL_OneButton.ReconnectTableLinker(),
                "reconnect_table_linker",
                sheets,
                "Table linker reconnect completed.",
                clearPending: false);
        }

        [MenuItem("Tools/GSSL/Sync All Sheets", priority = 23)]
        public static void SyncAllSheets()
        {
            if (!EnsureSettingReady() || TryRejectWhenBusy())
                return;

            var sheets = new List<string>();
            GSSL_AgentPending.WriteRunning("sync_all", sheets);
            BeginAwaitable(
                GSSL_OneButton.OneButtonProcessSpreadSheet(false),
                "sync_all",
                sheets,
                "Full spreadsheet sync (update only) completed.",
                clearPending: false);
        }

        [MenuItem("Tools/GSSL/Cancel Process", priority = 40)]
        public static void CancelProcessMenu()
        {
            GSSL_OneButton.CancelCurrentProcess();

            GSSL_AgentPending.WriteResult(new GSSL_AgentPending.AgentResult
            {
                status = GSSL_AgentPending.StatusSuccess,
                mode = "cancel",
                sheets = new List<string>(),
                message = "GSSL process cancel requested.",
            });
        }

        private static void ExecutePending(string expectedMode)
        {
            if (!EnsureSettingReady())
                return;

            if (TryRejectWhenBusy())
                return;

            var pending = GSSL_AgentPending.LoadPending();
            if (!GSSL_AgentPending.TryValidatePending(pending, out var validationError))
            {
                WriteError(expectedMode, pending?.sheets ?? new List<string>(), validationError);
                return;
            }

            if (pending.mode != expectedMode)
            {
                WriteError(
                    expectedMode,
                    pending.sheets,
                    $"Pending mode mismatch. Expected '{expectedMode}', but pending mode is '{pending.mode}'.");
                return;
            }

            GSSL_AgentPending.WriteRunning(pending.mode, pending.sheets);

            Awaitable operation = pending.mode switch
            {
                GSSL_AgentPending.ModeUpdate => GSSL_OneButton.IndividualUpdateSelectedSheets(pending.sheets),
                GSSL_AgentPending.ModeRegenerate => GSSL_OneButton.RegenerateSelectedSheets(pending.sheets),
                _ => throw new InvalidOperationException($"Unsupported mode: {pending.mode}"),
            };

            var successMessage = pending.mode == GSSL_AgentPending.ModeUpdate
                ? "Individual sheet update completed."
                : "Selected sheet regeneration completed.";

            BeginAwaitable(operation, pending.mode, pending.sheets, successMessage, clearPending: true);
        }

        private static bool EnsureSettingReady()
        {
            if (GSSL_Setting.CheckAndCreate())
                return true;

            WriteError(
                string.Empty,
                new List<string>(),
                "GSSL SettingData를 로드하지 못했습니다. Assets/GoogleSpreadSheetLoader/SettingData.asset을 확인하세요.");
            return false;
        }

        private static void BeginAwaitable(
            Awaitable operation,
            string mode,
            List<string> sheets,
            string successMessage,
            bool clearPending)
        {
            _isRunning = true;

            EditorApplication.CallbackFunction tick = null;
            tick = () =>
            {
                if (!operation.IsCompleted)
                    return;

                EditorApplication.update -= tick;
                CompleteAwaitable(operation, mode, sheets, successMessage, clearPending);
            };
            EditorApplication.update += tick;
        }

        private static void CompleteAwaitable(
            Awaitable operation,
            string mode,
            List<string> sheets,
            string successMessage,
            bool clearPending)
        {
            try
            {
                operation.GetAwaiter().GetResult();

                if (clearPending)
                    GSSL_AgentPending.ClearPending();

                GSSL_AgentPending.WriteResult(new GSSL_AgentPending.AgentResult
                {
                    status = GSSL_AgentPending.StatusSuccess,
                    mode = mode,
                    sheets = sheets,
                    message = successMessage,
                });
            }
            catch (Exception ex)
            {
                WriteError(mode, sheets, ex.Message);
            }
            finally
            {
                _isRunning = false;
            }
        }

        private static bool TryRejectWhenBusy()
        {
            if (!GSSL_OneButton.IsProcessRunning && !_isRunning)
                return false;

            GSSL_AgentPending.WriteResult(new GSSL_AgentPending.AgentResult
            {
                status = GSSL_AgentPending.StatusBusy,
                mode = string.Empty,
                sheets = new List<string>(),
                message = "Another GSSL process is already running.",
            });
            return true;
        }

        private static void WriteError(string mode, List<string> sheets, string message)
        {
            Debug.LogError($"[GSSL Agent] {message}");

            GSSL_AgentPending.WriteResult(new GSSL_AgentPending.AgentResult
            {
                status = GSSL_AgentPending.StatusError,
                mode = mode,
                sheets = sheets ?? new List<string>(),
                message = message,
            });
        }
    }
}
