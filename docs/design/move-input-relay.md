# 移動入力の中継コンポーネント（MoveInputRelay）設計書

ユーザ入力などの移動入力を `MoveControl` に渡す中継コンポーネントを、`com.utj.charactercontroller`（Tiny Character Controller。以下 TCC）に追加するための設計。

- チケット: [TSK-00001](../tickets/active/TSK-00001-move-input-relay.md)
- 状態: 実装済み（0.7.1）

## カテゴリ

- MIR: 移動入力の中継コンポーネント

## MIR-00100: 目的と範囲

TCC には、入力を `MoveControl` につなぐ部品が用意されていない。サンプルではこの部分を Visual Scripting のグラフ（`Assets/TCC/Contents/ScriptGraph/PlayerController.asset` の Move など）で組んでいるため、C# で作るゲームでは毎回独自に実装することになる。

この中継処理は、どのゲームでも必ず必要になる。一方で、入力の取得元・加工・移動させない条件・ほかへの通知はゲームごとに大きく変わる。そこで、**どのゲームでも共通する最低限の機能だけを TCC に置き、ゲーム固有の処理はゲーム側で追加する**。

対象外:

- 優先度（`MovePriority` / `TurnPriority` / `LookTargetControl.Priority` など）の操作
- `MoveControl.MoveSpeed` の操作
- 既存のサンプル（Visual Scripting のグラフ）の置き換え

どちらも MIR-00600（責務の境界）を参照。

## MIR-00200: 前提（MoveControl の挙動）

`Runtime/Components/Control/MoveControl.cs` の実装から分かっていること。設計はこれを前提にする。

| 挙動 | 該当箇所 | 設計への影響 |
|---|---|---|
| `Move(Vector2)` は値を保持するだけで、次に呼ばれるまでその値を使い続ける | `Move()` | 毎フレーム呼ぶ必要はない。入力が変わったときだけ呼べばよい |
| 使うのは入力の向きだけ（`direction.normalized`）。速度は `MoveSpeed` に向かって徐々に変わる | `IUpdateComponent.OnUpdate()` | `Move()` に渡す値の大きさは速度に影響しない。速度は `MoveSpeed` で指定する |
| 入力の有無は `sqrMagnitude > 0` で判定する | `Move()` | スティックのわずかな揺れでも最高速まで加速する。閾値を設定できると便利 |
| 入力元が止まっても、最後の値が残り続ける | `Move()` | 中継役が止まるときに、ゼロを送る必要がある |
| カメラ基準への変換と軸の固定（`StartLockAxis`）は MoveControl が行う | `IUpdateComponent.OnUpdate()` | 中継役ではカメラを扱わない |

### 速度の指定

TCC は「向きは `Move()`、速さは `MoveSpeed`」と、2つを別々に指定する作りになっている。サンプル UC-01-02_Walk_and_Run も、Sprint ボタンを押している間だけ `MoveSpeed` を書き換えて歩きと走りを切り替えている。

スティックの傾きで速度を変えたい場合は、`MoveInputRelay.RawInput` の大きさから `MoveSpeed` を決めるコンポーネントをゲーム側で用意する。`MoveSpeed` はほかの処理（ダッシュなど）も書き換えるので、**`MoveSpeed` を書き換える処理は1つにまとめる**。

## MIR-00300: 構成

2つの部品に分ける。本体は Input System に依存しないので、AI・リプレイ・ゲーム側の入力処理など、どこから入力しても使える。

```text
Packages/com.utj.charactercontroller/Runtime/
├─ Components/Utility/MoveInputRelay.cs          本体（asmdef: Utj.TinyCharacterController）
└─ Extensions/InputSystemSupport/
    └─ PlayerInputMoveSource.cs                  Input System から入力を読み取り、MoveInputRelay に渡す
                                                 （asmdef: TinyCharacterController.InputSystem）
```

| 部品 | 役割 | ゲーム固有の対応 |
|---|---|---|
| Input Actions アセット | デバイスの違い（スティック・WASD など）を吸収し、Vector2 にそろえる | バインディングの設定 |
| PlayerInputMoveSource | Move アクションを読み取り、MoveInputRelay に渡す | 拡張しない。入力の振り分けが必要なら、ゲーム側の入力処理に置き換える |
| MoveInputRelay | 入力を加工して MoveControl に渡す | 設定（軸の制限・閾値）で足りなければ `ProcessInput` を上書きする |

## MIR-00400: MoveInputRelay（本体）

### 配置

- 名前空間: `Unity.TinyCharacterController.Utility`
- メニュー: `TCC/Utility/MoveInputRelay`
- `IMove` を実装しないので、Control には入れない
- 名前空間に `...Input` は使わない。`UnityEngine.Input` と名前がぶつかるため

### Inspector の設定項目

| 項目 | 型 | 既定値 | 内容 |
|---|---|---|---|
| Move Control | `MoveControl` | 未設定なら同じ GameObject から取得 | 入力を渡す先 |
| Axis Constraint | `Both` / `HorizontalOnly` / `VerticalOnly` | `Both` | `HorizontalOnly` は `(x, 0)` にする。TCC サンプルの Move に相当する |
| Input Threshold | `float` | `0` | 大きさがこの値より小さい入力はゼロとして扱う |

### 公開 API

| メンバ | 内容 |
|---|---|
| `void SetInput(Vector2 input)` | 入力を受け取る窓口。誰が呼んでもよい |
| `Vector2 RawInput { get; }` | 受け取ったままの入力値 |
| `Vector2 AppliedInput { get; }` | 加工したあと、MoveControl に最後に渡した値 |
| `MoveControl MoveControl { get; set; }` | 入力を渡す先 |
| `AxisConstraint Axis { get; set; }` | 軸の制限。変えると現在の入力をもう一度渡す |
| `float InputThreshold { get; set; }` | 閾値。変えると現在の入力をもう一度渡す |

### 拡張ポイント

| メンバ | 内容 |
|---|---|
| `protected virtual Vector2 ProcessInput(Vector2 raw)` | 入力の加工。標準の処理は「軸の制限 → 閾値」 |
| `protected void Apply()` | `RawInput` をもう一度加工して MoveControl に渡し直す。外部の状態に応じて毎フレーム加工したい派生クラスは、これを自分で呼ぶ |

- 拡張ポイントはこの2つだけにする。ゲーム固有の処理で中継役の外に置けるもの（ほかのコンポーネントへの通知など）は、継承せずに別のコンポーネントとして並べる
- MoveControl は入力の向きしか使わないので、`ProcessInput` で意味があるのは**向きを変える加工だけ**（軸の制限、8方向へのスナップ、左右の反転など）。入力の大きさを変えても速度は変わらない（MIR-00200（前提（MoveControl の挙動） > 速度の指定））

### 動作

| タイミング | 処理 |
|---|---|
| `SetInput` | `RawInput` に保存する。有効なら `Apply()` |
| `OnEnable` | 保存している `RawInput` で `Apply()` |
| `OnDisable` | `MoveControl.Move(Vector2.zero)` を呼ぶ。`RawInput` はそのまま残す |
| `Update` | なし |

- MoveControl は入力を1つしか持てないので、`[DisallowMultipleComponent]` を付ける

## MIR-00500: PlayerInputMoveSource（Input System 用）

入力の振り分けが要らない構成で、そのまま使える**標準の最小実装**。MoveInputRelay と同じ GameObject に置けば、コードを書かずに動く。

- `PlayerInput` とアクション名（既定は `"Move"`）を指定する。サンプルの Visual Scripting ノードと同じ指定方法にする
- `performed` と `canceled` を受け取ったら `MoveInputRelay.SetInput()` を呼ぶ。`canceled` のときはゼロを渡す
- `OnEnable` のときに、アクションの現在の値を読み取って渡す。スティックを倒したまま有効になった場合への対策
- `OnDisable` のときにゼロを渡す
- MoveControl を直接呼ばない。必ず MoveInputRelay を経由する

### 使い分け

| 構成 | 使い方 |
|---|---|
| 入力をそのまま移動に使う（TCC のサンプルなど） | PlayerInputMoveSource と MoveInputRelay を並べる |
| ゲーム側で入力を振り分ける（モードによってスティックを別の操作に使うなど） | PlayerInputMoveSource は使わない。ゲーム側の入力処理から `MoveInputRelay.SetInput()` を直接呼ぶ |

PlayerInputMoveSource には拡張ポイントを用意しない。振り分けが必要になったら、継承せずに置き換える。

### 入力の型

- 読み取るのは Vector2 のアクションだけ（expected control type が `Vector2` / `Stick` / `Dpad`、または未指定）。1D Axis のアクションは扱わず、警告を出して何もしない
- 2D ゲームでも Move アクションは Vector2 のまま受け取り、MoveInputRelay の Axis Constraint を `HorizontalOnly` にする

### asmdef

- `TinyCharacterController.InputSystem` に `Utj.TinyCharacterController` への参照を追加した
- この asmdef には `defineConstraints: SUPPORT_TCC` が付いている。使う側のプロジェクトの Scripting Define Symbols に `SUPPORT_TCC` がないと、PlayerInputMoveSource はエラーも出さずにコンパイルから外れる。本体の MoveInputRelay を別の asmdef（条件なし）に置くのはこのため。この注意はクラスのコメントと CHANGELOG にも書く

## MIR-00600: 責務の境界

中継役は**優先度と `MoveSpeed` に一切触れない**。

| 中継役（MoveInputRelay）がやること | やらないこと（ゲーム側の担当） |
|---|---|
| 入力を受け取り、MoveControl に渡す | 優先度（Move / Turn / LookTarget）の操作 |
| 軸の制限と閾値 | `MoveSpeed` の操作（MIR-00200（前提（MoveControl の挙動） > 速度の指定）） |
| 止まるときにゼロを送る | ほかの移動処理への制御の受け渡し（優先度で決める） |
| | 入力の振り分け（モードによってスティックを別の操作に使うなど） |
| | ほかのコンポーネントへの通知 |

`enabled` と優先度は、次のように使い分ける。

- **`enabled`**: 「この入力元を使うかどうか」を切り替える。メニューやイベントシーンの間は使わない、といった場面
- **優先度**: 「ほかの Control が移動を引き継ぐ」ときに使う。TCC の Brain が、優先度の最も高い Control を選ぶ

## MIR-00700: 既存の実装との対応

### TCC サンプルの Move（Visual Scripting）

- MoveInputRelay（Axis Constraint: `HorizontalOnly`）と PlayerInputMoveSource の組み合わせで置き換えられる
- サンプルの Move は、移動中に毎フレーム `AnimatorMoveControl.TurnPriority` と `LookTargetControl.Priority` を 0 にしている。この処理は中継役に入れず、必要ならゲーム側で行う
- 既存のサンプルのグラフは変更しない

### ゲーム側で入力を振り分け、ほかの移動処理と切り替える場合

ゲーム側に「入力の振り分け役」と「特定の状況で移動を引き継ぐ処理（例: 目標に自動で近づく処理）」がある構成では、次のように分担する。

| 処理 | 担当 |
|---|---|
| 入力を受け取り、移動に使うかどうかを振り分ける | ゲーム側の入力処理。移動に使う入力は `MoveInputRelay.SetInput()` に渡す |
| 移動を引き継ぐ処理に、ユーザの入力も知らせる | ゲーム側の入力処理が、その処理に直接渡す |
| 移動を引き継いでいる間は、ユーザの入力で動かさない | 引き継ぐ処理の MovePriority を上げる（優先度で解決する） |
| 入力元を一時的に使わない | `MoveInputRelay.enabled = false` |
