using System;
using System.Collections.Generic;
using Backend.Object.Management;
using TMPro;
using UnityEngine;

namespace Backend.Object.UI
{
    /// <summary>
    /// 공개 방 목록 View. 새로고침·입장·닫기만 담당한다.
    /// </summary>
    public sealed class RoomListPanel : UIPanel<RoomListPresenter>
    {
        [SerializeField] private TextMeshProUGUI _titleText;
        [SerializeField] private TextMeshProUGUI _statusText;
        [SerializeField] private CommonButton _backButton;
        [SerializeField] private CommonButton _refreshButton;
        [SerializeField] private Transform _content;
        [SerializeField] private RoomListRow _rowTemplate;

        private readonly List<RoomListRow> _rows = new();
        private bool _layoutReady;

        /// <summary>닫기.</summary>
        public event Action BackClicked;

        /// <summary>목록 다시 조회.</summary>
        public event Action RefreshClicked;

        /// <summary>방 입장. 인자는 세션 Id.</summary>
        public event Action<string> JoinClicked;

        protected override bool DefaultHandleBackButton => true;

        protected override void Awake()
        {
            if (Application.isPlaying)
            {
                EnsureLayout();
            }

            base.Awake();
        }

        /// <summary>
        /// 프리팹 자식에 묶인 위젯을 찾아 이벤트를 묶는다.
        /// </summary>
        public void EnsureLayout()
        {
            if (_layoutReady && _backButton != null && _content != null)
            {
                return;
            }

            _titleText ??= FindWidget<TextMeshProUGUI>("Title");
            _statusText ??= FindWidget<TextMeshProUGUI>("Status");
            _backButton ??= FindButton("Back");
            _refreshButton ??= FindButton("Refresh");
            if (_content == null)
            {
                var content = CachedTransform.Find("List/Viewport/Content");
                _content = content != null ? content : CachedTransform.Find("Content");
            }

            if (_rowTemplate == null)
            {
                var template = CachedTransform.Find("List/Viewport/Content/RowTemplate");
                if (template == null)
                {
                    template = CachedTransform.Find("RowTemplate");
                }

                if (template != null)
                {
                    template.TryGetComponent(out _rowTemplate);
                }
            }

            BindButton(_backButton, () => BackClicked?.Invoke());
            BindButton(_refreshButton, () => RefreshClicked?.Invoke());
            if (_rowTemplate != null)
            {
                _rowTemplate.CachedGameObject.SetActive(false);
            }

            _layoutReady = true;
        }

        /// <summary>
        /// 상태 문구를 표시한다.
        /// </summary>
        public void SetStatus(string status)
        {
            EnsureLayout();
            if (_statusText != null)
            {
                _statusText.text = status ?? string.Empty;
            }
        }

        /// <summary>
        /// 공개 방 줄을 다시 그린다.
        /// </summary>
        public void SetRooms(IReadOnlyList<PublicRoomInfo> rooms)
        {
            EnsureLayout();
            var count = rooms != null ? rooms.Count : 0;
            EnsureRowCount(count);
            for (var i = 0; i < _rows.Count; i++)
            {
                var row = _rows[i];
                if (i >= count)
                {
                    row.CachedGameObject.SetActive(false);
                    continue;
                }

                row.CachedGameObject.SetActive(true);
                row.Bind(rooms[i]);
            }
        }

        private void EnsureRowCount(int count)
        {
            if (_content == null || _rowTemplate == null)
            {
                return;
            }

            while (_rows.Count < count)
            {
                var row = Instantiate(_rowTemplate, _content);
                row.EnsureLayout();
                row.JoinClicked += OnRowJoinClicked;
                _rows.Add(row);
            }
        }

        private void OnRowJoinClicked(string sessionId)
        {
            JoinClicked?.Invoke(sessionId);
        }

    }
}
