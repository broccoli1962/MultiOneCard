# GSSL Sheet Writing — Examples

## Common: schema + data

```
Row1  [#C9DAF8]  id-string | cost-float | duration-float | recipe_id-string
Row2  [white]    Item_A | 10 | 5 | Recipe_Temp
```

## Common: comment column

```
       A [#D9D9D9]     B [#C9DAF8 on R1]   C [#C9DAF8 on R1]
Row1   #               id-string           value-string
Row2   (memo)          production_...      10000
```

Trailing memo: empty header, column fill `#D9D9D9`.

## Localization: string sheets

시트 이름 예: `UIString_Localization`, `GameString_Localization` (이름에 Setting의 `Localization` 타입 문자열 포함).

```
Row1  [#C9DAF8]           # | id-string | Korean-string | English-string
Row2  [text #999999]      테스트 | test_string_* | … | …   ← keep; paste auto-translations here
Row3+ [data]              공통 | Confirm | 확인 | Confirm   ← add new keys below row 2
```

생성물 예: `Assets/Resources/Localize_Korean.json`, `Localize_English.json`.

## MCP format snippet

```json
{
  "backgroundColor": { "red": 0.7882353, "green": 0.85490197, "blue": 0.972549 }
}
```

Gray comment fill:

```json
{
  "backgroundColor": { "red": 0.8509804, "green": 0.8509804, "blue": 0.8509804 }
}
```
