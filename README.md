# puzzle-2048

2048 パズルゲーム — Unity 6 / WebGL

**Play:** https://naullama.github.io/puzzle-2048/

## 操作方法

| キー | 動作 |
|------|------|
| ↑ | 上に移動 |
| ↓ | 下に移動 |
| ← | 左に移動 |
| → | 右に移動 |

## 仕様

- 4×4 グリッド
- 同じ数字が接触すると 2 倍に合成
- 2048 を目指す
- 動けなくなるとゲームオーバー

## 開発

- Unity 6000.0.23f1
- WebGL ビルド → GitHub Pages 自動デプロイ
- CI: GitHub Actions (`game-ci/unity-builder`)

## セットアップ (初回)

GitHub Actions で Unity をビルドするには、以下のシークレットが必要です:

1. **`UNITY_LICENSE`** — Unity の個人ライセンス XML  
   取得方法: `game-ci/unity-request-activation-file` アクションを実行
2. **`UNITY_EMAIL`** — Unity アカウントのメールアドレス
3. **`UNITY_PASSWORD`** — Unity アカウントのパスワード

Settings → Secrets and variables → Actions に設定してください。
