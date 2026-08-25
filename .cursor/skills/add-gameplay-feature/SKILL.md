---
name: add-gameplay-feature
description: >-
  Unity 프로젝트에 새 게임플레이 기능(규칙/상태 System, 씬 Controller, 게임 오브젝트)을
  기존 계층 관례에 맞게 추가한다. 특정 프로젝트 경로에 묶이지 않으며,
  작업 전 해당 프로젝트의 System·Controller·생명주기 등록 지점을 탐색한다.
---

# Skill: 게임플레이 기능 추가 (System / Controller)

새 전투/퍼즐/턴 로직이나 씬 오브젝트를 추가할 때 사용한다.
`harness-boundaries` 가 있으면 준수한다: 기존 매니저/유틸/베이스는 **호출·구독·상속만**.

다른 프로젝트의 네임스페이스·경로·클래스명을 가정하지 말고,
**현재 프로젝트에서 가장 비슷한 기존 구현을 모델**로 삼는다.

## 계층 원칙 (프로젝트가 이 분리를 쓸 때)

| 계층 | 일반적인 형태 | 책임 |
|------|---------------|------|
| **System** | static 클래스 또는 프로젝트 관례의 규칙 계층 | 규칙·상태·턴 오케스트레이션·이벤트 발행. 씬 오브젝트 직접 소유 X |
| **Controller** | `MonoBehaviour` (+ 프로젝트 베이스) | 풀·스폰·씬 오브젝트 ↔ System 등록·연출 |
| **Object** | 개체 `MonoBehaviour`/데이터 홀더 | 개체 상태·입력·피격 연출, System API 호출 |

프로젝트가 System/Controller 분리를 쓰지 않으면 **가장 가까운 기존 기능의 구조**를 그대로 따른다.
새 아키텍처를 발명하지 않는다.

## 절차

### 1) 프로젝트 탐색 (필수)

하드코딩된 경로를 쓰지 말고 현재 워크스페이스에서 찾는다.

- 기존 System / Controller / 규칙·연출 페어 (폴더명·네임스페이스는 프로젝트마다 다름)
- 게임플레이 생명주기 등록 지점 (`Initialize`/`Dispose` 또는 동등 API를 호출하는 Manager/Context)
- 씬 진입·오브젝트 배선 지점 (`SceneContext` / `*Context.OnEnter*` / 동등)
- 데이터 출처 (테이블 매니저, 세션, 유저 데이터 등 — 프로젝트에 있는 것)
- 리소스 로드 관례 (Addressables 키 생성물, ResourceManager, ObjectPool 등)

이번 기능에 필요한 계층(System만 / System+Controller / Object)을 정한다.

### 2) System 작성 (규칙·상태가 필요할 때)

- 경로는 탐색한 기존 System 과 **같은 폴더·네임스페이스 관례**.
- 기존이 `Initialize()` / `Dispose()` 쌍이면 동일하게.
- 구독 정리는 프로젝트가 쓰는 방식 유지 (예: `CompositeDisposable`, cancellation token).
- 이벤트/상태는 기존 System 이 쓰는 패턴으로 노출 (예: R3 `Subject`→`Observable`, `ReactiveProperty` 등).
- 다른 System 호출 방식도 기존과 동일 (static 직접 호출 등). DI 를 새로 도입하지 않는다.

```csharp
// 형태만 참고 — 네임스페이스·이벤트 타입·구독 대상은 현재 프로젝트 모델에 맞출 것
public static class ExampleSystem
{
    private static readonly Subject<int> _onChanged = new();
    public static Observable<int> OnChanged => _onChanged;
    private static CompositeDisposable _subscriptions;

    public static void Initialize()
    {
        _subscriptions = new CompositeDisposable();
        // OtherSystem.OnSomething.Subscribe(...).AddTo(_subscriptions);
    }

    public static void Dispose() => _subscriptions?.Dispose();
}
```

### 3) 생명주기 등록

- 탐색으로 찾은 **등록 지점**에 `Initialize`/`Dispose`(또는 동등)를 **기존 순서 관례대로 한 줄씩** 추가한다.
- 매니저/컨텍스트의 그 외 로직·시그니처는 바꾸지 않는다. 등록만으로 부족하면 에스컬레이션.
- 순서 의존이 있으면 기존 나열 순서를 지킨다.

### 4) Controller 작성 (씬 오브젝트가 필요할 때)

- 기존 Controller 와 같은 베이스·폴더·네이밍.
- 프리팹/리소스는 프로젝트 관례 경로로 로드 (문자열 리터럴 남발 금지, 키가 있으면 키 사용).
- System 이벤트를 구독하고, 필요 시 System 에 씬 오브젝트 등록.
- 연출 완료 후 System 을 푸는 콜백 패턴이 있으면 동일하게.
- 파괴/종료 시 구독·풀 반환 정리. 앱 종료 가드(`IsQuitting` 등)가 있으면 따른다.

### 5) 진입점 배선

- 씬 오픈/진입 책임은 프로젝트의 Context/진입 코드에 둔다.
- 기존 Controller 배선 순서(로드 → 인스턴스 → 주입 → `Initialize` 등)를 복제한다.
- 개별 Controller/System 이 스스로 씬을 새로 구성하지 않는다.

### 6) 검증

- 입력이 같이 필요하면 해당 스킬(`setup-input-actions`)을 이어서 쓴다.

## 체크리스트

- [ ] 현재 프로젝트의 System/Controller/등록·진입 지점을 **탐색**한 뒤 작업
- [ ] 가장 비슷한 기존 페어를 모델로 복제 (새 구조 발명 금지)
- [ ] System 은 규칙·상태·이벤트, Controller 는 풀/스폰/연출 (분리가 있는 프로젝트)
- [ ] 생명주기 등록은 기존 지점에 최소 줄만 추가, 순서 준수
- [ ] 리소스는 프로젝트 키/매니저 경유
- [ ] `Dispose`/파괴 시 구독·풀 정리
- [ ] 기반 코드 공개 API 미변경 (필요 시 에스컬레이션)
