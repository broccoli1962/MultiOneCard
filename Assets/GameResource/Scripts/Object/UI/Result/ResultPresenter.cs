using System;
using Backend.App;
using Backend.Net;
using Backend.Object.Management;

namespace Backend.Object.UI
{
    /// <summary>
    /// 결과·재대결. 투표는 <see cref="NetClient.RematchVote"/> 로만 보내고 순위는 판결하지 않는다.
    /// 재대결 찬성 시 화면을 유지하고 전원 동의까지 기다린다. 20초 미투표는 반대로 보낸다.
    /// </summary>
    public sealed class ResultPresenter : UIPresenter<ResultPanel>
    {
        private static MatchEndView _pendingResult;
        private static string[] _pendingNicks;
        private static long _pendingDeadlineMs;
        private static Action<bool> _pendingVote;
        private static Action<bool> _pendingClosed;

        private MatchEndView _result;
        private string[] _nicks;
        private long _deadlineMs;
        private Action<bool> _vote;
        private Action<bool> _closed;
        private bool _voted;
        private bool _voteYes;
        private bool _finished;

        /// <summary>
        /// 결과 패널을 열기 전 순위와 투표 콜백을 넣는다.
        /// deadlineMs 가 0 이면 지금으로부터 20초.
        /// </summary>
        public static void Prepare(
            MatchEndView result,
            string[] nicks,
            long deadlineMs,
            Action<bool> vote,
            Action<bool> closed)
        {
            _pendingResult = result;
            _pendingNicks = nicks;
            _pendingDeadlineMs = deadlineMs;
            _pendingVote = vote;
            _pendingClosed = closed;
        }

        /// <summary>
        /// 순위표를 그리고 재대결 입력을 구독한다.
        /// </summary>
        public override void OnOpen()
        {
            _result = _pendingResult;
            _nicks = _pendingNicks;
            _deadlineMs = _pendingDeadlineMs > 0
                ? _pendingDeadlineMs
                : DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() + MatchRuntime.RematchSeconds * 1000L;
            _vote = _pendingVote;
            _closed = _pendingClosed;
            _voted = false;
            _voteYes = false;
            _finished = false;
            View.EnsureLayout();
            BindView();
            Refresh();
        }

        /// <summary>
        /// 입력 구독을 해제한다.
        /// </summary>
        public override void OnClose()
        {
            UnbindView();
        }

        /// <summary>
        /// 재대결 마감을 적용한다. 화면은 판결하지 않는다.
        /// </summary>
        public void Tick()
        {
            if (_finished || GameStateUtil.IsQuitting)
            {
                return;
            }

            if (RemainSeconds() <= 0 && !_voted)
            {
                Vote(false);
                Finish();
                return;
            }

            Refresh();
        }

        private void BindView()
        {
            View.YesClicked += OnYesClicked;
            View.NoClicked += OnNoClicked;
        }

        private void UnbindView()
        {
            if (View == null)
            {
                return;
            }

            View.YesClicked -= OnYesClicked;
            View.NoClicked -= OnNoClicked;
        }

        private void OnYesClicked()
        {
            Vote(true);
        }

        private void OnNoClicked()
        {
            Vote(false);
            Finish();
        }

        private void Vote(bool rematchYes)
        {
            if (_voted)
            {
                return;
            }

            _voted = true;
            _voteYes = rematchYes;
            _vote?.Invoke(rematchYes);
            Refresh();
        }

        private void Finish()
        {
            if (_finished || GameStateUtil.IsQuitting)
            {
                return;
            }

            _finished = true;
            if (!_voted)
            {
                Vote(false);
            }

            _closed?.Invoke(_voteYes);
            if (View != null)
            {
                UIManager.Close(View);
            }
        }

        private void Refresh()
        {
            if (View == null)
            {
                return;
            }

            View.Render(FormatRanks(_result, _nicks), RemainSeconds(), _voted, _voteYes);
        }

        private int RemainSeconds()
        {
            if (_deadlineMs <= 0)
            {
                return 0;
            }

            var left = _deadlineMs - DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
            return left > 0 ? (int)(left / 1000L) : 0;
        }

        private static string FormatRanks(MatchEndView result, string[] nicks)
        {
            if (result == null || result.ranks == null)
            {
                return "종료";
            }

            var lines = new string[result.ranks.Length];
            for (var seat = 0; seat < result.ranks.Length; seat++)
            {
                var nick = nicks != null && seat < nicks.Length && !string.IsNullOrEmpty(nicks[seat])
                    ? nicks[seat]
                    : "P" + seat;
                var rank = result.ranks[seat];
                var count = result.handCounts != null && seat < result.handCounts.Length ? result.handCounts[seat] : 0;
                var score = result.scores != null && seat < result.scores.Length ? result.scores[seat] : 0;
                lines[seat] = $"{rank}위  {nick}  장수{count}  점수{score}";
            }

            return string.Join("\n", lines);
        }
    }
}
