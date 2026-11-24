# Changelog

この CHANGELOG は、このフォーク（Extended_Project_TCC）で加えた変更だけを記録する。0.7.0 までの変更内容は記録していない。

## [0.7.3] - 2026-11-24

### Fixed

- `CharacterBrain2D`: 壁などのコライダーにわずか（約 0.01 以上）でもめり込むと、そのコライダーを通り抜けていた。移動の前に、重なっている環境のコライダーから押し出すようにした。動く Kinematic のコライダーにめり込まれたときも押し出される
- `CharacterBrain2D`: 地面に立っているときに、足元が地面の上面よりわずかに下になっていた。押し出しにより、すき間（`_skinWidth`）の分だけ上になる

## [0.7.2] - 2026-11-23

### Added

- 2D 物理（`Collider2D`）で作ったステージで動くキャラクターの部品を、アセンブリ `TinyCharacterController.Physics2D`（`Runtime/Extensions/Physics2DSupport/`）に追加した。物理シミュレーションは使わない
  - `CharacterBrain2D`（`Unity.TinyCharacterController.Brain`）: Kinematic の `Rigidbody2D` と `CapsuleCollider2D` を使い、`Physics2D` の判定で、コライダーの手前で止まり面に沿って滑る移動を行う Brain。XY 平面を動き、Z 座標は変えない。向きは既定で左右の 2 方向にすぐ切り替える（`TurnMode.SnapLeftRight`）
  - `GroundCheck2D`（`Unity.TinyCharacterController.Check`）: `GroundCheck` と同じ地面の判定を 2D で行う。`IGroundObject.GroundCollider` は常に `null` で、2D のコライダーは `GroundCollider2D` で返す
  - `HeadContactCheck2D`（`Unity.TinyCharacterController.Check`）: `HeadContactCheck` と同じ頭上の判定を 2D で行う。`JumpControl` を使う場合に必要
- `Documentations/` に上の3つのコンポーネントの文書を追加した

### 使う側のプロジェクトでの注意

- `TinyCharacterController.Physics2D` アセンブリは、2D 物理のモジュール（`com.unity.modules.physics2d`）が有効なときだけコンパイルされる。`SUPPORT_TCC` は不要
- 2D のキャラクターには 2D 版の Brain と Check を付ける。3D 版の Check（`GroundCheck` など）は 2D のコライダーを検出しない

## [0.7.1] - 2026-11-23

### Added

- `MoveInputRelay`（`Unity.TinyCharacterController.Utility`）: 移動入力を `MoveControl` に渡す中継コンポーネント。入力の取得元を問わず `SetInput(Vector2)` で入力を受け取り、軸の制限と閾値を適用して `MoveControl.Move()` に渡す。無効にすると `MoveControl` にゼロを渡す。優先度と `MoveSpeed` は操作しない
- `PlayerInputMoveSource`（`Unity.TinyCharacterController.Extensions`）: `PlayerInput` の Vector2 アクション（既定は `Move`）を読み取り、`MoveInputRelay` に渡す標準の最小実装

### Changed

- `TinyCharacterController.InputSystem` アセンブリが `Utj.TinyCharacterController` を参照するようにした

### Fixed

- `PlayerInputMoveSource`: PlayerInput の Behavior が `Send Messages` / `Broadcast Messages` のとき、入力のたびに `MissingMethodException` が出ていた。内部のメソッド名 `OnMove` が、PlayerInput の送る `OnMove(InputValue)` と同じ名前になっていたため。メソッド名を変えた

### 使う側のプロジェクトでの注意

- `PlayerInputMoveSource` を含む `TinyCharacterController.InputSystem` アセンブリは、Scripting Define Symbols に `SUPPORT_TCC` がないとコンパイルされない。定義がないとエラーにはならず、コンポーネントが見つからない状態になる。`MoveInputRelay` にはこの条件はない
