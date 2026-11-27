using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BattleResult
{
    public bool Win;
    public int Wave;
    public int Coins;
    public int Merges;
    public int BestCombo;
    public int BestTier;
    public int WinBonus;
    public int RecordTier;
    public int RecordBonus;
    public int Stars;
    public int Total => Coins + WinBonus + RecordBonus;
}

public class Battle : MonoBehaviour
{
    public const float HalfWidth = 5f;

    const float Step = 1f / 60f;
    const float ComboWindow = 3f;
    const float MagnetGap = 1.8f;
    const float JokerLife = 12f;

    // tier 0 - джокер. Первые две двойки уже на поле и сталкиваются с первым касанием.
    static readonly (float time, int tier, int side)[] wave =
    {
        (5f, 2, 1), (9f, 2, -1), (12f, 1, 1),
        (16f, 3, -1), (21f, 2, 1), (24f, 0, -1),
        (27f, 3, 1), (32f, 2, -1), (35f, 2, 1),
        (39f, 4, -1), (46f, 3, 1), (49f, 3, -1),
        (53f, 0, 1), (55f, 4, 1), (61f, 2, -1), (62f, 2, 1),
        (66f, 4, -1), (70f, 3, 1), (71f, 3, -1),
        (76f, 5, 1),
    };

    enum State { Idle, Waiting, Playing, Ending }

    [SerializeField] Ball ballPrefab;
    [SerializeField] Cannon cannon;
    [SerializeField] CameraRig rig;
    [SerializeField] Hud hud;
    [SerializeField] Color[] tierColors;

    public event Action<BattleResult> Finished;

    public readonly List<Ball> Balls = new();
    public int Shields { get; private set; }
    public float Ceiling => rig.Top - 1.6f;
    public float WaveProgress => Mathf.Clamp01((float)spawned / wave.Length);

    public static float[] JokerMarks()
    {
        var marks = new List<float>();
        for (int i = 0; i < wave.Length; i++)
            if (wave[i].tier == 0) marks.Add((i + 1f) / wave.Length);
        return marks.ToArray();
    }

    readonly List<(Ball a, Ball b)> merges = new();
    State state;
    BattleResult result;
    float time, accumulator, sinceMerge, invulnerable, healthScale;
    int spawned, combo;

    Color ColorOf(int tier) => tierColors[Mathf.Clamp(tier - 1, 0, tierColors.Length - 1)];

    public void ShowIdle()
    {
        Clear();
        healthScale = 1f;
        state = State.Idle;
        for (int i = 0; i < 4; i++)
        {
            var pos = new Vector2(-3.3f + i * 2.2f, 5f + (i % 2) * 2.5f);
            var ball = Create(i + 1, i == 2, false, pos, new Vector2(i % 2 == 0 ? 1.4f : -1.4f, 0f));
            ball.MergeDelay = float.MaxValue;
        }
    }

    public void Begin()
    {
        Clear();
        cannon.ResetPosition();
        result = new BattleResult { Wave = Progress.Wave };
        healthScale = 1f + 0.3f * (Progress.Wave - 1);
        Shields = Progress.Shields;
        time = accumulator = invulnerable = 0f;
        sinceMerge = ComboWindow;
        spawned = combo = 0;
        state = State.Waiting;
        for (int side = -1; side <= 1; side += 2)
            Create(1, false, false, new Vector2(side * 1.7f, 8.5f), new Vector2(-side * 2.4f, 0f));
        hud.Begin();
    }

    public void Clear()
    {
        StopAllCoroutines();
        foreach (var ball in Balls) Destroy(ball.gameObject);
        Balls.Clear();
        cannon.ClearBullets();
        state = State.Idle;
    }

    void Update()
    {
        if (state == State.Waiting)
        {
            // до первого касания поле замерло
            cannon.ReadInput();
            if (!cannon.Firing) return;
            state = State.Playing;
            hud.HideHint();
        }
        else if (state == State.Playing)
        {
            cannon.ReadInput();
        }

        accumulator += Mathf.Min(Time.deltaTime, 0.1f);
        while (accumulator >= Step)
        {
            accumulator -= Step;
            Simulate(Step);
        }
    }

    void Simulate(float dt)
    {
        bool playing = state == State.Playing;
        if (playing)
        {
            Spawn(dt);
            cannon.Tick(dt);
        }

        for (int i = Balls.Count - 1; i >= 0; i--)
        {
            var ball = Balls[i];
            ball.Tick(dt, Ceiling);
            if (ball.Joker && ball.Age > JokerLife)
            {
                Fx.Instance.Burst(ball.transform.position, Color.white, 16, 5f);
                Remove(ball);
            }
        }

        Collide(dt, playing);
        if (!playing) return;

        foreach (var (a, b) in merges) Merge(a, b);
        merges.Clear();

        sinceMerge += dt;
        invulnerable -= dt;
        CheckCannon();

        if (state == State.Playing && spawned == wave.Length && !HasEnemies()) Finish(true);
    }

    void Spawn(float dt)
    {
        time += dt;
        // поле пустое - не заставляем ждать следующий шар
        if (spawned < wave.Length && !HasEnemies() && time > 1f)
            time = Mathf.Max(time, wave[spawned].time - 0.5f);

        while (spawned < wave.Length && time >= wave[spawned].time)
        {
            var (_, tier, side) = wave[spawned++];
            bool joker = tier == 0;
            if (joker)
            {
                tier = 2;
                Sfx.Play(Sound.Joker);
                hud.Banner("ДЖОКЕР!", "Сливается с любым шаром");
            }

            float radius = joker ? 0.6f : Ball.RadiusOf(tier);
            var pos = new Vector2(side * (HalfWidth + radius), Ball.ApexOf(tier));
            var ball = Create(tier, false, joker, pos, new Vector2(-side * 2.2f, 0f));
            ball.Entering = true;
        }
    }

    Ball Create(int tier, bool gold, bool joker, Vector2 position, Vector2 velocity)
    {
        var ball = Instantiate(ballPrefab, position, Quaternion.identity, transform);
        float health = Ball.HealthOf(tier) * healthScale;
        ball.Setup(tier, gold, joker, health, joker ? Color.white : ColorOf(tier));
        ball.Velocity = velocity;
        Balls.Add(ball);
        return ball;
    }

    void Remove(Ball ball)
    {
        ball.Dead = true;
        Balls.Remove(ball);
        Destroy(ball.gameObject);
    }

    bool HasEnemies()
    {
        foreach (var ball in Balls)
            if (!ball.Joker) return true;
        return false;
    }

    static bool Twins(Ball a, Ball b)
    {
        if (a.Joker || b.Joker) return a.Joker != b.Joker;
        return a.Tier == b.Tier;
    }

    void Collide(float dt, bool canMerge)
    {
        for (int i = 0; i < Balls.Count; i++)
        {
            var a = Balls[i];
            for (int j = i + 1; j < Balls.Count; j++)
            {
                var b = Balls[j];
                if (a.Entering || b.Entering) continue;

                Vector2 delta = b.transform.position - a.transform.position;
                float distance = delta.magnitude;
                float touch = a.Radius + b.Radius;
                bool twins = canMerge && a.CanMerge && b.CanMerge && Twins(a, b);

                if (distance > touch)
                {
                    if (twins && distance < touch + MagnetGap)
                    {
                        Vector2 pull = delta / distance * (7f * dt);
                        a.Velocity += pull;
                        b.Velocity -= pull;
                    }
                    continue;
                }

                if (twins && !Pending(a) && !Pending(b))
                {
                    merges.Add((a, b));
                    continue;
                }

                Bounce(a, b, delta, distance, touch);
            }
        }
    }

    bool Pending(Ball ball)
    {
        foreach (var (a, b) in merges)
            if (a == ball || b == ball) return true;
        return false;
    }

    static void Bounce(Ball a, Ball b, Vector2 delta, float distance, float touch)
    {
        Vector2 normal = distance > 0.001f ? delta / distance : Vector2.right;
        float shareA = b.Mass / (a.Mass + b.Mass);
        float overlap = touch - distance;
        a.transform.position -= (Vector3)(normal * (overlap * shareA));
        b.transform.position += (Vector3)(normal * (overlap * (1f - shareA)));

        float closing = Vector2.Dot(a.Velocity - b.Velocity, normal);
        if (closing <= 0f) return;

        float impulse = 1.8f * closing / (1f / a.Mass + 1f / b.Mass);
        a.Velocity -= normal * (impulse / a.Mass);
        b.Velocity += normal * (impulse / b.Mass);
    }

    void Merge(Ball a, Ball b)
    {
        if (a.Dead || b.Dead) return;

        var target = a.Joker ? b : a;
        var other = a.Joker ? a : b;
        bool joker = other.Joker;
        int tier = target.Tier + 1;

        Vector2 position = joker ? target.transform.position : (a.transform.position + b.transform.position) * 0.5f;
        float vx = (a.Velocity.x * a.Mass + b.Velocity.x * b.Mass) / (a.Mass + b.Mass);
        Remove(a);
        Remove(b);

        var merged = Create(tier, true, false, position, new Vector2(vx, 6.5f));
        merged.MergeDelay = 0.35f;
        merged.Punch(1f);

        combo = sinceMerge < ComboWindow ? combo + 1 : 1;
        sinceMerge = 0f;
        result.Merges++;
        result.BestCombo = Mathf.Max(result.BestCombo, combo);
        result.BestTier = Mathf.Max(result.BestTier, tier);

        float radius = merged.Radius + 2.2f * Progress.BlastScale;
        Blast(merged, radius, Ball.HealthOf(tier - 1) * 0.9f * Progress.BlastScale);

        var fx = Fx.Instance;
        fx.Ring(position, radius, new Color(1f, 0.85f, 0.3f));
        fx.Burst(position, merged.Color, 28, 9f);
        fx.Burst(position, new Color(1f, 0.85f, 0.3f), 14, 6f);
        fx.HitStop(joker ? 0.14f : 0.07f);
        rig.Shake(0.12f + tier * 0.03f + (joker ? 0.15f : 0f));
        Sfx.Play(Sound.Merge, 0.85f + Mathf.Min(combo, 8) * 0.08f);
        Haptics.Light();

        Reward(position, (1 << tier) * Mathf.Min(combo, 5), 8);
        if (combo > 1) fx.Popup(position + Vector2.up * 1.2f, "КОМБО x" + combo, new Color(1f, 0.55f, 0.9f), 80f);

        if (tier > Progress.BestTier)
        {
            Progress.BestTier = tier;
            result.RecordTier = tier;
            result.RecordBonus += Progress.RecordBonus(tier);
            hud.Banner("НОВЫЙ РЕКОРД: " + (1 << tier), "Награда +" + Progress.RecordBonus(tier));
        }

        hud.Merged();
    }

    void Blast(Ball source, float radius, float damage)
    {
        foreach (var ball in Balls.ToArray())
        {
            if (ball == source || ball.Dead || ball.Entering) continue;

            Vector2 delta = ball.transform.position - source.transform.position;
            if (delta.magnitude - ball.Radius > radius) continue;

            ball.Velocity += delta.normalized * (3.5f / ball.Radius);
            ball.Velocity.y = Mathf.Max(ball.Velocity.y, 3f);
            Damage(ball, damage);
        }
    }

    public bool TryHit(Vector3 point)
    {
        foreach (var ball in Balls)
        {
            Vector2 delta = ball.transform.position - point;
            float reach = ball.Radius + 0.1f;
            if (delta.sqrMagnitude > reach * reach) continue;

            // влетающий шар не толкаем, иначе его можно выбить за край навсегда
            if (!ball.Entering) Push(ball, delta.x);

            Fx.Instance.Burst(point, Color.Lerp(ball.Color, Color.white, 0.5f), 3, 4f);
            Sfx.Play(Sound.Hit, 1.3f - ball.Radius * 0.3f, 0.5f);
            Damage(ball, Progress.Damage);
            return true;
        }
        return false;
    }

    // попадание сбоку толкает шар в сторону - так игрок сводит двойников
    static void Push(Ball ball, float offset)
    {
        float side = Mathf.Clamp(offset / ball.Radius, -1f, 1f);
        float push = side * (ball.Joker ? 1.6f : 0.4f / ball.Radius);
        ball.Velocity.x = Mathf.Clamp(ball.Velocity.x + push, -3.2f, 3.2f);
        if (ball.Velocity.y < 0f) ball.Velocity.y += (ball.Joker ? 1.2f : 0.3f) / ball.Radius;
    }

    void Damage(Ball ball, float amount)
    {
        if (ball.Joker)
        {
            ball.Punch(0.5f);
            return;
        }
        if (ball.TakeDamage(amount)) Pop(ball);
    }

    void Pop(Ball ball)
    {
        Vector2 position = ball.transform.position;
        int tier = ball.Tier;
        bool gold = ball.Gold;
        float radius = ball.Radius;
        var color = ball.Color;
        Remove(ball);

        var fx = Fx.Instance;
        fx.Burst(position, color, 14 + tier * 3, 7f);
        fx.Ring(position, radius * 1.6f, color);
        Sfx.Play(Sound.Pop, 1.25f - tier * 0.07f);
        rig.Shake(0.05f + tier * 0.015f);

        if (gold)
        {
            fx.Burst(position, new Color(1f, 0.85f, 0.3f), 20, 8f);
            Reward(position, (1 << tier) * 2, 7);
            return;
        }

        Reward(position, 1 << tier, 3);
        if (tier == 1 || state != State.Playing) return;

        for (int side = -1; side <= 1; side += 2)
        {
            var child = Create(tier - 1, false, false, position, new Vector2(side * 2.4f, 5f));
            float x = Mathf.Clamp(position.x + side * radius * 0.5f, -HalfWidth + child.Radius, HalfWidth - child.Radius);
            child.transform.position = new Vector2(x, position.y);
            child.MergeDelay = 1.2f;
        }
    }

    void Reward(Vector2 position, int amount, int icons)
    {
        result.Coins += amount;
        Fx.Instance.Popup(position, "+" + amount, new Color(1f, 0.9f, 0.35f), 56f);
        hud.CollectCoins(position, amount, icons);
    }

    void CheckCannon()
    {
        Vector2 hitbox = (Vector2)cannon.transform.position + new Vector2(0f, 0.75f);
        foreach (var ball in Balls)
        {
            if (ball.Entering) continue;

            Vector2 delta = (Vector2)ball.transform.position - hitbox;
            // хитбокс чуть меньше картинки, чтобы касание по краю прощалось
            if (delta.magnitude > ball.Radius + 0.4f) continue;

            ball.Velocity = new Vector2(delta.x >= 0f ? 2.6f : -2.6f, 10f);
            if (ball.Joker || invulnerable > 0f) continue;

            invulnerable = 1.6f;
            Shields--;
            cannon.Blink(invulnerable);
            hud.Hurt();
            rig.Shake(0.5f);
            Fx.Instance.HitStop(0.12f);
            Fx.Instance.Burst(hitbox, new Color(1f, 0.35f, 0.3f), 24, 7f);
            Sfx.Play(Sound.Hurt);
            Haptics.Heavy();
            if (Shields <= 0)
            {
                Finish(false);
                return;
            }
        }
    }

    void Finish(bool win)
    {
        state = State.Ending;
        cannon.Firing = false;
        cannon.ClearBullets();
        merges.Clear();

        result.Win = win;
        if (win)
        {
            result.WinBonus = result.Coins / 2;
            // три звезды за чистое прохождение, две - за один пропущенный удар
            int hits = Progress.Shields - Shields;
            result.Stars = hits == 0 ? 3 : hits == 1 ? 2 : 1;
            Progress.Wave++;
        }
        Progress.Coins += result.Total;
        Sfx.Play(win ? Sound.Win : Sound.Lose);
        hud.Banner(win ? "ПОБЕДА!" : "ПУШКА РАЗБИТА", "");
        StartCoroutine(Report());
    }

    IEnumerator Report()
    {
        yield return new WaitForSecondsRealtime(1.6f);
        Finished?.Invoke(result);
    }
}
