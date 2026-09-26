using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class StatusPointManager : MonoBehaviour
{
    public enum DevelopmentMode { InitialPoints, CombatCP }

    [Header("Development Phase")]
    [SerializeField, Tooltip("開始時の方式。戦闘開始時はBeginCombatDevelopmentを呼びます。")]
    private DevelopmentMode startingMode = DevelopmentMode.InitialPoints;

    [Header("Initial Point Settings")]
    [SerializeField, Min(0), Tooltip("初期機体の作成予算。既存シーンのpoint値を引き継ぎます。")]
    private int point = 15;
    [SerializeField, Min(0)] private int statusPointCost = 1;
    [SerializeField, Min(0)] private int aptitudePointCost = 3;

    [Header("CP Settings")]
    [SerializeField, Min(0), Tooltip("試作用の開始時CP。開発画面を閉じて開くだけではリセットされません。")]
    private int initialCP = 7500;
    [SerializeField, Min(0), Tooltip("HP・攻撃・装甲・速度を1段階上げる費用。")]
    private int statusUpgradeCost = 500;
    [SerializeField, Min(0), Tooltip("陸・海・空の適性を1段階上げる費用。")]
    private int aptitudeUpgradeCost = 500;

    [Header("Design Values")]
    public int MaxHP = 0, Attack = 0, Defence = 0, Speed = 0;
    public int sea = 0; // 0:C 1:B 2:A
    public int ground = 0;
    public int sky = 0;
    public int Humanoid, tank, Battleship, Fighterjet, Transformer, type;

    [Header("UI References")]
    public TextMeshProUGUI pointText;
    public TextMeshProUGUI MaxHPText;
    public TextMeshProUGUI attackText;
    public TextMeshProUGUI defenceText;
    public TextMeshProUGUI speedText;
    public TextMeshProUGUI groundText;
    public TextMeshProUGUI skyText;
    public TextMeshProUGUI seaText;
    public TextMeshProUGUI typeText;

    [Header("Left Status Preview")]
    [SerializeField, Tooltip("左側のステータス一覧のTextMeshProテキストを指定します。")]
    private TextMeshProUGUI statusSummaryText;

    public int CurrentCP { get; private set; }
    public int CurrentPoints { get; private set; }
    public DevelopmentMode CurrentMode { get; private set; }
    private bool initialized;
    // Record actual payments: undo cannot refund initial stats or mint CP after a price change.
    private readonly Stack<int> hpPayments = new Stack<int>();
    private readonly Stack<int> attackPayments = new Stack<int>();
    private readonly Stack<int> defencePayments = new Stack<int>();
    private readonly Stack<int> speedPayments = new Stack<int>();
    private readonly Stack<int> groundPayments = new Stack<int>();
    private readonly Stack<int> seaPayments = new Stack<int>();
    private readonly Stack<int> skyPayments = new Stack<int>();

    // Retained for compatibility with existing code and Inspector button bindings.
    public enum types { Humanoid, tank, battleship, fiterjet, transformer }
    public enum Rank { A, B, C }

    private void Awake() => Initialize();
    private void OnEnable() { Initialize(); UpdateText(); }

    private void Initialize()
    {
        if (initialized) return;
        initialized = true;
        CurrentCP = System.Math.Max(0, initialCP);
        CurrentPoints = System.Math.Max(0, point);
        CurrentMode = startingMode;
    }

    // One-way, idempotent transition. Existing design values are retained.
    public void BeginCombatDevelopment()
    {
        Initialize();
        if (CurrentMode == DevelopmentMode.CombatCP) return;
        hpPayments.Clear();
        attackPayments.Clear();
        defencePayments.Clear();
        speedPayments.Clear();
        groundPayments.Clear();
        seaPayments.Clear();
        skyPayments.Clear();
        CurrentMode = DevelopmentMode.CombatCP;
        UpdateText();
    }

    private bool TrySpend(int cost)
    {
        if (CurrentMode == DevelopmentMode.InitialPoints)
        {
            if (CurrentPoints < cost) return false;
            CurrentPoints -= cost;
        }
        else
        {
            if (CurrentCP < cost) return false;
            CurrentCP -= cost;
        }
        return true;
    }

    private void Refund(int cost)
    {
        if (CurrentMode == DevelopmentMode.InitialPoints) CurrentPoints += cost;
        else CurrentCP += cost;
    }

    private void AddStat(ref int value, Stack<int> payments)
    {
        Initialize();
        int cost = System.Math.Max(0, CurrentMode == DevelopmentMode.InitialPoints ? statusPointCost : statusUpgradeCost);
        if (value == int.MaxValue || !TrySpend(cost)) return;
        payments.Push(cost);
        value++;
        UpdateText();
    }

    private void RemoveStat(ref int value, Stack<int> payments)
    {
        if (payments.Count == 0) return;
        Refund(payments.Pop());
        value--;
        UpdateText();
    }

    private void CycleAptitude(ref int value, Stack<int> payments)
    {
        Initialize();
        if (value >= 2)
        {
            // Return only this editing session's upgrades to their original rank.
            while (payments.Count > 0)
            {
                Refund(payments.Pop());
                value--;
            }
        }
        else
        {
            int cost = System.Math.Max(0, CurrentMode == DevelopmentMode.InitialPoints ? aptitudePointCost : aptitudeUpgradeCost);
            if (!TrySpend(cost)) return;
            payments.Push(cost);
            value++;
        }
        UpdateText();
    }

    public void ADDMaxHP() => AddStat(ref MaxHP, hpPayments);
    public void RemoveMaxHP() => RemoveStat(ref MaxHP, hpPayments);
    public void ADDAttack() => AddStat(ref Attack, attackPayments);
    public void RemovAttack() => RemoveStat(ref Attack, attackPayments);
    public void ADDDefence() => AddStat(ref Defence, defencePayments);
    public void RemoveDefence() => RemoveStat(ref Defence, defencePayments);
    public void ADDSpeed() => AddStat(ref Speed, speedPayments);
    public void RemoveSpeed() => RemoveStat(ref Speed, speedPayments);
    public void ADDground() => CycleAptitude(ref ground, groundPayments);
    public void ADDsea() => CycleAptitude(ref sea, seaPayments);
    public void ADDsky() => CycleAptitude(ref sky, skyPayments);
    public void ADDtypes() { type = (type + 1) % 5; UpdateText(); }
    public void Removetypes() { type = (type + 4) % 5; UpdateText(); }

    // Call on newly deployed robots. This does not update already deployed robots automatically.
    public void ApplyMovementTo(RobotStrategy.Movement.RobotTerrainMovement robot)
    {
        if (robot != null) robot.ApplyDevelopmentValues(Speed, ground, sea, sky);
    }

    private static void SetText(TextMeshProUGUI label, string value)
    {
        if (label != null) label.text = value;
    }

    private static string RankText(int value) => value == 2 ? "A" : value == 1 ? "B" : "C";

    private void UpdateText()
    {
        SetText(pointText, CurrentMode == DevelopmentMode.InitialPoints
            ? $"Point: {CurrentPoints}" : $"CP: {CurrentCP:N0}");
        SetText(MaxHPText, MaxHP.ToString());
        SetText(attackText, Attack.ToString());
        SetText(defenceText, Defence.ToString());
        SetText(speedText, Speed.ToString());
        SetText(groundText, "ground:" + RankText(ground));
        SetText(seaText, "sea:" + RankText(sea));
        SetText(skyText, "sky:" + RankText(sky));
        string[] names = { "Humanoid", "Battleship", "Fiter jet", "Transform", "Tank" };
        SetText(typeText, type >= 0 && type < names.Length ? names[type] : "Unknown");
        string selectedType = type >= 0 && type < names.Length ? names[type] : "Unknown";
        SetText(statusSummaryText,
            $"Type: {selectedType}\n" +
            $"MaxHP: {MaxHP}\n" +
            $"Attack: {Attack}\n" +
            $"Defence: {Defence}\n" +
            $"Speed: {Speed}\n" +
            $"Sea: {RankText(sea)}\n" +
            $"Ground: {RankText(ground)}\n" +
            $"Sky: {RankText(sky)}");
    }
}
