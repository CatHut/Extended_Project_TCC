---
id: TSK-00002
title: Brain の Inspector に TCC コンポーネントの一覧と実行時の情報を表示する
status: 完了
priority: MID
created: 2026-11-22
closed: 2026-11-22
related:
  - Packages/com.utj.charactercontroller/Editor/CustomEditor/BrainEditor.cs
  - Packages/com.utj.charactercontroller/Editor/CustomEditor/BrainComponentsView.cs
  - Packages/com.utj.charactercontroller/Editor/CustomEditor/BrainRuntimeView.cs
---

## 概要

TCC で動かすキャラクタには、Brain・Settings・Control・Effect・Check のコンポーネントを同じ GameObject に並べる。TCC のものだけでも10個前後になり、Inspector が縦に長くなる。そのため、どのコンポーネントが何の役割か、どれが優先されているかが分かりにくい。

コンポーネントの数は構造上減らせない。Brain は、同じ GameObject の `GetComponents` で IMove / ITurn / IEffect などの実装を集め、Priority で選んで合成する。役割ごとに1コンポーネントに分かれていることが、この仕組みの前提になっている。コンポーネントを統合すると、組み合わせの自由度が失われる。そこで、ランタイムの構造は変えず、エディタ側で一覧と状態を見やすくする。

## 方針

ランタイムのコードは変更しない。Brain のカスタムエディタ（`BrainEditor`）を UI Toolkit で作り直し、Brain の設定の下に次の2つを表示する。

### 編集時の一覧（`BrainComponentsView`）

- 同じ GameObject の TCC コンポーネントを Core / Control / Effect / Check / Other に分けて表示する
- TCC 以外のクラスでも、IMove / ITurn / IEffect を実装していれば一覧に含め、`(custom)` と表示する
- 行ごとに、有効・無効の切り替えと、Move / Turn の Priority の設定値を編集できる（Undo 対応）
  - 設定値のフィールド名はコンポーネントごとに違う（`_movePriority`、`MovePriority`、`Priority` など）。名前に priority を含む int のフィールドを探し、名前に move / turn が含まれるかで列を決める。どちらも含まれない場合は、実装しているインターフェースの列に入れる
  - 該当するインターフェースを実装していないセルは `—`、フィールドが見つからないセルは `?` と表示する
- 同じ Priority の値が複数のコンポーネントにある場合に警告する。同時に有効になると、コンポーネントの並び順で先のものが選ばれるため
- 行の名前をクリックすると、そのコンポーネントの Inspector をその場に展開する
- Add メニュー（TCC の Control / Effect / Check の追加）と、各コンポーネントの Inspector をまとめて開閉するボタンを付ける
  - コンポーネントの個別表示は、HideFlags で隠すとシーンに保存されてしまうため隠さない。代わりに、Inspector の開閉状態（ユーザごとの設定）をまとめて切り替える

### 実行時の情報（`BrainRuntimeView`）

Play 中だけ表示する。値はすべて公開インターフェース（IBrain、IEffect、IGroundContact、IOverheadDetection）から読む。

- 現在の Move / Turn と、候補ごとの実効 Priority（棒グラフ付き）
- 速度の内訳（Control / Effect / 各 Effect / Total）
- 接地判定と頭上判定の状態
- 最優先の Move / Turn が切り替わった履歴（直近8件）。100ms ごとに確認しているので、それより短い間の切り替わりは記録されないことがある

## 完了条件

- [x] `BrainEditor` を作り直し、`BrainComponentsView` と `BrainRuntimeView` を追加する
- [x] コンパイルエラー・警告がない
- [x] サンプルシーン（UC-09-03_MetroidVania）の Player で、分類と Priority の列が正しく表示される
- [x] Play 中に、実効 Priority の変化と切り替わり履歴が表示される
- [x] Editor で見た目と操作（展開・一括開閉・Add メニュー）を確認する

## 作業ログ

- 2026-11-22: 作成（記録のため、実装・確認の後に作成）
  - コンポーネントの数を構造上減らせるかを検討した。Brain が同じ GameObject からインターフェースを集める仕組みのため、減らすと組み合わせの自由度が失われる。エディタ側で整理する方針にした
  - UC-09-03_MetroidVania の Player で確認した。11個のコンポーネントが Core 1 / Control 4 / Effect 2 / Check 4 に分かれ、JumpControl と MoveControl の Move Priority がどちらも 5 だという警告が出た
  - Play 中に LookTargetControl の Target を設定すると、実効 Priority が 0 から 10 に変わり、現在の Turn と切り替わり履歴に反映された
  - 実装をコミット・push した。コミットメッセージには「Editor での見た目の確認は未実施」とあるが、その後ユーザが Editor で確認し、問題がなかった
