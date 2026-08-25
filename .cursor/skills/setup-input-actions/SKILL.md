---
name: setup-input-actions
description: >-
  Unity Input System 입력을 프로젝트/기능에 맞게 생성·교체·적용한다.
  .inputactions 생성/편집, Generate C# Class 재생성, 생성 래퍼를 수정 없이 구독·배선할 때 사용.
  특정 프로젝트 경로에 묶이지 않으며, 작업 전 해당 프로젝트의 기존 입력 구조를 탐색한다.
---

# Skill: Input System 입력 생성·적용 (멀티 프로젝트)

입력 바인딩은 **프로젝트·기능마다 다르다.** 다른 프로젝트의 에셋/클래스명을 가정하지 말고,
**현재 프로젝트에서 기존 입력 구조를 먼저 탐색**한 뒤 같은 관례로 생성·적용한다.

## 절대 규칙: 생성 C# 래퍼는 손편집 금지

Input Actions 임포터의 **Generate C# Class** 로 나온 `.cs` 는 **수정하지 않는다.**
그대로 생성·재생성하고, 게임 코드에서는 **그 API를 호출·구독만** 한다.

| 해도 됨 | 하지 말 것 |
|---------|------------|
| `.inputactions` 생성/편집 | 생성 래퍼 `.cs` 직접 수정 |
| Generate C# Class 켜고 재임포트로 재생성 | 생성 래퍼에 메서드/필드/네임스페이스 손대기 |
| 게임측 코드에서 생성 클래스 인스턴스화·콜백 구독 | 생성물을 “맞게” 고치려는 리팩터 |

바인딩·액션·맵 변경 → **에셋만** 고치고 → **재생성**. 생성물이 기대와 다르면 에셋/임포터 설정을 고친다.

## 계층 (흐름 유지)

```
.inputactions  (편집/생성 O)
   │  Generate C# Class 재임포트
   ▼
생성 래퍼 .cs  (손편집 X — 그대로 사용)
   │  인스턴스화 + 콜백/폴링 구독만
   ▼
프로젝트의 입력 이음새 (있을 때만; 예: InputSystem 파사드)
   │
   ▼
구독자 / UI 뒤로가기 등
```

프로젝트를 탐색해 실제 이음새·구독 경로를 확인한다. 없으면 생성 래퍼를 직접 구독하는 최소 배선을 추가한다.

## 절차

### 1) 프로젝트 탐색 (필수)

하드코딩된 경로를 쓰지 말고 현재 워크스페이스에서 찾는다.

- `**/*.inputactions`
- Generate C# Class 산출물로 보이는 입력 래퍼 `.cs` (보통 에셋과 짝)
- 입력을 감싸는 게임측 클래스(이름·네임스페이스는 프로젝트마다 다름)
- Cancel/Back 을 UI에 연결하는 구독부

이번 기능에 필요한 **액션 맵 / 액션 / 컨트롤 타입**을 정한다.
기존 공개 계약(이벤트·Observable·콜백 시그니처)이 있으면 **유지**하고, 새 개념은 **추가**한다.

### 2) Input Actions 에셋 생성/편집

- 새 기능이면 `.inputactions` 를 새로 만들거나, 프로젝트 관례 경로에 둔다.
- `.inputactions` 는 JSON. `manage_asset` 또는 에디터로 편집.
- 기존 계약 유지가 필요하면 **액션 맵/액션 이름**을 맞춘다.
- 바인딩 변경은 **에셋에서만**.

### 3) C# 래퍼 생성 (Generate C# Class) — 생성 후 수정하지 않음

임포터에서 Generate C# Class 를 켜고, **그 프로젝트의** 클래스명/네임스페이스/경로를 지정한 뒤 재임포트한다.
경로는 탐색 결과 또는 사용자 지정값을 쓴다(다른 프로젝트 예시를 그대로 복사하지 말 것).

`unityMCP` `execute_code` 예시(속성명은 Input System 버전에 따라 다를 수 있음 → 실패 시 임포터/`unity_reflect` 확인):

```csharp
// execute_code (UnityEditor) — path/class/namespace 는 현재 프로젝트 값으로 교체
var path = "<Assets/.../YourActions.inputactions>";
var importer = UnityEditor.AssetImporter.GetAtPath(path);
var so = new UnityEditor.SerializedObject(importer);
so.FindProperty("m_GenerateWrapperCode").boolValue = true;
so.FindProperty("m_WrapperClassName").stringValue = "<GeneratedClassName>";
so.FindProperty("m_WrapperCodeNamespace").stringValue = "<Project.Namespace>";
// 필요 시: so.FindProperty("m_WrapperCodePath").stringValue = "<Assets/.../GeneratedClassName.cs>";
so.ApplyModifiedPropertiesWithoutUndo();
importer.SaveAndReimport();
return "reimported";
```

- 생성/재생성 후 **해당 `.cs` 를 편집하지 않는다.**
- `refresh_unity` → `read_console` 로 컴파일 확인.

### 4) 게임측 배선 (생성물 사용만)

- 생성 래퍼를 **있는 그대로** 인스턴스화하고, 노출된 액션에 콜백/폴링을 연결한다.
- 프로젝트에 입력 파사드가 있으면 그 패턴을 따른다. **공개 계약(이름·타입·의미)은 유지**, 필요 시 새 이벤트만 추가.
- 생성 래퍼 내용을 “정리”하거나 프로젝트 스타일에 맞게 고치지 않는다.

### 5) 취소/뒤로가기

Cancel/Back 액션을 바꿨다면, 탐색으로 찾은 UI 연결부만 새 액션에 맞게 갱신한다. 무관한 UI 로직은 건드리지 않는다.

### 6) 검증 (`verify-in-unity`)

`refresh_unity`(compile) → `read_console`(Error 0) → 필요 시 플레이로 입력 확인 후 `stop`.

## 체크리스트

- [ ] 현재 프로젝트의 `.inputactions` / 생성 래퍼 / 이음새를 **탐색**한 뒤 작업
- [ ] 바인딩은 `.inputactions` 에서만 편집
- [ ] 생성 래퍼 `.cs` 는 Generate C# Class 로만 만들·갱신하고 **손편집 없음**
- [ ] 게임 코드는 생성 API를 **호출·구독만**
- [ ] 기존 공개 입력 계약 유지(변경 시 구독자 동반 갱신)
- [ ] Cancel/Back 변경 시 UI 연결부만 갱신
- [ ] `refresh_unity` + `read_console` 검증, `.meta` 미생성
