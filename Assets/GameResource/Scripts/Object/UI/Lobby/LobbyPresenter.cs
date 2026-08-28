using Backend.App;
using Backend.Object.Management;
using Cysharp.Threading.Tasks;
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
        private const int RoomCodeMin = 4;
        private const int RoomCodeMax = 8;
        private bool _isPrivate;

        /// <summary>호스트가 방을 닫았을 때 로비에 띄울 안내.</summary>
        public const string HostClosedNotice = "호스트가 방을 종료하였습니다";

        private static string _pendingNotice;

        /// <summary>
        /// 로비를 열기 전 상태 문구를 넣는다.
        /// </summary>
        public static void PrepareNotice(string notice)
        {
            _pendingNotice = notice;
        }

        /// <summary>
        /// 호스트가 방을 닫은 뒤 로비로 보낸다.
        /// </summary>
        public static void OpenAfterHostClosed()
        {
            PrepareNotice(HostClosedNotice);
            UIManager.OpenAsync<LobbyPanel>().Forget();
        }

        /// <summary>
        /// 닉을 복원하고 입력을 구독한다.
        /// </summary>
        public override void OnOpen()
        {
            View.EnsureLayout();
            View.SetNick(PlayerPrefs.GetString(PrefNick, string.Empty));
            View.SetVisibility(_isPrivate);
            var notice = _pendingNotice;
            _pendingNotice = null;
            View.SetStatus(!string.IsNullOrEmpty(notice) ? notice : RelayStatus());
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
            View.VisibilityClicked += OnVisibilityClicked;
            View.CreateRoomClicked += OnCreateRoomClicked;
            View.RoomListClicked += OnRoomListClicked;
            View.JoinRoomClicked += OnJoinRoomClicked;
            View.BackClicked += OnBackClicked;
            View.SettingsClicked += OnSettingsClicked;
        }

        private void UnbindView()
        {
            if (View == null)
            {
                return;
            }

            View.NickChanged -= OnNickChanged;
            View.VisibilityClicked -= OnVisibilityClicked;
            View.CreateRoomClicked -= OnCreateRoomClicked;
            View.RoomListClicked -= OnRoomListClicked;
            View.JoinRoomClicked -= OnJoinRoomClicked;
            View.BackClicked -= OnBackClicked;
            View.SettingsClicked -= OnSettingsClicked;
        }

        private void OnNickChanged(string nick)
        {
            if (!TryNormalizeNick(nick, out var normalized))
            {
                return;
            }

            PlayerPrefs.SetString(PrefNick, normalized);
        }

        private void OnVisibilityClicked(bool isPrivate)
        {
            _isPrivate = isPrivate;
            View.SetVisibility(_isPrivate);
            View.SetStatus(isPrivate ? "비공개. 방 코드로만 입장" : "공개. 방 목록에 표시");
        }

        private void OnCreateRoomClicked()
        {
            if (!RequireNick(out var nick))
            {
                return;
            }

            OpenRoom(nick, string.Empty, SessionLimits.MaxPlayers, isHost: true, _isPrivate);
        }

        private void OnRoomListClicked()
        {
            if (!RequireNick(out _))
            {
                return;
            }

            UIManager.OpenAsync<RoomListPanel>().Forget();
        }

        private void OnJoinRoomClicked()
        {
            if (!RequireNick(out var nick))
            {
                return;
            }

            var code = View.RoomCodeText != null ? View.RoomCodeText.Trim() : string.Empty;
            if (!IsJoinCode(code))
            {
                View.SetStatus("방 코드를 입력하세요");
                return;
            }

            OpenRoom(nick, code, SessionLimits.MaxPlayers, isHost: false);
        }

        private static void OpenRoom(
            string nick,
            string roomCode,
            int seatCount,
            bool isHost,
            bool isPrivate = false)
        {
            RoomPresenter.Prepare(nick, roomCode, seatCount, isHost, isPrivate);
            UIManager.OpenAsync<RoomPanel>().Forget();
        }

        private void OnBackClicked()
        {
            UIManager.Close(View);
        }

        private void OnSettingsClicked()
        {
            UIManager.OpenAsync<SettingsPopup>().Forget();
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

        private static string RelayStatus()
        {
            if (!UgsLobbyRelay.IsProjectLinked)
            {
                return "릴레이는 Edit > Project Settings > Services 에서 Cloud 연결 필요";
            }

            if (WebBuild.IsPlayer)
            {
                return "공개 방은 방 목록에서 입장. 방을 연 탭을 유지하세요. 한 방 최대 "
                    + SessionLimits.MaxPlayers
                    + "인";
            }

            return "공개 방은 방 목록에서 입장. 한 방 최대 " + SessionLimits.MaxPlayers + "인";
        }

        /// <summary>Unity 세션 조인 코드(영문·숫자, 대소문자 무시).</summary>
        private static bool IsJoinCode(string value)
        {
            if (string.IsNullOrEmpty(value)
                || value.Length < RoomCodeMin
                || value.Length > RoomCodeMax)
            {
                return false;
            }

            for (var i = 0; i < value.Length; i++)
            {
                var c = value[i];
                var letter = (c >= 'A' && c <= 'Z') || (c >= 'a' && c <= 'z');
                var digit = c >= '0' && c <= '9';
                if (!letter && !digit)
                {
                    return false;
                }
            }

            return true;
        }
    }
}
