---
id: TSK-00001
title: 移動入力を MoveControl に渡す中継コンポーネントを TCC に追加する
status: 対応中
priority: MID
created: 2026-11-22
closed:
related:
  - docs/design/move-input-relay.md
  - Packages/com.utj.charactercontroller/Runtime/Components/Utility/MoveInputRelay.cs
  - Packages/com.utj.charactercontroller/Runtime/Extensions/InputSystemSupport/PlayerInputMoveSource.cs
  - Packages/com.utj.charactercontroller/Runtime/Extensions/InputSystemSupport/TinyCharacterController.InputSystem.asmdef
  - Packages/com.utj.charactercontroller/Tests/Scripts/MoveInputRelayTest.cs
  - Packages/com.utj.charactercontroller/Runtime/Components/Control/MoveControl.cs
  - Assets/TCC/Contents/ScriptGraph/PlayerController.asset
---

## 概要

TCC には、ユーザ入力などを `MoveControl` に渡す部品がなく、サンプルでは Visual Scripting のグラフで組んでいる。どのゲームでも必ず必要になる処理なので、最低限の中継コンポーネントを TCC に追加し、ゲーム固有の処理はゲーム側で追加する形にする。

## 方針

設計は [move-input-relay.md](../../design/move-input-relay.md) に書く。要点は次のとおり。

- 本体の `MoveInputRelay`（Input System に依存しない）と、Input System 用の標準の最小実装 `PlayerInputMoveSource` の2つに分ける（move-input-relay.md#MIR-00300（構成））
- 中継役は優先度と `MoveSpeed` に一切触れない。制御の受け渡しは TCC の優先度で行う（move-input-relay.md#MIR-00600（責務の境界））
- 拡張ポイントは `ProcessInput` と `Apply` だけにする。中継役の外に置ける処理は、継承せずに別のコンポーネントとして並べる（move-input-relay.md#MIR-00400（MoveInputRelay（本体） > 拡張ポイント））
- 互換性を壊さない機能追加なので、`package.json` のバージョンはパッチを上げる（0.7.0 → 0.7.1）。パッケージ直下に CHANGELOG.md を作り、変更内容を書く

## 完了条件

- [x] `MoveInputRelay` と `PlayerInputMoveSource` を実装する
- [x] `TinyCharacterController.InputSystem` の asmdef に `Utj.TinyCharacterController` への参照を追加する
- [x] `Tests` にテストを追加する（`SetInput` / 無効にしたときにゼロを送る / 軸の制限 / 閾値）
- [x] このプロジェクトで、コンパイルエラーがなく、テストが通る
- [x] `package.json` のバージョンを 0.7.1 に上げ、CHANGELOG に書く

## 作業ログ

- 2026-11-22: 作成。TCC サンプルの `PlayerController.asset` の Move（SubGraph）と、別プロジェクトでの中継処理の実装を比べて検討した
  - どれも、`MoveControl.Move()` に入力を渡すという中心の処理は同じ。違いは、入力の取得元・加工・移動させない条件・優先度の操作
  - 継承を前提にした基底クラスは採用しなかった。共通部分が十数行しかなく、変わる部分がそれぞれ別の方向に広がるため
  - 優先度の扱いは「中継役は優先度に触れない」に決めた。サンプルと同じく優先度を 0 にするオプションを付ける案は、優先度を別の処理で管理する構成と打ち消し合うので採用しなかった
- 2026-11-23: 設計を更新して実装した
  - `MoveControl` は入力の向きだけを使い、速度は `MoveSpeed` で指定する作りなので、`MoveSpeed` も中継役の対象外にした
  - PlayerInputMoveSource は標準の最小実装と位置づけ、拡張せずに置き換える方針にした。読み取るのは Vector2 のアクションだけにした
  - クラス名は `MoveInputRelay` / `PlayerInputMoveSource` に決めた。`SUPPORT_TCC` が必要なことは、クラスのコメントと CHANGELOG に書いた
  - 初版の設計書とチケットに、このリポジトリの外の情報（別プロジェクトのファイル名・クラス名、作業環境の規約）を書いていたので削除した
  - コンパイルエラーなし。`MoveInputRelayTest`（PlayMode、10件）はすべて成功。`PlayerInputMoveSource` はコンパイルされることだけ確認し、実際の入力での動作はまだ確認していない
