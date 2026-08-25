---
name: gssl-agent-workflow
description: Automate Google SpreadSheet Loader (GSSL) table edits and selective sheet sync from chat. Use when modifying spreadsheet-backed table data, writing pending sync requests, triggering GSSL MenuItem sync in the open Unity Editor, or verifying generated table assets and scripts.
disable-model-invocation: true
---

# GSSL Agent Workflow

Use this skill when the user asks to modify table data and refresh only the changed sheets through GSSL.

## Prerequisites

- Unity Editor is open on this project
- Unity Skills server is running (`Window > UnitySkills > Start Server`)
- MCP gsheets (`user-mcp-gsheets`) is available for Google Sheets read/write
- GSSL `SettingData` has a service account JSON absolute path configured (`Assets/GoogleSpreadSheetLoader/SettingData.asset`)
- Each target spreadsheet is shared with the service account email (viewer or higher)
- Target sheets already exist in `Assets/GoogleSpreadSheetLoader/Generated/Cache/cache_index.json`

## Service account setup

1. Create or use a Google Cloud service account with Sheets API enabled
2. Download the JSON key to a location outside the Unity project (example: `C:/credentials/gssl-service-account.json`)
3. In Unity: `Window > Google SpreadSheet Loader` -> Settings -> set **서비스 계정 JSON 경로** (absolute path or use **찾아보기**)
4. Share each spreadsheet with the `client_email` from the JSON (viewer+)

The JSON file path is stored in `SettingData.asset` and is not committed to git when kept outside the project.

## End-to-end flow

1. Inspect schema from `Assets/GoogleSpreadSheetLoader/Generated/Cache/cache_index.json` and `Generated/Cache/<SheetName>.txt` (**read only**)
2. Modify Google Sheets through MCP gsheets
3. Write `.cursor/gssl-pending.json`
4. Trigger Unity menu through Unity Skills
5. Poll `.cursor/gssl-result.json` until `status` is `success` or `error` (not `running`)
6. Optionally run `debug_check_compilation`
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

Trigger with Unity Skills:

```python
unity_skills.call_skill("editor_execute_menu", menuPath="Tools/GSSL/Sync Pending Sheets")
unity_skills.call_skill("editor_execute_menu", menuPath="Tools/GSSL/Regenerate Pending Sheets")
unity_skills.call_skill("editor_execute_menu", menuPath="Tools/GSSL/Reconnect Table Linker")
unity_skills.call_skill("editor_execute_menu", menuPath="Tools/GSSL/Cancel Process")
```

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
6. Poll `.cursor/gssl-result.json` until completion
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

- Sheet layout / colors / Localization row conventions: `gssl-sheet-writing`
- Generated-data rule: `.cursor/rules/gssl-generated-data.mdc`
- Package overview: `README.md` (Agent sync, Localization)
