using System.Collections.Generic;
using Unity.TinyCharacterController.Interfaces.Components;
using Unity.TinyCharacterController.Interfaces.Core;
using Unity.TinyCharacterController.Modifier;
using Unity.TinyCharacterController.Smb;
using Unity.TinyCharacterController.Utility;
using Unity.VisualScripting;
using UnityEngine;

namespace Unity.TinyCharacterController.Control
{
    /// <summary>
    /// Movement using root motion.
    /// This component is only active on frames when <see cref="WorkThisFrame()"/> is executed.<br/>
    /// Intended for use with <see cref="AnimatorMoveBehaviour"/>.
    /// The priority of this component is only active on the frame when <see cref="WorkThisFrame()"/> is called.
    /// </summary>
    [AddComponentMenu(MenuList.MenuControl + nameof(AnimatorMoveControl))]
    [DisallowMultipleComponent]
    [RenamedFrom("TinyCharacterController.AnimatorMove")]
    [Unity.VisualScripting.RenamedFrom("TinyCharacterController.Control.AnimatorMoveControl")]
    public class AnimatorMoveControl : MonoBehaviour,
        IPriorityLifecycle<IMove>,
        IMove,
        ITurn,
        IUpdateComponent
    {
        [Header("Priority")]
        public int MovePriority = 10;
        public int TurnPriority = 10;

        [Header("Fixed Position")]
        [Tooltip("When IsFixedPosition is enabled, subtract the descent blocked by the ground (or other colliders) from the following upward movement. " +
                 "Prevents the character from floating after landing when the root of the clip goes below its starting height (e.g. crouching on landing).")]
        [SerializeField] private bool _compensateBlockedDescent = true;

        private const float BlockedDescentThreshold = 0.0001f;

        private bool _isFixedPosition = false; // Perform movement with Warp, preventing external influences.
        private bool _isWorkComponent; // When this setting is true, priorities are enabled.
        private Vector3 _velocity; // Character's movement vector.
        private Vector3 _deltaPosition; // Character's movement in this frame. Used for IsFixedPosition.
        private float _blockedDescent; // Accumulated descent blocked by colliders, not yet cancelled.
        private bool _hasRequestedPosition; // Whether a position was requested by Warp in the previous frame.
        private Vector3 _requestedPosition; // Position requested by Warp in the previous frame.
        private float _turn; // Character's Y-axis direction.
        private bool _hasGroundCheck; // Enable GroundCheck correction.

        // Receive root motion movement.
        // This handles cases where objects from different hierarchies own the animator.
        private RootMotionReceiver _rootMotionReceiver;

        private Animator _animator;
        private Transform _transform;
        private IGroundContact _groundCheck;
        private IWarp _warp;
        private readonly Dictionary<int, List<AnimatorMoveBehaviour>> _behaviours = new();

        
        
        /// <summary>
        /// Whether to use ground normal for movement.
        /// </summary>
        public bool UseGroundNormal { get; set; } = true;

        /// <summary>
        /// Connect with the animator.
        /// Call this API after replacing the character model.
        /// </summary>
        public void Rebuild()
        {
            // Set up RootMotionReceiver component for child Animators.
            _animator = GetComponentInChildren<Animator>();
            if (_animator.TryGetComponent(out _rootMotionReceiver) == false)
                _rootMotionReceiver = _animator.gameObject.AddComponent<RootMotionReceiver>();
        }

        /// <summary>
        /// Set priorities for movement and direction.
        /// </summary>
        public int Priority
        {
            set
            {
                MovePriority = value;
                TurnPriority = value;
            }
        }

        #region ICharacterMove

        int IPriority<IMove>.Priority => _isWorkComponent ? MovePriority : 0;

        Vector3 IMove.MoveVelocity => _velocity;

        #endregion

        #region ICharacterTurn

        float ITurn.YawAngle => _turn;

        int IPriority<ITurn>.Priority => _isWorkComponent ? TurnPriority : 0;
        int ITurn.TurnSpeed => -1; // Direction is instantly reflected.

        #endregion

        private void Awake()
        {
            // Collect components.
            TryGetComponent(out _transform);
            TryGetComponent(out _warp);
            _hasGroundCheck = TryGetComponent(out _groundCheck);

            Rebuild();
        }

        void IUpdateComponent.OnUpdate(float deltaTime)
        {

            var isInProgress = IsInProgress(_animator.GetCurrentAnimatorStateInfo(0), out var behaviour);
            if (isInProgress == false)
                isInProgress = IsInProgress(_animator.GetNextAnimatorStateInfo(0), out behaviour);

            if (isInProgress == false)
            {
                _isWorkComponent = false;
                ResetBlockedDescent();
                return;
            }

            UseGroundNormal = behaviour.UseGroundNormal;
            _isFixedPosition = behaviour.IsFixedPosition;
            _isWorkComponent = true;
            
            // Root motion of Humanoid avatars is scaled by Animator.humanScale.
            // Optionally cancel it so that the movement distance does not depend on the avatar size.
            var scale = GetHumanScaleFactor(behaviour);
            var rootVelocity = _rootMotionReceiver.Velocity * scale;
            var rootDeltaPosition = _rootMotionReceiver.DeltaPosition * scale;

            // Calculate character movement.
            if (UseGroundNormal)
            {
                // Calculate the vector considering the slope of the ground.
                // Treat as a flat plane if there is no GroundCheck.
                var normal = _hasGroundCheck ? _groundCheck.GroundSurfaceNormal : Vector3.up;
                _velocity = Vector3.ProjectOnPlane(rootVelocity, normal);
                _deltaPosition = Vector3.ProjectOnPlane(rootDeltaPosition, normal);
            }
            else
            {
                // Calculate the vector without considering the slope of the ground.
                _velocity = rootVelocity;
                _deltaPosition = rootDeltaPosition;
            }

            if (_isFixedPosition && _compensateBlockedDescent)
                CompensateBlockedDescent();
            else
                ResetBlockedDescent();

            // Calculate character direction.
            var angle = _transform.rotation * _rootMotionReceiver.Rotation;
            _turn = angle.eulerAngles.y;

            _isWorkComponent = true;
        }

        private bool IsInProgress(in AnimatorStateInfo current, out AnimatorMoveBehaviour result)
        {
            var currentHash = current.fullPathHash;
            if( _behaviours.ContainsKey(currentHash) == false)
                CacheBehaviour(currentHash);

            foreach (var behaviour in _behaviours[currentHash])
            {
                if (behaviour.IsInRange(current))
                {
                    result = behaviour;
                    return true;
                }
            }

            result = null;
            return false;
        }

        void IPriorityLifecycle<IMove>.OnAcquireHighestPriority() { }

        void IPriorityLifecycle<IMove>.OnLoseHighestPriority()  { }
        
        void IPriorityLifecycle<IMove>.OnUpdateWithHighestPriority(float deltaTime)
        {
            // When _isFixedPosition is enabled, character movement is done with Warp, not Control.
            if (_isFixedPosition)
            {
                // Update the position through Warp.
                // Use the delta position directly. Converting it to velocity and multiplying by this frame's deltaTime
                // does not match the animation when the frame time fluctuates.
                _requestedPosition = _transform.position + _deltaPosition;
                _hasRequestedPosition = true;
                _warp.Move(_requestedPosition);
            }
        }

        /// <summary>
        /// If the character stopped above the position requested in the previous frame, accumulate the difference.
        /// While there is an accumulated amount, subtract it from the upward movement.
        /// Applying root motion as deltas loses the movement blocked by collisions, so without this,
        /// a clip whose root goes below the ground on landing makes the character float by that amount when it stands up.
        /// </summary>
        private void CompensateBlockedDescent()
        {
            if (_hasRequestedPosition)
            {
                // Count only near the ground, to exclude position jumps such as warps.
                var blocked = _transform.position.y - _requestedPosition.y;
                var nearGround = _hasGroundCheck == false || _groundCheck.IsOnGround;
                if (blocked > BlockedDescentThreshold && nearGround)
                    _blockedDescent += blocked;
                _hasRequestedPosition = false;
            }

            if (_blockedDescent > 0f && _deltaPosition.y > 0f)
            {
                var consume = Mathf.Min(_deltaPosition.y, _blockedDescent);
                _deltaPosition.y -= consume;
                _blockedDescent -= consume;
            }
        }

        /// <summary>
        /// Factor to multiply the root motion by.
        /// Returns ReferenceHumanScale / Animator.humanScale for Humanoid avatars
        /// when <see cref="AnimatorMoveBehaviour.NormalizeHumanScale"/> is enabled, otherwise 1.
        /// </summary>
        private float GetHumanScaleFactor(AnimatorMoveBehaviour behaviour)
        {
            if (behaviour.NormalizeHumanScale == false || _animator.isHuman == false)
                return 1f;

            var humanScale = _animator.humanScale;
            var reference = behaviour.ReferenceHumanScale;
            return humanScale > 0f && reference > 0f ? reference / humanScale : 1f;
        }

        /// <summary>
        /// Reset the compensation state. Called when leaving the range of <see cref="AnimatorMoveBehaviour"/>.
        /// </summary>
        private void ResetBlockedDescent()
        {
            _blockedDescent = 0f;
            _hasRequestedPosition = false;
        }
        
        private void CacheBehaviour(int hash)
        {
            var behaviours = new List<AnimatorMoveBehaviour>();
            foreach (var behaviour in _animator.GetBehaviours(hash, 0))
            {
                if (behaviour is AnimatorMoveBehaviour moveBehaviour)
                    behaviours.Add(moveBehaviour);
            }
            _behaviours.Add(hash, behaviours);
        }

        int IUpdateComponent.Order => Order.Control;

    }
}
