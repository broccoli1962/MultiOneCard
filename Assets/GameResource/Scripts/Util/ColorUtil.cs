using UnityEngine;

namespace Backend.Util
{
    public static class ColorUtil
    {
        public static Color LerpHSV(Color from, Color to, float t)
        {
            Color.RGBToHSV(from, out float h1, out float s1, out float v1);
            Color.RGBToHSV(to, out float h2, out float s2, out float v2);

            float h = LerpHue(h1, h2, t);
            float s = Mathf.Lerp(s1, s2, t);
            float v = Mathf.Lerp(v1, v2, t);
            var color = Color.HSVToRGB(h, s, v);
            color.a = Mathf.Lerp(from.a, to.a, t);
            return color;
        }

        private static float LerpHue(float from, float to, float t)
        {
            float delta = to - from;
            if (delta > 0.5f)
                delta -= 1f;
            else if (delta < -0.5f)
                delta += 1f;

            float hue = from + delta * t;
            if (hue < 0f)
                hue += 1f;
            else if (hue >= 1f)
                hue -= 1f;
            return hue;
        }
    }
}
