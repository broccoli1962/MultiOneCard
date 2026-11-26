using Backend.Util;
using UnityEngine;
using UnityEngine.Events;

namespace Backend.Object.UI
{
    /// <summary>패널과 부분 뷰가 직렬화된 프리팹 위젯을 조회할 때 쓰는 공통 기반.</summary>
    public abstract class PrefabView : CachedMonobehaviour
    {
        /// <summary>현재 뷰의 상대 경로에서 자식 오브젝트를 찾는다.</summary>
        protected GameObject FindChild(string path)
        {
            var child = CachedTransform.Find(path);
            return child != null ? child.gameObject : null;
        }

        /// <summary>이전 프리팹에서 참조가 비어 있을 때 기존 자식 컴포넌트를 찾는다.</summary>
        protected T FindWidget<T>(string path) where T : Component
        {
            var child = CachedTransform.Find(path);
            return child != null && child.TryGetComponent(out T widget) ? widget : null;
        }

        /// <summary>프리팹의 무음 UI 버튼을 찾는다.</summary>
        protected CommonButton FindButton(string path)
        {
            var button = FindWidget<CommonButton>(path);
            if (button != null) button.useSound = false;
            return button;
        }

        /// <summary>뷰가 소유하는 버튼 입력을 한 번만 연결한다.</summary>
        protected static void BindButton(CommonButton button, UnityAction action)
        {
            if (button == null) return;
            button.OnClick ??= new UnityEvent();
            button.OnClick.RemoveAllListeners();
            button.OnClick.AddListener(action);
        }
    }
}
