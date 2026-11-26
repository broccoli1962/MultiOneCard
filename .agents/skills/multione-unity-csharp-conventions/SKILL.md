---
name: multione-unity-csharp-conventions
description: "MultiOneCard C# 작성·수정 시 네이밍, UniTask, 로그, XML 주석과 컴포넌트 조회 규칙을 적용한다."
---

# Unity C# Conventions

## 네이밍 규칙

| 대상 | 규칙 | 예시 |
|------|------|------|
| private field | `_camelCase` | `_instance`, `_poolDictionary` |
| private const | `UPPER_SNAKE_CASE` | `PREF_BGM_ENABLED` |
| public property/method | `PascalCase` | `IsBgmEnabled`, `GetInstance()` |
| 지역 변수·파라미터 | `camelCase` | `targetParent`, `audioClip` |

## 비동기

- **UniTask** 사용 (Task 대신)
- fire-and-forget: `.Forget()` 또는 반환 타입 `UniTaskVoid`

```csharp
// BAD
Task.Run(() => DoSomething());

// GOOD
DoSomethingAsync().Forget();
```

## 로그

`Debug.Log`/`Warning`/`Error`에 `[ClassName]` 접두어 필수.

```csharp
Debug.LogError($"[PoolManager] Failed to load prefab for key: {key}");
```

## XML 주석

public/protected 메서드에 `<summary>` 필수.

```csharp
/// <summary>
/// 특정 게임 오브젝트를 다시 풀에 반환
/// </summary>
public void ReturnToPool(GameObject obj)
```

## GetComponent

선택적 컴포넌트 조회는 `TryGetComponent`를 사용한다. `GetComponent`는 컴포넌트가 없으면 null을 반환하므로 결과를 사용하기 전에 확인한다.

```csharp
// BAD
var rb = GetComponent<Rigidbody>();
if (rb != null) { ... }

// GOOD
if (TryGetComponent<Rigidbody>(out var rb)) { ... }
```

## #region

논리적 그룹 구분 시 사용 (예: `#region Sfx`, `#region Bgm`, `#endregion`).
