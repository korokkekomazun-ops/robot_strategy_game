using System;
using System.Collections.Generic;

namespace RobotStrategy.Battle
{
    // 設計図の一覧とCPを管理します。
    // Session-only immutable designs. Deployed units receive their own stat copies.
    public sealed class RobotDesign
    {
        public const int ValueCount = 8;
        public string DesignId { get; }
        public string Name { get; }
        public ChassisKind Kind { get; }
        public Nation Country { get; }
        private readonly int[] levels;
        public int this[int i] => levels[i];
        public RobotDesign(string name, int[] values, ChassisKind kind = ChassisKind.Humanoid, Nation country = Nation.None, string designId = null)
        {
            if (values == null || (values.Length != 7 && values.Length != RobotDesign.ValueCount)) throw new ArgumentException("Seven legacy or eight design values required");
            levels = new int[ValueCount];
            Array.Copy(values,levels,values.Length);
            for (int i = 0; i < RobotDesign.ValueCount; i++)
                if (levels[i] < 0 || (i >= 4 && levels[i] > 2)) throw new ArgumentOutOfRangeException(nameof(values));
            Name = name;
            DesignId = designId ?? Guid.NewGuid().ToString("N");
            Kind = kind;
            Country = country;
        }
        public int[] CopyLevels() => (int[])levels.Clone();
    }

    public sealed class DesignList
    {
        private readonly List<RobotDesign> designs = new List<RobotDesign>();
        public IReadOnlyList<RobotDesign> Designs => designs.AsReadOnly();
        public int CP { get; private set; }
        public DesignList(int initialCP, RobotDesign initialDesign = null)
        {
            CP = Math.Max(0, initialCP);
            designs.Add(initialDesign ?? new RobotDesign("初期機体", new[] { 0, 0, 0, 0, 1, 1, 1 }));
        }
        public static long Cost(RobotDesign basis, int[] values, int statCost, int aptitudeCost)
        {
            if (basis == null || values == null || (values.Length != 7 && values.Length != RobotDesign.ValueCount)) return -1;
            if(values.Length==7){var expanded=new int[RobotDesign.ValueCount];Array.Copy(values,expanded,7);values=expanded;}
            long total = 0;
            for (int i = 0; i < RobotDesign.ValueCount; i++)
            {
                if (values[i] < basis[i] || (i >= 4 && values[i] > 2) || values[i] > 10000) return -1;
                total += (long)(values[i] - basis[i]) * Math.Max(0, i < 4 ? statCost : aptitudeCost);
            }
            return total;
        }
        public bool TryCreate(RobotDesign basis, int[] values, int statCost, int aptitudeCost, out RobotDesign created)
        {
            created = null;
            long cost = Cost(basis, values, statCost, aptitudeCost);
            bool changed = false;
            if (cost < 0 || cost > CP) return false;
            if(values.Length==7){var expanded=new int[RobotDesign.ValueCount];Array.Copy(values,expanded,7);values=expanded;}
            for (int i = 0; i < RobotDesign.ValueCount; i++) changed |= values[i] != basis[i];
            if (!changed) return false;
            created = new RobotDesign(CountryRules.KindName(basis.Kind) + " 設計" + designs.Count, values, basis.Kind, basis.Country);
            CP -= (int)cost;
            designs.Add(created);
            return true;
        }
        public bool Capture(RobotDesign source, Nation owner)
        {
            if(source==null || owner==Nation.None)return false;
            foreach(var existing in designs)
            {
                if(existing.DesignId==source.DesignId)return false;
            }
            string name=source.Name.StartsWith("鹵獲：")?source.Name:"鹵獲："+source.Name;
            designs.Add(new RobotDesign(name,source.CopyLevels(),source.Kind,owner,source.DesignId));
            return true;
        }
        public static DesignList FromNetwork(int cp,RobotDesign[] values)
        {
            var result=new DesignList(cp,values[0]);
            result.designs.Clear();result.designs.AddRange(values);return result;
        }
        public bool TrySpend(int amount)
        {
            amount = Math.Max(0, amount);
            if (CP < amount) return false;
            CP -= amount;
            return true;
        }
        public void Earn(int amount) => CP = (int)Math.Min(int.MaxValue, (long)CP + Math.Max(0, amount));
    }
}
