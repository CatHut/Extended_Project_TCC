using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Unity.TinyCharacterController.Modifier
{
    [AddComponentMenu("")]
    [DisallowMultipleComponent]
    public class RootMotionReceiver : MonoBehaviour
    {
        public Vector3 Velocity { get; private set; }
        public Quaternion Rotation { get; private set; }

        /// <summary>
        /// Root motion movement of the last evaluated frame.
        /// Use this instead of <see cref="Velocity"/> * deltaTime when the movement must follow the animation exactly.
        /// </summary>
        public Vector3 DeltaPosition { get; private set; }

        private Animator _animator;

        void Awake()
        {
            TryGetComponent(out _animator);
        }

        void OnAnimatorMove()
        {
            Velocity = _animator.velocity;
            Rotation = _animator.deltaRotation;
            DeltaPosition = _animator.deltaPosition;
        }
    }
}
