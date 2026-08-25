using System;
using System.Collections.Generic;
using Backend.Net;

namespace Backend.App
{
    /// <summary>
    /// 기획서 §8 게임 입력 모음. PlayCard·지급·숨김·미러 버림은 드래그 드롭.
    /// 7·Q·K 선택은 시트에 따라 ChooseSuit 등으로만 발행한다.
    /// Unity 에 의존하지 않는다. Input System 바인딩은 Match UI 가 넣는다.
    /// </summary>
    public sealed class GamePointer
    {
        /// <summary>불법 장 알파. 낮을수록 더 투명하다.</summary>
        public const float IllegalAlpha = 0.22f;

        private readonly HashSet<int> _multiIds = new HashSet<int>();

        private int _selectedInstanceId = -1;
        private int _multiLimit;
        private bool _locked;
        private bool _playEnabled = true;
        private GamePointerSheet _sheet;

        /// <summary>선택된 손패 instanceId. 없으면 -1.</summary>
        public int SelectedInstanceId => _selectedInstanceId;

        /// <summary>내기 선택이 있으면 true.</summary>
        public bool HasSelection => _selectedInstanceId >= 0;

        /// <summary>ack 대기 등으로 입력이 잠겨 있으면 true.</summary>
        public bool IsLocked => _locked;

        /// <summary>현재 선택 시트. None 이면 PlayCard/Draw.</summary>
        public GamePointerSheet Sheet => _sheet;

        /// <summary>지급·미러 다중 선택.</summary>
        public IReadOnlyCollection<int> MultiSelectedIds => _multiIds;

        /// <summary>드래그 드롭 또는 Enter 후에만 발행. 인자는 PlayCard instanceId.</summary>
        public event Action<int> PlayCardRequested;

        /// <summary>덱 탭 또는 D.</summary>
        public event Action DrawRequested;

        /// <summary>7 이후 문양. SuitCode.</summary>
        public event Action<string> ChooseSuitRequested;

        /// <summary>Q Reverse|Give.</summary>
        public event Action<string> ChooseQueenModeRequested;

        /// <summary>K Extra|Hide.</summary>
        public event Action<string> ChooseKingModeRequested;

        /// <summary>Q 지급. instanceId 배열.</summary>
        public event Action<int[]> GiveCardsRequested;

        /// <summary>K 숨김. HideUnder instanceId.</summary>
        public event Action<int> HideUnderRequested;

        /// <summary>미러 버림. instanceId 배열.</summary>
        public event Action<int[]> MirrorDiscardRequested;

        /// <summary>선택·해제 후 화면을 다시 그린다.</summary>
        public event Action SelectionChanged;

        /// <summary>
        /// ack 대기 잠금. 잠그면 내기 선택을 지운다.
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
        /// 선택 시트를 연다. 같은 시트면 선택을 유지한다. 화면은 판결하지 않는다.
        /// </summary>
        public void SetSheet(GamePointerSheet sheet)
        {
            if (_sheet == sheet)
            {
                return;
            }

            _sheet = sheet;
            _playEnabled = sheet == GamePointerSheet.None;
            if (sheet != GamePointerSheet.GiveCards && sheet != GamePointerSheet.MirrorDiscard)
            {
                _multiLimit = 0;
            }

            var changed = _selectedInstanceId >= 0 || _multiIds.Count > 0;
            _selectedInstanceId = -1;
            _multiIds.Clear();
            if (changed)
            {
                SelectionChanged?.Invoke();
            }
        }

        /// <summary>
        /// 지급·미러에서 고를 수 있는 최대 장수. 0 이면 제한 없음.
        /// </summary>
        public void SetMultiLimit(int max)
        {
            _multiLimit = max < 0 ? 0 : max;
        }

        /// <summary>
        /// 내기 선택을 해제한다. PlayCard 는 보내지 않는다.
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
        /// 내기·다중 선택을 모두 지운다. 커맨드는 보내지 않는다.
        /// </summary>
        public void ClearAllSelections()
        {
            var changed = _selectedInstanceId >= 0 || _multiIds.Count > 0;
            _selectedInstanceId = -1;
            _multiIds.Clear();
            if (changed)
            {
                SelectionChanged?.Invoke();
            }
        }

        /// <summary>
        /// 카드 탭. 내기·지급·숨김·미러는 미리보기 선택(재탭은 해제). 확정은 드래그.
        /// </summary>
        public void TapCard(int instanceId)
        {
            if (_locked || instanceId < 0 || !CanPreviewSelect())
            {
                return;
            }

            if (_selectedInstanceId == instanceId)
            {
                _selectedInstanceId = -1;
                SelectionChanged?.Invoke();
                return;
            }

            _selectedInstanceId = instanceId;
            SelectionChanged?.Invoke();
        }

        /// <summary>
        /// 미리보기 선택만 한다. 이미 그 장이면 그대로 둔다. 커맨드는 보내지 않는다.
        /// </summary>
        public void SelectCard(int instanceId)
        {
            if (_locked || instanceId < 0 || !CanPreviewSelect())
            {
                return;
            }

            if (_selectedInstanceId == instanceId)
            {
                return;
            }

            _selectedInstanceId = instanceId;
            SelectionChanged?.Invoke();
        }

        /// <summary>
        /// 손패에서 테이블로 드래그해 놓은 장을 바로 낸다.
        /// </summary>
        public void RequestPlay(int instanceId)
        {
            if (_locked || !_playEnabled || instanceId < 0)
            {
                return;
            }

            IssuePlayCard(instanceId);
        }

        /// <summary>
        /// 지급 시트에서 테이블로 끌어 놓은 장을 준다. 여러 장이면 목표 장수에 도달할 때 보낸다.
        /// </summary>
        public void RequestGive(int instanceId)
        {
            QueueDropped(instanceId, GamePointerSheet.GiveCards, GiveCardsRequested);
        }

        /// <summary>
        /// 숨김 시트에서 테이블로 끌어 놓은 장을 숨긴다.
        /// </summary>
        public void RequestHide(int instanceId)
        {
            if (_locked || _sheet != GamePointerSheet.HideUnder || instanceId < 0)
            {
                return;
            }

            _selectedInstanceId = -1;
            HideUnderRequested?.Invoke(instanceId);
            SelectionChanged?.Invoke();
        }

        /// <summary>
        /// 미러 시트에서 테이블로 끌어 놓은 장을 버린다. 여러 장이면 목표 장수에 도달할 때 보낸다.
        /// </summary>
        public void RequestMirror(int instanceId)
        {
            QueueDropped(instanceId, GamePointerSheet.MirrorDiscard, MirrorDiscardRequested);
        }

        /// <summary>
        /// Enter. 일반 내기만 PlayCard. 지급·숨김·미러는 드래그로만 확정한다.
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
        /// Esc·우클릭·빈곳. 선택만 해제한다. 시트는 닫지 않는다.
        /// </summary>
        public void Cancel()
        {
            if (_locked)
            {
                return;
            }

            ClearAllSelections();
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

        /// <summary>
        /// 문양 버튼. ChooseSuit 만 발행한다.
        /// </summary>
        public void TapSuit(string suit)
        {
            if (_locked || _sheet != GamePointerSheet.Suit || string.IsNullOrEmpty(suit))
            {
                return;
            }

            ChooseSuitRequested?.Invoke(suit);
        }

        /// <summary>
        /// Q Reverse|Give 버튼. ChooseQueenMode 만 발행한다.
        /// </summary>
        public void TapQueenMode(string queenMode)
        {
            if (_locked || _sheet != GamePointerSheet.QueenMode || string.IsNullOrEmpty(queenMode))
            {
                return;
            }

            ChooseQueenModeRequested?.Invoke(queenMode);
        }

        /// <summary>
        /// K Extra|Hide 버튼. ChooseKingMode 만 발행한다.
        /// </summary>
        public void TapKingMode(string kingMode)
        {
            if (_locked || _sheet != GamePointerSheet.KingMode || string.IsNullOrEmpty(kingMode))
            {
                return;
            }

            ChooseKingModeRequested?.Invoke(kingMode);
        }

        /// <summary>
        /// 기획서 §8 단축키. 시트에 따라 문양/Q/K/드로우.
        /// </summary>
        public void PressHotkey(string key)
        {
            if (_locked || string.IsNullOrEmpty(key))
            {
                return;
            }

            var code = char.ToUpperInvariant(key[0]);
            switch (_sheet)
            {
                case GamePointerSheet.Suit:
                    if (TryMapSuit(code, out var suit))
                    {
                        TapSuit(suit);
                    }

                    return;
                case GamePointerSheet.QueenMode:
                    if (code == 'R')
                    {
                        TapQueenMode(QueenModeName.Reverse);
                    }
                    else if (code == 'G')
                    {
                        TapQueenMode(QueenModeName.Give);
                    }

                    return;
                case GamePointerSheet.KingMode:
                    if (code == 'E')
                    {
                        TapKingMode(KingModeName.Extra);
                    }
                    else if (code == 'H')
                    {
                        TapKingMode(KingModeName.Hide);
                    }

                    return;
                default:
                    if (code == 'D')
                    {
                        Draw();
                    }

                    return;
            }
        }

        private bool CanPreviewSelect()
        {
            return _playEnabled
                || _sheet == GamePointerSheet.GiveCards
                || _sheet == GamePointerSheet.HideUnder
                || _sheet == GamePointerSheet.MirrorDiscard;
        }

        private void QueueDropped(int instanceId, GamePointerSheet sheet, Action<int[]> requested)
        {
            if (_locked || _sheet != sheet || instanceId < 0)
            {
                return;
            }

            if (_multiIds.Contains(instanceId))
            {
                return;
            }

            var need = _multiLimit > 0 ? _multiLimit : 1;
            if (_multiIds.Count >= need)
            {
                return;
            }

            _multiIds.Add(instanceId);
            if (_multiIds.Count >= need)
            {
                IssueMulti(requested);
                return;
            }

            SelectionChanged?.Invoke();
        }

        private void IssuePlayCard(int instanceId)
        {
            _selectedInstanceId = -1;
            PlayCardRequested?.Invoke(instanceId);
            SelectionChanged?.Invoke();
        }

        private void IssueMulti(Action<int[]> requested)
        {
            var ids = new int[_multiIds.Count];
            _multiIds.CopyTo(ids);
            _multiIds.Clear();
            _selectedInstanceId = -1;
            requested?.Invoke(ids);
            SelectionChanged?.Invoke();
        }

        private static bool TryMapSuit(char code, out string suit)
        {
            switch (code)
            {
                case 'S':
                    suit = SuitCode.Spade;
                    return true;
                case 'H':
                    suit = SuitCode.Heart;
                    return true;
                case 'D':
                    suit = SuitCode.Diamond;
                    return true;
                case 'C':
                    suit = SuitCode.Club;
                    return true;
                case 'R':
                    suit = SuitCode.Star;
                    return true;
                case 'M':
                    suit = SuitCode.Moon;
                    return true;
                default:
                    suit = null;
                    return false;
            }
        }
    }

    /// <summary>
    /// GamePointer 가 받는 선택 시트. 호스트 프롬프트와 맞출 뿐 판결이 아니다.
    /// </summary>
    public enum GamePointerSheet
    {
        None,
        Suit,
        QueenMode,
        KingMode,
        GiveCards,
        HideUnder,
        MirrorDiscard,
    }
}
