using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace RobotStrategy.Battle
{
    // 各基地の回復箱。回復と補充はホストだけが判断します。
    public partial class BattleManager
    {
        [Header("回復箱（Heel Box）")]
        [SerializeField] private bool enableHealthBoxes = true;
        [SerializeField] private Sprite healthBoxSprite;
        [SerializeField, Min(1), Tooltip("取得した機体のHP回復量")]
        private int robotHealAmount = 100;
        [SerializeField, Min(0), Tooltip("取得した機体の基地の回復量。0で基地回復なし")]
        private int baseHealAmount = 100;
        [SerializeField, Min(.1f), Tooltip("箱を取得できる距離（マス）")]
        private float healthBoxRange = 1.5f;
        [SerializeField, Min(1), Tooltip("消費後、次の箱を補充するまでの秒数")]
        private float healthBoxSeconds = 30;
        private readonly Dictionary<int, GameObject> healthBoxes = new Dictionary<int, GameObject>();
        private readonly Dictionary<int, float> nextHealthBox = new Dictionary<int, float>();

        private GameObject ShowHealthBox(int seat, Vector3 position)
        {
            if (!healthBoxes.TryGetValue(seat, out var box))
            {
                box = new GameObject("P" + (seat + 1) + " 回復箱", typeof(SpriteRenderer));
                box.transform.SetParent(transform, false);
                var art = box.GetComponent<SpriteRenderer>();
                art.sprite = healthBoxSprite != null ? healthBoxSprite : whiteSprite;
                art.color = healthBoxSprite != null ? Color.white : Color.green;
                art.sortingOrder = 12;
                box.transform.localScale = Vector3.one * SpriteScale(art.sprite, 1);
                healthBoxes.Add(seat, box);
            }
            box.transform.position = position;
            return box;
        }

        private void RemoveHealthBox(int seat)
        {
            if (!healthBoxes.TryGetValue(seat, out var box)) return;
            box.SetActive(false);
            Destroy(box);
            healthBoxes.Remove(seat);
        }

        private void TickHealthBoxes()
        {
            foreach (var home in bases)
            {
                if (home == null) continue;
                int seat = home.OwnerSlot;
                if (!enableHealthBoxes || !home.Alive) { RemoveHealthBox(seat); continue; }
                if (!healthBoxes.TryGetValue(seat, out var box))
                {
                    if (nextHealthBox.TryGetValue(seat, out float next) && Time.time < next) continue;
                    Vector3 inward = (center - home.transform.position).normalized;
                    box = ShowHealthBox(seat, ClampPosition(home.transform.position + inward * 3));
                }
                foreach (var unit in units)
                {
                    // 国が同じでもプレイヤーが異なれば取得できません。
                    if (unit == null || !unit.Alive || unit.IsBase || unit.Design == null || unit.OwnerSlot != seat) continue;
                    if ((unit.transform.position - box.transform.position).sqrMagnitude > Mathf.Pow(Mathf.Max(.1f, healthBoxRange), 2)) continue;
                    if (unit.HP >= unit.MaxHP && (baseHealAmount <= 0 || home.HP >= home.MaxHP)) continue;
                    int healed = unit.Heal(robotHealAmount);
                    int repaired = home.Heal(baseHealAmount);
                    if (healed + repaired == 0) continue;
                    string message = "回復箱：機体HP +" + healed + " ／ 基地HP +" + repaired;
                    if (unit.Team == BattleTeam.Player) feedback = message;
                    if (networkMode)
                        foreach (var peer in peers.Values) if (peer.slot == seat) peer.report = message;
                    RemoveHealthBox(seat);
                    nextHealthBox[seat] = Time.time + Mathf.Max(1, healthBoxSeconds);
                    break; // 1箱につき1機だけが取得します。
                }
            }
        }

        private BoxReport[] GetHealthBoxes() => healthBoxes.Select(pair => new BoxReport
        { seat = pair.Key, x = pair.Value.transform.position.x, y = pair.Value.transform.position.y }).ToArray();

        private void ApplyHealthBoxes(BoxReport[] reports)
        {
            var live = new HashSet<int>();
            if (reports != null) foreach (var box in reports)
            { live.Add(box.seat); ShowHealthBox(box.seat, new Vector3(box.x, box.y, 0)); }
            foreach (int seat in healthBoxes.Keys.Where(seat => !live.Contains(seat)).ToArray()) RemoveHealthBox(seat);
        }
    }
}
