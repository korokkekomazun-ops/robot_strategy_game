using System.Collections;
using TMPro;
using UnityEngine;

namespace RobotStrategy.Battle
{
    // 戦闘開始のカウントダウンと終了画面を管理します。
    public partial class BattleManager
    {
        [Header("Battle presentation")]
        [SerializeField, Min(.1f)] private float countdownStepSeconds = 1f;
        [SerializeField, Min(.1f)] private float battleStartMessageSeconds = .8f;
        private GameObject mapChoiceOverlay, countdownOverlay, resultOverlay;
        private TextMeshProUGUI countdownText, resultText;
        private Nation bossDefeatedBy = Nation.None;
        private string bossFinisher = "";
        private bool countingDown;

        private void BuildBattleFlowUI()
        {
            Transform canvas=developmentOverlay.transform.parent;
            mapChoiceOverlay=Overlay(canvas,"マップ選択");
            var map=Panel(mapChoiceOverlay.transform,"マップ選択パネル",Vector2.one*.5f,Vector2.one*.5f,Vector2.zero,new Vector2(720,360));
            Text(map,"Title",24,24,672,50,28).text="1. マップを選択";
            BuildMapChoices(map);
            countdownOverlay=Overlay(canvas,"戦闘開始カウントダウン");
            countdownText=Text(countdownOverlay.transform,"Countdown",0,0,700,180,80);
            var cr=countdownText.rectTransform;cr.anchorMin=cr.anchorMax=cr.pivot=Vector2.one*.5f;cr.anchoredPosition=Vector2.zero;
            countdownText.alignment=TextAlignmentOptions.Center;
            countdownOverlay.SetActive(false);
            resultOverlay=Overlay(canvas,"戦闘リザルト");
            var resultPanel=Panel(resultOverlay.transform,"リザルトパネル",Vector2.one*.5f,Vector2.one*.5f,Vector2.zero,new Vector2(720,420));
            Text(resultPanel,"Title",24,24,672,60,34).text="戦闘終了";
            resultText=Text(resultPanel,"Result",24,110,672,230,28);
            resultText.text="ボスを倒した機体の国家が勝利します。";
            resultOverlay.SetActive(false);
            ShowPreparationPage(PreparationPage.Map);
        }

        private IEnumerator BattleCountdown()
        {
            countingDown=true;
            countdownOverlay.SetActive(true);countdownOverlay.transform.SetAsLastSibling();
            for(int i=3;i>=1;i--)
            {
                countdownText.text=i.ToString();
                yield return new WaitForSecondsRealtime(Mathf.Max(.1f,countdownStepSeconds));
            }
            countdownText.text="戦闘スタート";
            // Keep all units and the spawn timer stopped until the announcement completes.
            yield return new WaitForSecondsRealtime(Mathf.Max(.1f,battleStartMessageSeconds));
            countdownOverlay.SetActive(false);countingDown=false;battleStarted=true;
            nextSpawn=Time.time+Mathf.Max(.1f,spawnInterval);
            feedback=playerNation+"国で戦闘開始。機体を選択して地面をクリックで移動。";
        }

        private void ShowBattleResult()
        {
            developmentOverlay.SetActive(false);deploymentOverlay.SetActive(false);
            choiceOverlay.SetActive(false);countdownOverlay.SetActive(false);
            resultOverlay.SetActive(true);resultOverlay.transform.SetAsLastSibling();
            resultText.text=boss!=null&&!boss.Alive
                ? (bossDefeatedBy==Nation.None?"勝利国：不明":bossDefeatedBy+"国の勝利")
                    +"\n中央の怪獣を撃破しました。\n最後の一撃："+bossFinisher
                : "敗北\nすべての拠点が破壊されました。";
        }
    }
}
