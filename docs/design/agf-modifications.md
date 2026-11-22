# ActionGameFramework での com.utj.charactercontroller 改造内容

`Reference/ActionGameFramework`（以下 AGF）で `Packages/com.utj.charactercontroller`（Tiny Character Controller。以下 TCC）に入れた改造を、コミットごとにまとめた資料。どの改造を Extended_Project_TCC に反映するかを検討するときの元資料とする。

- 調べた範囲: AGF リポジトリの `Packages/com.utj.*` と `Packages/UnityChan.SpringBone`
- 調べた日: 2026-11-22
- CM-01200 は、この資料の作成後に AGF の調査用ブランチ `UnityWorkspaceReference` で入れた修正。AGF の main には入っていない

## カテゴリ

- MOD: 改造の全体像
- CM: コミット別の改造内容

## MOD-00100: 前提

- AGF の履歴は TCC upstream の `02e3eca`（2024-04-30）を祖先に持つ。Extended_Project_TCC の `Packages/` はこのコミットから変わっていなかったので、改造は AGF の下記コミットだけに入っている
- 差分があるのは **`com.utj.charactercontroller` だけ**。SpringBone / gameobjectfolder / savedata / scenarioimporter / sceneloader に改造はない
- AGF 側の未コミット変更（`.mat` の `version: 9→10`、sceneloader のサンプルアセットの削除）は Unity が自動で行った変更で、改造ではない
- AGF の Unity は 6000.2.6f2、Extended_Project_TCC の Unity は 6000.6.10f1

### Extended_Project_TCC 側で先に入った変更との関係

- `a02c47d`（InstanceID 系 API を EntityId に置き換え）で `GameObjectPool.cs` / `PooledGameObject.cs` を変更済み。CM-00500（GameObjectPool の改修）を反映するときは、同じ箇所の周り（`CreateInstance` の表示名を作る行）で衝突する
- `velocity` / `drag` は 6.6 でも警告で済み、コンパイルエラーにはならない（CM-00100（Unity 6 の Rigidbody API 対応））

## MOD-00200: 一覧

差し引きで変更なしになるコミットは「（差し引きなし）」と書いた。「反映」列は Extended_Project_TCC への反映状況。

| ID | AGF コミット | 日付 | 内容 | 主な対象 | 公開 API の追加 | 既存の挙動の変化 | 反映 |
|---|---|---|---|---|---|---|---|
| CM-00100 | `7fc3fb2ac` | 2025-08-24 | Unity 6 の Rigidbody API 対応 | RigidbodyBrain, PooledGameObject | なし | なし | 未 |
| CM-00200 | `7649337fa` | 2025-09-21 | MoveControl のブレーキ・加速を公開 | MoveControl | あり | なし | 未 |
| CM-00300 | `2b229a909` → `55f965d6f` | 2025-09-24 | Gravity / ExtraForce の改造と Revert（差し引きなし） | Gravity, ExtraForce | — | — | 対象外 |
| CM-00400 | `88a70300e` | 2025-12-13 | ルートモーションの移動量を deltaPosition で扱う | RootMotionReceiver, AnimatorMoveControl | あり | **あり** | 済（一部） |
| CM-00500 | `a6a075dee`, `a57654926` | 2025-12-20 | GameObjectPool をランタイムで作れるようにし、初回生成位置を直す | GameObjectPool | あり | **あり** | 未 |
| CM-00600 | `9b4d7ff4a` | 2026-01-24 | Indicator の Offset の不具合修正と `SetTargetAndOffset` 追加 | IndicatorSystem, Indicator | あり | 不具合修正 | 未 |
| CM-00700 | `db9975804` | 2026-01-31 | GroundCheck に Inspector 確認用のフィールドを追加 | GroundCheck | なし | なし | 未 |
| CM-00800 | `a0ec504e4` | 2026-02-08 | ScreenSpace-Camera 用の Indicator を新規追加 | ScreenSpaceCameraIndicator(+System) | あり | なし | 未 |
| CM-00900 | `cae25a465`, `340c5e2f1` | 2026-05-15〜19 | ルートモーションの移動量を humanScale で正規化 | AnimatorMoveControl | Inspector 項目 | **あり** | 済（選択式に変更） |
| CM-01000 | `4116a79b0` → `3cac306b8` | 2026-05-24 | ISS-00051 の調査ログと低 FPS 対策を入れて削除（差し引きなし） | CharacterBrain, AnimatorMoveControl | — | — | 対象外 |
| CM-01100 | `00a607a07` | 2026-05-24 | Arts 実行中に XZ 移動を止めるインターフェースを追加 | CharacterBrain, IArtsXzMoveBlocker | あり | なし（実装がなければ） | 未 |
| CM-01200 | `9b9a09b5c`（`UnityWorkspaceReference`） | 2026-11-22 | 地面に止められて下がり切れなかった分を、その後の上昇から差し引く | AnimatorMoveControl | Inspector 項目 | **あり** | 済 |

## CM-00100: Unity 6 の Rigidbody API 対応

- コミット: `7fc3fb2ac`「プロジェクト更新」
- 変更
  - `RigidbodyBrain`: `rig.drag` → `rig.linearDamping`、`_rigidbody.velocity` → `_rigidbody.linearVelocity`
  - `PooledGameObject`: `_rigidbody.velocity` → `_rigidbody.linearVelocity`
- 同じコミットで AGF の `Packages/manifest.json` / `packages-lock.json` も変わっているが、これは AGF プロジェクトの変更なので反映対象外
- 単純な名前の置き換えで、挙動は変わらない

## CM-00200: MoveControl のブレーキ・加速を公開

- コミット: `7649337fa`「公開プロパティを追加」
- 変更: `MoveControl` に、既存のシリアライズフィールド `_brakePower` / `_accelerator` を読み書きするプロパティ `BrakePower` / `Accelerator` を追加
- AGF での使い方: `SM_CharacterBase` が、マスタデータ（PlayerData / EnemyData の `BrakePower` / `Accelerator` 列）の値を実行時に設定する
- 追加だけなので、既存の挙動は変わらない

## CM-00300: Gravity / ExtraForce の改造と Revert（差し引きなし）

- コミット: `2b229a909`「Gravity:公開プロパティ追加 ExtraForce:Gravityを考慮するように調整」→ 同じ日に `55f965d6f` で Revert
- 入れようとした内容
  - `Gravity` に、1 フレームあたりの落下速度を返す `FallSpeedPerFrame` を追加
  - `ExtraForce` で、空中にいるときに Gravity の落下分を速度に加える
- 最終的な差分はない。反映の対象外

## CM-00400: ルートモーションの移動量を deltaPosition で扱う

- コミット: `88a70300e`「Animationを使用したRootMotionの制御を調整」
- 変更
  - `RootMotionReceiver`: `OnAnimatorMove` で `animator.deltaPosition` / `deltaRotation` を受け取り、`DeltaPosition` / `DeltaRotation` プロパティで公開する
  - `AnimatorMoveControl`: `_velocity` とは別に `_deltaPosition` を持ち、地面の法線への投影も同じように行う。**`IsFixedPosition` の behaviour では、Warp の移動量を `_velocity * deltaTime` から `_deltaPosition` に変更**
- 目的（コードのコメントより）: 速度に直してから戻す計算で失われる精度を避ける
  - 実際に失われるのは精度ではなく、前のフレームの速度に今のフレームの deltaTime を掛けることによるずれ。deltaTime が揺れると、移動距離がアニメーションと合わなくなる
  - AGF で課題だった「上昇して着地する技で、着地位置が浮く」問題の原因はこれではなかった（CM-01200）
- 影響: `IsFixedPosition` を使う AnimatorMoveBehaviour の移動量が変わる。とくに deltaTime が揺れるときの移動距離が、アニメーションどおりになる方向に変わる

### Extended_Project_TCC への反映

- CM-01200 と一緒に反映した。反映したのは、`RootMotionReceiver.DeltaPosition` の追加と、`IsFixedPosition` の Warp で deltaPosition を使う変更
- `RootMotionReceiver.DeltaRotation` は、既存の `Rotation` と同じ値（`animator.deltaRotation`）なので追加していない
- `IsFixedPosition` でない区間は、従来どおり速度で Brain に渡す（`IMove.MoveVelocity`）

## CM-00500: GameObjectPool をランタイムで作れるようにし、初回生成位置を直す

- コミット: `a6a075dee`「GameObjectPoolを調整」、`a57654926`「初回生成時の生成位置の不具合を修正」
- 変更
  - `Awake`: `_prefab` が設定されているときだけ、登録と初期化を行う。未設定ならエラーにせず何もしない
  - `ManualInitialize(PooledGameObject prefab)` を追加。`AddComponent` で作った Pool に後からプレハブを渡して初期化できる
  - `Get(position, rotation)` で新しくインスタンスを作るときは、`Instantiate(prefab, position, rotation, parent)` で最初から指定位置に作る。Awake が正しい座標で走るようにするため。プールから取り出す場合は従来どおり `SetPositionAndRotation` で動かす
- AGF での使い方: `StageEnemySpawnService` が、敵のプレハブごとに Pool を実行時に作り `ManualInitialize` で初期化する
- 影響
  - プレハブ未設定の Pool は、これまで Awake で NullReferenceException になっていた（`Register` → `PrefabID` → `Initialize` の中で `_prefab.gameObject` を参照するため）。改造後は何もせずに終わるので、設定漏れに気付きにくくなる
  - 新しく作るインスタンスの Awake / OnEnable が呼ばれる時点の位置が、原点から指定位置に変わる
- Extended_Project_TCC の `a02c47d` と同じ箇所の周りを変えているので、反映時に衝突する（MOD-00100（前提））

## CM-00600: Indicator の Offset の不具合修正と SetTargetAndOffset 追加

- コミット: `9b4d7ff4a`「インジケータ表示調整」
- 変更
  - `IndicatorSystem.UpdateUiPositionImmediate`（登録直後の位置計算）で `Offset` が足されていなかったのを直した。毎フレームの計算では足されていたので、登録した最初のフレームだけ位置がずれていた
  - `Indicator.SetTargetAndOffset(Transform, Vector3)` を追加。Target と Offset を同時に変え、登録済みなら登録し直す。Target を変えたあとに Offset を変えると、古い Offset で登録されてしまう問題を避けるため
- 修正部分は upstream の不具合修正としてそのまま使える

## CM-00700: GroundCheck に Inspector 確認用のフィールドを追加

- コミット: `db9975804`「デバッグ機能追加」
- 変更: `[SerializeField] private bool _isOnGround` を追加し、毎フレーム `IsOnGround` の値を書き写す。Inspector で接地状態を見るためのもの。ほかは空白・改行の整形だけ
- 検討点: 本来の値はプロパティにあり、このフィールドは表示専用。シリアライズされるので、Inspector で値を書き換えられるように見える。残すか、`[ReadOnly]` 相当の表示やカスタムエディタに置き換えるかを決める

## CM-00800: ScreenSpace-Camera 用の Indicator を新規追加

- コミット: `a0ec504e4`「ScreenSpace-Camera対応のIndicatorコンポーネントを追加」
- 変更: 次の 2 ファイルを新規に追加（既存ファイルは変更なし）
  - `UI/ScreenSpaceCameraIndicator.cs`（297 行）: `Indicator` を元にしたコンポーネント
  - `Manager/ScreenSpaceCameraIndicatorSystem.cs`（423 行）: `IndicatorSystem` を元にしたまとめ処理
- 元の `Indicator` との違い
  - 元の `Indicator` は ScreenSpace-Overlay の Canvas でしか正しく動かない
  - こちらは画面座標を `RectTransformUtility.ScreenPointToLocalPointInRectangle` で Canvas のローカル座標に直し、`anchoredPosition` に設定する。Canvas のカメラは、最初に登録されたコンポーネントの親 Canvas（rootCanvas）の `worldCamera` から取る
  - 最初から `SetTargetAndOffset` を持つ（CM-00600）
- AGF での使い方: `TargetIndicatorController`（ターゲット表示の UI）
- 検討点
  - 元のクラスをほぼ丸ごと複製しているので、同じ修正を 2 か所に入れる必要がある。共通化するかを決める
  - Canvas のカメラを 1 つしか持たないので、カメラの違う複数の Canvas に置くと正しく動かない

## CM-00900: ルートモーションの移動量を humanScale で正規化

- コミット: `cae25a465`「移動量補正更新」、`340c5e2f1`「初期値修正」
- 変更
  - `AnimatorMoveControl` に Inspector 項目 `_referenceHumanScale`（基準アバターの humanScale）を追加。初期値は最初 `0.6516`、`340c5e2f1` で `1` に変更
  - ルートモーションの `Velocity` と `DeltaPosition` を `現在の humanScale / _referenceHumanScale` で割る。Generic / None のアバターは humanScale が 1 なので影響しない
- 目的（Tooltip より）: アバターごとの体格差を吸収し、同じアニメーションで同じ距離を動かす。AGF にはアバターの切り替え機能があり、アバターごとに大きさが違う。大きさで移動距離が変わると、技の性能そのものが変わってしまうため
- 影響: 基準値の初期値が `1` なので、**humanScale が 1 でない Humanoid では、Inspector を何も変えなくても移動量が変わる**。AGF の Player（humanScale 0.6516）では、ルートモーションの移動量が約 1.535 倍（`1 / 0.6516`）になっていた。CM-01200 の浮きも、この倍率で 0.28 から 0.43 に拡大されていた
- 問題点: Generic のアバターにも割り算をしている。Generic は humanScale が 1 なので、基準値を 1 以外にすると Generic の移動量まで変わる

### Extended_Project_TCC への反映

モーション（ステート）ごとに、正規化するかどうかを選べる形にして反映した。AGF は AnimatorMoveControl に基準値を 1 つ持つので、すべてのモーションが同じ基準になる。購入したモーションなど、作成に使ったモデルがモーションごとに違う場合に対応できないため、AnimatorMoveBehaviour に持たせた。

#### 考え方

Humanoid のクリップは、ルートの移動量を**モーション作成に使ったモデル（A）の humanScale で割った値**で保存し、再生するアバターの humanScale を掛けて戻す。クリップには A の humanScale は記録されない。

| 設定 | アバター B で再生したときの移動量（A で作った移動量を 1m とする） |
|---|---|
| 正規化 無効（初期値。Unity 標準） | 1m × hB / hA（アバターの大きさに比例する） |
| 正規化 有効、Reference Human Scale = hA | 1m（どのアバターでも A と同じ） |

#### 仕様

- AnimatorMoveBehaviour の Inspector 項目
  - `Normalize Human Scale`: 正規化するかどうか。**初期値は無効**で、元の TCC と同じ挙動になる
  - `Reference Human Scale`: モーション作成に使ったモデルの humanScale。初期値は `1`
- 有効なときは、ルートモーションの速度と deltaPosition に `Reference Human Scale / Animator.humanScale` を掛ける。回転には掛けない
- **Humanoid のアバター（`Animator.isHuman`）にだけ適用する**。AGF の問題点を直したもの
- Inspector（`AnimatorMoveBehaviourEditor`）に、Model 欄と「Prefab から取得」ボタンを置いた。Humanoid の Avatar を持つ Prefab または FBX を指定してボタンを押すと、その humanScale を `Reference Human Scale` に書き込む。アニメーション用の FBX でも、自分の Avatar を持っていれば取得できる
- humanScale は標準の Inspector には表示されない。値を確かめる場合もこのボタンを使う

#### AGF の設定を移す場合

AGF は基準値 1 で全モーションを正規化していた。同じ挙動にするには、各 AnimatorMoveBehaviour の `Normalize Human Scale` を有効にし、`Reference Human Scale` を 1 のままにする。モデル A の距離に合わせる場合は、`Reference Human Scale` に A の humanScale を設定する（技の移動距離が A の humanScale 倍に変わるので、AGF で調整済みの技の距離も変わる）

## CM-01000: ISS-00051 の調査ログと低 FPS 対策を入れて削除（差し引きなし）

- コミット: `4116a79b0`「ISSと低FPS時のすり抜け対応」→ `3cac306b8`「ISS51のログを一旦削除」
- 背景: AGF の ISS-00051（飛びかかり技の途中で、Player が背の低い敵のコライダーに乗り上げる）の調査
- `4116a79b0` で入れた内容
  - `USE_CATHUT_DEBUG` が有効なときだけ動く調査ログ（`Iss00051Logger`）。`OnControllerColliderHit` と Move の前後で、想定外の上昇を記録する
  - `CharacterBrain` の Warp 処理: CharacterController を一時的に無効にして座標を直接書き換えていたのを、`skinWidth` ごとに分けて `Move` する形に変更。低 FPS で壁を抜けるのを防ぐため
- `3cac306b8` で、ログだけでなく **Warp の分割移動も含めて元に戻している**。最終的な差分はない
- 検討点: 分割移動（低 FPS のすり抜け対策）も一緒に戻したのが意図的だったかは、コミットからは分からない。必要なら改めて検討する

## CM-01100: Arts 実行中に XZ 移動を止めるインターフェースを追加

- コミット: `00a607a07`「TSK120」（AGF の TSK-00120「Arts実行中のターゲット敵接触によるXZ移動キャンセル」。2026-05-25 完了）
- 変更
  - インターフェース `IArtsXzMoveBlocker`（`bool ShouldBlockXzMove { get; }`）を追加
  - `CharacterBrain` は Awake で同じ GameObject から `IArtsXzMoveBlocker` を探す。見つかって `ShouldBlockXzMove` が true のときは、`ApplyPosition` で速度の XZ 成分を 0 にし、`MovePosition` で XZ 座標を現在位置のままにする
- AGF での使い方: `ArtsXzMoveBlocker`（`CatHut.BattleSystem`）が実装している。技（Arts）の実行中にターゲットの敵と接触していれば止める。ISS-00051 の乗り上げ対策として入れたもの
- 影響: インターフェースを実装したコンポーネントがなければ、何も変わらない
- 検討点
  - インターフェース名とコメントに、AGF 固有の言葉（「Arts」「ターゲット敵」）が入っている。汎用パッケージに置くなら `IXzMoveBlocker` のような中立な名前とコメントに変えることを勧める。ただし AGF 側の実装クラスも合わせて変える必要がある
  - XZ を止める判定が毎回の移動で呼ばれるので、実装側の判定は軽くしておく必要がある（AGF の実装はプロパティを読むたびに判定する）

## CM-01200: 地面に止められて下がり切れなかった分を、その後の上昇から差し引く

- コミット: AGF `9b9a09b5c`（調査用ブランチ `UnityWorkspaceReference`）。Extended_Project_TCC には同じ内容を反映済み
- 背景: AGF で、上昇してから着地する技（`054_Spear_SkillJudgement` など）の着地位置が 0.3〜0.5m 浮き、モーションが終わると重力で地面まで落ちていた

### 原因

フレームごとのログで確かめた。対象の技の AnimatorMoveBehaviour は、Range 0〜1、Use Ground Normal は false、Is Fixed Position は true。

- このクリップのルートの Y は、着地のしゃがみで開始時より約 0.28 下がり、そのあと元の高さに戻る
- ルートモーションを毎フレームの差分として `CharacterController.Move` に渡すので、地面より下に行けなかった分は失われる。そのあとの立ち上がりの上昇だけが反映され、その分だけ浮く
- ステートの終わりで AnimatorMoveControl が外れると、Gravity が効いて落ちる
- 速度ベースの計算（CM-00400）は原因ではない。deltaPosition で動かしても同じように浮く

これは差分でルートモーションを適用する方式に共通の性質で、Unity 標準の `OnAnimatorMove` で `CharacterController.Move(animator.deltaPosition)` としても同じことが起きる。TCC には、衝突で軌道がずれたときの補正がなかった。

### 変更

- `AnimatorMoveControl` は、`IsFixedPosition` の区間で、前のフレームに Warp で要求した位置と実際の位置を比べる。要求より上で止められていれば、その差を未相殺分として貯める
- 未相殺分があるあいだは、上向きの移動量から差し引く。下向きと水平の移動はそのまま
- 貯めるのは、GroundCheck の `IsOnGround` が true のときだけ（GroundCheck がなければ常に貯める）。ワープなどで位置が飛んだ場合を誤って数えないため
- AnimatorMoveBehaviour の区間を抜けたとき、または `IsFixedPosition` でない区間に入ったときにリセットする
- Inspector 項目 `Compensate Blocked Descent`（初期値は有効）でオフにできる

### 影響

- `IsFixedPosition` の区間で、衝突で下降が止められたあとの上昇が、止められた分だけ小さくなる。止められなければ何も変わらない
- `IsFixedPosition` でない区間（速度で Brain に渡す区間）には補正が入らない。Gravity などほかの Effect と合成されるので、要求した位置を AnimatorMoveControl から特定できないため
- 天井などで上昇が止められた場合は補正しない

### 確認

- AGF の `054_Spear_SkillJudgement` で、着地後に浮かず、重力で落ちる動きもなくなることを確認した
- Extended_Project_TCC では、コンパイルが通ることだけを確認した（サンプルでの動作確認はしていない）
