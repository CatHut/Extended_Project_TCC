# Changelog

この CHANGELOG は、このフォーク（Extended_Project_TCC）で加えた変更だけを記録する。0.7.0 までの変更内容は記録していない。

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
