# MultiOneCard — Codex 프로젝트 지침

이 지침과 `.agents/skills/`는 이 저장소와 하위 폴더에만 적용한다. Unity 프로젝트 루트는 저장소 루트이며 Assets 경로는 그 안을 기준으로 해석한다.

## 공통 규칙

- 요청 범위만 변경하고 기존 작업을 보존한다. 현재 코드·설정을 먼저 확인하고 변경에 맞는 검증을 수행한다.
- `.meta`를 직접 작성·수정하지 않는다. Unity 임포터가 관리한다.
- `Assets/GoogleSpreadSheetLoader/Generated/**`, `Assets/Resources/Localize_*.json`은 직접 수정하지 않는다. Google Sheets 수정 → GSSL update로 반영한다. 임시 디버깅 예외는 사용자가 명시한 경우에만 해당한다.
- Input System 생성 C# 래퍼는 직접 수정하지 않는다. `.inputactions` 및 임포터 설정을 수정하고 재생성한다.
- 개발 작업을 완료하고 검증했으면 이번 작업 관련 변경만 한국어 형식으로 커밋한다. 문서·스킬 변환만으로 이 정책을 확대하지 않는다. 사용자의 커밋 금지 지시는 우선한다. `+커푸`는 관련 변경 커밋 후 일반 푸시까지 의미하며, 그 외에는 요청 없이 푸시하지 않는다.

## 작업별 스킬

아래 링크는 이 AGENTS.md 기준이다. 관련 작업을 시작할 때 해당 SKILL.md를 읽고 필요한 참조만 추가로 읽는다. 모든 스킬을 한꺼번에 읽지 않는다.

| 작업 | 스킬 |
|---|---|
| 프로젝트 개발·구조·에셋 소유권 | [multione-project-development](.agents/skills/multione-project-development/SKILL.md) |
| C# 코드 작성·수정 | [multione-unity-csharp-conventions](.agents/skills/multione-unity-csharp-conventions/SKILL.md) |
| 씬·매니저·리소스·풀링 | [multione-unity-scene-system](.agents/skills/multione-unity-scene-system/SKILL.md) |
| UI·HUD·팝업·MVP | [multione-unity-ui-system](.agents/skills/multione-unity-ui-system/SKILL.md) |
| 게임플레이 기능 추가 | [multione-add-gameplay-feature](.agents/skills/multione-add-gameplay-feature/SKILL.md) |
| 입력·바인딩 | [multione-setup-input-actions](.agents/skills/multione-setup-input-actions/SKILL.md) |
| Aseprite 픽셀아트 | [multione-aseprite-sprite-draw](.agents/skills/multione-aseprite-sprite-draw/SKILL.md) |
| GSSL 데이터 변경·동기화 | [multione-gssl-agent-workflow](.agents/skills/multione-gssl-agent-workflow/SKILL.md) |
| GSSL 시트 작성·서식 | [multione-gssl-sheet-writing](.agents/skills/multione-gssl-sheet-writing/SKILL.md) |
| 커밋·메시지·+커푸 | [multione-korean-git-commit](.agents/skills/multione-korean-git-commit/SKILL.md) |
| 전체·관심사별 커밋 | [multione-grouped-git-commit](.agents/skills/multione-grouped-git-commit/SKILL.md) |

GSSL 두 스킬은 원본의 명시 호출 정책을 유지한다. 사용자가 데이터·시트 변경을 요청하면 위 링크를 직접 읽어 적용한다.
프로젝트 지침은 여기와 `.agents/skills`에서 관리한다. Unity 에디터 작업은 프로젝트 루트에서 Unity CLI를 사용하고 `unity status --project-path . --json`으로 대상을 확인한다. 단, Unity GSSL 코드가 사용하는 `.cursor/gssl-pending.json`과 `.cursor/gssl-result.json` 경로는 유지한다.
