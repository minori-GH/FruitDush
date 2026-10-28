# Fruit Dash

Unity・C#で個人開発した、スマートフォン向けの2Dアクションゲームです。
プレイヤーを操作し、フルーツを集めて制限時間内にスタート地点へ戻るとゲームクリアです。
集めたフルーツのスコアを競います。

## ファイル構成

| フォルダ | 内容 |
| --- | --- |
| [Scripts/](Scripts/) | ゲームに使用しているC#スクリプト |
| [docs/](docs/) | WebGL版の起動ページ、実行用ファイル |

## ゲームをプレイする

| 配信先 | 公開URL |
| --- | --- |
| WebGL版（GitHub Pages） | [ゲームをプレイ](https://minori-gh.github.io/FruitDush/) |
| iOS版（App Store） | [AppStore](https://apps.apple.com/jp/app/フルーツダッシュ-1分で持ち帰れ-60秒ランゲーム/id6772475963) |

## 開発環境

| 項目 | 内容 |
| --- | --- |
| ゲームエンジン | Unity6 |
| 開発言語 | C# |
| 開発体制 | 個人開発 |
| 主な使用パッケージ | TextMeshPro、Unity Localization、Google Mobile Ads SDK |

## 操作方法

| 操作 | 動作 |
| --- | --- |
| 短押しして離す | 地面にいるときにジャンプする |
| 長押しして離す | 左右の移動方向を切り替える |

## ゲーム詳細

- Startボタンを押すとプレイヤーが右方向へ自動で走り始めます。
- 長押しして離すと方向転換し、反対方向へ走ります。
- フルーツに触れるとスコアが加算されます。
- 制限時間は60秒で、スタート地点から一度離れてから時間内に戻るとクリア、時間切れになるとゲームオーバーになります。
- クリア画面では今回のスコアとベストスコアを表示し、記録を更新すると「NEW RECORD」が表示されます。
- ゲームオーバー時は、リワード広告を視聴することで残り時間が10秒追加されます。
- Retryボタンを押すと最初の画面へ戻ります。Retryを5回押すごとにインタースティシャル広告が表示されます。

## 主な実装機能

| 機能 | 内容 |
| --- | --- |
| 状態・時間管理 | 開始、プレイ中、ポーズ、ゲームオーバー、クリア状態の管理、制限時間の管理 |
| プレイヤー操作 | オートラン、短押しジャンプ、長押し後の方向転換、敵接触時のノックバック・操作停止 |
| カメラ | プレイヤーの進行方向の少し前を追従するカメラ |
| スコア | フルーツごとの得点加算、クリア時のベストスコア保存 |
| 敵 | 左右往復移動 |
| UI | 各状態での表示パネルの管理 |
| アニメーション | プレイヤーの移動・空中・被弾に応じたアニメーション、敵・アイテムのアニメーション |
| BGM・SE | BGM、ジャンプ・被弾・クリック・ゲームオーバー・クリア時の効果音再生 |
| ローカライズ | 10言語のローカライズ、言語に応じたフォント切り替え |
| 広告 | AdMobのインタースティシャル広告・リワード広告 |

## スクリプト構成

| スクリプト | 主な役割 |
| --- | --- |
| [GameManager.cs](Scripts/GameManager.cs) | ゲームの状態、残り時間、スコア、広告視聴後の復帰を管理 |
| [PlayerController.cs](Scripts/PlayerController.cs) | 入力、移動、ジャンプ、敵に触れたときの処理、アニメーション |
| [CameraFollow2D.cs](Scripts/CameraFollow2D.cs) | プレイヤーの進行方向に合わせて少し前を追従するカメラの管理 |
| [EnemyMove.cs](Scripts/EnemyMove.cs) | 敵を初期位置から左右に往復移動させる |
| [EnemyContact.cs](Scripts/EnemyContact.cs) | 敵との接触をPlayerControllerへ渡す |
| [FruitItem.cs](Scripts/FruitItem.cs) | フルーツ取得時に得点を加算し、効果音とスコア表示を呼び出す |
| [ScorePopup.cs](Scripts/ScorePopup.cs) | 「+スコア」を上へ動かし、一定時間後に消す |
| [StartGoalTrigger.cs](Scripts/StartGoalTrigger.cs) | スタート地点から離れた後、戻ってきたことを判定 |
| [UIManager.cs](Scripts/UIManager.cs) | 画面の切り替え、時間・スコアの表示、各ボタンの処理 |
| [TimeUIEffectController.cs](Scripts/TimeUIEffectController.cs) | 残り時間に応じた時計とカウントダウンの演出 |
| [ResultBestScoreUI.cs](Scripts/ResultBestScoreUI.cs) | クリア時のベストスコア保存、「NEW RECORD」の表示 |
| [AudioManager.cs](Scripts/AudioManager.cs) | 効果音の再生 |
| [LocalizedTMPFontSwitcher.cs](Scripts/LocalizedTMPFontSwitcher.cs) | 言語の変更に合わせたフォント切り替え |
| [AdManager.cs](Scripts/AdManager.cs) | 広告の読み込み、表示、終了後の処理 |

