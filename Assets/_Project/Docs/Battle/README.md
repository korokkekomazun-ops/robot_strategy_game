# ロボット戦闘システム

砂地・川付きマップと10秒ごとの集団増援は [マップ・増援ガイド](MapsAndWaves.md) を参照してください。

4人でのLAN対戦は [LAN対戦ガイド](LAN対戦ガイド.md) を参照してください。以下の開始手順はNPCと確認するオフライン用です。

## 開始する場所
Assets/Scenes/robotdevelop.unityを開いて再生します。
森林マップ → 国家 → 機体の種類 → 初期ステータス → MainScene → 3・2・1 → 戦闘 → リザルト。
出撃準備では敵やマップを生成しません。確定した国家・機体種類・配分値をMainSceneへ一度だけ渡します。
MainSceneだけを直接再生した場合も、動作確認用の準備画面が表示されます。

## 見た目を編集する場所
Assets/_Project/Prefabs/System/BattleManager.prefabをダブルクリックし、Battle UIを開きます。
robotdevelopとMainSceneはこの共通Prefabを参照しています。
各シーンのHierarchyで編集する場合はPrefabのOverrideになります。両方へ反映したい変更はPrefab側で行ってください。
新しい画面が見つからない場合、robotdevelopでTools > Robot Strategy > Create Editable Battle UIを実行できます。
元のrobotdevelopのCanvasは削除せず、非表示にしています。

## スクリプトを読む順番
1. Assets/_Project/Scripts/Battle/BattleSetup.cs：準備画面の切り替え、出撃先のMainSceneへの移動、初期機体の配備。
2. Assets/_Project/Scripts/Battle/BattleStart.cs：国家と設計をシーン間で渡す、小さな一時保存。
3. Assets/_Project/Scripts/Battle/BattleFlow.cs：マップ選択の表示、カウントダウン、リザルト。
4. Assets/_Project/Scripts/Battle/BattleManager.cs：マップ生成、戦闘、クリック移動。
5. Assets/_Project/Scripts/Development/CountryScreen.cs / BuildScreen.cs：国家・種類・ポイント・CPの処理。
6. その他のUI/Artworkファイル：画面生成、機械風装飾、仮の機体表示。

旧Prototypes/RobotBattle/Scriptsから、GUIDを維持してAssets/_Project/Scriptsの役割別フォルダーへ移動しました。
移動速度と地形判定は従来どおりAssets/_Project/Scripts/Movementです。

## 確認状況
Unity参照DLLによるコンパイル、Prefabとシーン内の参照整合性、保存後の一致を確認済みです。
Unityの実際のシーン遷移・Prefabインポート・Play操作は未確認です。
現状のBuild ProfilesにはMainSceneが登録済みです。Title／robotdevelop／MainSceneがシーン一覧へ登録されています。