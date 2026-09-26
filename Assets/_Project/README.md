# プロジェクトのファイル案内

ゲーム本体のコードは `Assets/_Project/Scripts`、編集専用ツールは `Assets/_Project/Editor` にあります。以前の `Prototypes/RobotBattle/Scripts` は役割ごとに移動しました。

|フォルダー|担当|
|---|---|
|Scripts/Battle|BattleManager、準備、開始・終了、バランス|
|Scripts/Development|初期機体、国家選択、CP開発、設計図|
|Scripts/Combat|戦場の1体、ダメージ計算、能力計算|
|Scripts/Units|配備・所属色、機体の見た目、敵増援の位置|
|Scripts/Maps|マップ種類とタイル配置|
|Scripts/Movement|地形適性による移動|
|Scripts/Network|LAN接続・操作同期・送受信データ|
|Scripts/UI|タイトル、編集できる画面、鉄板・ねじの装飾|
|Scripts/Camera・Common・System|既存のカメラ・共通処理・管理の土台|
|Editor|Unity専用の性能調整ウィンドウ等|
|Art/Maps・Robots・Fonts|地形・機体・フォント素材|
|Prefabs/System/BattleManager.prefab|MainSceneとrobotdevelopで共用するゲーム設定・UI|
|Docs/Battle|操作・調整・素材・LANの説明|
|Demos/TerrainMovement|本編から独立した移動確認デモ|

普段は `Scripts/Battle/BattleManager.cs` から読みます。関連するpartialファイルは同じBattleManagerクラスの一部分で、別々のコンポーネントを追加する必要はありません。

`BattlePreview.cs` は同じpartialクラスに属するため、Editorフォルダーではなく `Scripts/Battle/Editing` にあります。全体をUNITY_EDITORで囲んでおり、プレイヤービルドには入りません。

旧画面の `StatusPointManager`／`MainUIManager`／`LobbyUIManager` は既存参照を保つため残しています。現在の開発はDevelopScreen、直接IP接続はLanConnectが担当します。

開始は `Assets/Scenes/Title.unity`。素材と性能は共通PrefabのInspector、またはTools → Robot Strategy → Balance Settingsから調整します。

改名対応表は [ScriptNames.md](Docs/Battle/ScriptNames.md)、画像差し替えは [ArtworkGuide.md](Docs/Battle/ArtworkGuide.md)、LAN接続は [LAN対戦ガイド.md](Docs/Battle/LAN対戦ガイド.md) を参照してください。
