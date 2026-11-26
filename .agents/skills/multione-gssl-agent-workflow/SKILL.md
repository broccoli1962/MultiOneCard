---
name: multione-gssl-agent-workflow
description: "GSSL 테이블·로컬라이즈를 Google Sheets에서 수정하고 현재 Unity Editor에서 선택 시트 동기화와 생성물 검증을 수행한다."
---

# GSSL Agent Workflow

Use this skill when the user asks to modify table data and refresh only the changed sheets through GSSL.

## Prerequisites

- Unity Editor is open on this project
- A connected Unity CLI supports Editor menu execution and console inspection; inspect its actual tool schema before calling it.
- A connected Google Sheets tool is available for Google Sheets read/write
- GSSL `SettingData` has a service account JSON absolute path configured (`Assets/GoogleSpreadSheetLoader/SettingData.asset`)
- Each target spreadsheet is shared with the service account email (viewer or higher)
- Target sheets already exist in `Assets/GoogleSpreadSheetLoader/Generated/Cache/cache_index.json`

## Service account setup

1. Create or use a Google Cloud service account with Sheets API enabled
2. Download the JSON key to a location outside the Unity project (example: `C:/credentials/gssl-service-account.json`)
3. In Unity: `Window > Google SpreadSheet Loader` -> Settings -> set **서비스 계정 JSON 경로** (absolute path or use **찾아보기**)
4. Share each spreadsheet with the `client_email` from the JSON (viewer+)

The credential file must remain outside git. The path itself may be serialized in `SettingData.asset`; inspect its diff for local configuration before staging.

## End-to-end flow

1. Inspect schema from `Assets/GoogleSpreadSheetLoader/Generated/Cache/cache_index.json` and `Generated/Cache/<SheetName>.txt` (**read only**)
2. Modify Google Sheets through MCP gsheets
3. Write `.cursor/gssl-pending.json`
4. Trigger the Unity menu through the available Editor integration
5. Poll the current run’s `.cursor/gssl-result.json` until `status` is `success` or `error` (not `running`)
6. Inspect compilation and console errors through the available Editor integration
7. Report git diff for generated scripts/assets/localization files

## Cache policy (mandatory)

`Assets/GoogleSpreadSheetLoader/Generated/Cache/` files are **GSSL-owned generated artifacts**.

`Assets/Resources/Localize_*.json` is also **GSSL-generated** from Localization sheets. Never hand-edit it.

### Never in real usage

- Do **not** edit `Generated/Cache/*.txt` to apply table data changes
- Do **not** hand-edit `Assets/Resources/Localize_*.json` (or other GSSL outputs) to add/change keys or values
- Do **not** hand-edit cache as a shortcut when sync fails
- Do **not** use `mode: "regenerate"` right after MCP sheet edits as a substitute for download

### Allowed uses of cache files

- **Read only** for schema/column inspection before editing Google Sheets
- **Read only** for verifying that `mode: "update"` sync refreshed cache after GSSL completed
- **Logic inspection only** when the user explicitly asks to debug GSSL/sync behavior

### If sync fails after Google Sheets edit

1. Read `.cursor/gssl-result.json` and Unity console errors
2. Retry `Tools/GSSL/Sync Pending Sheets`
3. Diagnose GSSL/Agent Bridge issues
4. Tell the user what blocked sync

Do **not** patch cache or `Localize_*.json` manually unless the user explicitly requests a temporary logic/debug investigation.

### Correct data path

```
Google Sheets (source of truth)
  -> mode: update (IndividualUpdateSelectedSheets)
  -> Generated/Cache/*.txt (written by GSSL)
  -> Generated scripts / ScriptableObjects / Localize_*.json
```

## Pending file

Path: `.cursor/gssl-pending.json`

```json
{
  "mode": "update",
  "sheets": ["ProductionBuilding"]
}
```

- `mode: "update"` -> download selected sheets from Google Sheets, then regenerate scripts/assets
- `mode: "regenerate"` -> regenerate from local cache only; do not use right after MCP sheet edits

## Unity menu commands

Use the connected Unity CLI's documented menu-execution operation. Do not assume a Python `unity_skills` module exists. Menu paths in this project:

- `Tools/GSSL/Sync Pending Sheets`
- `Tools/GSSL/Regenerate Pending Sheets`
- `Tools/GSSL/Reconnect Table Linker`
- `Tools/GSSL/Cancel Process`

Use `Sync Pending Sheets` after Google Sheets edits.

## Result file

Path: `.cursor/gssl-result.json`

Expected success shape:

```json
{
  "status": "success",
  "mode": "update",
  "sheets": ["ProductionBuilding"],
  "message": "Individual sheet update completed.",
  "finishedAt": "2026-07-10T12:00:00.0000000+09:00"
}
```

Status values:

- `running` -> process started
- `success` -> completed; pending file is deleted
- `error` -> validation or execution failed; inspect `message`
- `busy` -> another GSSL process is already running

## Sheet editing rules

- Header format is `column-type` (example: `id-string`, `idx-int`, `cost-string`)
- Sheet names map to generated types:
  - `ProductionBuilding` -> `ProductionBuildingData`, `ProductionBuildingTable`
- `spreadSheetId` and sheet registration come from `cache_index.json`
- If a sheet is missing from cache, tell the user to run spreadsheet-level sync in GSSL first

## Related sheet expansion

GSSL already expands generation scope internally:

- Localization sheet selected -> all cached Localization sheets participate in generation
- `*string*` sheet selected -> download only selected sheets, but generation may include all cached `*string*` sheets

Do not manually add unrelated sheets to pending unless the user explicitly wants them refreshed.

## Typical request handling

### Modify one row and sync

1. Read cache/schema for the target sheet (**read only**)
2. Find row by key column through MCP gsheets
3. Update the cell(s) in Google Sheets
4. Write pending:

```json
{
  "mode": "update",
  "sheets": ["ProductionBuilding"]
}
```

5. Execute `Tools/GSSL/Sync Pending Sheets`
6. Poll the current run’s `.cursor/gssl-result.json` until completion
7. Read result and inspect generated outputs under:
   - `Assets/GoogleSpreadSheetLoader/Generated/Script/`
   - `Assets/GoogleSpreadSheetLoader/Generated/SerializeObject/TableData/`
   - `Assets/Resources/Localize_*.json` when localization sheets were involved

### Regenerate only from cache

Use only when Google Sheets were not changed and local cache is already correct:

```json
{
  "mode": "regenerate",
  "sheets": ["ProductionBuilding"]
}
```

Then execute `Tools/GSSL/Regenerate Pending Sheets`.

## Do not use

- `OneButtonProcessSpreadSheet` for chat-driven partial updates
- batchmode Unity execution while the Editor is already open
- `mode: "regenerate"` immediately after MCP sheet edits
- **Direct edits to `Generated/Cache/*.txt` in real usage** (see Cache policy above)

## Verification checklist

- `.cursor/gssl-result.json` reports `success`
- `.cursor/gssl-pending.json` is removed after success
- target cache file under `Generated/Cache/` is updated for `mode: "update"`
- generated C# / ScriptableObject outputs reflect the requested change
- no compile errors after sync

## Related

- Sheet layout / colors / Localization row conventions: [multione-gssl-sheet-writing](../multione-gssl-sheet-writing/SKILL.md)
- Generated-data rule: [생성물 정책](../multione-gssl-agent-workflow/references/generated-data.md)
- Inspect the installed GSSL source for runtime contracts; do not assume the repository root README documents GSSL.

## Codex execution boundaries

All Assets and `.cursor/gssl-*.json` paths are relative to the Unity project root, not the Git root. The `.cursor` protocol paths are hardcoded in `GSSL_AgentPending.cs`; keep them unchanged.
If no Sheets write connector is available, report the missing capability and prepare the requested edits without substituting generated-file edits.
Verify the result belongs to the run just triggered (timestamp, sheet list, mode); an old success file is not evidence of completion. Do not resubmit sheet writes when retrying only the sync. On busy, wait for the existing process; on repeated unchanged errors or no progress for five minutes, report the blocker instead of endlessly retrying. Retry sync only after identifying a transient error or fixing its cause.
Read [generated data](references/generated-data.md) and [cache policy](references/cache-policy.md) when handling these outputs. For a user-requested temporary debugging exception, isolate it and do not present it as a table data update.
