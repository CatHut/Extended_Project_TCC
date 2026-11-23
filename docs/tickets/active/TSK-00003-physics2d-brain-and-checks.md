---
id: TSK-00003
title: 2D 物理（Collider2D）のステージで動く Brain と Check を追加する
status: 対応中
priority: MID
created: 2026-11-23
closed:
related:
  - docs/design/physics2d-support.md
  - Packages/com.utj.charactercontroller/Runtime/Components/Brain/CharacterBrain.cs
  - Packages/com.utj.charactercontroller/Runtime/Components/Check/GroundCheck.cs
  - Packages/com.utj.charactercontroller/Runtime/Components/Check/HeadContactCheck.cs
---

## 概要

TCC の Brain と Check は 3D 物理（PhysX）で当たり判定をしている。そのため、`BoxCollider2D` などの 2D のコライダーで作ったステージでは、地面をすり抜けて落ち続ける。`CharacterController.Move` も `GroundCheck` の判定も、2D のコライダーを見ていないためである。

横から見た 2D のアクションゲームで TCC を使えるように、2D 物理で当たり判定をする Brain と Check を追加する。物理シミュレーションは使わない。

## 方針

設計は [physics2d-support.md](../../design/physics2d-support.md) に書く。要点は次のとおり。

- 既存のコードは変更しない。`BrainBase` の派生の `CharacterBrain2D` と、既存と同じインターフェースを実装した `GroundCheck2D`・`HeadContactCheck2D` を、別のアセンブリ（`Runtime/Extensions/Physics2DSupport/`）に追加する（physics2d-support.md#PHS-00300（構成））
- `CharacterBrain2D` は Kinematic の `Rigidbody2D` と `CapsuleCollider2D` を使い、`Physics2D.CapsuleCast` で当たったら止まる・面に沿って滑る移動を行う（physics2d-support.md#PHS-00400（CharacterBrain2D））
- 向きは、既定で左右の 2 方向に即座に切り替える（physics2d-support.md#PHS-00400（CharacterBrain2D > 向き（`ApplyRotation`）））
- `HeadContactCheck2D` も作るのは、`JumpControl` が `IOverheadDetection` を必須にしているため
- めり込みの押し出し・`WallCheck` の 2D 版などは、最初の版の確認後に判断する（physics2d-support.md#PHS-00700（未決事項））
- 互換性を壊さない機能追加なので、`package.json` のバージョンはパッチを上げる（0.7.1 → 0.7.2）。CHANGELOG に書く

## 完了条件

- [x] `CharacterBrain2D`・`GroundCheck2D`・`HeadContactCheck2D` と、アセンブリ定義を追加する
- [x] PlayMode テストを追加する（落下して `BoxCollider2D` の地面に着地する / 壁で止まる / ジャンプして天井で止まる / 向きが左右に切り替わる）
- [ ] このプロジェクトで、コンパイルエラーがなく、既存のテストを含めてすべて通る
- [ ] 2D のステージで、`MoveControl`・`JumpControl`・`Gravity` を組み合わせて、移動・ジャンプ・着地ができることを画面で確認する
- [x] `package.json` のバージョンを 0.7.2 に上げ、CHANGELOG に書く
- [x] `Documentations/Components/Runtime/` に3つのコンポーネントの文書（日英）を追加し、目次（`Componentlist_ja.md`・`Componentlist_en.md`）に載せる

## 作業ログ

- 2026-11-23: 作成。既存の Brain・Check・Control・Effect の 3D 物理への依存を調べた
  - Control と Effect は Check をインターフェースで使っており、具体的なクラスに依存していない。Brain と Check の 2D 版を足せば、そのまま使える
  - `JumpControl` は `IOverheadDetection` を null チェックなしで使うので、天井の判定の 2D 版も最初から必要
  - `MenuList` は internal なので、別のアセンブリからは使えない。コンポーネントメニューの文字列は 2D 版のアセンブリの中に持つ
- 2026-11-23: 完了条件に `Documentations/` の文書の追加を加えた。`Documentations/` は更新してよい（コンポーネントの変更に合わせて保守する）とルールを改めたため（CLAUDE.md の EPT-00200（ドキュメント））
- 2026-11-23: 実装した
  - 移動は `Rigidbody2D.Slide` を使わず、現在位置から `Physics2D.CapsuleCast` で判定する自前の処理にした。TCC は Update で Transform を動かす作りで、`Slide` は `Rigidbody2D` 側の位置を基準にするため、同期のずれを避けた
  - キャストの結果から、距離 0 の当たり（開始時に重なっている）と、移動方向を向いていない面を除外した。距離 0 を除外しないと、めり込んだときにどの方向にも動けなくなる
  - 向き（`SnapLeftRight`）は、補間後の回転ではなく、Turn の要求する向き（`BrainBase.YawAngle`）で判定した。`TurnSpeed` があっても、すぐに切り替わる
  - `UpdateTiming` が `Utj.BatchProcess` アセンブリにあるため、参照に加えた
  - 利用側のプロジェクト（2D の確認用）でコンパイルエラー・警告なし
  - PlayMode テスト（Physics2DSupportTest、7件）はすべて成功。`MoveControl` は速度が停止のしきい値を超えてから向きを要求するので、向きのテストは 0.2 秒待つようにした
  - 全体の PlayMode テストは 222 件中 217 件成功、5 件失敗。失敗は `BrainTest.移動("CharacterBrain")`・`BrainTest.回転`（2件、90 に対して 89.9998）・`MoveWithPlatformTest.地面を回す`（2件）。この 2 クラスだけを流しても同じ 5 件が失敗し、テストのファイルは元の Project_TCC のリリースから変わっていないので、今回の変更より前からの失敗と判断した。テストはバッチモード（`unity test`）で流した
