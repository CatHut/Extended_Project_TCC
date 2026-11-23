using System.Collections.Generic;
using Unity.TinyCharacterController.Interfaces.Components;
using Unity.TinyCharacterController.Interfaces.Core;
using Unity.TinyCharacterController.Interfaces.Utility;
using Unity.TinyCharacterController.Utility;
using UnityEngine;
using UnityEngine.Events;

namespace Unity.TinyCharacterController.Check
{
    /// <summary>
    /// Ceiling detection for a stage built with 2D colliders.
    ///
    /// Performs the same detection as HeadContactCheck with Physics2D (a circle cast upward).
    /// JumpControl requires <see cref="IOverheadDetection"/>, so add this component when using JumpControl
    /// with CharacterBrain2D.
    /// </summary>
    [RequireComponent(typeof(CharacterSettings))]
    [AddComponentMenu(Physics2DMenuList.MenuCheck + nameof(HeadContactCheck2D))]
    [DisallowMultipleComponent]
    public class HeadContactCheck2D : MonoBehaviour,
        IOverheadDetection,
        IEarlyUpdateComponent,
        IComponentCondition
    {
        private const int MaxHits = 5;

        /// <summary>
        /// Offset from the head position.
        /// </summary>
        [Header("Settings")]
        [SerializeField, Range(0, 0.5f)]
        private float _headPositionOffset = 0.1f;

        /// <summary>
        /// Maximum distance from the feet at which objects above can be detected.
        /// </summary>
        [SerializeField, Range(0, 10f)]
        public float MaxHeight = 2.5f;

        [Header("Callbacks")]
        [SerializeField]
        private UnityEvent _onContact;

        [SerializeField]
        private UnityEvent _onChangeInRange;

        private static readonly RaycastHit2D[] Hits = new RaycastHit2D[MaxHits];

        private readonly OwnCollider2DCache _ownColliders = new();
        private CharacterSettings _settings;
        private ITransform _transform;

        /// <summary>
        /// True if the head hit a collider in the current frame.
        /// </summary>
        public bool IsHitCollisionInThisFrame { get; private set; }

        /// <summary>
        /// Distance from the feet to the detected object above.
        /// </summary>
        public float DistanceFromRootPosition { get; private set; }

        /// <summary>
        /// True if the head is in contact with a collider.
        /// </summary>
        public bool IsHeadContact { get; private set; }

        /// <summary>
        /// True if a collider exists above the head within <see cref="MaxHeight"/>.
        /// </summary>
        public bool IsObjectOverhead { get; private set; }

        /// <summary>
        /// The point of the detected collider above the head.
        /// </summary>
        public Vector3 ContactPoint { get; private set; }

        /// <summary>
        /// The detected object above the head. Null if nothing is detected.
        /// </summary>
        public GameObject ContactedObject { get; private set; }

        /// <summary>
        /// The 2D collider of the detected object above the head. Null if nothing is detected.
        /// </summary>
        public Collider2D ContactedCollider2D { get; private set; }

        int IEarlyUpdateComponent.Order => Order.Check;

        private float Height => _settings.Height + _headPositionOffset;

        private void Awake()
        {
            TryGetComponent(out _transform);
            TryGetComponent(out _settings);
            _ownColliders.Gather(this);
        }

        /// <summary>
        /// Collects the 2D colliders under the character again.
        /// Call this after adding or removing colliders under the character at runtime.
        /// </summary>
        public void GatherOwnColliders()
        {
            _ownColliders.Gather(this);
        }

        void IEarlyUpdateComponent.OnUpdate(float deltaTime)
        {
            using var profile = new ProfilerScope(nameof(HeadContactCheck2D));

            var preInRange = IsObjectOverhead;

            // Cast from the center of the body to avoid contact with the ground.
            var offset = _settings.Height * 0.5f;
            IsObjectOverhead = DetectCollidersAboveHead(offset, out var closestHit);

            if (IsObjectOverhead)
            {
                SetPropertiesWhenInRange(closestHit, offset);
                if (IsHitCollisionInThisFrame)
                    _onContact?.Invoke();
            }
            else
            {
                SetPropertiesWhenOutOfRange();
            }

            if (preInRange != IsObjectOverhead)
                _onChangeInRange?.Invoke();
        }

        void IComponentCondition.OnConditionCheck(List<string> messageList)
        {
            if (_settings == null)
                TryGetComponent(out _settings);

            if (_settings != null && _settings.Height > MaxHeight)
                messageList.Add("Max Height should be set to a value greater than or equal to _settings.Height.");
        }

        private void SetPropertiesWhenOutOfRange()
        {
            var distance = MaxHeight + _headPositionOffset;
            ContactPoint = new Vector3(0, distance, 0);
            DistanceFromRootPosition = distance;
            IsHeadContact = false;
            ContactedObject = null;
            ContactedCollider2D = null;
            IsHitCollisionInThisFrame = false;
        }

        private void SetPropertiesWhenInRange(in RaycastHit2D closestHit, float offset)
        {
            var preContactHead = IsHeadContact;

            // The sum of the cast distance, the start offset of the cast, and the radius of the circle.
            DistanceFromRootPosition = closestHit.distance + offset + _settings.Radius + _headPositionOffset;
            ContactPoint = new Vector3(closestHit.point.x, closestHit.point.y, _transform.Position.z);
            ContactedObject = closestHit.collider.gameObject;
            ContactedCollider2D = closestHit.collider;

            IsHeadContact = DistanceFromRootPosition < Height;
            IsHitCollisionInThisFrame = !preContactHead && IsHeadContact;
        }

        private bool DetectCollidersAboveHead(float offset, out RaycastHit2D closestHit)
        {
            var position = _transform.Position;
            var origin = new Vector2(position.x, position.y + offset + _headPositionOffset);
            var distance = MaxHeight + _headPositionOffset - offset - _settings.Radius;
            if (distance <= 0)
            {
                closestHit = default;
                return false;
            }

            var filter = Physics2DQueryUtility.CreateEnvironmentFilter(_settings.EnvironmentLayer);
            var count = Physics2D.CircleCast(origin, _settings.Radius, Vector2.up, filter, Hits, distance);
            return Physics2DQueryUtility.ClosestHit(Hits, count, distance, _ownColliders, out closestHit);
        }

        private void OnDrawGizmosSelected()
        {
            if (_settings == null)
                TryGetComponent(out _settings);
            if (_settings == null)
                return;

            if (Application.isPlaying && IsHeadContact)
                Gizmos.color = Color.red;

            var position = transform.position;
            var radius = _settings.Radius;
            var headPosition = position + new Vector3(0, Height - radius, 0);
            var top = Application.isPlaying
                ? position + new Vector3(0, DistanceFromRootPosition - radius, 0)
                : position + new Vector3(0, MaxHeight, 0);

            Gizmos.DrawWireSphere(headPosition, radius);
            Gizmos.DrawWireSphere(top, radius);
            Gizmos.DrawLine(headPosition + new Vector3(radius, 0, 0), top + new Vector3(radius, 0, 0));
            Gizmos.DrawLine(headPosition - new Vector3(radius, 0, 0), top - new Vector3(radius, 0, 0));
        }
    }
}
