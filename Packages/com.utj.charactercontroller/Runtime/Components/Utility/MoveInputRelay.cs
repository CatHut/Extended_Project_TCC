using Unity.TinyCharacterController.Control;
using UnityEngine;

namespace Unity.TinyCharacterController.Utility
{
    /// <summary>
    /// Relays movement input to <see cref="MoveControl"/>, applying optional processing.
    ///
    /// Any input source can pass a value through <see cref="SetInput"/>:
    /// <c>PlayerInputMoveSource</c> (Input System), a game-specific input handler, AI, replays and so on.
    /// The relay forwards the input to <see cref="MoveControl.Move"/> only when the value changes,
    /// because <see cref="MoveControl"/> keeps using the last value until the next call.
    /// When this component is disabled, it sends zero so that the character does not keep moving
    /// with the last input. The received input is kept and sent again when the component is enabled.
    ///
    /// Responsibilities that this component does NOT have (they belong to the game side):
    /// - Changing priorities (MovePriority, TurnPriority, and so on).
    ///   Use priorities to let another control take over the movement.
    /// - Changing <see cref="MoveControl.MoveSpeed"/>.
    ///   <see cref="MoveControl"/> uses only the direction of the input, so the magnitude of the input
    ///   does not affect the speed. To change the speed by the stick tilt, set MoveSpeed from
    ///   <see cref="RawInput"/> in another component. Keep a single owner of MoveSpeed.
    /// - Routing input (for example, using the stick for target selection instead of movement).
    ///
    /// Override <see cref="ProcessInput"/> when the Inspector settings are not enough.
    /// Only processing that changes the direction is meaningful (axis constraints, snapping, flipping).
    /// </summary>
    [AddComponentMenu(MenuList.Utility + nameof(MoveInputRelay))]
    [DisallowMultipleComponent]
    public class MoveInputRelay : MonoBehaviour
    {
        /// <summary>
        /// Axis constraint applied to the input.
        /// </summary>
        public enum AxisConstraint
        {
            /// <summary>Use both X and Y.</summary>
            Both,

            /// <summary>Use only X. Y is set to 0 (for example, side-scrolling games).</summary>
            HorizontalOnly,

            /// <summary>Use only Y. X is set to 0.</summary>
            VerticalOnly,
        }

        /// <summary>
        /// The MoveControl that receives the input.
        /// If not set, the MoveControl on the same GameObject is used.
        /// </summary>
        [SerializeField]
        private MoveControl _moveControl;

        /// <summary>
        /// Axis constraint applied to the input.
        /// </summary>
        [SerializeField]
        private AxisConstraint _axisConstraint = AxisConstraint.Both;

        /// <summary>
        /// Input whose magnitude is smaller than this value is treated as zero.
        /// MoveControl accelerates to full speed with any non-zero input, so this prevents
        /// small stick noise from moving the character.
        /// </summary>
        [SerializeField, Min(0)]
        private float _inputThreshold = 0;

        /// <summary>
        /// The input received by <see cref="SetInput"/>, before processing.
        /// </summary>
        public Vector2 RawInput { get; private set; }

        /// <summary>
        /// The value last passed to <see cref="MoveControl.Move"/>, after processing.
        /// </summary>
        public Vector2 AppliedInput { get; private set; }

        /// <summary>
        /// The MoveControl that receives the input.
        /// </summary>
        public MoveControl MoveControl
        {
            get
            {
                ResolveMoveControl();
                return _moveControl;
            }
            set
            {
                _moveControl = value;
                if (isActiveAndEnabled)
                    Apply();
            }
        }

        /// <summary>
        /// Axis constraint applied to the input. Changing it applies the current input again.
        /// </summary>
        public AxisConstraint Axis
        {
            get => _axisConstraint;
            set
            {
                _axisConstraint = value;
                if (isActiveAndEnabled)
                    Apply();
            }
        }

        /// <summary>
        /// Input whose magnitude is smaller than this value is treated as zero.
        /// Changing it applies the current input again.
        /// </summary>
        public float InputThreshold
        {
            get => _inputThreshold;
            set
            {
                _inputThreshold = Mathf.Max(0, value);
                if (isActiveAndEnabled)
                    Apply();
            }
        }

        /// <summary>
        /// Sets the movement input.
        /// The value is always kept, and is passed to MoveControl only while this component is enabled.
        /// </summary>
        /// <param name="input">Movement input, such as the left stick value.</param>
        public void SetInput(Vector2 input)
        {
            RawInput = input;
            if (isActiveAndEnabled)
                Apply();
        }

        /// <summary>
        /// Processes the raw input before it is passed to MoveControl.
        /// The default implementation applies the axis constraint and then the input threshold.
        /// </summary>
        /// <param name="raw">The input received by <see cref="SetInput"/>.</param>
        /// <returns>The value passed to <see cref="MoveControl.Move"/>.</returns>
        protected virtual Vector2 ProcessInput(Vector2 raw)
        {
            var input = _axisConstraint switch
            {
                AxisConstraint.HorizontalOnly => new Vector2(raw.x, 0),
                AxisConstraint.VerticalOnly => new Vector2(0, raw.y),
                _ => raw,
            };

            if (input.sqrMagnitude < _inputThreshold * _inputThreshold)
                return Vector2.zero;

            return input;
        }

        /// <summary>
        /// Processes <see cref="RawInput"/> again and passes it to MoveControl.
        /// Call this from a derived class when the processing depends on state other than the input
        /// (for example, when it should be updated every frame).
        /// </summary>
        protected void Apply()
        {
            if (!ResolveMoveControl())
                return;

            AppliedInput = ProcessInput(RawInput);
            _moveControl.Move(AppliedInput);
        }

        protected virtual void Awake()
        {
            ResolveMoveControl();
        }

        protected virtual void OnEnable()
        {
            Apply();
        }

        protected virtual void OnDisable()
        {
            // MoveControl keeps the last input. Send zero so that the character stops.
            AppliedInput = Vector2.zero;
            if (_moveControl != null)
                _moveControl.Move(Vector2.zero);
        }

        private void Reset()
        {
            TryGetComponent(out _moveControl);
        }

        private bool ResolveMoveControl()
        {
            if (_moveControl == null)
                TryGetComponent(out _moveControl);

            return _moveControl != null;
        }
    }
}
