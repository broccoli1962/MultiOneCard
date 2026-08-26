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

        /// <summary>
        /// 닉을 복원하고 입력을 구독한다.
        /// </summary>
        public override void OnOpen()
        {
            View.EnsureLayout();
            View.SetNick(PlayerPrefs.GetString(PrefNick, string.Empty));
            View.SetLanHost(GatewaySettings.LanHost);
            View.SetConnectionModeVisible(!WebBuild.IsPlayer);
            if (WebBuild.IsPlayer)
            {
                GatewaySettings.SaveMode(ConnectionMode.Relay);
            }

            View.SetMode(GatewaySettings.Mode);
            View.SetStatus(ModeStatus(GatewaySettings.Mode));
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
            View.LanHostChanged += OnLanHostChanged;
            View.ModeClicked += OnModeClicked;
            View.QuickMatchClicked += OnQuickMatchClicked;
            View.CreateRoomClicked += OnCreateRoomClicked;
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
            View.LanHostChanged -= OnLanHostChanged;
            View.ModeClicked -= OnModeClicked;
            View.QuickMatchClicked -= OnQuickMatchClicked;
            View.CreateRoomClicked -= OnCreateRoomClicked;
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

        private void OnLanHostChanged(string host)
        {
            GatewaySettings.SaveLanHost(host);
            View.SetLanHost(GatewaySettings.LanHost);
            View.SetStatus(string.IsNullOrEmpty(GatewaySettings.LanHost)
                ? "호스트 IP를 입력하세요"
                : "호스트 IP 저장됨");
        }

        private void OnModeClicked(ConnectionMode mode)
        {
            if (WebBuild.IsPlayer)
            {
                mode = ConnectionMode.Relay;
            }

            GatewaySettings.SaveMode(mode);
            View.SetMode(mode);
            View.SetStatus(ModeStatus(mode));
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

            OpenRoom(nick, RandomRoomCode(), SessionLimits.ClampPlayers(seatCount), isHost: true);
        }

        private void OnCreateRoomClicked()
        {
            if (!RequireNick(out var nick))
            {
                return;
            }

            OpenRoom(nick, RandomRoomCode(), SessionLimits.MaxPlayers, isHost: true);
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

        private static void OpenRoom(string nick, string roomCode, int seatCount, bool isHost)
        {
            RoomPresenter.Prepare(nick, roomCode, seatCount, isHost);
            UIManager.OpenAsync<RoomPanel>().Forget();
        }

        private static string RandomRoomCode()
        {
            return UnityEngine.Random.Range(0, 1000000).ToString("D6");
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

        private static string ModeStatus(ConnectionMode mode)
        {
            if (WebBuild.IsPlayer)
            {
                return UgsLobbyRelay.IsProjectLinked
                    ? "릴레이. 호스트 화면의 방 코드로 입장. 방을 연 탭을 유지하세요. 한 방 최대 "
                        + SessionLimits.MaxPlayers
                        + "인"
                    : "릴레이는 Edit > Project Settings > Services 에서 Cloud 연결 필요";
            }

            if (mode == ConnectionMode.Lan)
            {
                return "랜. 게스트는 서버 주소에 호스트 IP";
            }

            return UgsLobbyRelay.IsProjectLinked
                ? "릴레이. 호스트 화면의 방 코드로 입장. 한 방 최대 " + SessionLimits.MaxPlayers + "인"
                : "릴레이는 Edit > Project Settings > Services 에서 Cloud 연결 필요";
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
