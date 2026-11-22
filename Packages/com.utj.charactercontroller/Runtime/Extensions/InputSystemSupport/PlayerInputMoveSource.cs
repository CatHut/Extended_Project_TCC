#if PACKAGE_INPUT_SYSTEM_EXISTS

using Unity.TinyCharacterController.Utility;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Unity.TinyCharacterController.Extensions
{
    /// <summary>
    /// Reads a Vector2 action (default: "Move") from <see cref="PlayerInput"/>
    /// and passes the value to <see cref="MoveInputRelay"/>.
    ///
    /// This is the standard minimal input source.
    /// Use it as is when the movement input does not need to be routed:
    /// add this component next to <see cref="MoveInputRelay"/>, and no code is required.
    /// When the game needs to route the input (for example, using the stick for target selection
    /// in some modes), do not extend this component. Replace it with a game-specific input handler
    /// that calls <see cref="MoveInputRelay.SetInput"/> directly.
    ///
    /// Differences between devices (stick, WASD, and so on) are handled by the bindings in the
    /// Input Actions asset (for example, a "2D Vector" composite). Differences between 2D and 3D
    /// games are handled by <see cref="MoveInputRelay.Axis"/>. Keep the action as Vector2 for both.
    ///
    /// Requirements:
    /// - The action must be a Vector2 (Value / Vector2, Stick, Dpad). 1D Axis actions are not supported.
    /// - This assembly is compiled only when the scripting define symbol SUPPORT_TCC is set
    ///   in the project that uses this package.
    /// </summary>
    [AddComponentMenu("TCC/Utility/" + nameof(PlayerInputMoveSource))]
    [DisallowMultipleComponent]
    public class PlayerInputMoveSource : MonoBehaviour
    {
        /// <summary>
        /// The PlayerInput to read. If not set, the PlayerInput on the same GameObject is used.
        /// </summary>
        [SerializeField]
        private PlayerInput _playerInput;

        /// <summary>
        /// Name of the Vector2 action to read.
        /// </summary>
        [SerializeField]
        private string _actionName = "Move";

        /// <summary>
        /// The relay that receives the input. If not set, the MoveInputRelay on the same GameObject is used.
        /// </summary>
        [SerializeField]
        private MoveInputRelay _relay;

        private InputAction _action;

        private void OnEnable()
        {
            if (_playerInput == null)
                TryGetComponent(out _playerInput);
            if (_relay == null)
                TryGetComponent(out _relay);

            if (_playerInput == null || _relay == null)
            {
                Debug.LogWarning($"[{nameof(PlayerInputMoveSource)}] {name}: PlayerInput or MoveInputRelay is not found.", this);
                return;
            }

            _action = _playerInput.actions != null ? _playerInput.actions.FindAction(_actionName) : null;
            if (_action == null)
            {
                Debug.LogWarning($"[{nameof(PlayerInputMoveSource)}] {name}: Action '{_actionName}' is not found.", this);
                return;
            }

            if (!IsVector2Action(_action))
            {
                Debug.LogWarning($"[{nameof(PlayerInputMoveSource)}] {name}: Action '{_actionName}' is not a Vector2 action (expected control type: {_action.expectedControlType}).", this);
                _action = null;
                return;
            }

            _action.performed += OnMove;
            _action.canceled += OnMove;

            // Send the current value in case the stick is already held when this component is enabled.
            _relay.SetInput(_action.ReadValue<Vector2>());
        }

        private void OnDisable()
        {
            if (_action != null)
            {
                _action.performed -= OnMove;
                _action.canceled -= OnMove;
                _action = null;
            }

            // This source no longer provides input.
            if (_relay != null)
                _relay.SetInput(Vector2.zero);
        }

        private void OnMove(InputAction.CallbackContext context)
        {
            var value = context.phase == InputActionPhase.Canceled
                ? Vector2.zero
                : context.ReadValue<Vector2>();

            _relay.SetInput(value);
        }

        private static bool IsVector2Action(InputAction action)
        {
            var type = action.expectedControlType;

            // An empty control type accepts any control. Assume it is used as Vector2.
            return string.IsNullOrEmpty(type)
                   || type == "Vector2"
                   || type == "Stick"
                   || type == "Dpad";
        }
    }
}

#endif
