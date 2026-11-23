# CLAUDE.md（Extended_Project_TCC）

Project_TCC（Tiny Character Controller）を Unity 6.6 向けに更新・拡張するリポジトリ。パッケージ本体は `Packages/com.utj.*`。

## EPT-00100: 公開リポジトリ

**このリポジトリは GitHub で公開している。** コミット・push の前に [docs/public-repo-policy.md](docs/public-repo-policy.md) に従って内容を確認する。特に次の点に注意する。

- 認証情報・個人情報・ローカルの絶対パスをコミットしない
- 非公開リポジトリ（ワークスペース内のほかのリポジトリを含む）のコード・パス・コミットハッシュ・チケット番号を、文書・コメント・コミットメッセージに書かない
- コミット前に public-repo-policy.md#PUB-00400（コミット前の確認手順）を行う

## EPT-00200: ドキュメント

| 置き場所 | 内容 |
|---|---|
| `Documentations/` | コンポーネントのリファレンスとチュートリアル（元の Project_TCC から引き継いだもの）。コンポーネントを追加・変更したら合わせて更新する |
| `README.md`・`CONTRIBUTING.md` | 元の Project_TCC から引き継いだ文書。必要なら更新してよい |
| `LICENSE` | 変更しない |
| `docs/` | このリポジトリで追加した設計書・チケット・指針 |

### Documentations/ の更新

- コンポーネントを追加したら、`Documentations/Components/Runtime/<クラス名>_ja.md` と `_en.md` を作り、目次（`Documentations/Componentlist_ja.md`・`Componentlist_en.md`）の該当する分類に追記する
- 既存のコンポーネントの公開 API・Inspector の項目・挙動を変えたら、そのコンポーネントの文書（日英とも）を直す
- 書き方は既存の文書に合わせる（見出し: Namespace・概要・機能と操作・プロパティ・メソッドなど）。項番は付けない（元の文書の形式を保つため）
- 既存のファイルは文字コード（BOM 付き UTF-8）を変えない
- README.md の License Notice など、ライセンスとクレジットの表記は消さない（public-repo-policy.md#PUB-00700（ライセンス））

## EPT-00300: テストの実行

- **PlayMode テストは、このプロジェクトを Editor で開いて流す。** Editor が起動していれば、`unity command test run --mode playmode --async-tests --project-path <このプロジェクトのフルパス>` で開始し、`unity command test status --detail failures --json --project-path <同じパス>` で `"status": "completed"` になるまで結果を取る
- **バッチモード（`unity test`。Editor を開かずに流す）の結果だけで、失敗と判断しない。** バッチモードでは次の 5 件が失敗するが、Editor ではすべて成功する（2026-11-23 確認。原因は未調査）
  - `BrainTest.移動("CharacterBrain")`
  - `BrainTest.回転("CharacterBrain")`・`BrainTest.回転("TransformBrain")`
  - `MoveWithPlatformTest.地面を回す`（TransformBrain・CharacterBrain）
- バッチモードで失敗が出たら、Editor で流し直してから判断する。Editor が開いていなければ、ユーザに開いてもらう
- このプロジェクトを開くと、`Assets/AddressableAssetsData/AssetGroups/Built In Data.asset` が書き換わる（バッチモードでも Editor でも）。作業と関係ない差分なので、コミットに含めない
