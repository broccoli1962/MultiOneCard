using Backend.Object.Management;
using UnityEngine;

namespace Backend.Object.UI
{
    /// <summary>
    /// 로비 입력 검증. 매칭 서버는 이 단계에서 붙이지 않는다.
    /// </summary>
    public sealed class LobbyPresenter : UIPresenter<LobbyPanel>
    {
        private const string PrefNick = "guest_nick";
        private const int NickMin = 2;
        private const int NickMax = 12;
        private const int RoomCodeLength = 6;

        /// <summary>
        /// 닉을 복원하고 입력을 구독한다.
        /// </summary>
        public override void OnOpen()
        {
            View.EnsureLayout();
            View.SetNick(PlayerPrefs.GetString(PrefNick, string.Empty));
            View.SetStatus("닉을 입력하세요");
            BindView();
        }

        /// <summary>
        /// 입력 구독을 해제한다.
        /// </summary>
        public override void OnClose()
        {
            UnbindView();
        }

        private void BindView()
        {
            View.NickChanged += OnNickChanged;
            View.QuickMatchClicked += OnQuickMatchClicked;
            View.CreateRoomClicked += OnCreateRoomClicked;
            View.JoinRoomClicked += OnJoinRoomClicked;
            View.BackClicked += OnBackClicked;
        }

        private void UnbindView()
        {
            if (View == null)
            {
                return;
            }

            View.NickChanged -= OnNickChanged;
            View.QuickMatchClicked -= OnQuickMatchClicked;
            View.CreateRoomClicked -= OnCreateRoomClicked;
            View.JoinRoomClicked -= OnJoinRoomClicked;
            View.BackClicked -= OnBackClicked;
        }

        private void OnNickChanged(string nick)
        {
            if (!TryNormalizeNick(nick, out var normalized))
            {
                return;
            }

            PlayerPrefs.SetString(PrefNick, normalized);
        }

        private void OnQuickMatchClicked(int seatCount)
        {
            if (!RequireNick(out var nick))
            {
                return;
            }

            if (seatCount != 2 && seatCount != 4 && seatCount != 6)
            {
                View.SetStatus("퀵매치는 2·4·6인만 가능");
                return;
            }

            View.SetStatus($"{nick} · 퀵매치 {seatCount}인 대기");
        }

        private void OnCreateRoomClicked()
        {
            if (!RequireNick(out var nick))
            {
                return;
            }

            View.SetStatus($"{nick} · 방 만들기");
        }

        private void OnJoinRoomClicked()
        {
            if (!RequireNick(out var nick))
            {
                return;
            }

            var code = View.RoomCodeText != null ? View.RoomCodeText.Trim() : string.Empty;
            if (code.Length != RoomCodeLength || !IsDigits(code))
            {
                View.SetStatus("룸코드 6자리를 입력하세요");
                return;
            }

            View.SetStatus($"{nick} · 룸 {code} 입장");
        }

        private void OnBackClicked()
        {
            UIManager.Close(View);
        }

        private bool RequireNick(out string nick)
        {
            if (!TryNormalizeNick(View.NickText, out nick))
            {
                View.SetStatus("닉은 2~12자");
                return false;
            }

            PlayerPrefs.SetString(PrefNick, nick);
            return true;
        }

        private static bool TryNormalizeNick(string value, out string nick)
        {
            nick = value != null ? value.Trim() : string.Empty;
            return nick.Length >= NickMin && nick.Length <= NickMax;
        }

        private static bool IsDigits(string value)
        {
            for (var i = 0; i < value.Length; i++)
            {
                if (value[i] < '0' || value[i] > '9')
                {
                    return false;
                }
            }

            return true;
        }
    }
}
