# 素材の差し替え方

今回の設定（2026-09-26）

|用途|Project内の素材|BattleManagerの欄|
|---|---|---|
|森林マップの地面|Assets/_Project/Art/Maps/地面.gif|Forest Sprite|
|砂地・砂漠|Assets/_Project/Art/Maps/nomal (2).gif|Sand Sprite|
|別候補の砂地（未割当）|Assets/_Project/Art/Maps/New Piskel (1).gif|必要ならSand Spriteへ差替え|
|戦闘機|Assets/_Project/Art/Robots/warplane.gif|Fighter Sprite|
|船|Assets/_Project/Art/Robots/battleship_2024_2.gif|Ship Sprite|

5枚とも32×32・1コマです。GIFの元画像は描き換えず使用しています。Pointフィルター、無圧縮、Mip Mapsなし、32 Pixels Per Unit、32×32の全範囲で取り込んでいます。

「nomal (2)」には上側に水色の縁があるため、砂漠一面ではその縁も繰り返されます。これは画像内の色であり、水地形ではありません。縁のない砂だけにしたい場合は、同梱の「New Piskel (1)」へ差し替えられます。移動適性は画像名・色ではなくタイルの地形種類で決まります。

## 自分で機体画像を変える

1. 再生を停止します。
2. ProjectでAssets/_Project/Prefabs/System/BattleManager.prefabを選択します。Prefab Modeで開いた場合は一番上のBattleManagerを選びます。
3. Inspectorの「Chassis sprites」を探します。
4. 画像アセットの左の三角を開き、子のSprite（例：warplane_0）を該当欄へドラッグします。
5. Prefabを保存します。MainSceneではTools → Robot Strategy → Preview Battle in MainSceneで見本を更新できます。
6. Titleから再生し、国・機体を選んで出撃します。画像は既存の移動処理に乗るため、画像用に移動スクリプトを書く必要はありません。

Tank Sprite＝戦車、Fighter Sprite＝戦闘機、Ship Sprite＝船、Transformer Sprite＝変形機体、Humanoid Sprite＝人型、Giant Sprite＝巨大機体です。今回は専用素材がある戦闘機と船を設定しました。ほかの種類には引き続き仮の形状が出ますが、同じ操作で画像へ交換できます。

素材画像を置くだけでは戦場に配置されません。上記Sprite欄への割当が必要です。シーン上のPrefabインスタンスに設定の上書きがあると、共通Prefabよりインスタンス側が優先されます。

## 原画の色と陣営色

Chassis Sprite Tint Strengthは画像へ陣営色を混ぜる量です。0なら原画の色、1なら陣営色を強く混ぜます。初期設定は0です。4人の識別には、引き続きHPバーと足元の色を使います。仮図形と拠点の色分けは従来どおりです。この設定は個体生成時に適用されます。

## 移動とアニメーションの違い

今回の素材は1枚絵なので、絵の形を保ったまま目的地へ移動します。歩行・羽ばたき・変形など絵自体を切り替えるには、複数コマの素材と再生処理が必要です。GIFであるだけで複数コマとは限りません。

## スキルツリーについて

後から追加可能です。まず「解放する機種」「必要な前提スキル」「消費CP」「効果」を決めれば、現在の国家・設計・CPの仕組みにつなげられます。今回は素材の反映のみで、スキルツリーは未実装です。
