using UnityEngine;
using UnityEngine.SceneManagement;

namespace RobotStrategy.Battle
{
    // マップから初期機体作成までの準備画面を切り替えます。
    public partial class BattleManager
    {
        private bool IsPreparationScene => gameObject.scene.name == "robotdevelop";
        private enum PreparationPage { Map, Nation, Chassis, Stats, Hidden }

        // All preparation page visibility is managed here.
        private void ShowPreparationPage(PreparationPage page)
        {
            mapChoiceOverlay.SetActive(page == PreparationPage.Map);
            choiceOverlay.SetActive(page == PreparationPage.Nation || page == PreparationPage.Chassis);
            nationPanel.SetActive(page == PreparationPage.Nation);
            initialTypePanel.SetActive(page == PreparationPage.Chassis);
            developmentOverlay.SetActive(page == PreparationPage.Stats);
            GameObject front = page == PreparationPage.Map ? mapChoiceOverlay
                : page == PreparationPage.Stats ? developmentOverlay : choiceOverlay;
            if (page != PreparationPage.Hidden) front.transform.SetAsLastSibling();
            if (page == PreparationPage.Stats)
            {
                var panel = FindUI(developmentOverlay.transform, PositionKey("Panel:開発画面", 0, 0));
                if (panel != null) panel.SetActive(true);
            }
        }

        private void ConfirmInitialDesign(RobotDesign design)
        {
            if(networkMode)
            {
                SendCommand(new PlayerOrder{op="ready",kind=(int)design.Kind,levels=design.CopyLevels()});
                return;
            }
            if (IsPreparationScene)
            {
                if (!Application.CanStreamedLevelBeLoaded("MainScene"))
                { feedback = "Build ProfilesにMainSceneを登録してください。"; return; }
                BattleStart.Prepare(design,battleMap);
                SceneManager.LoadScene("MainScene");
                return;
            }
            DeployInitialDesign(design);
        }

        private void DeployInitialDesign(RobotDesign design)
        {
            playerNation = design.Country;
            selectedKind = design.Kind;
            catalog = new DesignList(CountryRules.InitialCP(startingBattleCP, playerNation, nationACPBonus), design);
            chosenDesign = 0;
            DeployFourNations(design);
            initialSetup = false;
            CloseDraft();
            ShowPreparationPage(PreparationPage.Hidden);
            StartCoroutine(BattleCountdown());
        }

        private void BuildAllUI()
        {
            BuildBattleUI();
            BuildNationFlow();
            BuildBattleFlowUI();
            BuildLanUI();
        }
    }
}
