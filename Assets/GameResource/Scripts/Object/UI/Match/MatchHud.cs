using Backend.Net;
using UnityEngine;
using UnityEngine.UI;

namespace Backend.Object.UI
{
    /// <summary>
    /// 매치 HUD. 턴·초·방향, 요구 무늬, 공격/Q 스택, 조커값 흑/빨/파, 알약, 덱 장수만 표시한다.
    /// </summary>
    public sealed class MatchHud : UIView
    {
        [SerializeField] private Font _font;
        [SerializeField] private Text _hudText;

        private bool _layoutReady;
        private long _deadlineMs;
        private string _hudBase = string.Empty;

        /// <summary>
        /// 프리팹 미배선이어도 HUD 텍스트를 채운다.
        /// </summary>
        public void EnsureLayout(Font font)
        {
            if (font != null)
            {
                _font = font;
            }

            if (_layoutReady && _hudText != null)
            {
                return;
            }

            var rt = CachedRectTransform;
            rt.anchorMin = new Vector2(0.5f, 1f);
            rt.anchorMax = new Vector2(0.5f, 1f);
            rt.pivot = new Vector2(0.5f, 1f);
            rt.anchoredPosition = new Vector2(0f, -12f);
            rt.sizeDelta = new Vector2(1000f, 140f);

            _hudText = FindOrCreateText("HudText", 30f);
            _layoutReady = true;
        }

        /// <summary>
        /// 공개 매치 뷰를 HUD 문구로 그린다. 초는 <see cref="Tick"/> 이 갱신한다.
        /// </summary>
        public void Bind(PublicMatchView match, int viewingSeat)
        {
            EnsureLayout(_font);
            _deadlineMs = match != null ? match.deadlineMs : 0;
            _hudBase = FormatHud(match, viewingSeat);
            RefreshTimer();
        }

        /// <summary>
        /// 서버 deadline 기준 남은 초를 갱신한다.
        /// </summary>
        public void Tick()
        {
            RefreshTimer();
        }

        private void RefreshTimer()
        {
            if (_hudText == null)
            {
                return;
            }

            var remain = 0;
            if (_deadlineMs > 0)
            {
                var left = _deadlineMs - System.DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
                remain = left > 0 ? (int)(left / 1000L) : 0;
            }

            _hudText.text = string.IsNullOrEmpty(_hudBase)
                ? string.Empty
                : $"{_hudBase} {remain}초";
        }

        private static string FormatHud(PublicMatchView match, int viewingSeat)
        {
            if (match == null)
            {
                return "대기";
            }

            var dir = match.direction < 0 ? "시계" : "반시계";
            var suit = string.IsNullOrEmpty(match.requiredSuit) ? "-" : match.requiredSuit;
            var pill = FormatPill(match.requiredColor);
            var spear = match.spearInStack ? "죽창" : string.Empty;
            return $"보기P{viewingSeat} 턴P{match.currentSeat} {dir} 무늬{suit} 공격+{match.attackStack}{spear} Q×{match.queenStack} 흑{match.jokerBw} 빨{match.jokerColor} 파{match.jokerMoon} 알약{pill} 덱{match.deckCount}";
        }

        private static string FormatPill(string requiredColor)
        {
            if (requiredColor == ColorCode.Black)
            {
                return "흑";
            }

            if (requiredColor == ColorCode.Red)
            {
                return "빨";
            }

            if (requiredColor == ColorCode.Blue)
            {
                return "파";
            }

            return "-";
        }

        private Text FindOrCreateText(string name, float fontSize)
        {
            var existing = CachedTransform.Find(name);
            GameObject go;
            if (existing != null)
            {
                go = existing.gameObject;
            }
            else
            {
                go = new GameObject(name, typeof(RectTransform), typeof(Text));
                go.transform.SetParent(CachedTransform, false);
            }

            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;

            if (!go.TryGetComponent(out Text text))
            {
                text = go.AddComponent<Text>();
            }

            text.fontSize = (int)fontSize;
            text.alignment = TextAnchor.MiddleCenter;
            text.color = Color.white;
            text.raycastTarget = false;
            text.horizontalOverflow = HorizontalWrapMode.Wrap;
            text.verticalOverflow = VerticalWrapMode.Truncate;
            if (_font != null)
            {
                text.font = _font;
            }

            return text;
        }
    }
}
