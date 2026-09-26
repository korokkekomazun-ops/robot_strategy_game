# 回復箱

戦闘開始後、各軍の基地から中央へ3マスの場所に1箱出現します。
所有するプレイヤーの機体を箱から1.5マス以内へ移動すると、機体とその基地を各100HP回復し、箱を消費します。
双方が満タンなら消費しません。最大HPを超える回復や、破壊済み機体・基地の復活はありません。
消費後30秒で補充します。基地が破壊されるとその軍の箱は消えます。
NPCにも同じ取得条件が適用されますが、回復箱へ向かう専用AIはありません。

## Unityでの調整

Projectの Assets/_Project/Prefabs/System/BattleManager.prefab を選択し、BattleManagerコンポーネントの「回復箱（Heel Box）」を開きます。

- Enable Health Boxes：回復箱を使用するか。
- Health Box Sprite：heel box.gifの画像。
- Robot Heal Amount：取得した機体の回復量（100）。
- Base Heal Amount：基地の回復量（100）。0なら機体だけを回復。
- Health Box Range：取得距離（1.5マス）。
- Health Box Seconds：消費後の補充間隔（30秒）。

処理は Scripts/Units/HealthBoxes.cs、HP加算は Scripts/Combat/BattleUnit.cs の Heal です。
LANではホストだけが取得・回復・補充を判定し、各PCへ箱とHPを同期します。全員を同じ更新版で起動してください。

## 確認手順

1. 戦闘を開始して自基地の中央側に箱があることを確認。
2. 傷ついた自軍機体を選び、箱付近へ移動。HP上昇と箱の消滅を確認。
3. 基地だけが傷ついた場合も自軍機体を近づけ、基地回復を確認。
4. 両方満タンでは箱が残ること、消費から30秒後に再出現することを確認。
5. LANでは別国家と同じ国家の別プレイヤー双方で他人の箱を取得できないこと、全PCで箱とHPが一致することを確認。

編集時・プレイヤー用のC#コンパイルは確認済み。Unity再生と4台LAN実機試験は未実施です。
