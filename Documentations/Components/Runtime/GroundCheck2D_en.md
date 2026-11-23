# GroundCheck2D

#### **Namespace**: Unity.TinyCharacterController.Check
---

## Summary:
`GroundCheck2D` is a component that detects the ground on a stage built with 2D colliders. It performs the same detection as `GroundCheck` with `Physics2D` (a circle cast downward). The Inspector settings and the values returned through `IGroundContact` and `IGroundObject` have the same meaning as `GroundCheck`. The calculations of this component are performed at the timing of `OnUpdate`. Use it with `CharacterBrain2D`.

## Features and Operation:
- **Ground Detection**: Detects contact with 2D colliders and determines whether the character is on the ground. The targets are the layers in `CharacterSettings.EnvironmentLayer`, excluding triggers and the character's own colliders.
- **Ground Contact Information**: Provides the distance to the ground, the surface normal, the contact point and the ground object.
- **Ground Object Change Notification**: Invokes a Unity event when the ground object changes. It can be used with `MoveWithPlatform`.

## Notes:
- `IGroundObject.GroundCollider` (a 3D `Collider`) always returns `null`. Use `GroundCollider2D` to get the 2D collider.

## Properties
| Name | Description |
|------------------|------|
| `_ambiguousDistance` | Maximum distance at which the character is considered on the ground (ambiguous ground detection). |
| `_preciseDistance` | Distance at which the character is strictly considered on the ground. |
| `_maxSlope` | Maximum slope angle recognized as ground. |
| `_onChangeGroundObject` | Unity event invoked when the ground object changes. |
| `IsOnGround` | Indicates whether the character is on the ground. |
| `IsFirmlyOnGround` | Indicates whether the character is strictly on the ground. |
| `GroundCollider2D` | The current 2D collider of the ground. |
| `GroundSurfaceNormal` | The normal of the ground surface. |
| `GroundContactPoint` | The point of contact with the ground. |
| `DistanceFromGround` | The distance to the ground. |
| `GroundObject` | The ground object. |
| `IsChangeGroundObject` | Whether the ground object changed in the current frame. |
| `OnChangeGroundObject` | Unity event invoked when the ground object changes. |

## Methods
| Name | Function |
|------------------|------|
| ``public bool`` Raycast( ``Vector3 position, float distance, out RaycastHit2D hit`` ) | Performs a downward 2D raycast that ignores the character's colliders. |
| ``public void`` GatherOwnColliders() | Collects the 2D colliders under the character again. |
