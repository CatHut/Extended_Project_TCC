using System.Collections.Generic;
using UnityEngine;

namespace Unity.TinyCharacterController.Utility
{
    /// <summary>
    /// Component menu paths for the Physics2D support components.
    /// Same paths as the built-in components (MenuList is internal to the core assembly).
    /// </summary>
    internal static class Physics2DMenuList
    {
        public const string MenuBrain = "TCC/Brain/";
        public const string MenuCheck = "TCC/Check/";
    }

    /// <summary>
    /// Keeps the 2D colliders under the character so that queries can ignore them.
    /// </summary>
    internal sealed class OwnCollider2DCache
    {
        private readonly List<Collider2D> _colliders = new();

        /// <summary>
        /// Collects the 2D colliders under the owner, including inactive ones.
        /// </summary>
        public void Gather(Component owner)
        {
            _colliders.Clear();
            owner.GetComponentsInChildren(true, _colliders);
        }

        public bool Contains(Collider2D collider) => _colliders.Contains(collider);
    }

    /// <summary>
    /// Helpers shared by the Physics2D support components.
    /// </summary>
    internal static class Physics2DQueryUtility
    {
        /// <summary>
        /// Creates a filter that hits non-trigger colliders on the given layers.
        /// </summary>
        public static ContactFilter2D CreateEnvironmentFilter(LayerMask layer)
        {
            var filter = new ContactFilter2D
            {
                useTriggers = false,
            };
            filter.SetLayerMask(layer);
            return filter;
        }

        /// <summary>
        /// Finds the closest hit, ignoring the character's own colliders and hits that started overlapping
        /// (distance 0, which means the query started inside the collider).
        /// </summary>
        public static bool ClosestHit(RaycastHit2D[] hits, int count, float maxDistance,
            OwnCollider2DCache ownColliders, out RaycastHit2D closestHit)
        {
            var min = maxDistance;
            closestHit = default;
            var isHit = false;

            for (var i = 0; i < count; i++)
            {
                var hit = hits[i];
                if (hit.collider == null || hit.distance <= 0 || hit.distance > min ||
                    ownColliders.Contains(hit.collider))
                    continue;

                min = hit.distance;
                closestHit = hit;
                isHit = true;
            }

            return isHit;
        }
    }
}
