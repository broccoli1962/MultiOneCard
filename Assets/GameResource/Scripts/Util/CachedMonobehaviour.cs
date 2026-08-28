using UnityEngine;

namespace Backend.Util
{
    public abstract class CachedMonobehaviour : MonoBehaviour
    {
        private Transform _cachedTransform;
        public Transform CachedTransform => _cachedTransform ??= transform;

        private GameObject _cachedGameObject;
        public GameObject CachedGameObject => _cachedGameObject ??= gameObject;

        private RectTransform _cachedRectTransform;
        private bool _rectTransformResolved;
        public RectTransform CachedRectTransform
        {
            get
            {
                if (!_rectTransformResolved)
                {
                    _cachedRectTransform = GetComponent<RectTransform>();
                    _rectTransformResolved = true;
                }

                return _cachedRectTransform;
            }
        }
    }
}
