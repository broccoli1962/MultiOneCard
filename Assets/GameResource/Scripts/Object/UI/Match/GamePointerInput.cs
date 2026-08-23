using System;
using Backend.App;
using UnityEngine.InputSystem;

namespace Backend.Object.UI
{
    /// <summary>
    /// Input System 으로 Enter/Esc/우클릭과 문양·Q·K 단축키를 <see cref="GamePointer"/> 에 넣는다.
    /// </summary>
    public sealed class GamePointerInput : IDisposable
    {
        private static readonly string[] HotkeyPaths =
        {
            "<Keyboard>/s",
            "<Keyboard>/h",
            "<Keyboard>/d",
            "<Keyboard>/c",
            "<Keyboard>/r",
            "<Keyboard>/m",
            "<Keyboard>/g",
            "<Keyboard>/e",
        };

        private readonly GamePointer _pointer;
        private readonly InputAction _confirm;
        private readonly InputAction _cancel;
        private readonly InputAction _hotkeys;

        /// <summary>
        /// 액션을 만들고 켠다.
        /// </summary>
        public GamePointerInput(GamePointer pointer)
        {
            _pointer = pointer ?? throw new ArgumentNullException(nameof(pointer));

            _confirm = new InputAction("GamePointerConfirm", InputActionType.Button);
            _confirm.AddBinding("<Keyboard>/enter");
            _confirm.AddBinding("<Keyboard>/numpadEnter");

            _cancel = new InputAction("GamePointerCancel", InputActionType.Button);
            _cancel.AddBinding("<Keyboard>/escape");
            _cancel.AddBinding("<Mouse>/rightButton");

            _hotkeys = new InputAction("GamePointerHotkeys", InputActionType.Button);
            for (var i = 0; i < HotkeyPaths.Length; i++)
            {
                _hotkeys.AddBinding(HotkeyPaths[i]);
            }

            _confirm.performed += OnConfirm;
            _cancel.performed += OnCancel;
            _hotkeys.performed += OnHotkey;
            _confirm.Enable();
            _cancel.Enable();
            _hotkeys.Enable();
        }

        /// <summary>
        /// 액션을 끄고 해제한다.
        /// </summary>
        public void Dispose()
        {
            _confirm.performed -= OnConfirm;
            _cancel.performed -= OnCancel;
            _hotkeys.performed -= OnHotkey;
            _confirm.Disable();
            _cancel.Disable();
            _hotkeys.Disable();
            _confirm.Dispose();
            _cancel.Dispose();
            _hotkeys.Dispose();
        }

        private void OnConfirm(InputAction.CallbackContext _)
        {
            _pointer.Confirm();
        }

        private void OnCancel(InputAction.CallbackContext _)
        {
            _pointer.Cancel();
        }

        private void OnHotkey(InputAction.CallbackContext ctx)
        {
            if (ctx.control == null)
            {
                return;
            }

            _pointer.PressHotkey(ctx.control.name);
        }
    }
}
