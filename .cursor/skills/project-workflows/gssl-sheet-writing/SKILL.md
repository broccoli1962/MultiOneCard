---
name: gssl-sheet-writing
description: >-
  Write and format Google Spreadsheets consumed by GSSL (sky-blue name-type
  header rows, gray comment columns, Localization test-row convention). Use when
  creating or editing GSSL tables, schema/type rows, comment/note columns,
  Localization-style string sheets, or applying sheet cell colors for table data.
disable-model-invocation: true
---

# GSSL Sheet Writing

GSSL 테이블용 **Google Sheets 작성·서식** 규칙. 동기화는 `gssl-agent-workflow`를 따른다.

프로젝트 UI에 키를 연결하는 방식(컴포넌트, 확장 메서드 등)은 **소비 프로젝트** 책임이다. 이 스킬은 시트·서식·GSSL 생성 계약만 다룬다.

## Source of truth

```
Google Sheets (MCP user-mcp-gsheets)
  → .cursor/gssl-pending.json (mode: update)
  → Tools/GSSL/Sync Pending Sheets
  → Generated/Cache · scripts · Localize_*.json
```

**절대 금지:** `Assets/GoogleSpreadSheetLoader/Generated/**`, `Assets/Resources/Localize_*.json` 직접 편집.

스키마 확인은 Cache **읽기 전용**: `cache_index.json`, `Generated/Cache/<Sheet>.txt`.

## Colors (verified conventions)

| 역할 | Hex | RGB (0–1, Sheets API) | 예 |
|------|-----|------------------------|----|
| **변수명+타입 행** (하늘색) | `#C9DAF8` | `0.7882353, 0.85490197, 0.972549` | Common 테이블 row 1 |
| **코멘트/메모 열** (회색) | `#D9D9D9` | `0.8509804, 0.8509804, 0.8509804` | `#` 열, 트레일링 메모열 |
| 코멘트 열 변형 | `#999999` | `0.6, 0.6, 0.6` | 중간 메모열 (기존 시트 맞춤) |
| Localization **테스트 행** 글자색 | `#999999` | `0.6, 0.6, 0.6` | Localization 시트 row 2 |

MCP `sheets_format_cells` / `sheets_batch_format_cells` 시 `backgroundColor`에 위 RGB 분수를 쓴다.

## Common table layout (tableStyle Common)

대부분 게임/콘텐츠 테이블:

| 행 | 역할 |
|----|------|
| **Row 1** | 스키마 행: `변수명-타입` 헤더. 데이터 컬럼만 **하늘색 `#C9DAF8`** |
| **Row 2+** | 데이터 행 (흰 배경) |

### Header format

- `name-type` 한 셀 (예: `id-string`, `idx-int`, `base_price-float`, `type-ItemType`)
- GSSL은 헤더에 `-`가 **없으면** 코드젠 필드로 쓰지 않는다 → 코멘트 열에 적합

### Comment / note columns

- 열 헤더: `#` 또는 **빈 문자열** (타입 없음)
- 열 전체(또는 타입 행 포함) **회색 `#D9D9D9`** 채움 (기존 시트가 `#999999`면 그 시트에 맞춤)
- 데이터 열 사이·맨 끝 어디에든 둘 수 있음
- 코멘트 열은 런타임 필드가 아님 — 사람용 메모만

### New column / sheet checklist

1. Cache로 기존 스키마 확인 (읽기)
2. 시트에 `name-type` 헤더 추가 → row 1 해당 셀 `#C9DAF8`
3. 메모 열이면 `#`/빈 헤더 + `#D9D9D9`, 타입 행 하늘색에 섞지 않음
4. 데이터 행 채움
5. `gssl-agent-workflow`로 Sync Pending (`mode: update`)

## Localization sheets (tableStyle Localization)

시트 **이름**에 Setting의 localization 타입 문자열(기본값 `Localization`)이 포함되어야 한다. 실제 시트명은 프로젝트마다 다르다 (예: `UIString_Localization`, `GameString_Localization`).

| 행 | 역할 | 서식 |
|----|------|------|
| **Row 1** | 스키마: `#`(선택), `id-string`, `{Language}-string` … | 하늘색 `#C9DAF8` |
| **Row 2** | **테스트 행** (자동번역·파이프라인용, 권장) | 글자색 `#999999`; 삭제·덮어쓰기 금지 |
| **Row 3+** | 실제 로컬라이즈 키 | 일반 데이터 |

### Rules

- 키 열 헤더는 `id-string` (또는 `ID-string`). GSSL이 이 열을 JSON `Key`로 사용한다
- 나머지 `*-string` 언어 열마다 `Assets/Resources/Localize_{열이름}.json` 생성 (예: `Korean-string` → `Localize_Korean.json`)
- 새 키는 **항상 row 2(테스트 행) 아래**에 추가 (테스트 행을 쓰는 경우)
- 테스트 행 ID 예: `test_string_ui`, `test_string_general` — 프로젝트에서 자유롭게 정하되 유지
- `#` 열: 카테고리/메모. 그룹별 색을 써도 되지만 **테스트 행을 지우지 않는 것**이 핵심
- 가변 문구는 시트에 `{0}` 플레이스홀더
- UI/코드에 키를 연결하는 방법은 소비 프로젝트 가이드를 따른다

GSSL Localize 생성은 row 1 이후 **모든 행**(테스트 행 포함)을 JSON에 넣는다. 테스트 키 유지는 의도된 동작이다.

## EnumType sheets (tableStyle EnumType)

예: Enum 정의 시트. `EnumName` / `EnumName-idx` 쌍 + 열 그룹별 색상. **일반 Common 하늘색 타입 행 규칙을 강제하지 않는다.** 기존 시트 패턴을 복제한다.

## Workflow (edit → sync)

1. `cache_index.json`에서 `spreadSheetId` / `sheetName` / `tableStyle` 확인
2. MCP gsheets로 값·서식 수정 (`sheets_get_values`, `sheets_update_values`, `sheets_format_cells` …)
3. `.cursor/gssl-pending.json` → `{ "mode": "update", "sheets": ["SheetName"] }`
4. Unity: `Tools/GSSL/Sync Pending Sheets`
5. `.cursor/gssl-result.json` `success` 확인 후 Generated diff만 커밋 대상

상세: `gssl-agent-workflow`.

## Anti-patterns

| Don't | Do |
|-------|----|
| Generated Cache / `Localize_*.json` 손편집 | Sheets → GSSL update |
| 타입 행을 흰색으로 두기 / 데이터 행을 하늘색으로 | row 1만 `#C9DAF8` |
| 코멘트 열에 `foo-string` 같은 타입 헤더 | `#` 또는 빈 헤더 + 회색 |
| Localization을 Common처럼 row 2부터 실데이터만 채움 | row 2 = 테스트(권장), row 3+ = 키 |
| Localization 테스트 행 삭제·ID 변경 | 유지하고 그 아래에 추가 |
| 시트 수정 직후 `mode: regenerate` | `mode: update` |
| EnumType에 Common 서식 강제 | 기존 Enum 시트 패턴 유지 |
| 이 패키지 스킬에 프로젝트 UI 바인딩을 의존 | 시트 계약만 여기, UI는 프로젝트 쪽 |

## Related

- Sync: `gssl-agent-workflow`
- Rules: `.cursor/rules/gssl-generated-data.mdc`, `gssl-agent-cache.mdc`
- 패키지 README: Localization / Agent 동기화 절
- 짧은 예시: `examples.md`
