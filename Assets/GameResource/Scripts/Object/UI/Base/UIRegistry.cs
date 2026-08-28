using System;
using System.Collections.Generic;
using UnityEngine;

namespace Backend.Object.UI
{
    /// <summary>
    /// UI 가 배치될 Canvas 레이어를 관리한다.
    /// 씬에 한 번 배치(예: UIRoot) 하고, Inspector 에서 UILayer 별 RectTransform 을 매핑한다.
    /// </summary>
    public class UIRegistry : MonoBehaviour
    {
        [Serializable]
        private struct LayerEntry
        {
            public UILayer layer;
            public RectTransform root;
        }

        [SerializeField] private List<LayerEntry> _layers = new();

        private Dictionary<UILayer, RectTransform> _lookup;

        private void Awake()
        {
            BuildLookup();
        }

        private void BuildLookup()
        {
            _lookup = new Dictionary<UILayer, RectTransform>(_layers.Count);
            for (int i = 0; i < _layers.Count; i++)
            {
                var entry = _layers[i];
                if (entry.root == null)
                {
                    Debug.LogWarning($"[UIRegistry] Layer '{entry.layer}' has no RectTransform assigned.");
                    continue;
                }
                _lookup[entry.layer] = entry.root;
            }
        }

        public RectTransform GetRoot(UILayer layer)
        {
            if (_lookup == null) BuildLookup();
            return _lookup != null && _lookup.TryGetValue(layer, out var root) ? root : null;
        }
    }
}
