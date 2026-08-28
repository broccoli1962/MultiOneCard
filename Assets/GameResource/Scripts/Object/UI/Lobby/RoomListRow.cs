using System;
using Backend.Object.Management;
using Backend.Util;
using TMPro;
using UnityEngine;

namespace Backend.Object.UI
{
    /// <summary>
    /// 방 목록 한 줄. 이름·인원·입장만 표시한다.
    /// </summary>
    public sealed class RoomListRow : CachedMonobehaviour
    {
        [SerializeField] private TextMeshProUGUI _nameText;
        [SerializeField] private TextMeshProUGUI _seatsText;
        [SerializeField] private CommonButton _joinButton;

        private string _sessionId;

        /// <summary>이 방 입장. 인자는 세션 Id.</summary>
        public event Action<string> JoinClicked;

        /// <summary>
        /// 자식 위젯을 찾아 클릭을 묶는다.
        /// </summary>
        public void EnsureLayout()
        {
            _nameText ??= FindText("Name");
            _seatsText ??= FindText("Seats");
            if (_joinButton == null)
            {
                if (TryGetComponent(out CommonButton selfButton))
                {
                    _joinButton = selfButton;
                }
                else
                {
                    var join = CachedTransform.Find("Join");
                    if (join != null)
                    {
                        join.TryGetComponent(out _joinButton);
                    }
                }
            }

            if (_joinButton != null)
            {
                _joinButton.useSound = false;
                _joinButton.OnClick.RemoveAllListeners();
                _joinButton.OnClick.AddListener(OnJoinClicked);
            }
        }

        /// <summary>
        /// 공개 방 한 줄을 채운다.
        /// </summary>
        public void Bind(PublicRoomInfo room)
        {
            EnsureLayout();
            _sessionId = room.Id;
            if (_nameText != null)
            {
                _nameText.text = string.IsNullOrEmpty(room.Name) ? "방" : room.Name;
            }

            if (_seatsText != null)
            {
                _seatsText.text = room.PlayerCount + "/" + room.MaxPlayers;
            }
        }

        private void OnJoinClicked()
        {
            if (!string.IsNullOrEmpty(_sessionId))
            {
                JoinClicked?.Invoke(_sessionId);
            }
        }

        private TextMeshProUGUI FindText(string name)
        {
            var existing = CachedTransform.Find(name);
            return existing != null && existing.TryGetComponent(out TextMeshProUGUI text) ? text : null;
        }
    }
}
