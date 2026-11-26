---
name: multione-project-development
description: "MultiOneCard 개발 시 프로젝트 구조, 최소 변경 원칙, Unity 에셋 소유권을 적용한다."
---

# MultiOneCard 프로젝트 개발

이 저장소의 Unity 프로젝트는 저장소 루트에 있다. 이 문서의 Assets 경로는 Unity 프로젝트 기준이며, 실행 전 `Assets`, `Packages`, `ProjectSettings`가 있는 루트를 확인한다.

## 현재 구조

- `Assets/GameResource/Scripts/Object/`: Management, Net, UI. 카드 규칙과 앱·네트워크 계층은 Scripts/Rules, Scripts/App, Scripts/Net에 있다.
- `Assets/GameResource/Scripts/Util/`: Addressable, Attribute, Input, Interface, Enum, Management 등.
- `Assets/Scenes/`: 씬. `Assets/GameResource/Prefab/`: 프리팹.
- UniTask, Addressables, Input System, R3, LitMotion을 사용한다. 버전은 `Packages/manifest.json`에서 확인한다.
- 리소스는 ResourceManager, 풀은 ObjectPoolManager, 사운드는 AudioManager를 사용한다.
- 씬 진입·정리는 SceneContext/AppFlow, 관리형 UI는 UIManager가 소유한다. 관련 작업에서 각각의 스킬을 읽는다.

## 변경 원칙

요청을 검증 가능한 결과로 정하고 가장 가까운 기존 구현을 먼저 확인한다. 결과를 바꿀 중요한 모호성만 질문하고, 합리적인 가정은 밝힌 뒤 진행한다. 요청과 무관한 정리, 새 추상화, 구조 개편은 추가하지 않는다. 이번 변경이 만든 미사용 코드만 정리하고 기존 작업은 보존한다. 검증은 변경 위험에 맞게 선택한다.

## Unity 에셋

`.meta`를 직접 생성하거나 편집하지 않는다. Unity 임포터가 생성하게 하고 필요한 경우 Unity가 만든 파일을 관련 에셋과 함께 포함한다. 에셋 이동도 GUID 보존을 위해 Unity의 에셋 이동 기능을 사용한다.

앱 종료 중 매니저 접근·인스턴스 생성·풀 반환 전 `GameStateUtil.IsQuitting`을 확인한다. 생성 입력 래퍼와 GSSL 산출물은 각 생성 경로로 갱신한다.
