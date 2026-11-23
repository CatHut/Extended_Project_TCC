using Unity.TinyCharacterController.Interfaces.Components;
using Unity.TinyCharacterController.Interfaces.Core;
using Unity.TinyCharacterController.Utility;
using UnityEngine;
using UnityEngine.Events;

namespace Unity.TinyCharacterController.Check
{
    /// <summary>
    /// Ground detection for a stage built with 2D colliders.
    ///
    /// Performs the same detection as GroundCheck with Physics2D (a circle cast downward).
    /// The Inspector settings and the values returned through <see cref="IGroundContact"/> and
    /// <see cref="IGroundObject"/> have the same meaning as GroundCheck.
    /// <see cref="IGroundObject.GroundCollider"/> (a 3D Collider) is always null.
    /// Use <see cref="GroundCollider2D"/> to get the 2D collider of the ground.
    /// </summary>
    [RequireComponent(typeof(CharacterSettings))]
    [AddComponentMenu(Physics2DMenuList.MenuCheck + nameof(GroundCheck2D))]
    [DisallowMultipleComponent]
    public class GroundCheck2D : MonoBehaviour,
        IGroundContact,
        IGroundObject,
        IEarlyUpdateComponent
    {
        private const int MaxHits = 5;

        /// <summary>
        /// Maximum distance from the ground at which the character is considered on the ground.
        /// </summary>
        [SerializeField, Range(0, 2f)]
        private float _ambiguousDistance = 0.2f;

        /// <summary>
        /// Maximum distance from the ground at which the character is considered firmly on the ground.
        /// </summary>
        [SerializeField, Range(0, 0.5f)]
        private float _preciseDistance = 0.02f;

        /// <summary>
        /// Maximum slope angle that the character can stand on.
        /// </summary>
        [SerializeField, Range(0, 90)]
        private int _maxSlope = 60;

        [Header("Callbacks")]
        [SerializeField]
        private UnityEvent<GameObject> _onChangeGroundObject;

        private static readonly RaycastHit2D[] Hits = new RaycastHit2D[MaxHits];

        private readonly OwnCollider2DCache _ownColliders = new();
        private ITransform _transform;
        private CharacterSettings _settings;

        /// <summary>
        /// Event invoked when the ground object changes.
        /// </summary>
        public UnityEvent<GameObject> OnChangeGroundObject => _onChangeGroundObject;

        /// <summary>
        /// True if the character is within the ambiguous distance from the ground.
        /// </summary>
        public bool IsOnGround { get; private set; }

        /// <summary>
        /// True if the character is within the precise distance from the ground and the slope is within the limit.
        /// </summary>
        public bool IsFirmlyOnGround { get; private set; }

        /// <summary>
        /// The 2D collider of the ground. Null if no ground is detected.
        /// </summary>
        public Collider2D GroundCollider2D { get; private set; }

        /// <summary>
        /// Always null. This component detects 2D colliders. Use <see cref="GroundCollider2D"/>.
        /// </summary>
        Collider IGroundObject.GroundCollider => null;

        /// <summary>
        /// The normal of the ground surface.
        /// </summary>
        public Vector3 GroundSurfaceNormal { get; private set; } = Vector3.up;

        /// <summary>
        /// The point where the character touches the ground.
        /// </summary>
        public Vector3 GroundContactPoint { get; private set; }

        /// <summary>
        /// Distance from the character's feet to the ground.
        /// </summary>
        public float DistanceFromGround { get; private set; }

        /// <summary>
        /// The ground object. Null if the character is not on the ground.
        /// </summary>
        public GameObject GroundObject { get; private set; }

        /// <summary>
        /// True if the ground object changed in the current frame.
        /// </summary>
        public bool IsChangeGroundObject { get; private set; }

        UnityEvent<GameObject> IGroundObject.OnChangeGroundObject => _onChangeGroundObject;

        int IEarlyUpdateComponent.Order => Order.Check;

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

        /// <summary>
        /// Performs a downward raycast that ignores the colliders attached to the character.
        /// </summary>
        /// <param name="position">Start position.</param>
        /// <param name="distance">Range of the ray.</param>
        /// <param name="hit">The closest hit.</param>
        /// <returns>True if a collider is hit.</returns>
        public bool Raycast(Vector3 position, float distance, out RaycastHit2D hit)
        {
            var filter = Physics2DQueryUtility.CreateEnvironmentFilter(_settings.EnvironmentLayer);
            var count = Physics2D.Raycast(position, Vector2.down, filter, Hits, distance);
            return Physics2DQueryUtility.ClosestHit(Hits, count, distance, _ownColliders, out hit);
        }

        void IEarlyUpdateComponent.OnUpdate(float deltaTime)
        {
            using var _ = new ProfilerScope("Ground Check 2D");

            var preGroundObject = GroundObject;
            var radius = _settings.Radius;
            var offset = radius * 2;
            var position = _transform.Position;
            var origin = new Vector2(position.x, position.y + offset);
            var distance = _ambiguousDistance + offset;

            var filter = Physics2DQueryUtility.CreateEnvironmentFilter(_settings.EnvironmentLayer);
            var count = Physics2D.CircleCast(origin, radius, Vector2.down, filter, Hits, distance);
            var isHit = Physics2DQueryUtility.ClosestHit(Hits, count, distance, _ownColliders, out var hit);

            if (isHit)
            {
                var normal = new Vector3(hit.normal.x, hit.normal.y, 0);
                var inLimitAngle = Vector3.Angle(Vector3.up, normal) < _maxSlope;

                // The circle starts at (offset) above the feet, so the feet are (offset - radius) below its bottom.
                DistanceFromGround = hit.distance - (offset - radius);
                IsOnGround = DistanceFromGround < _ambiguousDistance;
                IsFirmlyOnGround = DistanceFromGround <= _preciseDistance && inLimitAngle;
                GroundContactPoint = new Vector3(hit.point.x, hit.point.y, position.z);
                GroundSurfaceNormal = normal;
                GroundCollider2D = hit.collider;
                GroundObject = IsOnGround ? hit.collider.gameObject : null;
            }
            else
            {
                DistanceFromGround = _ambiguousDistance;
                IsOnGround = false;
                IsFirmlyOnGround = false;
                GroundContactPoint = Vector3.zero;
                GroundSurfaceNormal = Vector3.zero;
                GroundCollider2D = null;
                GroundObject = null;
            }

            IsChangeGroundObject = preGroundObject != GroundObject;
            if (IsChangeGroundObject)
                _onChangeGroundObject?.Invoke(GroundObject);
        }

        private void OnDrawGizmosSelected()
        {
            if (_settings == null)
                TryGetComponent(out _settings);
            if (_settings == null)
                return;

            var position = transform.position;
            var radius = _settings.Radius;

            if (Application.isPlaying)
            {
                Gizmos.color = IsOnGround ? Color.red : Gizmos.color;
                Gizmos.color = IsFirmlyOnGround ? Color.blue : Gizmos.color;
            }

            var bottom = IsOnGround && Application.isPlaying
                ? radius - DistanceFromGround
                : radius - _ambiguousDistance;

            Gizmos.DrawWireSphere(position + new Vector3(0, radius - _preciseDistance, 0), radius);
            Gizmos.DrawWireSphere(position + new Vector3(0, bottom, 0), radius);
        }
    }
}
