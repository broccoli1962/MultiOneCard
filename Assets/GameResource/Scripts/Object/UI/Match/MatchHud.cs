using Backend.Net;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Backend.Object.UI
{
    /// <summary>
    /// 매치 HUD. 낼 사람(닉네임), 최근 카드, 남은 초, 방향만 표시한다.
    /// </summary>
    public sealed class MatchHud : UIView
    {
        [SerializeField] private TMP_FontAsset _font;
        [SerializeField] private TextMeshProUGUI _hudText;

        private bool _layoutReady;
        private long _deadlineMs;
        private string _hudBase = string.Empty;

        /// <summary>
        /// 프리팹 자식에 묶인 HUD 텍스트를 찾는다.
        /// </summary>
        public void EnsureLayout(TMP_FontAsset font)
        {
            if (font != null)
            {
                _font = font;
            }

            if (_layoutReady && _hudText != null)
            {
                return;
            }

            _hudText ??= FindOrCreateText("HudText");
            if (_hudText != null)
            {
                _hudText.textWrappingMode = TextWrappingModes.Normal;
                _hudText.overflowMode = TextOverflowModes.Overflow;
                _hudText.raycastTarget = false;
                if (_font != null)
                {
                    _hudText.font = _font;
                }
            }

            _layoutReady = true;
        }

        /// <summary>
        /// 공개 매치 뷰와 최근 수를 HUD 문구로 그린다. 초는 <see cref="Tick"/> 이 갱신한다.
        /// </summary>
        public void Bind(PublicMatchView match, string lastPlay, string[] nicks = null)
        {
            EnsureLayout(_font);
            _deadlineMs = match != null ? match.deadlineMs : 0;
            _hudBase = FormatHud(match, lastPlay, nicks);
            SetDirIcons(match);
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
                : $"{_hudBase}\n{remain}초";
        }

        private static string FormatHud(PublicMatchView match, string lastPlay, string[] nicks)
        {
            if (match == null)
            {
                return "대기";
            }

            var clockwise = match.direction < 0;
            var dir = clockwise ? "시계" : "반시계";
            var recent = string.IsNullOrEmpty(lastPlay) ? "아직 낸 장 없음" : lastPlay;
            return $"턴 {NickOf(nicks, match.currentSeat)}\n최근 {recent}\n{dir}";
        }

        private void SetDirIcons(PublicMatchView match)
        {
            var cw = CachedTransform.Find("DirCW");
            var ccw = CachedTransform.Find("DirCCW");
            var show = match != null;
            if (cw != null)
            {
                cw.gameObject.SetActive(show && match.direction < 0);
            }

            if (ccw != null)
            {
                ccw.gameObject.SetActive(show && match.direction >= 0);
            }
        }

        private static string NickOf(string[] nicks, int seat)
        {
            if (nicks != null && seat >= 0 && seat < nicks.Length && !string.IsNullOrEmpty(nicks[seat]))
            {
                return nicks[seat];
            }

            return "P" + seat;
        }

        private TextMeshProUGUI FindOrCreateText(string name)
        {
            var existing = CachedTransform.Find(name);
            return existing != null && existing.TryGetComponent(out TextMeshProUGUI text) ? text : null;
        }
    }
}
