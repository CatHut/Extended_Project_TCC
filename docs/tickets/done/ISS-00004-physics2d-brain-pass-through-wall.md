---
id: ISS-00004
title: CharacterBrain2D が壁にわずかにめり込むと、その壁を通り抜ける
status: 完了
priority: HIGH
created: 2026-11-24
closed: 2026-11-24
related:
  - docs/design/physics2d-support.md
  - Packages/com.utj.charactercontroller/Runtime/Extensions/Physics2DSupport/CharacterBrain2D.cs
  - Packages/com.utj.charactercontroller/Tests/Scripts/Physics2DSupportTest.cs
---

## 概要

`CharacterBrain2D` で動くキャラクターが、段差の壁（`BoxCollider2D`）の中に入り込んでしまうことがある。一度入り込むと、壁の中を自由に動ける。

## 方針

### 再現手順

1. 左の面が x = 5 の `BoxCollider2D` の壁と、地面を置く
2. 半径 0.5 のキャラクターを x = 4.51（壁に 0.01 めり込んだ位置）に置き、右へ移動の入力を続ける

### 期待動作

壁の手前（x ≒ 4.49）で止まる。

### 実際の動作

壁の中を通り抜けて、反対側の壁まで進む。めり込みが 0.002 程度なら止まる。

### 原因

カプセルを飛ばす判定（`CastCapsule`）は、距離 0 の当たり（キャストの開始時にすでに重なっているコライダー）を除外している。めり込んだときにどの方向にも動けなくなるのを防ぐためである（physics2d-support.md#PHS-00400（CharacterBrain2D > 位置の更新（`ApplyPosition`）））。しかし、めり込みを解消する処理がないため、一度めり込むと、そのコライダーは判定から外れたままになる。

最初にめり込むきっかけは特定できていない。ほかの Kinematic のコライダーが動いてきてめり込む場合も、同じ経路で通り抜ける（physics2d-support.md の未決事項にあった「めり込みの押し出し」）。また、地面に立っているときに足元が地面の上面より約 0.005 下にあった（吸い付けの補正でわずかに沈んでいた）。

### 修正

移動の前に、重なっている環境のコライダーから押し出す処理（`Depenetrate`）を加えた。`Physics2D.OverlapCapsule` で重なりを探し、`Physics2D.Distance` でいちばん深いものを選んで、最短の方向へ押し出し、`_skinWidth` のすき間を空ける（最大 4 回）。

- `Physics2D.Distance` は `Rigidbody2D` の姿勢を使うので、判定の前に自分の `Rigidbody2D.position` を判定する位置に合わせる
- 動く Kinematic のコライダーにめり込まれたときも押し出される（physics2d-support.md の未決事項から外した）
- 地面に立っているときの足元は、地面の上面からすき間（`_skinWidth`）の分だけ上になった

## 完了条件

- [x] 再現手順で、壁の手前で止まる
- [x] テストを追加する（壁にめり込んだ位置から押し出され壁を抜けない・動く Kinematic のコライダーにめり込まれると押し出される）
- [x] PlayMode テストがすべて成功する（Editor で 224 件）
- [x] `Documentations/` の CharacterBrain2D（日英）と設計書を更新する
- [x] ユーザが実際の操作で確認する

## 作業ログ

- 2026-11-24: 作成・修正。原因を再現で確認し、押し出しの処理を加えた。PlayMode テスト 224 件がすべて成功。別プロジェクトでも、再現手順で止まることを確認した。不具合の報告と修正の承認はユーザから受けた。ユーザの確認待ち
- 2026-11-24: ユーザが実際の操作で確認（今のところ壁を通り抜けない）。完了
