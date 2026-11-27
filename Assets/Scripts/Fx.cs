using System;
using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class Fx : MonoBehaviour
{
    public static Fx Instance { get; private set; }

    [SerializeField] Camera cam;
    [SerializeField] ParticleSystem particles;
    [SerializeField] SpriteRenderer ringPrefab;
    [SerializeField] TMP_Text popupPrefab;
    [SerializeField] Image coinPrefab;
    [SerializeField] RectTransform layer;

    Coroutine hitStop;

    void Awake() => Instance = this;

    public Vector2 ToScreen(Vector3 world) => cam.WorldToScreenPoint(world);

    public void Burst(Vector3 position, Color color, int count, float speed)
    {
        var p = new ParticleSystem.EmitParams();
        for (int i = 0; i < count; i++)
        {
            Vector2 dir = UnityEngine.Random.insideUnitCircle;
            p.position = position + new Vector3(dir.x * 0.3f, dir.y * 0.3f, -2f);
            p.velocity = dir * (speed * UnityEngine.Random.Range(0.6f, 1.3f));
            p.startColor = color;
            p.startSize = UnityEngine.Random.Range(0.12f, 0.34f);
            p.startLifetime = UnityEngine.Random.Range(0.3f, 0.7f);
            particles.Emit(p, 1);
        }
    }

    public void Ring(Vector3 position, float radius, Color color) => StartCoroutine(RingRoutine(position, radius, color));

    IEnumerator RingRoutine(Vector3 position, float radius, Color color)
    {
        var ring = Instantiate(ringPrefab, new Vector3(position.x, position.y, -2.5f), Quaternion.identity, transform);
        float unit = ring.sprite.bounds.size.x;
        yield return Tween.Run(0.35f, t =>
        {
            float scale = Mathf.Lerp(0.3f, radius * 2f, Tween.OutCubic(t)) / unit;
            ring.transform.localScale = new Vector3(scale, scale, 1f);
            color.a = 1f - t * t;
            ring.color = color;
        });
        Destroy(ring.gameObject);
    }

    public void Popup(Vector3 world, string text, Color color, float size) =>
        StartCoroutine(PopupRoutine(ToScreen(world), text, color, size));

    IEnumerator PopupRoutine(Vector2 from, string text, Color color, float size)
    {
        var label = Instantiate(popupPrefab, layer);
        label.text = text;
        label.fontSize = size;
        label.color = color;

        var rect = label.rectTransform;
        rect.position = from;
        // не даём подписи вылезти за край экрана
        float half = label.preferredWidth * 0.5f * layer.lossyScale.x + 20f;
        from.x = Mathf.Clamp(from.x, half, Screen.width - half);

        yield return Tween.Run(0.8f, t =>
        {
            rect.position = from + Vector2.up * (Tween.OutCubic(t) * 110f * layer.lossyScale.y);
            rect.localScale = Vector3.one * (t < 0.25f ? Tween.OutBack(t / 0.25f) : 1f);
            label.alpha = Mathf.Clamp01((1f - t) * 3f);
        });
        Destroy(label.gameObject);
    }

    public void FlyCoins(Vector2 from, RectTransform to, int amount, int icons, Action<int> onArrive)
    {
        icons = Mathf.Clamp(amount, 1, icons);
        for (int i = 0; i < icons; i++)
        {
            int share = amount / icons + (i < amount % icons ? 1 : 0);
            StartCoroutine(CoinRoutine(from, to, i * 0.04f, share, onArrive));
        }
    }

    IEnumerator CoinRoutine(Vector2 from, RectTransform to, float delay, int share, Action<int> onArrive)
    {
        var coin = Instantiate(coinPrefab, layer);
        var rect = coin.rectTransform;
        rect.position = from;

        float unit = layer.lossyScale.x;
        Vector2 scatter = from + UnityEngine.Random.insideUnitCircle * (130f * unit);
        yield return Tween.Run(0.22f, t => rect.position = Vector2.Lerp(from, scatter, Tween.OutCubic(t)));
        yield return new WaitForSecondsRealtime(delay);
        yield return Tween.Run(0.4f, t => rect.position = Vector2.Lerp(scatter, to.position, t * t));

        Destroy(coin.gameObject);
        onArrive?.Invoke(share);
        Sfx.Play(Sound.Coin, UnityEngine.Random.Range(0.97f, 1.06f), 0.6f);
    }

    public void ClearUi()
    {
        StopAllCoroutines();
        hitStop = null;
        Time.timeScale = 1f;
        foreach (Transform child in layer) Destroy(child.gameObject);
        foreach (Transform child in transform)
            if (child != particles.transform) Destroy(child.gameObject);
    }

    public void HitStop(float duration)
    {
        if (hitStop != null) StopCoroutine(hitStop);
        hitStop = StartCoroutine(HitStopRoutine(duration));
    }

    IEnumerator HitStopRoutine(float duration)
    {
        Time.timeScale = 0.05f;
        yield return new WaitForSecondsRealtime(duration);
        Time.timeScale = 1f;
        hitStop = null;
    }
}
