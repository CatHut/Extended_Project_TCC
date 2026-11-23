using Unity.TinyCharacterController.Core;
using Unity.TinyCharacterController.Interfaces.Components;
using Unity.TinyCharacterController.Utility;
using UnityEngine;

namespace Unity.TinyCharacterController.Brain
{
    /// <summary>
    /// A brain that moves the character on a stage built with 2D colliders
    /// (BoxCollider2D, TilemapCollider2D and so on).
    ///
    /// The character moves on the XY plane (Y is up). Physics simulation is not used:
    /// the Rigidbody2D is Kinematic, and the movement is resolved with Physics2D queries.
    /// When the capsule hits a collider, the character stops in front of it and slides along the surface.
    /// The height and width of the capsule are determined by
    /// <see cref="CharacterSettings.Height"/> and <see cref="CharacterSettings.Radius"/>.
    ///
    /// Use the 2D checks (GroundCheck2D, HeadContactCheck2D) with this brain.
    /// The 3D checks do not detect 2D colliders.
    /// </summary>
    [AddComponentMenu(Physics2DMenuList.MenuBrain + nameof(CharacterBrain2D))]
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Rigidbody2D))]
    [RequireComponent(typeof(CapsuleCollider2D))]
    [RequireComponent(typeof(CharacterSettings))]
    [DefaultExecutionOrder(Order.UpdateBrain)]
    public class CharacterBrain2D : BrainBase, ICharacterSettingUpdateReceiver
    {
        /// <summary>
        /// How the character's direction is applied to the Transform.
        /// </summary>
        public enum TurnMode
        {
            /// <summary>
            /// Face right (rotation 0) or left (rotation 180 around Y) immediately,
            /// depending on the direction that the turn control requests.
            /// </summary>
            SnapLeftRight,

            /// <summary>Apply the rotation as is (same as CharacterBrain).</summary>
            Free,
        }

        private const float MinMoveDistance = 0.0001f;
        private const float FacingThreshold = 0.01f;
        private const int MaxHits = 8;

        /// <summary>
        /// How the character's direction is applied to the Transform.
        /// </summary>
        [SerializeField]
        private TurnMode _turnMode = TurnMode.SnapLeftRight;

        /// <summary>
        /// Gap kept between the capsule and the colliders.
        /// </summary>
        [SerializeField, Range(0.001f, 0.05f)]
        private float _skinWidth = 0.01f;

        /// <summary>
        /// Maximum number of times the movement slides along surfaces in one update.
        /// </summary>
        [SerializeField, Range(1, 5)]
        private int _maxIterations = 3;

        private static readonly RaycastHit2D[] Hits = new RaycastHit2D[MaxHits];

        private readonly OwnCollider2DCache _ownColliders = new();
        private Rigidbody2D _rigidbody;
        private CapsuleCollider2D _capsule;
        private EarlyUpdateBrainBase _earlyUpdate;
        private IGroundContact _groundCheck;
        private bool _hasGroundCheck;

        /// <summary>
        /// How the character's direction is applied to the Transform.
        /// </summary>
        public TurnMode Turn
        {
            get => _turnMode;
            set => _turnMode = value;
        }

        /// <summary>
        /// True if the character faces right. Updated only in <see cref="TurnMode.SnapLeftRight"/>.
        /// </summary>
        public bool IsFacingRight { get; private set; } = true;

        /// <summary>
        /// True if the movement hit a collider in the last update.
        /// </summary>
        public bool IsHitInThisFrame { get; private set; }

        /// <summary>
        /// The normal of the last collider that the movement hit in the last update.
        /// Vector2.zero if nothing was hit.
        /// </summary>
        public Vector2 LastHitNormal { get; private set; }

        /// <summary>
        /// Update timing. CharacterBrain2D updates during the Update phase.
        /// </summary>
        public override UpdateTiming Timing => UpdateTiming.Update;

        private void Reset()
        {
            var settings = GetComponent<CharacterSettings>();
            ((ICharacterSettingUpdateReceiver)this).OnUpdateSettings(settings);
            SetupRigidbody(GetComponent<Rigidbody2D>());
        }

        private void Awake()
        {
            Initialize();
            GatherComponents();

            SetupRigidbody(_rigidbody);
            ((ICharacterSettingUpdateReceiver)this).OnUpdateSettings(Settings);
            IsFacingRight = Vector3.Dot(CachedTransform.right, Vector3.right) >= 0;
        }

        private void GatherComponents()
        {
            TryGetComponent(out _rigidbody);
            TryGetComponent(out _capsule);
            _hasGroundCheck = TryGetComponent(out _groundCheck);
            _ownColliders.Gather(this);

            _earlyUpdate = gameObject.AddComponent<EarlyUpdateBrain>();

            // Disable the component at the start to prevent it from running immediately.
            _earlyUpdate.enabled = false;
        }

        private void OnEnable()
        {
            _earlyUpdate.enabled = true;
        }

        private void OnDisable()
        {
            _earlyUpdate.enabled = false;
        }

        private void Update()
        {
            UpdateBrain();
        }

        /// <summary>
        /// Collects the 2D colliders under the character again.
        /// Call this after adding or removing colliders under the character at runtime.
        /// </summary>
        public void GatherOwnColliders()
        {
            _ownColliders.Gather(this);
        }

        private static void SetupRigidbody(Rigidbody2D body)
        {
            if (body == null)
                return;

            // Physics simulation is not used. The brain moves the character.
            body.bodyType = RigidbodyType2D.Kinematic;
            body.gravityScale = 0;
            body.interpolation = RigidbodyInterpolation2D.None;
        }

        /// <summary>
        /// Update the character's position.
        /// </summary>
        /// <param name="total">The final velocity.</param>
        /// <param name="deltaTime">Delta time.</param>
        protected override void ApplyPosition(in Vector3 total, float deltaTime)
        {
            var delta = new Vector2(total.x, total.y) * deltaTime;

            // If GroundCheck is present, correct the position to fit the ground.
            if (_hasGroundCheck && _groundCheck.IsFirmlyOnGround && total.y <= 0)
                delta.y -= _groundCheck.DistanceFromGround;

            var position = CachedTransform.position;
            var moved = MoveWithCollision(position, delta);
            CachedTransform.position = new Vector3(moved.x, moved.y, position.z);
        }

        /// <summary>
        /// Apply the character's rotation.
        /// </summary>
        /// <param name="rotation">The final rotation.</param>
        protected override void ApplyRotation(in Quaternion rotation)
        {
            if (_turnMode == TurnMode.Free)
            {
                CachedTransform.rotation = rotation;
                return;
            }

            // Use the requested direction, not the interpolated one, so that the character turns immediately.
            UpdateFacing(YawAngle);
            CachedTransform.rotation = FacingRotation;
        }

        protected override void SetPositionDirectly(in Vector3 newPosition)
        {
            CachedTransform.position = newPosition;
        }

        protected override void SetRotationDirectly(in Quaternion newRotation)
        {
            if (_turnMode == TurnMode.Free)
            {
                CachedTransform.rotation = newRotation;
                return;
            }

            UpdateFacing(newRotation.eulerAngles.y);
            CachedTransform.rotation = FacingRotation;
        }

        protected override void MovePosition(in Vector3 newPosition)
        {
            var position = CachedTransform.position;
            var delta = new Vector2(newPosition.x - position.x, newPosition.y - position.y);
            var moved = MoveWithCollision(position, delta);
            CachedTransform.position = new Vector3(moved.x, moved.y, position.z);
        }

        protected override void GetPositionAndRotation(out Vector3 position, out Quaternion rotation)
        {
            CachedTransform.GetPositionAndRotation(out position, out rotation);
        }

        void ICharacterSettingUpdateReceiver.OnUpdateSettings(CharacterSettings settings)
        {
            if (_capsule == null)
                TryGetComponent(out _capsule);
            if (_capsule == null || settings == null)
                return;

            // The pivot is at the feet, same as CharacterBrain.
            var width = settings.Radius * 2;
            _capsule.direction = CapsuleDirection2D.Vertical;
            _capsule.size = new Vector2(width, Mathf.Max(settings.Height, width));
            _capsule.offset = new Vector2(0, settings.Height * 0.5f);
        }

        private Quaternion FacingRotation => IsFacingRight ? Quaternion.identity : Quaternion.Euler(0, 180, 0);

        private void UpdateFacing(float yawAngle)
        {
            // In TCC, yaw 90 faces +X (right) and yaw -90 faces -X (left).
            // Keep the current direction when the yaw faces the depth direction (0 or 180).
            var x = Mathf.Sin(yawAngle * Mathf.Deg2Rad);
            if (x > FacingThreshold)
                IsFacingRight = true;
            else if (x < -FacingThreshold)
                IsFacingRight = false;
        }

        /// <summary>
        /// Moves the capsule by delta. When it hits a collider, it stops in front of it
        /// and slides along the surface with the remaining movement.
        /// </summary>
        private Vector2 MoveWithCollision(Vector2 position, Vector2 delta)
        {
            IsHitInThisFrame = false;
            LastHitNormal = Vector2.zero;

            for (var i = 0; i < _maxIterations; i++)
            {
                var distance = delta.magnitude;
                if (distance < MinMoveDistance)
                    break;

                var direction = delta / distance;
                if (CastCapsule(position, direction, distance + _skinWidth, out var hit) == false)
                {
                    position += delta;
                    break;
                }

                IsHitInThisFrame = true;
                LastHitNormal = hit.normal;

                // Stop in front of the collider, keeping the skin width.
                var travel = Mathf.Max(0, hit.distance - _skinWidth);
                position += direction * travel;

                // Keep only the movement along the surface.
                var remaining = delta - direction * travel;
                var intoSurface = Vector2.Dot(remaining, hit.normal);
                if (intoSurface < 0)
                    remaining -= hit.normal * intoSurface;

                delta = remaining;
            }

            return position;
        }

        private bool CastCapsule(Vector2 position, Vector2 direction, float distance, out RaycastHit2D closestHit)
        {
            // Calculate the capsule from the current position so that the query does not depend on
            // the synchronization of the Rigidbody2D position.
            var scale = CachedTransform.lossyScale;
            var center = position + new Vector2(_capsule.offset.x * scale.x, _capsule.offset.y * scale.y);
            var size = new Vector2(_capsule.size.x * Mathf.Abs(scale.x), _capsule.size.y * Mathf.Abs(scale.y));
            var filter = Physics2DQueryUtility.CreateEnvironmentFilter(Settings.EnvironmentLayer);

            var count = Physics2D.CapsuleCast(center, size, CapsuleDirection2D.Vertical, 0,
                direction, filter, Hits, distance);

            var min = distance;
            closestHit = default;
            var isHit = false;

            for (var i = 0; i < count; i++)
            {
                var hit = Hits[i];

                // Ignore the character's own colliders, colliders that the capsule already overlaps
                // (distance 0, so that the character can move out of them),
                // and surfaces that do not face the movement.
                if (hit.collider == null || _ownColliders.Contains(hit.collider) ||
                    hit.distance <= 0 || hit.distance > min ||
                    Vector2.Dot(hit.normal, direction) >= 0)
                    continue;

                min = hit.distance;
                closestHit = hit;
                isHit = true;
            }

            return isHit;
        }
    }
}
