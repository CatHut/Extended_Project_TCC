using System;
using System.Collections;
using System.Collections.Generic;
using Unity.TinyCharacterController.Attributes;
using UnityEngine;
using Unity.TinyCharacterController;
using Unity.TinyCharacterController.Control;

namespace Unity.TinyCharacterController.Smb
{
    /// <summary>
    /// Component to control AnimatorMoveControl from the StateMachine side.
    /// </summary>
    public class AnimatorMoveBehaviour : StateMachineBehaviour
    {
        /// <summary>
        /// Setting the duration during which AnimatorMoveControl's priority is active.
        /// </summary>
        [SerializeField, HideInInspector, Range(0, 1)] [Tooltip("Duration to raise the priority of AnimationMove")]
        private float _start = 0, _end = 1;

        [HideInInspector, SerializeField] private bool _copyed = false;

        [SerializeField, MinMax(0, 1)] private Vector2 _range;

        private void OnValidate()
        {
            if (_copyed == false)
            {
                _range.x = _start;
                _range.y = _end;
                _copyed = true;
            }
        }

        /// <summary>
        /// Determines whether to consider ground detection when moving the character.
        /// </summary>
        [SerializeField] [Tooltip("Determines whether to use ground detection when moving the character with AnimationMove. For example, set to False when moving up and down like on a ladder.")]
        private bool _useGroundNormal = true;

        public bool UseGroundNormal => _useGroundNormal;

        /// <summary>
        /// Process character movement as a Warp instead of using Control.
        /// </summary>
        [SerializeField] 
        private bool _isFixedPosition = false;

        public bool IsFixedPosition => _isFixedPosition;

        /// <summary>
        /// For Humanoid avatars, scale the root motion movement by <see cref="ReferenceHumanScale"/> / Animator.humanScale.
        /// When enabled, avatars of different sizes move the same distance as the avatar used to create the motion.
        /// When disabled, the movement follows the avatar size (default Unity behavior).
        /// </summary>
        [SerializeField]
        [Tooltip("For Humanoid avatars, unify the movement distance regardless of the avatar size. When disabled, the movement follows the avatar size (default Unity behavior).")]
        private bool _normalizeHumanScale = false;

        /// <summary>
        /// The humanScale of the model used to create the motion.
        /// </summary>
        [SerializeField, Min(0.0001f)]
        [Tooltip("The humanScale of the model used to create this motion. All avatars move the same distance as this model.")]
        private float _referenceHumanScale = 1f;

        public bool NormalizeHumanScale => _normalizeHumanScale;

        public float ReferenceHumanScale => _referenceHumanScale;

        public bool IsInRange(AnimatorStateInfo info)
        {
            var normalizedTime = info.loop ? info.normalizedTime % 1 : info.normalizedTime;

            return _range.x < normalizedTime && _range.y > normalizedTime;
        }
    }
}
