using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public class Cannon : MonoBehaviour
{
    const float BulletSpeed = 24f;
    const float MaxX = Battle.HalfWidth - 0.75f;

    [SerializeField] Camera cam;
    [SerializeField] Battle battle;
    [SerializeField] Transform model, barrel, wheelLeft, wheelRight;
    [SerializeField] Transform bulletPrefab;
    [SerializeField] SpriteRenderer shield;
    [SerializeField] GameObject stage;
    [SerializeField] Transform stageRays;
    [SerializeField] float dragSensitivity = 1.25f;

    public float TargetX { get; set; }
    public bool Firing { get; set; }
    public bool AutoPlay { get; set; }

    readonly List<Transform> bullets = new();
    readonly Stack<Transform> pool = new();
    Vector3 barrelRest;
    Quaternion modelRest;
    float lastPointerX;
    float cooldown, recoil, blink;
    bool showcase;

    void Awake()
    {
        barrelRest = barrel.localPosition;
        modelRest = model.localRotation;
        shield.enabled = false;
        stage.SetActive(false);
    }

    public void ResetPosition()
    {
        showcase = false;
        TargetX = 0f;
        Firing = false;
        blink = 0f;
        transform.position = Vector3.zero;
        transform.localScale = Vector3.one;
        model.localRotation = modelRest;
        model.gameObject.SetActive(true);
        shield.enabled = false;
        stage.SetActive(false);
        ClearBullets();
    }

    public void Showcase(Vector3 position)
    {
        ResetPosition();
        showcase = true;
        stage.SetActive(true);
        transform.position = position;
        transform.localScale = Vector3.one * 1.45f;
    }

    public void Celebrate() => recoil = 1.6f;

    public void Blink(float time) => blink = time;

    public void ReadInput()
    {
        if (AutoPlay) return;

        var pointer = Pointer.current;
        if (pointer == null) return;

        float x = pointer.position.ReadValue().x;
        if (pointer.press.wasPressedThisFrame) lastPointerX = x;
        Firing = pointer.press.isPressed;
        if (!Firing) return;

        float unitsPerPixel = cam.orthographicSize * 2f / Screen.height;
        TargetX = Mathf.Clamp(TargetX + (x - lastPointerX) * unitsPerPixel * dragSensitivity, -MaxX, MaxX);
        lastPointerX = x;
    }

    public void Tick(float dt)
    {
        var pos = transform.position;
        float x = Mathf.MoveTowards(pos.x, Mathf.Clamp(TargetX, -MaxX, MaxX), 26f * dt);
        float moved = x - pos.x;
        transform.position = new Vector3(x, pos.y, pos.z);

        float roll = -moved / 0.36f * Mathf.Rad2Deg;
        wheelLeft.Rotate(0f, 0f, roll, Space.World);
        wheelRight.Rotate(0f, 0f, roll, Space.World);

        cooldown -= dt;
        if (Firing && cooldown <= 0f)
        {
            cooldown += 1f / Progress.FireRate;
            if (cooldown < 0f) cooldown = 0f;
            Shoot();
        }

        MoveBullets(dt);
    }

    void Shoot()
    {
        var bullet = pool.Count > 0 ? pool.Pop() : Instantiate(bulletPrefab);
        bullet.gameObject.SetActive(true);
        bullet.position = new Vector3(transform.position.x, 1.75f, 0.1f);
        float size = 1f + Progress.Levels[Progress.Power] * 0.07f;
        bullet.localScale = bulletPrefab.localScale * size;
        bullets.Add(bullet);
        recoil = 1f;
        Sfx.Play(Sound.Shot, Random.Range(0.92f, 1.08f), 0.35f);
    }

    void MoveBullets(float dt)
    {
        for (int i = bullets.Count - 1; i >= 0; i--)
        {
            var bullet = bullets[i];
            bullet.position += Vector3.up * (BulletSpeed * dt);
            if (bullet.position.y > battle.Ceiling + 1f || battle.TryHit(bullet.position)) Release(i);
        }
    }

    void Release(int index)
    {
        var bullet = bullets[index];
        bullet.gameObject.SetActive(false);
        pool.Push(bullet);
        bullets.RemoveAt(index);
    }

    public void ClearBullets()
    {
        for (int i = bullets.Count - 1; i >= 0; i--) Release(i);
    }

    void Update()
    {
        recoil = Mathf.MoveTowards(recoil, 0f, Time.deltaTime * 14f);
        barrel.localPosition = barrelRest + Vector3.down * (recoil * 0.14f);
        model.localScale = new Vector3(1f + recoil * 0.05f, 1f - recoil * 0.05f, 1f);

        if (showcase)
        {
            model.localRotation = Quaternion.Euler(8f, Mathf.Sin(Time.time * 1.2f) * 35f, 0f) * modelRest;
            stageRays.Rotate(0f, 0f, 15f * Time.deltaTime);
            return;
        }

        if (blink > 0f)
        {
            blink -= Time.deltaTime;
            model.gameObject.SetActive(blink <= 0f || Mathf.Repeat(blink, 0.2f) > 0.08f);
            shield.enabled = blink > 0f;
        }
    }
}
