# 移動入力の中継コンポーネント（MoveInputRelay）設計書

ユーザ入力などの移動入力を `MoveControl` に渡す中継コンポーネントを、`com.utj.charactercontroller`（Tiny Character Controller。以下 TCC）に追加するための設計。

- チケット: [TSK-00001](../tickets/active/TSK-00001-move-input-relay.md)
- 状態: 設計中（未実装）

## カテゴリ

- MIR: 移動入力の中継コンポーネント

## MIR-00100: 目的と範囲

TCC には、入力を `MoveControl` につなぐ部品が用意されていない。サンプルではこの部分を Visual Scripting のグラフ（`Assets/TCC/Contents/ScriptGraph/PlayerController.asset` の Move など）で組んでいるため、C# で作るゲームでは毎回独自に実装することになる。

この中継処理は、どのゲームでも必ず必要になる。一方で、入力の取得元・加工・移動させない条件・ほかへの通知はゲームごとに大きく変わる。そこで、**どのゲームでも共通する最低限の機能だけを TCC に置き、ゲーム固有の処理はゲーム側で追加する**。

対象外:

- 優先度（`MovePriority` / `TurnPriority` / `LookTargetControl.Priority` など）の操作。MIR-00600（責務の境界）を参照
- 既存のサンプル（Visual Scripting のグラフ）の置き換え

## MIR-00200: 前提（MoveControl の挙動）

`Runtime/Components/Control/MoveControl.cs` の実装から分かっていること。設計はこれを前提にする。

| 挙動 | 該当箇所 | 設計への影響 |
|---|---|---|
| `Move(Vector2)` は値を保持するだけで、次に呼ばれるまでその値を使い続ける | `Move()` | 毎フレーム呼ぶ必要はない。入力が変わったときだけ呼べばよい |
| 使うのは入力の向きだけ（`direction.normalized`）。速度は `_moveSpeed` に向かって加速する | `IUpdateComponent.OnUpdate()` | 入力の大きさは速度に影響しない。スティックの倒し具合による歩き・走りの切り替えはできない |
| 入力の有無は `sqrMagnitude > 0` で判定する | `Move()` | スティックのわずかな揺れでも最高速まで加速する。閾値を設定できると便利 |
| 入力元が止まっても、最後の値が残り続ける | `Move()` | 中継役が止まるときに、ゼロを送る必要がある |
| カメラ基準への変換と軸の固定（`StartLockAxis`）は MoveControl が行う | `IUpdateComponent.OnUpdate()` | 中継役ではカメラを扱わない |

## MIR-00300: 構成

2つの部品に分ける。本体は Input System に依存しないので、AI・リプレイ・ゲーム側の入力振り分け処理など、どこから入力しても使える。

```text
Packages/com.utj.charactercontroller/Runtime/
├─ Components/Utility/MoveInputRelay.cs          本体（asmdef: Utj.TinyCharacterController）
└─ Extensions/InputSystemSupport/
    └─ PlayerInputMoveSource.cs                  Input System から入力を読み取り、MoveInputRelay に渡す
                                                 （asmdef: TinyCharacterController.InputSystem）
```

## MIR-00400: MoveInputRelay（本体）

### 配置

- 名前空間: `Unity.TinyCharacterController.Utility`
- メニュー: `TCC/Utility/`（`MenuList.Utility`）
- `IMove` を実装しないので、Control には入れない
- 名前空間に `...Input` は使わない。`UnityEngine.Input` と名前がぶつかるため

### Inspector の設定項目

| 項目 | 型 | 既定値 | 内容 |
|---|---|---|---|
| MoveControl | `MoveControl` | 未設定なら同じ GameObject から取得 | 入力を渡す先 |
| 軸の制限 | `Both` / `HorizontalOnly` / `VerticalOnly` | `Both` | `HorizontalOnly` は `(x, 0)` にする。TCC サンプルの Move に相当する |
| 入力閾値 | `float` | `0` | 大きさがこの値より小さい入力はゼロとして扱う |

### 公開 API

| メンバ | 内容 |
|---|---|
| `void SetInput(Vector2 input)` | 入力を受け取る窓口。誰が呼んでもよい |
| `Vector2 RawInput { get; }` | 受け取ったままの入力値 |
| `Vector2 AppliedInput { get; }` | 加工したあと、MoveControl に渡した値 |

### 拡張ポイント

| メンバ | 内容 |
|---|---|
| `protected virtual Vector2 ProcessInput(Vector2 raw)` | 入力の加工。標準の処理は「軸の制限 → 閾値」 |
| `protected void Apply()` | `RawInput` をもう一度加工して MoveControl に渡し直す。外部の状態に応じて毎フレーム加工したい派生クラスは、これを自分で呼ぶ |

拡張ポイントはこの2つだけにする。ゲーム固有の処理で中継役の外に置けるもの（ほかのコンポーネントへの通知など）は、継承せずに別のコンポーネントとして並べる。

### 動作

| タイミング | 処理 |
|---|---|
| `SetInput` | `RawInput` に保存する。有効なら `Apply()` |
| `OnEnable` | 保存している `RawInput` で `Apply()` |
| `OnDisable` | `MoveControl.Move(Vector2.zero)` を呼ぶ。`RawInput` はそのまま残す |
| `Update` | なし |

- MoveControl は入力を1つしか持てないので、`[DisallowMultipleComponent]` を付ける

## MIR-00500: PlayerInputMoveSource（Input System 用）

- `PlayerInput` とアクション名（既定は `"Move"`）を指定する。サンプルの Visual Scripting ノードと同じ指定方法にする
- `performed` と `canceled` を受け取ったら `MoveInputRelay.SetInput()` を呼ぶ。`canceled` のときはゼロを渡す
- `OnEnable` のときに、アクションの現在の値を読み取って渡す。スティックを倒したまま有効になった場合への対策
- MoveControl を直接呼ばない。必ず MoveInputRelay を経由する

### asmdef の変更

- `TinyCharacterController.InputSystem` は今は Input System しか参照していないので、`Utj.TinyCharacterController` への参照を追加する
- この asmdef には `defineConstraints: SUPPORT_TCC` が付いている。使う側のプロジェクトに `SUPPORT_TCC` の定義がないと、PlayerInputMoveSource はエラーも出さずにコンパイルから外れる。本体の MoveInputRelay を別の asmdef（条件なし）に置くのはこのため

## MIR-00600: 責務の境界

中継役は**優先度に一切触れない**。

| 中継役（MoveInputRelay）がやること | やらないこと（ゲーム側の担当） |
|---|---|
| 入力を受け取り、MoveControl に渡す | 優先度（Move / Turn / LookTarget）の操作 |
| 軸の制限と閾値 | ほかの移動処理への制御の受け渡し（優先度で決める） |
| 止まるときにゼロを送る | 入力の振り分け（ターゲット選択モードなど） |
| | ほかのコンポーネントへの通知 |

`enabled` と優先度は、次のように使い分ける。

- **`enabled`**: 「この入力元を使うかどうか」を切り替える。メニューやイベントシーンの間は使わない、といった場面
- **優先度**: 「ほかの Control が移動を引き継ぐ」ときに使う。TCC の Brain が、優先度の最も高い Control を選ぶ

## MIR-00700: 既存の実装との対応

### TCC サンプルの Move（Visual Scripting）

- MoveInputRelay（軸の制限: `HorizontalOnly`）と PlayerInputMoveSource の組み合わせで置き換えられる
- サンプルの Move は、移動中に毎フレーム `AnimatorMoveControl.TurnPriority` と `LookTargetControl.Priority` を 0 にしている。この処理は中継役に入れず、必要ならゲーム側で行う
- 既存のサンプルのグラフは変更しない

### ActionGameFramework の SM_FreeRun

`Reference/ActionGameFramework` の `SM_FreeRun`（`Assets/ProjectAssets/Scripts/BattleSystem/Movement/SM_FreeRun.cs`）を、この設計で置き換える場合の対応。

| 今の処理 | 新しい構成での担当 |
|---|---|
| BattleInputHandler からの入力（`SetInputVector`） | `MoveInputRelay.SetInput()` |
| `SetFreeRunActive` | `MoveInputRelay.enabled` |
| 接近中（SM_ArtsApproaching）は処理しない | 接近処理側の MovePriority を上げる（優先度で解決） |
| SM_ArtsApproaching への入力の通知 | BattleInputHandler が直接渡す |
| `moveSpeed` / `smoothTime` | 使われていないので移さない |

## MIR-00800: 未決事項

- クラス名（仮: `MoveInputRelay` / `PlayerInputMoveSource`）
- 使う側のプロジェクトで `SUPPORT_TCC` の定義が必要になることを、どこに書いておくか
