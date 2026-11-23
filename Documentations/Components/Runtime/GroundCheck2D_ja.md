# GroundCheck2D

#### **Namespace**: Unity.TinyCharacterController.Check
---

## 概要:
`GroundCheck2D` は、2D のコライダーで作ったステージで地面を検出するコンポーネントです。`GroundCheck` と同じ判定を `Physics2D`（真下に向けた円のキャスト）で行います。Inspector の項目と、`IGroundContact`・`IGroundObject` で返す値の意味は `GroundCheck` と同じです。このコンポーネントの計算は `OnUpdate` のタイミングで実行されます。`CharacterBrain2D` と組み合わせて使います。

## 機能と操作:
- **地面検出**: 2D のコライダーとの接触を検出し、接地しているかどうかを判断する。判定の対象は `CharacterSettings.EnvironmentLayer` のレイヤーで、トリガーと自分のコライダーは除く。
- **地面接触情報の取得**: 地面までの距離、接触面の向き、接触点、地面のオブジェクトを提供する。
- **地面オブジェクトの変更通知**: 地面オブジェクトが変わったときに Unity イベントを発行する。`MoveWithPlatform` と組み合わせて使える。

## 注意:
- `IGroundObject.GroundCollider`（3D の `Collider`）は常に `null` を返す。2D のコライダーは `GroundCollider2D` で取得する。

## プロパティ
| Name | Description |
|------------------|------|
| `_ambiguousDistance` | 地面に接触していると認識される最大距離（曖昧な地面検出に使用）。 |
| `_preciseDistance` | 地面に接触していると厳密に認識される距離。 |
| `_maxSlope` | 地面と認識される最大の傾斜角度。 |
| `_onChangeGroundObject` | 地面のオブジェクトが変わった際に呼び出される Unity イベント。 |
| `IsOnGround` | 地面に接触しているかどうかを示す。 |
| `IsFirmlyOnGround` | 厳密に地面に接触しているかどうかを示す。 |
| `GroundCollider2D` | 現在の地面の 2D コライダー。 |
| `GroundSurfaceNormal` | 地面表面の向き。 |
| `GroundContactPoint` | 地面との接触点。 |
| `DistanceFromGround` | 地面までの距離。 |
| `GroundObject` | 地面オブジェクト。 |
| `IsChangeGroundObject` | 地面オブジェクトが現在のフレームで変更されたかどうか。 |
| `OnChangeGroundObject` | 地面のオブジェクトが変わった際に呼び出される Unity イベント。 |

## メソッド
| Name | Function |
|------------------|------|
| ``public bool`` Raycast( ``Vector3 position, float distance, out RaycastHit2D hit`` ) | キャラクターのコライダーを無視して、真下に 2D のレイキャストを行う。 |
| ``public void`` GatherOwnColliders() | キャラクター配下の 2D コライダーを集め直す。 |
