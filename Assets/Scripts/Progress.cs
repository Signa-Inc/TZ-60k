using UnityEngine;

// Прогресс живёт только до закрытия игры, сохранений в прототипе нет.
public static class Progress
{
    public const int Rate = 0;
    public const int Power = 1;
    public const int Blast = 2;
    public const int Armor = 3;

    public static int Coins;
    public static int Wave = 1;
    public static int BestTier = 2;
    public static readonly int[] Levels = new int[4];

    static readonly int[] baseCost = { 300, 320, 350, 700 };
    static readonly int[] maxLevel = { 10, 10, 8, 3 };

    public static float FireRate => FireRateAt(Levels[Rate]);
    public static float Damage => DamageAt(Levels[Power]);
    public static float BlastScale => BlastAt(Levels[Blast]);
    public static int Shields => ShieldsAt(Levels[Armor]);

    public static float FireRateAt(int level) => 9f + level * 1.4f;
    public static float DamageAt(int level) => 1f + level * 0.3f;
    public static float BlastAt(int level) => 1f + level * 0.2f;
    public static int ShieldsAt(int level) => 3 + level;

    public static int RecordBonus(int tier) => (1 << tier) * 3;
    public static bool IsMaxed(int id) => Levels[id] >= maxLevel[id];
    public static int Cost(int id) => Mathf.RoundToInt(baseCost[id] * Mathf.Pow(1.55f, Levels[id]) / 10f) * 10;
    public static bool CanBuy(int id) => !IsMaxed(id) && Coins >= Cost(id);

    public static bool TryBuy(int id)
    {
        if (!CanBuy(id)) return false;
        Coins -= Cost(id);
        Levels[id]++;
        return true;
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    static void Reset()
    {
        Coins = 0;
        Wave = 1;
        BestTier = 2;
        System.Array.Clear(Levels, 0, Levels.Length);
    }
}
