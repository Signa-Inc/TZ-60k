using TMPro;
using UnityEngine;

public class Ball : MonoBehaviour
{
    const float Gravity = 12f;

    static readonly int ColorId = Shader.PropertyToID("_Color");
    static readonly int FillId = Shader.PropertyToID("_Fill");
    static readonly int FlashId = Shader.PropertyToID("_Flash");
    static readonly int GoldId = Shader.PropertyToID("_Gold");
    static readonly int RainbowId = Shader.PropertyToID("_Rainbow");

    [SerializeField] Transform visual;
    [SerializeField] Renderer body;
    [SerializeField] TextMeshPro label;
    [SerializeField] Transform star;

    public int Tier { get; private set; }
    public bool Gold { get; private set; }
    public bool Joker { get; private set; }
    public float Radius { get; private set; }
    public Color Color { get; private set; }
    public float Mass => Radius * Radius;
    public bool CanMerge => !Entering && !Dead && MergeDelay <= 0f;

    [HideInInspector] public Vector2 Velocity;
    [HideInInspector] public float MergeDelay;
    [HideInInspector] public float Age;
    [HideInInspector] public bool Entering;
    [HideInInspector] public bool Dead;

    float hp, maxHp;
    float flash, squash;
    MaterialPropertyBlock block;

    public static float RadiusOf(int tier) => Mathf.Min(0.5f * Mathf.Pow(1.18f, tier - 1), 1.9f);
    public static float HealthOf(int tier) => Mathf.Round(5f * Mathf.Pow(1.7f, tier - 1));
    public static float ApexOf(int tier) => Mathf.Min(5.6f + 0.6f * tier, 10f);

    public void Setup(int tier, bool gold, bool joker, float health, Color color)
    {
        Tier = tier;
        Gold = gold;
        Joker = joker;
        Color = color;
        Radius = joker ? 0.6f : RadiusOf(tier);
        hp = maxHp = health;

        float d = Radius * 2f;
        body.transform.localScale = new Vector3(d, d, d);
        label.gameObject.SetActive(!joker);
        star.gameObject.SetActive(joker);
        if (!joker)
        {
            label.text = (1 << tier).ToString();
            label.color = gold ? new Color(1f, 0.93f, 0.55f) : Color.white;
            label.fontSize = Radius * (tier < 7 ? 10f : 7.5f);
            label.rectTransform.sizeDelta = new Vector2(d, d);
            label.transform.localPosition = new Vector3(0f, 0f, -Radius - 0.02f);
        }
        else
        {
            star.localPosition = new Vector3(0f, 0f, -Radius - 0.1f);
        }
        Paint();
    }

    public bool TakeDamage(float amount)
    {
        hp -= amount;
        flash = 1f;
        squash = Mathf.Max(squash, 0.45f);
        return hp <= 0f;
    }

    public void Punch(float strength) => squash = strength;

    public void Tick(float dt, float ceiling)
    {
        Age += dt;
        MergeDelay -= dt;
        Vector2 pos = transform.position;

        if (Entering)
        {
            pos.x += Velocity.x * dt;
            if (Mathf.Abs(pos.x) <= Battle.HalfWidth - Radius) Entering = false;
            transform.position = pos;
            return;
        }

        Velocity.y -= Gravity * (Joker ? 0.55f : 1f) * dt;
        pos += Velocity * dt;

        float wall = Battle.HalfWidth - Radius;
        if (Mathf.Abs(pos.x) > wall)
        {
            pos.x = Mathf.Sign(pos.x) * wall;
            Velocity.x = -Mathf.Sign(pos.x) * Mathf.Abs(Velocity.x);
        }

        if (pos.y < Radius && Velocity.y < 0f)
        {
            pos.y = Radius;
            Velocity.y = Mathf.Sqrt(2f * Gravity * (Joker ? 0.55f : 1f) * (ApexOf(Tier) - Radius));
            squash = 1f;
        }

        if (pos.y > ceiling - Radius && Velocity.y > 0f) Velocity.y = 0f;

        // шар не должен зависать на месте или разгоняться бесконечно
        float speed = Mathf.Abs(Velocity.x);
        if (speed < 1.1f) Velocity.x = Mathf.MoveTowards(Velocity.x, Velocity.x < 0f ? -1.1f : 1.1f, 2f * dt);
        else if (speed > 2.6f) Velocity.x = Mathf.MoveTowards(Velocity.x, Mathf.Sign(Velocity.x) * 2.6f, 3f * dt);

        transform.position = pos;
    }

    void Update()
    {
        flash = Mathf.MoveTowards(flash, 0f, Time.deltaTime * 9f);
        squash = Mathf.MoveTowards(squash, 0f, Time.deltaTime * 4f);

        float wobble = Mathf.Sin(squash * 9f) * squash * 0.18f;
        float spawn = Gold ? 1f + Mathf.Max(0f, 0.25f - Age) * 1.6f : 1f;
        visual.localScale = new Vector3(1f + wobble, 1f - wobble, 1f) * spawn;

        if (Joker) star.localRotation = Quaternion.Euler(0f, 0f, Time.time * 120f);
        Paint();
    }

    void Paint()
    {
        block ??= new MaterialPropertyBlock();
        block.SetColor(ColorId, Color);
        block.SetFloat(FillId, Joker ? 1f : Mathf.Clamp01(hp / maxHp));
        block.SetFloat(FlashId, flash * 0.7f);
        block.SetFloat(GoldId, Gold ? 1f : 0f);
        block.SetFloat(RainbowId, Joker ? 1f : 0f);
        body.SetPropertyBlock(block);
    }
}
