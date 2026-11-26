---
name: multione-unity-scene-system
description: "MultiOneCard 씬 전환, SceneContext, 매니저, 풀링과 Addressables 로드를 구현·수정한다."
---

# Unity Scene System

## 씬 진입점 (SceneContext)

`SceneContext` 는 `MonoBehaviour` 가 아닌 일반 abstract 클래스다. 씬에 배치하지 않으며, `SceneId` 별로 `SceneContexts` 레지스트리(팩토리)에 등록하고 `AppFlow` 가 단계별로 직접 `await` 한다.

- `PreloadAsync()` — 씬 활성화 **전**. 이 프로젝트는 기존 화면을 유지한 채 다음 씬에 필요한 Addressables를 준비한다.
- `EnterAsync()` — 씬 활성화 **후** 1회 실행. **해당 씬을 구성하는 UI 묶음을 여기서 오픈한다.**
- `ExitAsync()` — 다음 씬 로드 **전**, 이전 씬이 아직 살아있는 상태에서 호출 (앱 종료 중에는 미호출). 구독 해제·풀 반환 등 비동기 정리를 여기서 처리한다.

```csharp
public sealed class LobbyContext : SceneContext
{
    public override UniTask PreloadAsync()
        => UIManager.PreloadAsync<LobbyPanel>();

    public override async UniTask EnterAsync()
    {
        await UIManager.OpenAsync<LobbyPanel>();
        await UIManager.OpenAsync<TopNavBar>();
        await UIManager.OpenAsync<BottomNavBar>();
    }
}
```

`Scene/SceneContexts.cs` 에 팩토리를 등록한다 (인스턴스는 캐시하지 않고 진입마다 새로 생성):

```csharp
SceneId.GameScene => new GameSceneContext(),
```

빌드 씬은 TitleScene과 GameScene이다. 에디터에서 별도 테스트 씬을 직접 실행하면 코어만 초기화한다.

### 책임 분리 원칙

- 씬을 구성하는 UI 묶음(예: 로비 = `LobbyPanel` + `TopNavBar` + `BottomNavBar`)을 결정하고 오픈하는 것은 **SceneContext 의 책임**이다.
- 개별 Panel/View 는 형제 UI(NavBar 등)를 **직접 오픈하지 않는다**. 자기 자신의 동작만 책임진다.
- UI 간 통신은 static `Observable` 등 진입 순서에 무관한 채널을 사용한다 (인스턴스 직접 참조 X).
- 씬 전환 시 `AppFlow`는 현재 컨텍스트의 ExitAsync 이후 `UIManager.CloseAllUIAsync()` 로 활성 UI 와 백 스택을 정리하므로, `SceneContext` 에서 개별 Close 호출은 보통 불필요하다.

## 부트 + 씬 전환 (AppFlow)

부트스트랩과 씬 전환은 `Scene/AppFlow.cs` 한 파일의 선형 흐름이다. `LoadingScene` 같은 스플래시 전용 씬이나 씬마다 배치하는 `Boot` 프리팹은 없다.

- 앱 시작 1회: `[RuntimeInitializeOnLoadMethod(BeforeSceneLoad)]` 로 진입 → 코드 생성 `BootCurtain`(즉시 검은 화면) → `GameManager.InitializeCore()` → 첫 씬의 `SceneContext.PreloadAsync`/`EnterAsync`.
- 씬 전환: `AppFlow.LoadSceneAsync(SceneId)` 가 입력을 차단하는 커튼을 직접 관리한다. 대상 씬의 PreloadAsync, 현재 씬의 ExitAsync, Unity 씬 로드, 새 씬 EnterAsync 순서로 실행한다.
- 커튼(Splash/Loading) 연출의 주인은 `AppFlow` 다. `UIManager` 는 타입을 모르는 UI 스위치로만 쓰이며 로딩 전용 API를 갖지 않는다.
- `AppFlow.WaitUntilReadyAsync()` 로 부트스트랩 완료(코어 초기화 + 첫 씬 진입)를 대기할 수 있다.

## 새 Manager 추가

`SingletonGameObject<T>` 상속. 싱글톤 인스턴스는 DontDestroyOnLoad로 씬 전환 중 유지한다.

```csharp
public class MyManager : SingletonGameObject<MyManager> { }
```

## Manager 내 함수 규칙

Manager 내부의 함수는 해당 형태로 분리하여 작성

- `private void MyFunction_Internal()` - 함수 내부 로직 구현
- `public static void MyFunction()` - 실제 호출되는 함수 Instance를 통하여 Internal 함수 호출.

외부 호출 형태
```csharp
MyManager.MyFunction();
```

## ResourceManager

- `LoadResource<T>(address)` — 동기 로드
- `LoadResourceAsync<T>(address)` — 비동기 로드
- `LoadComponentAsync<T>(address)` — 프리팹을 GameObject로 로드 후 컴포넌트 추출 (MonoBehaviour 로드 시 사용)
- `ReleaseResource(address)` — 해제

### AddressableKeys — 주소 문자열 관리

`ResourceManager` 의 모든 로드 함수에 **문자열 리터럴을 직접 전달하는 것을 금지**한다.
반드시 `AddressableKeys` 의 카테고리별 `Get` 메서드를 통해 주소를 얻어야 한다.

| 카테고리 | 대상 | 사용 예 |
|----------|------|---------|
| `AddressableKeys.UI` | UI 프리팹 | `AddressableKeys.UI.Get<QuestBox>()` |
| `AddressableKeys.InGame` | 인게임 에셋/씬/이미지 | `AddressableKeys.InGame.Get("Panel")` |
| `AddressableKeys.Sounds` | 사운드 | `AddressableKeys.Sounds.Get("popSound")` |

- `Get<T>()` — 타입 이름으로 조회 (클래스명과 키 이름이 일치할 때 사용)
- `Get(string keyName)` — 문자열 이름으로 조회

```csharp
// 올바른 사용
var prefab = await ResourceManager.LoadComponentAsync<QuestBox>(
    AddressableKeys.UI.Get<QuestBox>());

var panel = await ResourceManager.LoadComponentAsync<Panel>(
    AddressableKeys.InGame.Get("Panel"));

// 금지 — 문자열 리터럴 직접 전달
var prefab = await ResourceManager.LoadComponentAsync<QuestBox>("QuestBox"); // ❌
```

새 에셋을 Addressable에 등록할 때는 반드시 `AddressableKeys.cs` 의 해당 카테고리 `Keys` 딕셔너리에도 함께 추가한다.

## ObjectPoolManager — 풀링 대상 addressable 경로

- `AddressableKeys.cs`에 등록된 대상을 찾아 적용.

## GameStateUtil

인스턴스 생성 전·풀 반환 전 `GameStateUtil.IsQuitting` 체크 필수.
