using UnityEngine;

/// <summary>
/// 게임 상태를 추적하고 확인하는 유틸리티 클래스
/// </summary>
public static class GameStateUtil
{
    private static bool _isQuitting = false;

    /// <summary>
    /// 앱이 종료 중인지 확인합니다.
    /// 에디터의 플레이 모드 종료, 모바일 앱 종료 등 모든 종료 상황을 포함합니다.
    /// </summary>
    public static bool IsQuitting => _isQuitting;

    /// <summary>
    /// 종료 플래그를 설정합니다. (내부 사용)
    /// RuntimeInitializeOnLoadMethod를 통해 자동으로 초기화됩니다.
    /// </summary>
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void Initialize()
    {
        _isQuitting = false;
        Application.quitting += OnApplicationQuitting;

#if UNITY_EDITOR
        // 에디터에서 플레이 모드가 변경될 때 초기화
        UnityEditor.EditorApplication.playModeStateChanged += OnPlayModeStateChanged;
#endif
    }

    /// <summary>
    /// 앱 종료 시 호출되는 콜백
    /// </summary>
    private static void OnApplicationQuitting()
    {
        _isQuitting = true;
        // Debug.Log("[GameStateUtil] 앱 종료 중...");
    }

#if UNITY_EDITOR
    /// <summary>
    /// 에디터 플레이 모드 변경 시 호출되는 콜백
    /// </summary>
    private static void OnPlayModeStateChanged(UnityEditor.PlayModeStateChange state)
    {
        switch (state)
        {
            case UnityEditor.PlayModeStateChange.ExitingPlayMode:
                _isQuitting = true;
                // Debug.Log("[GameStateUtil] 에디터 플레이 모드 종료 중...");
                break;
            case UnityEditor.PlayModeStateChange.EnteredEditMode:
                _isQuitting = false;
                break;
            case UnityEditor.PlayModeStateChange.EnteredPlayMode:
                _isQuitting = false;
                break;
        }
    }
#endif

    /// <summary>
    /// Manager나 싱글톤 인스턴스에 안전하게 접근할 수 있는지 확인합니다.
    /// 종료 중이 아니고 게임이 실행 중일 때만 true를 반환합니다.
    /// </summary>
    public static bool CanAccessManagers()
    {
        return !_isQuitting && Application.isPlaying;
    }

    /// <summary>
    /// 게임이 현재 실행 중인지 확인 (에디터 플레이 모드 또는 빌드된 게임)
    /// </summary>
    public static bool IsPlaying()
    {
        return Application.isPlaying;
    }

    /// <summary>
    /// 게임이 포커스를 가지고 있는지 확인
    /// </summary>
    public static bool HasFocus()
    {
        return Application.isFocused;
    }

    /// <summary>
    /// 게임 상태 정보를 문자열로 반환
    /// </summary>
    public static string GetStateInfo()
    {
        string info = $"플레이 중: {Application.isPlaying}\n";
        info += $"종료 중: {_isQuitting}\n";
        info += $"포커스: {Application.isFocused}\n";
        info += $"플랫폼: {Application.platform}";
        
        return info;
    }
}
