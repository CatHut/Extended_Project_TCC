# CharacterBrain2D

#### **Namespace**: Unity.TinyCharacterController.Brain
---

## Summary:
`CharacterBrain2D` is a Brain that moves the character on a stage built with 2D colliders (`BoxCollider2D`, `TilemapCollider2D` and so on). The character moves on the XY plane (Y is up). Physics simulation is not used: it uses a Kinematic `Rigidbody2D` and a `CapsuleCollider2D`, and with `Physics2D` queries it stops in front of colliders and slides along their surfaces. The height and width of the capsule are determined by `CharacterSettings.Height` and `CharacterSettings.Radius`.

Use it with `GroundCheck2D` and `HeadContactCheck2D`. The 3D checks (`GroundCheck` and so on) do not detect 2D colliders. `HeadContactCheck2D` is required when using `JumpControl`.

## Features and Operation:
- **2D Collision**: Casts the capsule in the direction of the movement and stops in front of the collider. The remaining movement keeps only the component along the surface, repeated up to `_maxIterations` times. The targets are the layers in `CharacterSettings.EnvironmentLayer`, excluding triggers and the character's own colliders.
- **Ground Snapping**: When `GroundCheck2D` is firmly on the ground and the character is not moving up, moves the character down by the distance to the ground (same as `CharacterBrain`).
- **Direction**: With `TurnMode.SnapLeftRight` (default), faces right (rotation 0) or left (180 degrees around Y) immediately, depending on the requested direction. With `TurnMode.Free`, applies the rotation as is.
- **Fixed Z**: The Z coordinate does not change.
- **Rigidbody2D Settings**: Sets the `Rigidbody2D` to Kinematic with gravity 0 in `Awake`. It is used to receive contact notifications with triggers (`OnTriggerEnter2D`).

## Notes:
- With `TurnMode.SnapLeftRight`, the front of the character is the local +X of the Transform (when facing right). `transform.forward` keeps facing the depth direction, so processing that uses `transform.forward` as the front (such as `MoveControl.DeltaDirectionAngle`) does not get the expected values.
- There is no pushing of other characters or Rigidbodies (`CharacterBrain.Pushable`).
- There is no depenetration when another collider moves into the character.

## Properties
| Name | Description |
|------------------|------|
| `_turnMode` | How the direction is applied. `SnapLeftRight` (default) or `Free`. |
| `_skinWidth` | Gap kept between the capsule and the colliders. |
| `_maxIterations` | Maximum number of times the movement slides along surfaces in one update. |
| `Turn` | Gets or sets how the direction is applied (`_turnMode`). |
| `IsFacingRight` | Whether the character faces right. Updated only with `SnapLeftRight`. |
| `IsHitInThisFrame` | Whether the movement hit a collider in the last update. |
| `LastHitNormal` | The surface normal of the last collider hit in the last update. `Vector2.zero` if nothing was hit. |

## Methods
| Name | Function |
|------------------|------|
| ``public void`` GatherOwnColliders() | Collects the 2D colliders under the character again. Call it after adding or removing colliders under the character at runtime. |
