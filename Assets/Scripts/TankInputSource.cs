using UnityEngine;
using UnityEngine.InputSystem;

namespace Tanks
{
    public class TankInputSource : MonoBehaviour
    {
        [SerializeField] private InputActionAsset _actions;

        private InputAction _moveAction;
        private InputAction _pointAction;
        private bool _useActions;

        public Vector2 Move
        {
            get
            {
                if (_useActions)
                {
                    return _moveAction.ReadValue<Vector2>();
                }

                Keyboard keyboard = Keyboard.current;
                if (keyboard == null)
                {
                    return Vector2.zero;
                }

                return new Vector2(
                    (keyboard.dKey.isPressed ? 1f : 0f) - (keyboard.aKey.isPressed ? 1f : 0f),
                    (keyboard.wKey.isPressed ? 1f : 0f) - (keyboard.sKey.isPressed ? 1f : 0f));
            }
        }

        public Vector2 PointerPosition
        {
            get
            {
                if (_useActions)
                {
                    return _pointAction.ReadValue<Vector2>();
                }

                Mouse mouse = Mouse.current;
                return mouse == null ? new Vector2(Screen.width * 0.5f, Screen.height * 0.5f) : mouse.position.ReadValue();
            }
        }

        private void OnEnable()
        {
            if (_actions == null)
            {
                return;
            }

            _moveAction = _actions.FindAction("Player/Move", throwIfNotFound: false);
            _pointAction = _actions.FindAction("UI/Point", throwIfNotFound: false);

            if (_moveAction == null || _pointAction == null)
            {
                _useActions = false;
                return;
            }

            _actions.Enable();
            _useActions = true;
        }

        private void OnDisable()
        {
            if (_useActions)
            {
                _actions.Disable();
                _useActions = false;
            }
        }
    }
}