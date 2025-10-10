# **Unityプロジェクト「Mitsunoazi」仕様書**

## **1\. プロジェクト概要**

複数のウェブカメラから入力された映像をキャプチャし、ユーザーが選択した**Status**（Crazy / Attacker / Blocker / Healer）を付与します。確定された画像は、対応する**タイムライン演出**で再生された後、**サブディスプレイ**に累積表示されます。  
---

## **2\. システム要件**

* **Unity:** 2022.3.6f1 (2D)  
* **ディスプレイ解像度:** 1920×1080（想定）  
* **ウェブカメラキャプチャ解像度:** 640×360

---

## **3\. 主要フロー**

1. **キャプチャ**: ユーザーが数字キー (0–9)を押すと、対応するカメラで映像キャプチャを開始し、状態が Processing へ移行します。  
2. **画像処理**: Unity内で輝度に基づき、画素の透過処理を行います。  
3. **一時保存**:  
   * **生画像**: Assets/StreamingAssets/ImageCapture/  
   * **透過後**: Assets/StreamingAssets/ImageStaged/  
4. **ステータス選択**: Processing 中、または処理完了後の SelectingStatus 状態中に、数字キーで **Status** を選択します。  
5. **確定**: A–Eキー（カメラ0–4に対応）で画像を確定します。  
   * Processing 中に押された場合は「**確定予約**」として保持し、処理完了時に自動実行されます。  
   * SelectingStatus 中に押された場合は即時実行されます。  
6. **最終保存**: ImageStaged/ 内のファイル名にステータス名を付与し、Assets/StreamingAssets/ImageConfirmed/ へ移動します。  
7. **タイムライン再生**: 確定したステータスに応じたプレハブ群からランダムに1つを選択し、キャプチャ画像（Sprite）を差し替えて演出を再生します。  
8. **サブディスプレイ表示**: タイムライン再生終了後、該当画像をサブディスプレイへ追加表示します。

---

## **4\. 機能仕様詳細**

### **4.1 入力（キーマップ）**

| 割り当て | キャプチャ開始、ステータス選択キ | 確定キー | 備考 |
| :---- | :---- | :---- | :---- |
| カメラ 0 | 0, 1 | A | 0: 正順, 1: 逆順 |
| カメラ 1 | 2, 3 | B | 2: 正順, 3: 逆順 |
| カメラ 2 | 4, 5 | C | 4: 正順, 5: 逆順 |
| カメラ 3 | 6, 7  | D | 6: 正順, 7: 逆順 |
| カメラ 4 | 8, 9  | E | 8: 正順, 9: 逆順 |

* **正順**: Crazy → Attacker → Blocker → Healer → ...  
* **逆順**: Crazy → Healer → Blocker → Attacker → ...

### **4.2 状態管理 (カメラ単位)**

* Ready  
  * 数字キー押下で Processing へ移行  
* Processing  
  * 数字キーで事前のステータス選択が可能  
  * 確定キーで**確定予約** (ConfirmationPending \= true)  
  * 画像処理完了後:  
    * 予約あり: 即時確定し Ready へ  
    * 予約なし: SelectingStatus へ  
* SelectingStatus  
  * 数字キーでステータス変更が可能  
  * 確定キーで確定処理を実行し Ready へ

### **4.3 画像処理・ファイル管理**

* **透過処理**: 輝度 (0.299×R+0.587×G+0.114×B) が上位10%以上の画素を透明化 (alpha=0) します。  
* **保存フォルダ** (Assets/StreamingAssets/):  
  * ImageCapture/: キャプチャした生のPNG画像  
  * ImageStaged/: 透過処理後の一次保存PNG画像  
  * ImageConfirmed/: ステータス確定後の最終保存PNG画像  
* **ファイル命名規則**:  
  * **一次保存**: webcam\_\<cameraIndex\>\_\<yyyyMMddHHmmss\>.png  
  * **最終保存**: webcam\_\<cameraIndex\>\_\<yyyyMMddHHmmss\>\_\<Status\>.png

### **4.4 タイムライン再生**

* ステータスごとに用意されたプレハブ群からランダムに1つを選択し、PlayableDirector で再生します。  
* TimelineInfo.ImageDisplayRenderer が参照する SpriteRenderer に、ロードしたキャプチャ画像のSpriteを設定します。  
* 再生停止時にサブディスプレイへ画像パスを通知し、自身のインスタンスを破棄します。

### **4.5 サブディスプレイ表示**

* アプリケーション起動時に ImageConfirmed/ フォルダ内の全PNG画像を列挙し、表示します。  
* タイムライン再生が停止するたび、AddNewImage(path) を介して画像を追加表示します。

---

## **5\. スクリプト構成と役割**

### **5.1 フォルダ構成 (主要部分)**

Assets/  
├── Medias/  
├── Prefabs/  
│   ├── ImageDisplaySub  
│   └── TimelineDisplay  
├── Scenes/  
│   └── Main  
├── Scripts/  
│   ├── Add/  
│   ├── Core/  
│   ├── DevOnly/      \# 現状スクリプトなし  
│   └── Sys/  
├── StreamingAssets/  
│   ├── ImageCapture/  
│   ├── ImageConfirmed/  
│   └── ImageStaged/  
└── Timeline/  
    ├── Ability/  
    └── Default/

### **5.2 主要スクリプトの役割 (Core)**

* WebcamCaptureManager.cs  
  * キー入力に基づき、WebCamTexture でキャプチャを実行し、生のPNG画像を保存します。  
* CaptureStateManager.cs  
  * カメラごとの状態 (Ready, Processing等) と確定予約を管理します。確定時にファイル移動とTimelinePlayerの呼び出しを行います。  
* ImageProcessor.cs  
  * 画像の透過処理とPNG形式での保存（同期/非同期）を担当します。また、ImageStaged から ImageConfirmed へのファイル移動も行います。  
* StatusManager.cs  
  * Status 列挙型の管理と、正順/逆順の循環選択APIを提供します。  
* TimelinePlayer.cs  
  * ステータスに応じたタイムラインプレハブをランダムに再生し、Spriteの差し替えやインスタンスの破棄を管理します。  
* TimelineInfo.cs  
  * タイムライン内でキャプチャ画像を表示するための SpriteRenderer への参照を保持します。

### **5.3 主要スクリプトの役割 (Sys)**

* CleanupAssetsOnQuit.cs  
  * Escキーでのアプリケーション終了と、終了直前の一次保存フォルダ (ImageCapture, ImageStaged) のクリーンアップを行います。  
* SubDisplayManager.cs  
  * サブディスプレイを有効化し、ImageConfirmed フォルダ内の画像を表示・管理します。  
* BgmLooper.cs  
  * BGMのループ再生と、マウスホイールによる音量調整（新/旧Input System対応）機能を提供します。

### **5.4 主要スクリプトの役割 (Add)**

* SubImagePosition.cs  
  * サブディスプレイ上の画像の初期座標を記録します。  
* SubImageMotion.cs  
  * Perlinノイズを用いて、記録された座標を基準に画像を微小に動かし続けます。  
* SubImageStart.cs  
  * サブディスプレイに画像が登場する際のアニメーション（フェードイン、スケール等）を制御します。

---

## **6\. 設計原則と開発規約**

* **仕様の遵守** \* 本仕様書に記載された内容を正とし、許可なく仕様を変更・追加しないこと。  
* **解釈の確認** \* 仕様に不明瞭な点や解釈の余地がある場合は、自己判断で実装を進めず、必ず事前に確認を行うこと。  
  * 実装に着手する際は、どのように仕様を解釈したかを逐一報告することが望ましい。

