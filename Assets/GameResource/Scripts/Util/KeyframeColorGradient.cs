using System;
using System.Collections.Generic;
using UnityEngine;

namespace Backend.Util
{
    [Serializable]
    public class KeyframeColorGradient
    {
        [SerializeField] private List<Color> _colorKeys = new()
        {
            new Color(0.55f, 0.85f, 0.35f, 1f),
            new Color(1f, 0.9f, 0.2f, 1f),
            new Color(1f, 0.2f, 0.2f, 1f),
        };

        public Color Evaluate(int index, int count)
        {
            if (_colorKeys == null || _colorKeys.Count == 0)
                return Color.white;

            if (_colorKeys.Count == 1 || count <= 1)
                return _colorKeys[0];

            float t = (float)index / (count - 1);
            float scaled = t * (_colorKeys.Count - 1);
            int lower = Mathf.FloorToInt(scaled);
            int upper = Mathf.Min(lower + 1, _colorKeys.Count - 1);
            float localT = scaled - lower;
            return ColorUtil.LerpHSV(_colorKeys[lower], _colorKeys[upper], localT);
        }
    }
}
