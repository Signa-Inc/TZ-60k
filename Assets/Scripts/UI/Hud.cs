using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class Hud : MonoBehaviour
{
    [SerializeField] Battle battle;
    [SerializeField] TMP_Text coinsText, waveText;
    [SerializeField] RectTransform coinIcon, coinChip;
    [SerializeField] RectTransform progressFill;
    [SerializeField] RectTransform[] jokerMarks;
    [SerializeField] Image[] shields;
    [SerializeField] Image damageFlash;
    [SerializeField] CanvasGroup banner;
    [SerializeField] TMP_Text bannerTitle, bannerText;
    [SerializeField] GameObject hint;
    [SerializeField] TMP_Text hintText;
    [SerializeField] RectTransform hintHand;

    int coins;
    bool mergeExplained;
    Coroutine bannerRoutine, punchRoutine;

    public void Begin()
    {
        StopAllCoroutines();
        bannerRoutine = punchRoutine = null;
        coins = 0;
        coinsText.text = "0";
        waveText.text = "Волна " + Progress.Wave;
        banner.alpha = 0f;
        damageFlash.color = Color.clear;
        hint.SetActive(true);
        hintText.text = "Зажми и веди: пушка стреляет сама";
        hintHand.gameObject.SetActive(true);

        // джокеры отмечены на полосе волны, чтобы их можно было ждать
        var marks = Battle.JokerMarks();
        for (int i = 0; i < jokerMarks.Length; i++)
        {
            jokerMarks[i].gameObject.SetActive(i < marks.Length);
            if (i >= marks.Length) continue;
            jokerMarks[i].anchorMin = jokerMarks[i].anchorMax = new Vector2(marks[i], 0.5f);
        }

        Update();
    }

    public void HideHint() => hint.SetActive(false);

    public void Merged()
    {
        if (mergeExplained) return;
        mergeExplained = true;
        StartCoroutine(Tutorial());
    }

    IEnumerator Tutorial()
    {
        yield return ShowHint("Золотой шар не делится и приносит вдвое больше монет", 0.5f, 4f);
        yield return ShowHint("Толкай выстрелами одинаковые шары друг в друга", 0.4f, 4.5f);
    }

    IEnumerator ShowHint(string text, float delay, float duration)
    {
        hint.SetActive(false);
        yield return new WaitForSeconds(delay);
        hintText.text = text;
        hintHand.gameObject.SetActive(false);
        hint.SetActive(true);
        yield return Tween.PopIn(hint.transform, 0f);
        yield return new WaitForSeconds(duration);
        hint.SetActive(false);
    }

    public void CollectCoins(Vector3 world, int amount, int icons)
    {
        Fx.Instance.FlyCoins(Fx.Instance.ToScreen(world), coinIcon, amount, icons, share =>
        {
            coins += share;
            coinsText.text = coins.ToString();
            if (!isActiveAndEnabled) return;
            if (punchRoutine != null) StopCoroutine(punchRoutine);
            punchRoutine = StartCoroutine(Tween.Punch(coinChip, 0.14f));
        });
    }

    public void Banner(string title, string text)
    {
        if (bannerRoutine != null) StopCoroutine(bannerRoutine);
        bannerRoutine = StartCoroutine(BannerRoutine(title, text));
    }

    IEnumerator BannerRoutine(string title, string text)
    {
        bannerTitle.text = title;
        bannerText.text = text;
        banner.alpha = 1f;
        yield return Tween.PopIn(banner.transform, 0f);
        yield return new WaitForSecondsRealtime(1.4f);
        yield return Tween.Run(0.25f, t => banner.alpha = 1f - t);
    }

    public void Hurt()
    {
        StartCoroutine(Tween.Run(0.5f, t => damageFlash.color = new Color(1f, 0.15f, 0.2f, (1f - t) * 0.45f)));
    }

    void Update()
    {
        for (int i = 0; i < shields.Length; i++)
        {
            shields[i].gameObject.SetActive(i < Progress.Shields);
            shields[i].color = i < battle.Shields ? Color.white : new Color(0.15f, 0.15f, 0.25f, 0.55f);
        }

        var max = progressFill.anchorMax;
        max.x = Mathf.MoveTowards(max.x, Mathf.Max(0.06f, battle.WaveProgress), Time.unscaledDeltaTime * 0.5f);
        progressFill.anchorMax = max;

        if (hintHand.gameObject.activeInHierarchy)
            hintHand.anchoredPosition = new Vector2(Mathf.Sin(Time.unscaledTime * 3f) * 220f, hintHand.anchoredPosition.y);
    }
}
