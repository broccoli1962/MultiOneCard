using System;

namespace Backend.App
{
    /// <summary>
    /// 기획서 §8 게임 입력 모음. 선택→같은 장 재탭 또는 Enter 로만 PlayCard 를 발행한다.
    /// Unity 에 의존하지 않는다. Input System 바인딩은 Match UI 가 넣는다.
    /// </summary>
    public sealed class GamePointer
    {
        /// <summary>기획서 §8 불법 장 투명도.</summary>
        public const float IllegalAlpha = 0.4f;

        private int _selectedInstanceId = -1;
        private bool _locked;
        private bool _playEnabled = true;

        /// <summary>선택된 손패 instanceId. 없으면 -1.</summary>
        public int SelectedInstanceId => _selectedInstanceId;

        /// <summary>내기 선택이 있으면 true.</summary>
        public bool HasSelection => _selectedInstanceId >= 0;

        /// <summary>ack 대기 등으로 입력이 잠겨 있으면 true.</summary>
        public bool IsLocked => _locked;

        /// <summary>재탭/Enter 확정 후에만 발행. 인자는 PlayCard instanceId.</summary>
        public event Action<int> PlayCardRequested;

        /// <summary>덱 탭 또는 D.</summary>
        public event Action DrawRequested;

        /// <summary>선택·해제 후 화면을 다시 그린다.</summary>
        public event Action SelectionChanged;

        /// <summary>
        /// ack 대기 잠금. 잠그면 선택을 지운다.
        /// </summary>
        public void SetLocked(bool locked)
        {
            if (_locked == locked)
            {
                return;
            }

            _locked = locked;
            if (locked)
            {
                ClearSelection();
            }
        }

        /// <summary>
        /// 일반 내기(PlayCard) 입력을 켤지. 문양·Q·K 시트 중에는 끈다.
        /// </summary>
        public void SetPlayEnabled(bool enabled)
        {
            if (_playEnabled == enabled)
            {
                return;
            }

            _playEnabled = enabled;
            if (!enabled)
            {
                ClearSelection();
            }
        }

        /// <summary>
        /// 선택을 해제한다. PlayCard 는 보내지 않는다.
        /// </summary>
        public void ClearSelection()
        {
            if (_selectedInstanceId < 0)
            {
                return;
            }

            _selectedInstanceId = -1;
            SelectionChanged?.Invoke();
        }

        /// <summary>
        /// 카드 탭. 다른 장이면 선택만, 같은 장 재탭이면 PlayCard.
        /// 한 번의 탭으로는 나가지 않는다.
        /// </summary>
        public void TapCard(int instanceId)
        {
            if (_locked || !_playEnabled || instanceId < 0)
            {
                return;
            }

            if (_selectedInstanceId == instanceId)
            {
                IssuePlayCard(instanceId);
                return;
            }

            _selectedInstanceId = instanceId;
            SelectionChanged?.Invoke();
        }

        /// <summary>
        /// Enter. 선택된 장이 있을 때만 PlayCard.
        /// </summary>
        public void Confirm()
        {
            if (_locked || !_playEnabled || _selectedInstanceId < 0)
            {
                return;
            }

            IssuePlayCard(_selectedInstanceId);
        }

        /// <summary>
        /// Esc·우클릭·빈곳. 선택만 해제한다.
        /// </summary>
        public void Cancel()
        {
            if (_locked)
            {
                return;
            }

            ClearSelection();
        }

        /// <summary>
        /// 덱 탭 또는 D. 선택을 지운 뒤 Draw 를 요청한다.
        /// </summary>
        public void Draw()
        {
            if (_locked || !_playEnabled)
            {
                return;
            }

            ClearSelection();
            DrawRequested?.Invoke();
        }

        private void IssuePlayCard(int instanceId)
        {
            _selectedInstanceId = -1;
            PlayCardRequested?.Invoke(instanceId);
            SelectionChanged?.Invoke();
        }
    }
}
