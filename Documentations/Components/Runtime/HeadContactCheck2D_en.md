# HeadContactCheck2D

#### **Namespace**: Unity.TinyCharacterController.Check
---

## Summary:
`HeadContactCheck2D` is a component that detects objects above the head on a stage built with 2D colliders. It performs the same detection as `HeadContactCheck` with `Physics2D` (a circle cast upward). `JumpControl` requires `IOverheadDetection`, so this component is required when using `JumpControl` with `CharacterBrain2D`.

## Features and Operation:
- **Overhead Detection**: Casts a circle upward from the center of the body and detects colliders within `MaxHeight`. The targets are the layers in `CharacterSettings.EnvironmentLayer`, excluding triggers and the character's own colliders.
- **Head Contact**: Considers the head in contact when the distance to the detected collider is shorter than `CharacterSettings.Height`.
- **Event Notification**: Invokes Unity events when the head makes contact (`_onContact`) and when an object enters or leaves the range above the head (`_onChangeInRange`).

## Properties
| Name | Description |
|------------------|------|
| `_headPositionOffset` | Offset from the head position. |
| `MaxHeight` | Maximum height (from the feet) at which objects above can be detected. |
| `_onContact` | Unity event invoked when the head makes contact. |
| `_onChangeInRange` | Unity event invoked when an object enters or leaves the range above the head. |
| `IsHeadContact` | Whether the head is in contact with a collider. |
| `IsObjectOverhead` | Whether a collider exists within `MaxHeight`. |
| `IsHitCollisionInThisFrame` | Whether the head made contact in the current frame. |
| `DistanceFromRootPosition` | Distance from the feet to the object above. |
| `ContactPoint` | The point of contact with the object above. |
| `ContactedObject` | The object above. `null` if nothing is detected. |
| `ContactedCollider2D` | The 2D collider of the object above. `null` if nothing is detected. |

## Methods
| Name | Function |
|------------------|------|
| ``public void`` GatherOwnColliders() | Collects the 2D colliders under the character again. |
