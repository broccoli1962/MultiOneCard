using System;
using System.Collections.Generic;
using Backend.Net;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace Backend.Object.UI
{
    /// <summary>
    /// 대기실·매치 채팅 서브뷰. 전송은 Presenter 가 <see cref="NetClient"/> 로만 보낸다.
    /// </summary>
    public sealed class ChatView : UIView
    {
        /// <summary>기획서 §9 퀵챗 id.</summary>
        public static readonly string[] QuickIds =
        {
            "q_nice", "q_gg", "q_hurry", "q_go", "q_oops", "q_wow", "q_thanks", "q_again",
        };

        private static readonly string[] QuickLabels =
        {
            "나이스", "수고", "빨리", "가자", "실수", "대박", "고마워", "한판더",
        };

        private const int VisibleLines = 30;

        [SerializeField] private TMP_FontAsset _font;
        [SerializeField] private TextMeshProUGUI _logText;
        [SerializeField] private TMP_InputField _input;
        [SerializeField] private CommonButton _sendButton;
        [SerializeField] private RectTransform _quickRow;
        [SerializeField] private CommonButton[] _quickButtons = new CommonButton[8];

        private readonly List<string> _lines = new List<string>(VisibleLines);
        private bool _layoutReady;

        /// <summary>본문 전송. 80자 이내.</summary>
        public event Action<string> SendClicked;

        /// <summary>퀵챗 id. q_nice 등.</summary>
        public event Action<string> QuickClicked;

        /// <summary>현재 입력 본문.</summary>
        public string InputText => _input != null ? _input.text : string.Empty;

        /// <summary>
        /// 프리팹 자식에 묶인 채팅 위젯을 찾아 이벤트를 묶는다.
        /// </summary>
        public void EnsureLayout(TMP_FontAsset font = null)
        {
            if (font != null)
            {
                _font = font;
            }

            if (_layoutReady && _input != null && _logText != null)
            {
                return;
            }

            _logText ??= FindOrCreateText("Log");
            _quickRow ??= FindOrCreateRect("QuickRow");
            BindQuickButtons();
            _input ??= FindOrCreateInput("ChatInput");
            _sendButton ??= FindOrCreateButton("Send");

            BindButton(_sendButton, OnSendPressed);
            BindInputEndEdit(_input, OnInputEndEdit);
            for (var i = 0; i < QuickIds.Length; i++)
            {
                var quickId = QuickIds[i];
                BindButton(QuickButton(i), () => QuickClicked?.Invoke(quickId));
            }

            if (_logText != null && _lines.Count == 0)
            {
                _logText.text = string.Empty;
            }

            _layoutReady = true;
        }

        /// <summary>
        /// 채팅 한 줄을 붙인다. user / quick / system.
        /// </summary>
        public void Append(string chatType, string nick, string text, string quickId)
        {
            EnsureLayout();
            _lines.Add(FormatLine(chatType, nick, text, quickId));
            while (_lines.Count > VisibleLines)
            {
                _lines.RemoveAt(0);
            }

            if (_logText != null)
            {
                _logText.text = string.Join("\n", _lines);
            }
        }

        /// <summary>
        /// 입력칸을 비운다.
        /// </summary>
        public void ClearInput()
        {
            EnsureLayout();
            if (_input != null)
            {
                _input.SetTextWithoutNotify(string.Empty);
            }
        }

        /// <summary>
        /// 로그를 비운다.
        /// </summary>
        public void ClearLog()
        {
            _lines.Clear();
            if (_logText != null)
            {
                _logText.text = string.Empty;
            }
        }

        /// <summary>
        /// 퀵챗 id 의 표시 문구.
        /// </summary>
        public static string QuickLabel(string quickId)
        {
            for (var i = 0; i < QuickIds.Length; i++)
            {
                if (QuickIds[i] == quickId)
                {
                    return QuickLabels[i];
                }
            }

            return quickId ?? string.Empty;
        }

        /// <summary>
        /// 채팅 버튼 레드닷을 켜거나 끈다.
        /// </summary>
        public static void SetUnreadDot(CommonButton button, bool unread)
        {
            if (button == null)
            {
                return;
            }

            var dot = button.CachedTransform.Find("RedDot");
            if (dot != null)
            {
                dot.gameObject.SetActive(unread);
            }
        }

        private void Update()
        {
            var keyboard = Keyboard.current;
            if (keyboard == null)
            {
                return;
            }

            if (keyboard.f1Key.wasPressedThisFrame) QuickClicked?.Invoke(QuickIds[0]);
            else if (keyboard.f2Key.wasPressedThisFrame) QuickClicked?.Invoke(QuickIds[1]);
            else if (keyboard.f3Key.wasPressedThisFrame) QuickClicked?.Invoke(QuickIds[2]);
            else if (keyboard.f4Key.wasPressedThisFrame) QuickClicked?.Invoke(QuickIds[3]);
            else if (keyboard.f5Key.wasPressedThisFrame) QuickClicked?.Invoke(QuickIds[4]);
            else if (keyboard.f6Key.wasPressedThisFrame) QuickClicked?.Invoke(QuickIds[5]);
            else if (keyboard.f7Key.wasPressedThisFrame) QuickClicked?.Invoke(QuickIds[6]);
            else if (keyboard.f8Key.wasPressedThisFrame) QuickClicked?.Invoke(QuickIds[7]);
        }

        private void OnSendPressed()
        {
            if (IsImeComposing())
            {
                return;
            }

            SendClicked?.Invoke(InputText);
        }

        private void OnInputEndEdit(string value)
        {
            if (IsImeComposing() || !WasSubmitPressed())
            {
                return;
            }

            SendClicked?.Invoke(value);
        }

        private static bool IsImeComposing()
        {
            return !string.IsNullOrEmpty(Input.compositionString);
        }

        private static bool WasSubmitPressed()
        {
            var keyboard = Keyboard.current;
            if (keyboard == null)
            {
                return false;
            }

            return keyboard.enterKey.wasPressedThisFrame || keyboard.numpadEnterKey.wasPressedThisFrame;
        }

        private static string FormatLine(string chatType, string nick, string text, string quickId)
        {
            if (chatType == ChatType.System)
            {
                return text ?? string.Empty;
            }

            var who = string.IsNullOrEmpty(nick) ? "?" : nick;
            if (chatType == ChatType.Quick)
            {
                return who + ": " + QuickLabel(quickId);
            }

            return who + ": " + (text ?? string.Empty);
        }

        private void BindQuickButtons()
        {
            if (_quickButtons == null || _quickButtons.Length != QuickIds.Length)
            {
                _quickButtons = new CommonButton[QuickIds.Length];
            }

            if (_quickRow == null)
            {
                return;
            }

            for (var i = 0; i < QuickIds.Length; i++)
            {
                _quickButtons[i] ??= FindOrCreateChildButton(_quickRow, "Quick_" + QuickIds[i]);
            }
        }

        private CommonButton QuickButton(int index)
        {
            return _quickButtons != null && index >= 0 && index < _quickButtons.Length
                ? _quickButtons[index]
                : null;
        }

        private TextMeshProUGUI FindOrCreateText(string name)
        {
            var go = FindOrCreate(name);
            return go != null && go.TryGetComponent(out TextMeshProUGUI text) ? text : null;
        }

        private TMP_InputField FindOrCreateInput(string name)
        {
            var go = FindOrCreate(name);
            return go != null && go.TryGetComponent(out TMP_InputField input) ? input : null;
        }

        private CommonButton FindOrCreateButton(string name)
        {
            var go = FindOrCreate(name);
            if (go == null || !go.TryGetComponent(out CommonButton button))
            {
                return null;
            }

            button.useSound = false;
            return button;
        }

        private CommonButton FindOrCreateChildButton(Transform parent, string name)
        {
            var existing = parent.Find(name);
            if (existing == null || !existing.TryGetComponent(out CommonButton button))
            {
                return null;
            }

            button.useSound = false;
            return button;
        }

        private RectTransform FindOrCreateRect(string name)
        {
            var go = FindOrCreate(name);
            return go != null ? go.GetComponent<RectTransform>() : null;
        }

        private GameObject FindOrCreate(string name)
        {
            var existing = CachedTransform.Find(name);
            return existing != null ? existing.gameObject : null;
        }

        private static void BindButton(CommonButton button, UnityEngine.Events.UnityAction action)
        {
            if (button == null)
            {
                return;
            }

            button.OnClick.RemoveAllListeners();
            button.OnClick.AddListener(action);
        }

        private static void BindInputEndEdit(TMP_InputField input, UnityEngine.Events.UnityAction<string> action)
        {
            if (input == null)
            {
                return;
            }

            input.onEndEdit.RemoveAllListeners();
            input.onEndEdit.AddListener(action);
        }
    }
}
