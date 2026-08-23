using System;
using Backend.App;
using UnityEngine.InputSystem;

namespace Backend.Object.UI
{
    /// <summary>
    /// Input System 으로 Enter/Esc/우클릭/D 를 <see cref="GamePointer"/> 에 넣는다.
    /// </summary>
    public sealed class GamePointerInput : IDisposable
    {
        private readonly GamePointer _pointer;
        private readonly InputAction _confirm;
        private readonly InputAction _cancel;
        private readonly InputAction _draw;

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

            _draw = new InputAction("GamePointerDraw", InputActionType.Button);
            _draw.AddBinding("<Keyboard>/d");

            _confirm.performed += OnConfirm;
            _cancel.performed += OnCancel;
            _draw.performed += OnDraw;
            _confirm.Enable();
            _cancel.Enable();
            _draw.Enable();
        }

        /// <summary>
        /// 액션을 끄고 해제한다.
        /// </summary>
        public void Dispose()
        {
            _confirm.performed -= OnConfirm;
            _cancel.performed -= OnCancel;
            _draw.performed -= OnDraw;
            _confirm.Disable();
            _cancel.Disable();
            _draw.Disable();
            _confirm.Dispose();
            _cancel.Dispose();
            _draw.Dispose();
        }

        private void OnConfirm(InputAction.CallbackContext _)
        {
            _pointer.Confirm();
        }

        private void OnCancel(InputAction.CallbackContext _)
        {
            _pointer.Cancel();
        }

        private void OnDraw(InputAction.CallbackContext _)
        {
            _pointer.Draw();
        }
    }
}
