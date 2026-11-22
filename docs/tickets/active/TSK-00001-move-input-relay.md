---
id: TSK-00001
title: 移動入力を MoveControl に渡す中継コンポーネントを TCC に追加する
status: 検討中
priority: MID
created: 2026-11-22
closed:
related:
  - docs/design/move-input-relay.md
  - Packages/com.utj.charactercontroller/Runtime/Components/Control/MoveControl.cs
  - Packages/com.utj.charactercontroller/Runtime/Extensions/InputSystemSupport/TinyCharacterController.InputSystem.asmdef
  - Assets/TCC/Contents/ScriptGraph/PlayerController.asset
---

## 概要

TCC には、ユーザ入力などを `MoveControl` に渡す部品がなく、サンプルでは Visual Scripting のグラフで組んでいる。どのゲームでも必ず必要になる処理なので、最低限の中継コンポーネントを TCC に追加し、ゲーム固有の処理はゲーム側で追加する形にする。

## 方針

設計は [move-input-relay.md](../../design/move-input-relay.md) に書く。要点は次のとおり。

- 本体の `MoveInputRelay`（Input System に依存しない）と、Input System 用の `PlayerInputMoveSource` の2つに分ける（move-input-relay.md#MIR-00300（構成））
- 中継役は優先度に一切触れない。制御の受け渡しは TCC の優先度で行う（move-input-relay.md#MIR-00600（責務の境界））
- 拡張ポイントは `ProcessInput` と `Apply` だけにする。中継役の外に置ける処理は、継承せずに別のコンポーネントとして並べる（move-input-relay.md#MIR-00400（MoveInputRelay（本体） > 拡張ポイント））
- 互換性を壊さない機能追加なので、`package.json` のバージョンはパッチを上げる（0.7.0 → 0.7.1）。CHANGELOG にも書く（Shared/README.md#SH-00400（バージョンとタグ））

## 完了条件

- [ ] move-input-relay.md#MIR-00800（未決事項）が決まっている
- [ ] `MoveInputRelay` と `PlayerInputMoveSource` を実装する
- [ ] `TinyCharacterController.InputSystem` の asmdef に `Utj.TinyCharacterController` への参照を追加し、`python scripts/deps.py --check` で ERROR がない
- [ ] `Tests` に EditMode テストを追加する（`SetInput` / `OnDisable` でゼロを送る / 軸の制限 / 閾値）
- [ ] Extended_Project_TCC で、コンパイルエラーがなく、テストが通る
- [ ] `package.json` のバージョンを 0.7.1 に上げ、CHANGELOG に書く
- [ ] TCC を `file:` で参照しているほかのプロジェクトを、`deps.py` の逆引きで確認して報告する（WR-00400（リポジトリ横断作業のルール））

## 作業ログ

- 2026-11-22: 作成。ActionGameFramework の `SM_Move.cs` / `SM_FreeRun.cs` と、TCC サンプルの `PlayerController.asset` の Move（SubGraph）を比べて検討した
  - 3つとも、`MoveControl.Move()` に入力を渡すという中心の処理は同じ。違いは、入力の取得元・加工・移動させない条件・優先度の操作
  - 継承を前提にした基底クラスは採用しなかった。共通部分が十数行しかなく、変わる部分がそれぞれ別の方向に広がるため
  - 優先度の扱いは「中継役は優先度に触れない」（案A）に決めた。サンプルと同じく優先度を 0 にするオプションを付ける案（案B）は、AGF の ArtsControl のように優先度を管理する処理と打ち消し合うので採用しなかった
  - `MoveControl` は入力の向きだけを使うので、AGF の `SM_FreeRun` の入力を縮小する処理は速度に影響しない
