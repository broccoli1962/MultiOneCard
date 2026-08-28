using System;
using Backend.App;
using Backend.Object.Management;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace Backend.Object.UI
{
    /// <summary>
    /// 공개 릴레이 방 목록. 조회와 입장만 담당한다.
    /// </summary>
    public sealed class RoomListPresenter : UIPresenter<RoomListPanel>
    {
        private const string PrefNick = "guest_nick";
        private const int NickMin = 2;
        private const int NickMax = 12;

        private int _querySeq;

        /// <summary>
        /// 입력을 구독하고 목록을 불러온다.
        /// </summary>
        public override void OnOpen()
        {
            View.EnsureLayout();
            View.BackClicked += OnBackClicked;
            View.RefreshClicked += OnRefreshClicked;
            View.JoinClicked += OnJoinClicked;
            QueryAsync().Forget();
        }

        /// <summary>
        /// 입력 구독을 해제한다.
        /// </summary>
        public override void OnClose()
        {
            _querySeq++;
            if (View == null)
            {
                return;
            }

            View.BackClicked -= OnBackClicked;
            View.RefreshClicked -= OnRefreshClicked;
            View.JoinClicked -= OnJoinClicked;
        }

        private void OnBackClicked()
        {
            UIManager.Close(View);
        }

        private void OnRefreshClicked()
        {
            QueryAsync().Forget();
        }

        private void OnJoinClicked(string sessionId)
        {
            if (string.IsNullOrEmpty(sessionId))
            {
                View.SetStatus("방을 찾을 수 없음");
                return;
            }

            if (!TryNormalizeNick(PlayerPrefs.GetString(PrefNick, string.Empty), out var nick))
            {
                View.SetStatus("닉은 2~12자");
                return;
            }

            RoomPresenter.Prepare(
                nick,
                string.Empty,
                SessionLimits.MaxPlayers,
                isHost: false,
                isPrivate: false,
                sessionId);
            UIManager.Close(View);
            UIManager.OpenAsync<RoomPanel>().Forget();
        }

        private async UniTaskVoid QueryAsync()
        {
            var seq = ++_querySeq;
            View.SetStatus("방 목록 불러오는 중");
            if (!UgsLobbyRelay.IsProjectLinked)
            {
                View.SetRooms(Array.Empty<PublicRoomInfo>());
                View.SetStatus("릴레이는 Edit > Project Settings > Services 에서 Cloud 연결 필요");
                return;
            }

            try
            {
                var rooms = await UgsLobbyRelay.QueryPublicAsync();
                if (seq != _querySeq || View == null)
                {
                    return;
                }

                View.SetRooms(rooms);
                View.SetStatus(rooms.Count == 0 ? "공개 방이 없습니다" : rooms.Count + "개");
            }
            catch (Exception e)
            {
                Debug.LogError($"[RoomListPresenter] Query failed: {e}");
                if (seq != _querySeq || View == null)
                {
                    return;
                }

                View.SetRooms(Array.Empty<PublicRoomInfo>());
                View.SetStatus(FormatQueryError(e.Message));
            }
        }

        private static string FormatQueryError(string message)
        {
            if (string.IsNullOrEmpty(message))
            {
                return "방 목록을 불러오지 못함";
            }

            if (message.IndexOf("Cloud", StringComparison.OrdinalIgnoreCase) >= 0
                || message.IndexOf("Authentication", StringComparison.OrdinalIgnoreCase) >= 0)
            {
                return message;
            }

            return "방 목록을 불러오지 못함";
        }

        private static bool TryNormalizeNick(string value, out string nick)
        {
            nick = value != null ? value.Trim() : string.Empty;
            return nick.Length >= NickMin && nick.Length <= NickMax;
        }
    }
}
