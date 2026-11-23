# CharacterBrain2D

#### **Namespace**: Unity.TinyCharacterController.Brain
---

## 概要:
`CharacterBrain2D` は、2D のコライダー（`BoxCollider2D`・`TilemapCollider2D` など）で作ったステージの上でキャラクターを動かす Brain です。キャラクターは XY 平面を動きます（Y が上）。物理シミュレーションは使わず、Kinematic の `Rigidbody2D` と `CapsuleCollider2D` を使い、`Physics2D` の判定でコライダーに当たったら止まり、面に沿って滑らせます。カプセルの高さと幅は `CharacterSettings.Height` と `CharacterSettings.Radius` で決まります。

`GroundCheck2D`・`HeadContactCheck2D` と組み合わせて使います。3D 用の Check（`GroundCheck` など）は 2D のコライダーを検出しません。`JumpControl` を使う場合は `HeadContactCheck2D` が必要です。

## 機能と操作:
- **2D の当たり判定**: カプセルを移動方向に飛ばし、コライダーの手前で止める。残りの移動は面に沿った成分だけにして、最大 `_maxIterations` 回まで繰り返す。判定の対象は `CharacterSettings.EnvironmentLayer` のレイヤーで、トリガーと自分のコライダーは除く。
- **地面への吸い付け**: `GroundCheck2D` がしっかり接地していて上に向かって動いていないとき、地面までの距離だけ下げる（`CharacterBrain` と同じ）。
- **向きの反映**: `TurnMode.SnapLeftRight`（既定）では、要求された向きに応じて右向き（回転 0）か左向き（Y 軸まわりに 180°）にすぐ切り替える。`TurnMode.Free` では回転をそのまま反映する。
- **Z 座標の固定**: Z 座標は変えない。
- **Rigidbody2D の設定**: `Awake` で `Rigidbody2D` を Kinematic・重力 0 にする。トリガーとの接触の通知（`OnTriggerEnter2D`）を受けるために使う。

## 注意:
- `TurnMode.SnapLeftRight` では、キャラクターの前は Transform のローカルの +X（右向きのとき）になる。`transform.forward` は奥行き方向を向いたままなので、`transform.forward` を前として使う処理（`MoveControl.DeltaDirectionAngle` など）は値が合わない。
- ほかのキャラクターや Rigidbody を押す処理（`CharacterBrain.Pushable`）はない。
- ほかのコライダーが動いてきてめり込んだときに押し出す処理はない。

## プロパティ
| Name | Description |
|------------------|------|
| `_turnMode` | 向きの反映方法。`SnapLeftRight`（既定）または `Free`。 |
| `_skinWidth` | カプセルとコライダーの間に空けるすき間。 |
| `_maxIterations` | 1 回の更新で面に沿って滑らせる最大回数。 |
| `Turn` | 向きの反映方法（`_turnMode`）を取得・設定する。 |
| `IsFacingRight` | 右を向いているかどうか。`SnapLeftRight` のときだけ更新される。 |
| `IsHitInThisFrame` | 直前の更新で移動がコライダーに当たったかどうか。 |
| `LastHitNormal` | 直前の更新で最後に当たったコライダーの面の向き。当たっていなければ `Vector2.zero`。 |

## メソッド
| Name | Function |
|------------------|------|
| ``public void`` GatherOwnColliders() | キャラクター配下の 2D コライダーを集め直す。実行中にキャラクター配下のコライダーを追加・削除した後に呼ぶ。 |
