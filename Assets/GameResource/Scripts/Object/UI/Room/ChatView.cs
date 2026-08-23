using System;
using System.Collections.Generic;
using Backend.Net;
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

        private const int BodyMaxChars = 80;
        private const int VisibleLines = 30;

        [SerializeField] private Font _font;
        [SerializeField] private Text _logText;
        [SerializeField] private InputField _input;
        [SerializeField] private CommonButton _sendButton;
        [SerializeField] private RectTransform _quickRow;

        private readonly List<string> _lines = new List<string>(VisibleLines);
        private readonly CommonButton[] _quickButtons = new CommonButton[8];
        private bool _layoutReady;

        /// <summary>본문 전송. 80자 이내.</summary>
        public event Action<string> SendClicked;

        /// <summary>퀵챗 id. q_nice 등.</summary>
        public event Action<string> QuickClicked;

        /// <summary>현재 입력 본문.</summary>
        public string InputText => _input != null ? _input.text : string.Empty;

        /// <summary>
        /// 프리팹 미배선이어도 채팅 레이아웃을 채운다.
        /// </summary>
        public void EnsureLayout(Font font = null)
        {
            if (font != null)
            {
                _font = font;
            }

            if (_layoutReady && _input != null && _logText != null)
            {
                return;
            }

            var rt = CachedRectTransform;
            if (rt != null)
            {
                rt.anchorMin = new Vector2(0f, 0f);
                rt.anchorMax = new Vector2(1f, 0f);
                rt.pivot = new Vector2(0.5f, 0f);
                rt.anchoredPosition = new Vector2(0f, 16f);
                rt.sizeDelta = new Vector2(-32f, 420f);
            }

            if (!TryGetComponent(out Image bg))
            {
                bg = CachedGameObject.AddComponent<Image>();
            }

            bg.color = new Color(0.06f, 0.08f, 0.12f, 0.92f);
            bg.raycastTarget = true;

            _logText = FindOrCreateText("Log", new Vector2(0.5f, 1f), new Vector2(0f, -110f), new Vector2(-24f, 180f), 22f);
            _logText.alignment = TextAnchor.LowerLeft;
            _logText.horizontalOverflow = HorizontalWrapMode.Wrap;
            _logText.verticalOverflow = VerticalWrapMode.Overflow;
            Stretch(_logText.rectTransform, new Vector2(0f, 0.32f), new Vector2(1f, 1f), 12f, 8f, -12f, -8f);

            _quickRow = FindOrCreateRect("QuickRow");
            Stretch(_quickRow, new Vector2(0f, 0.18f), new Vector2(1f, 0.32f), 8f, 4f, -8f, -4f);
            if (!_quickRow.TryGetComponent(out HorizontalLayoutGroup quickLayout))
            {
                quickLayout = _quickRow.gameObject.AddComponent<HorizontalLayoutGroup>();
            }

            quickLayout.childAlignment = TextAnchor.MiddleCenter;
            quickLayout.spacing = 6f;
            quickLayout.childForceExpandWidth = true;
            quickLayout.childControlHeight = true;
            quickLayout.padding = new RectOffset(4, 4, 2, 2);

            for (var i = 0; i < QuickIds.Length; i++)
            {
                _quickButtons[i] = FindOrCreateChildButton(_quickRow, "Quick_" + QuickIds[i], QuickLabels[i]);
            }

            _input = FindOrCreateInput("ChatInput", BodyMaxChars, "채팅 80자");
            Stretch(_input.GetComponent<RectTransform>(), new Vector2(0f, 0f), new Vector2(0.78f, 0.18f), 8f, 8f, -6f, -8f);

            _sendButton = FindOrCreateButton("Send", "전송", new Vector2(0.89f, 0.09f), new Vector2(0f, 0f), new Vector2(140f, 56f));
            var sendRt = _sendButton.CachedRectTransform;
            sendRt.anchorMin = new Vector2(0.8f, 0f);
            sendRt.anchorMax = new Vector2(1f, 0.18f);
            sendRt.offsetMin = new Vector2(4f, 8f);
            sendRt.offsetMax = new Vector2(-8f, -8f);

            BindButton(_sendButton, OnSendPressed);
            BindInputEndEdit(_input, OnInputEndEdit);
            for (var i = 0; i < _quickButtons.Length; i++)
            {
                var quickId = QuickIds[i];
                BindButton(_quickButtons[i], () => QuickClicked?.Invoke(quickId));
            }

            if (_lines.Count == 0)
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

            _logText.text = string.Join("\n", _lines);
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

        private Text FindOrCreateText(string name, Vector2 anchor, Vector2 pos, Vector2 size, float fontSize)
        {
            var go = FindOrCreate(name, typeof(RectTransform), typeof(Text));
            var textRt = go.GetComponent<RectTransform>();
            textRt.anchorMin = anchor;
            textRt.anchorMax = anchor;
            textRt.pivot = new Vector2(0.5f, 0.5f);
            textRt.anchoredPosition = pos;
            textRt.sizeDelta = size;
            if (!go.TryGetComponent(out Text text))
            {
                text = go.AddComponent<Text>();
            }

            ApplyTextStyle(text, fontSize, TextAnchor.MiddleLeft);
            return text;
        }

        private InputField FindOrCreateInput(string name, int characterLimit, string placeholder)
        {
            var go = FindOrCreate(name, typeof(RectTransform), typeof(Image), typeof(InputField));
            if (go.TryGetComponent(out Image image))
            {
                image.color = new Color(0.12f, 0.12f, 0.14f, 0.95f);
            }

            if (!go.TryGetComponent(out InputField input))
            {
                input = go.AddComponent<InputField>();
            }

            var text = FindOrCreateChildText(go.transform, "Text", Color.white);
            var place = FindOrCreateChildText(go.transform, "Placeholder", new Color(1f, 1f, 1f, 0.35f));
            place.text = placeholder;
            input.textComponent = text;
            input.placeholder = place;
            input.characterLimit = characterLimit;
            input.lineType = InputField.LineType.SingleLine;
            return input;
        }

        private Text FindOrCreateChildText(Transform parent, string name, Color color)
        {
            var existing = parent.Find(name);
            GameObject go;
            if (existing != null)
            {
                go = existing.gameObject;
            }
            else
            {
                go = new GameObject(name, typeof(RectTransform), typeof(Text));
                go.transform.SetParent(parent, false);
            }

            var textRt = go.GetComponent<RectTransform>();
            textRt.anchorMin = Vector2.zero;
            textRt.anchorMax = Vector2.one;
            textRt.offsetMin = new Vector2(10f, 4f);
            textRt.offsetMax = new Vector2(-10f, -4f);
            if (!go.TryGetComponent(out Text text))
            {
                text = go.AddComponent<Text>();
            }

            text.color = color;
            text.raycastTarget = false;
            text.alignment = TextAnchor.MiddleLeft;
            text.fontSize = 24;
            text.supportRichText = false;
            var font = ResolveFont();
            if (font != null)
            {
                text.font = font;
            }

            return text;
        }

        private CommonButton FindOrCreateButton(string name, string label, Vector2 anchor, Vector2 pos, Vector2 size)
        {
            var go = FindOrCreate(name, typeof(RectTransform), typeof(Image), typeof(CommonButton));
            var buttonRt = go.GetComponent<RectTransform>();
            buttonRt.anchorMin = anchor;
            buttonRt.anchorMax = anchor;
            buttonRt.pivot = new Vector2(0.5f, 0.5f);
            buttonRt.anchoredPosition = pos;
            buttonRt.sizeDelta = size;
            if (go.TryGetComponent(out Image image))
            {
                image.color = new Color(0.16f, 0.16f, 0.18f, 0.92f);
            }

            if (!go.TryGetComponent(out CommonButton button))
            {
                button = go.AddComponent<CommonButton>();
            }

            button.useSound = false;
            EnsureButtonLabel(go.transform, label, 24f);
            return button;
        }

        private CommonButton FindOrCreateChildButton(Transform parent, string name, string label)
        {
            var existing = parent.Find(name);
            GameObject go;
            if (existing != null)
            {
                go = existing.gameObject;
            }
            else
            {
                go = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(CommonButton));
                go.transform.SetParent(parent, false);
            }

            go.GetComponent<RectTransform>().sizeDelta = new Vector2(96f, 48f);
            if (go.TryGetComponent(out Image image))
            {
                image.color = new Color(0.2f, 0.2f, 0.22f, 0.95f);
            }

            if (!go.TryGetComponent(out CommonButton button))
            {
                button = go.AddComponent<CommonButton>();
            }

            button.useSound = false;
            EnsureButtonLabel(go.transform, label, 18f);
            return button;
        }

        private void EnsureButtonLabel(Transform parent, string label, float fontSize)
        {
            var existing = parent.Find("Label");
            GameObject go;
            if (existing != null)
            {
                go = existing.gameObject;
            }
            else
            {
                go = new GameObject("Label", typeof(RectTransform), typeof(Text));
                go.transform.SetParent(parent, false);
            }

            var labelRt = go.GetComponent<RectTransform>();
            labelRt.anchorMin = Vector2.zero;
            labelRt.anchorMax = Vector2.one;
            labelRt.offsetMin = Vector2.zero;
            labelRt.offsetMax = Vector2.zero;
            if (!go.TryGetComponent(out Text text))
            {
                text = go.AddComponent<Text>();
            }

            text.text = label;
            ApplyTextStyle(text, fontSize, TextAnchor.MiddleCenter);
        }

        private void ApplyTextStyle(Text text, float fontSize, TextAnchor alignment)
        {
            text.fontSize = (int)fontSize;
            text.alignment = alignment;
            text.color = Color.white;
            text.raycastTarget = false;
            text.horizontalOverflow = HorizontalWrapMode.Wrap;
            text.verticalOverflow = VerticalWrapMode.Truncate;
            var font = ResolveFont();
            if (font != null)
            {
                text.font = font;
            }
        }

        private Font ResolveFont()
        {
            return _font != null ? _font : Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        }

        private RectTransform FindOrCreateRect(string name)
        {
            return FindOrCreate(name, typeof(RectTransform)).GetComponent<RectTransform>();
        }

        private GameObject FindOrCreate(string name, params Type[] components)
        {
            var existing = CachedTransform.Find(name);
            if (existing != null)
            {
                return existing.gameObject;
            }

            var go = new GameObject(name, components);
            go.transform.SetParent(CachedTransform, false);
            return go;
        }

        private static void Stretch(RectTransform rt, Vector2 min, Vector2 max, float left, float bottom, float right, float top)
        {
            rt.anchorMin = min;
            rt.anchorMax = max;
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.offsetMin = new Vector2(left, bottom);
            rt.offsetMax = new Vector2(right, top);
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

        private static void BindInputEndEdit(InputField input, UnityEngine.Events.UnityAction<string> action)
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
