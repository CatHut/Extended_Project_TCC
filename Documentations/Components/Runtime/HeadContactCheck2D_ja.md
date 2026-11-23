# HeadContactCheck2D

#### **Namespace**: Unity.TinyCharacterController.Check
---

## 概要:
`HeadContactCheck2D` は、2D のコライダーで作ったステージで頭上のオブジェクトを検出するコンポーネントです。`HeadContactCheck` と同じ判定を `Physics2D`（真上に向けた円のキャスト）で行います。`JumpControl` は `IOverheadDetection` を必須にしているため、`CharacterBrain2D` で `JumpControl` を使う場合はこのコンポーネントが必要です。

## 機能と操作:
- **頭上の検出**: 体の中心から真上に円を飛ばし、`MaxHeight` までの範囲にあるコライダーを検出する。判定の対象は `CharacterSettings.EnvironmentLayer` のレイヤーで、トリガーと自分のコライダーは除く。
- **頭の接触判定**: 検出したコライダーまでの距離が `CharacterSettings.Height` より短ければ、頭が接触しているとみなす。
- **イベントの通知**: 頭が接触したとき（`_onContact`）と、頭上の範囲にオブジェクトが入った・出たとき（`_onChangeInRange`）に Unity イベントを発行する。

## プロパティ
| Name | Description |
|------------------|------|
| `_headPositionOffset` | 頭の位置からのオフセット。 |
| `MaxHeight` | 頭上のオブジェクトを検出する最大の高さ（足元から）。 |
| `_onContact` | 頭が接触したときに呼び出される Unity イベント。 |
| `_onChangeInRange` | 頭上の範囲にオブジェクトが入った・出たときに呼び出される Unity イベント。 |
| `IsHeadContact` | 頭がコライダーに接触しているかどうか。 |
| `IsObjectOverhead` | `MaxHeight` までの範囲にコライダーがあるかどうか。 |
| `IsHitCollisionInThisFrame` | 現在のフレームで頭が接触したかどうか。 |
| `DistanceFromRootPosition` | 足元から頭上のオブジェクトまでの距離。 |
| `ContactPoint` | 頭上のオブジェクトとの接触点。 |
| `ContactedObject` | 頭上のオブジェクト。検出していなければ `null`。 |
| `ContactedCollider2D` | 頭上のオブジェクトの 2D コライダー。検出していなければ `null`。 |

## メソッド
| Name | Function |
|------------------|------|
| ``public void`` GatherOwnColliders() | キャラクター配下の 2D コライダーを集め直す。 |
