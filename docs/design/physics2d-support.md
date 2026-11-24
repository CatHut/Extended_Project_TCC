# 2D 物理（Physics2D）対応の設計

TCC（`com.utj.charactercontroller`）のキャラクタを、`Collider2D`（`BoxCollider2D`・`TilemapCollider2D` など）で作ったステージの上で動かせるようにする。対象は、横から見た 2D のアクションゲーム（XY 平面で動き、Y が上）。

- チケット: [TSK-00003](../tickets/done/TSK-00003-physics2d-brain-and-checks.md)

## カテゴリ

- PHS: 2D 物理対応

## PHS-00100: 背景

Unity の 3D 物理（PhysX）と 2D 物理（Box2D）は、別の物理エンジンで、それぞれ別の世界を持つ。3D の API（`CharacterController`・`Physics.SphereCast` など）からは、`Collider2D` が見えない。

TCC の部品のうち、コライダーに触れるのは Brain と Check だけである。

| 部品 | 役割 | 3D 物理への依存 |
|---|---|---|
| Control（`MoveControl`・`JumpControl` など） | 動きたい速度を出す | なし（Check の結果をインターフェースで読む） |
| Effect（`Gravity`・`MoveWithPlatform` など） | 外から加わる速度を出す | ほぼなし（`Physics.gravity` の値を読むだけ） |
| Check（`GroundCheck`・`HeadContactCheck` など） | 周りを調べて結果を出す | あり（`Physics.SphereCast` など） |
| Brain（`CharacterBrain` など） | 速度を合計し、当たり判定をして位置を決める | あり（`CharacterController.Move`） |

Control と Effect は、Check をインターフェース（`IGroundContact`・`IGroundObject`・`IOverheadDetection`・`IWallCheck`）越しに使っている。具体的なクラスには依存していない。そのため、Brain と Check の 2D 版を同じインターフェースで作れば、Control と Effect はそのまま使える。

2D 物理には `CharacterController` にあたるコンポーネントがない。そのため、当たったら止まる・面に沿って滑る移動は、2D 版の Brain が自分で行う。

## PHS-00200: 方針

- **物理シミュレーションは使わない。** キャラクタの `Rigidbody2D` は Kinematic にする（物理エンジンからは動かされない）。重力は従来どおり TCC の `Gravity` が速度として出す
- **当たり判定は `Physics2D` の判定 API で行う。** キャラクタの形（カプセル）を移動方向に飛ばし、当たる位置を調べる
- **既存のコードは変更しない。** 2D 版は、既存の Brain・Check と同じインターフェース・同じ基底クラス（`BrainBase`）で、別のクラスとして追加する。3D 版の挙動は変わらない
- **既存の 3D の部品と混ぜない。** 2D のキャラクタには、2D 版の Brain と Check を付ける。`CharacterController`・`Collider`（3D）は付けない

## PHS-00300: 構成

```text
Runtime/Extensions/Physics2DSupport/
├── TinyCharacterController.Physics2D.asmdef
├── CharacterBrain2D.cs        Brain（BrainBase の派生）
├── GroundCheck2D.cs           IGroundContact / IGroundObject
├── HeadContactCheck2D.cs      IOverheadDetection（JumpControl が必須とする）
└── Physics2DSupportUtility.cs  メニューのパス、自分のコライダーの一覧、判定の共通処理（internal）
```

- アセンブリ `TinyCharacterController.Physics2D` は `Utj.TinyCharacterController` と `Utj.BatchProcess`（`UpdateTiming` がある）を参照する
- 2D 物理のモジュール（`com.unity.modules.physics2d`）が無効なプロジェクトではコンパイルしない（`versionDefines` で定義を作り、`defineConstraints` で条件にする）。`SUPPORT_TCC` は条件にしない
- 名前空間は既存と同じにする（`Unity.TinyCharacterController.Brain`・`Unity.TinyCharacterController.Check`）
- 既存の Check と同じように、`CharacterSettings` の `Height`・`Radius`・`EnvironmentLayer` を使う

## PHS-00400: CharacterBrain2D

### 必要なコンポーネント

| コンポーネント | 設定 |
|---|---|
| `Rigidbody2D` | Kinematic。`Awake` で Body Type を Kinematic にし、重力の影響を受けないようにする |
| `CapsuleCollider2D` | 縦向き。大きさは `CharacterSettings` の `Height`・`Radius` から決める（`CharacterBrain` が `CharacterController` の大きさを決めるのと同じ） |
| `CharacterSettings` | 既存のもの |

`Rigidbody2D` を付けるのは、ほかのオブジェクト（スイッチ・トゲなどのトリガー）との接触の通知（`OnTriggerEnter2D`）を受けるためである。2D 物理では、接触する2つのコライダーの少なくとも一方に `Rigidbody2D` が必要になる。

### 位置の更新（`ApplyPosition`）

1. Z の速度を 0 にする（XY 平面だけを動く）
2. `GroundCheck2D` があり、しっかり接地していて、上に向かって動いていなければ、地面までの距離だけ下げる（`CharacterBrain` と同じ吸い付けの補正）
3. めり込みの押し出し（移動の前）: カプセルが環境のコライダーに重なっていれば（`Physics2D.OverlapCapsule`）、`Physics2D.Distance` でいちばん深いものを選び、最短の方向へ押し出して `_skinWidth` のすき間を空ける。最大 4 回繰り返す。`Physics2D.Distance` は `Rigidbody2D` の姿勢を使うので、判定の前に自分の `Rigidbody2D.position` を判定する位置に合わせる（すぐに判定に反映される）。位置を指定する版の `Physics2D.Distance` は、両方のコライダーが `Rigidbody2D` に付いていないと使えないため使わない
4. カプセルを移動方向に飛ばし（`Physics2D.CapsuleCast`）、当たったら、当たった位置の手前（すき間 `_skinWidth`）まで動かす。残りの移動量は、当たった面に沿った成分だけにして、もう一度飛ばす。これを最大 `_maxIterations` 回（既定 3）繰り返す
5. 判定の対象は `CharacterSettings.EnvironmentLayer` のレイヤーで、トリガーは除く。次の当たりは結果から除外する
   - 自分のコライダー
   - 距離 0 の当たり（キャストの開始時にすでに重なっている。除外しないと、めり込んだときにどの方向にも動けなくなる）。この除外のため、押し出しがないと、壁にわずか（約 0.01 以上）でもめり込んだキャラクタはその壁を通り抜ける。押し出しはこれを防ぐ
   - 移動方向を向いていない面（面の向きと移動方向の内積が 0 以上）

キャストの起点はキャラクタの現在の位置から計算する。そのため、キャラクタ自身の `Rigidbody2D` の位置の同期を待たずに、そのフレームの中で続けて判定できる。

### 向き（`ApplyRotation`）

TCC の向きは Y 軸まわりの回転（ヨー）で、`TurnSpeed` に応じて徐々に回る。2D の Sprite は、回る途中で細く見える。そのため、向きの反映方法を選べるようにする。

| `_turnMode` | 動作 |
|---|---|
| `SnapLeftRight`（既定） | Turn の要求する向き（`BrainBase.YawAngle`。補間前の値）が右寄りなら 0°、左寄りなら 180° に、すぐに切り替える。TCC のヨーは 90° が +X、-90° が -X なので、ヨーの sin の符号で判定する。奥行き方向（0° と 180°）を向いているときは今の向きを保つ |
| `Free` | 受け取った回転をそのまま反映する（`CharacterBrain` と同じ） |

`SnapLeftRight` でも Transform を回すので、子のオブジェクト（攻撃の判定など）は向きに合わせて反転する。キャラクタの前は Transform のローカルの +X になり、`transform.forward` は奥行き方向を向いたままになる。そのため、`transform.forward` を前として使う処理（`MoveControl.DeltaDirectionAngle` など）は値が合わない。

`MoveControl` は、移動速度が停止のしきい値を超えてから向きを要求する。そのため、入力してから向きが変わるまで数フレームかかる（3D でも同じ）。

### 対象外

- ほかのキャラクタや Rigidbody を押す処理（`CharacterBrain` の `Pushable`）は作らない
- `CharacterBrain` の軸の固定（`_freezeAxis`）は作らない。Z は常に固定する

## PHS-00500: GroundCheck2D

`GroundCheck` と同じ判定を、`Physics2D.CircleCast`（真下に向けて飛ばす）で行う。Inspector の項目（`_ambiguousDistance`・`_preciseDistance`・`_maxSlope`・`_onChangeGroundObject`）と、`IGroundContact`・`IGroundObject` で返す値の意味は `GroundCheck` と同じにする。

- `IGroundObject.GroundCollider` は 3D の `Collider` 型なので、常に `null` を返す。2D のコライダーは、追加のプロパティ `GroundCollider2D` で返す
- `MoveWithPlatform` は `IGroundObject.GroundObject`（`GameObject`）しか使わないので、そのまま使える
- 実行順は `GroundCheck` と同じ（`Order.Check`）

## PHS-00600: HeadContactCheck2D

`JumpControl` は `IOverheadDetection` を必須にしている（ないと実行時に例外になる）。そのため、ジャンプを使うには天井の判定も必要になる。`HeadContactCheck` と同じ判定を、`Physics2D.CircleCast`（真上に向けて飛ばす）で行う。

## PHS-00700: 未決事項

| 項目 | 内容 |
|---|---|
| 位置の同期 | Transform で動かしたほかのコライダーを判定 API が見るのは、物理の位置の同期の後になる（`Physics2D.SyncTransforms`、または物理の更新）。動く床などで判定が1フレーム遅れるかを確認する |
| 壁の判定 | `WallCheck` の 2D 版。必要になったら作る |
| `ExtraForce` | 吹き飛んだ先のコライダーを 3D の `CapsuleCast` で調べてイベントを出している。2D では、このイベントが出ない（移動そのものは Brain が壁で止める） |
| `Gravity` の重力の値 | `Physics.gravity`（3D の設定）を使う。`Physics2D.gravity` に切り替えるかは、必要になったら決める |
