# スクリプトの改名と移動

ファイル名は拡張子を除いて10〜15文字です。主要なクラス名も同じ名前へ変更しました。Unity標準のコールバック名、保存済みの設定名、通信データの項目名は維持しています。

## まず覚える名前

|名前|日本語での役割|
|---|---|
|BattleManager|戦闘全体の管理|
|BattleUnit|戦場にいる1体|
|BattleStatus|機体の基本能力と相性|
|BattleDamage|ダメージ計算|
|BattleStart|開発画面から戦闘への受け渡し|
|BattleSetup|出撃前の画面切り替え|
|BuildScreen|戦闘中の開発画面|
|CountryScreen|国と初期機体の選択画面|
|CountryRules|A〜D国の特典|
|DesignList|設計図一覧とCP|
|PlaceRobots|四隅への機体配置|
|EnemyGroup|敵の集団出現|
|LanConnect|同じLANへの接続|
|OnlineBattle|4人対戦の同期|
|PlayerSeats|参加順と四隅の割り当て|
|NetworkData|通信で送る情報の形|
|ScreenEditor|再生前に編集できるUI|
|StatusWindow|Unityの性能調整画面|

`Battle`は戦闘、`Unit`は1体、`Status`は能力、`Screen`は画面、`Rules`はルール、`List`は一覧、`Place`は配置、`Connect`は接続という意味です。

|以前のファイル|現在のパス|
|---|---|
|BattleDesignCatalog.cs|Assets/_Project/Scripts/Development/DesignList.cs|
|BattleMapLayout.cs|Assets/_Project/Scripts/Maps/MapPattern.cs|
|BattleSession.cs|Assets/_Project/Scripts/Battle/BattleStart.cs|
|CombatDamage.cs|Assets/_Project/Scripts/Combat/BattleDamage.cs|
|ForestBalance.cs|Assets/_Project/Scripts/Battle/BattleStatus.cs|
|ForestBattleDevelopment.cs|Assets/_Project/Scripts/Development/BuildScreen.cs|
|ForestBattleFlow.cs|Assets/_Project/Scripts/Battle/BattleFlow.cs|
|ForestBattleMaps.cs|Assets/_Project/Scripts/Maps/BattleMaps.cs|
|ForestBattlePrototype.cs|Assets/_Project/Scripts/Battle/BattleManager.cs|
|ForestChassisArtwork.cs|Assets/_Project/Scripts/Units/RobotImages.cs|
|ForestEditableUI.cs|Assets/_Project/Scripts/UI/ScreenEditor.cs|
|ForestEditorPreview.cs|Assets/_Project/Scripts/Battle/Editing/MapPreview.cs|
|ForestLanBattle.cs|Assets/_Project/Scripts/Network/OnlineBattle.cs|
|ForestMechanicalUI.cs|Assets/_Project/Scripts/UI/MetalScreen.cs|
|ForestNationDeployment.cs|Assets/_Project/Scripts/Units/PlaceRobots.cs|
|ForestNationFlow.cs|Assets/_Project/Scripts/Development/CountryScreen.cs|
|ForestPreparation.cs|Assets/_Project/Scripts/Battle/BattleSetup.cs|
|LanBattleConnection.cs|Assets/_Project/Scripts/Network/LanConnect.cs|
|LanBattleProtocol.cs|Assets/_Project/Scripts/Network/NetworkData.cs|
|LanSeatOrder.cs|Assets/_Project/Scripts/Network/PlayerSeats.cs|
|MechanicalRivet.cs|Assets/_Project/Scripts/UI/ButtonScrew.cs|
|NationRules.cs|Assets/_Project/Scripts/Development/CountryRules.cs|
|PrototypeCombatUnit.cs|Assets/_Project/Scripts/Combat/BattleUnit.cs|
|PrototypeUIElement.cs|Assets/_Project/Scripts/UI/ScreenPart.cs|
|RobotBalanceWindow.cs|Assets/_Project/Editor/StatusWindow.cs|
|UnitStatMath.cs|Assets/_Project/Scripts/Combat/StatusMath.cs|
|WaveSpawnRules.cs|Assets/_Project/Scripts/Units/EnemyGroup.cs|

## 型名

|旧クラス・型名|新しい名前|
|---|---|
|ForestBattlePrototype|BattleManager|
|BattleDesignCatalog|DesignList|
|BattleDesign|RobotDesign|
|BattleMapLayout|MapPattern|
|BattleSession|BattleStart|
|CombatDamage|BattleDamage|
|ChassisBalance|RobotStatus|
|ChassisMatchup|AttackMatch|
|LanBattleConnection|LanConnect|
|PrototypeCombatUnit|BattleUnit|
|PrototypeUnitSettings|UnitSettings|
|PrototypeTeam|BattleTeam|
|PrototypeUIElement|ScreenPart|
|RobotBalanceWindow|StatusWindow|
|BattleScenePreview|ScenePreview|
|MechanicalRivet|ButtonScrew|
|LanSeatOrder|PlayerSeats|
|NationRules|CountryRules|
|UnitStatMath|StatusMath|
|WaveSpawnRules|EnemyGroup|
|WaveSpawnOffset|EnemyPoint|
|LanCommand|PlayerOrder|
|LanDesign|DesignInfo|
|LanPlayer|PlayerInfo|
|LanUnit|UnitReport|
|LanSnapshot|BattleState|

名前空間はRobotStrategy.PrototypeからRobotStrategy.Battleへ変更しました。共通PrefabはAssets/_Project/Prefabs/System/BattleManager.prefabです。Prefabと各スクリプトのGUIDを維持し、シーンの参照先と保存された設定を引き継いでいます。
