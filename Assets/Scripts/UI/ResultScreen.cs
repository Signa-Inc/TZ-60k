using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class ResultScreen : MonoBehaviour
{
    [SerializeField] TMP_Text title, subtitle;
    [SerializeField] Image ribbon;
    [SerializeField] Sprite winRibbon, loseRibbon;
    [SerializeField] RectTransform rays;
    [SerializeField] Image[] stars;
    [SerializeField] Sprite starOn, starOff;
    [SerializeField] GameObject[] tiles;
    [SerializeField] TMP_Text mergesValue, comboValue, tierValue;
    [SerializeField] Image tierBall;
    [SerializeField] Sprite[] ballSprites;
    [SerializeField] GameObject bonusLine, recordLine;
    [SerializeField] TMP_Text bonusText, recordText;
    [SerializeField] RectTransform rewardBox, rewardRow, rewardIcon;
    [SerializeField] TMP_Text rewardText;
    [SerializeField] TMP_Text balanceText;
    [SerializeField] RectTransform balanceChip, balanceIcon;
    [SerializeField] GameObject buttons;
    [SerializeField] TMP_Text retryText;
    [SerializeField] GameObject upgradeBadge;

    public void Show(BattleResult result)
    {
        gameObject.SetActive(true);
        StopAllCoroutines();

        title.text = result.Win ? "ПОБЕДА!" : "ПОРАЖЕНИЕ";
        subtitle.text = result.Win ? "Волна " + result.Wave + " пройдена" : "Монеты остаются с тобой";
        ribbon.sprite = result.Win ? winRibbon : loseRibbon;
        rays.gameObject.SetActive(result.Win);
        retryText.text = result.Win ? "ДАЛЬШЕ" : "ЕЩЁ РАЗ";

        mergesValue.text = result.Merges.ToString();
        comboValue.text = "x" + Mathf.Max(1, result.BestCombo);
        int tier = Mathf.Max(1, result.BestTier);
        tierValue.text = (1 << tier).ToString();
        tierBall.sprite = ballSprites[Mathf.Min(tier, ballSprites.Length) - 1];

        bonusText.text = "Бонус за победу  +" + result.WinBonus;
        recordText.text = "Рекорд слияния " + (1 << result.RecordTier) + "  +" + result.RecordBonus;
        LayoutReward(result);

        StartCoroutine(Reveal(result));
    }

    // сумма и строки под ней встают по центру карточки, сколько бы строк ни было
    void LayoutReward(BattleResult result)
    {
        int lines = (result.WinBonus > 0 ? 1 : 0) + (result.RecordBonus > 0 ? 1 : 0);
        float sumY = lines == 2 ? 62f : lines == 1 ? 34f : -14f;
        float lineY = lines == 2 ? -68f : -108f;

        rewardRow.anchoredPosition = new Vector2(0f, sumY);

        ((RectTransform)bonusLine.transform).anchoredPosition = new Vector2(0f, lineY);
        // при поражении строки бонуса нет - рекорд встаёт на её место
        ((RectTransform)recordLine.transform).anchoredPosition = new Vector2(0f, result.WinBonus > 0 ? lineY - 86f : lineY);
    }

    IEnumerator Reveal(BattleResult result)
    {
        int balance = Progress.Coins - result.Total;
        balanceText.text = balance.ToString();
        rewardText.text = "0";

        foreach (var star in stars) star.sprite = starOff;
        foreach (var tile in tiles) tile.SetActive(false);
        bonusLine.SetActive(false);
        recordLine.SetActive(false);
        upgradeBadge.SetActive(AnyAffordable());

        Appear(ribbon.gameObject);
        Appear(rewardBox.gameObject);
        // кнопки сразу, чтобы рестарт не ждал конца анимации
        Appear(buttons);
        yield return new WaitForSecondsRealtime(0.3f);

        for (int i = 0; i < result.Stars; i++)
        {
            stars[i].sprite = starOn;
            StartCoroutine(Tween.Punch(stars[i].transform, 0.45f));
            Sfx.Play(Sound.Coin, 0.8f + i * 0.2f);
            yield return new WaitForSecondsRealtime(0.22f);
        }

        foreach (var tile in tiles)
        {
            Appear(tile);
            yield return new WaitForSecondsRealtime(0.08f);
        }

        yield return CountReward(0, result.Coins, 0.6f);
        int reward = result.Coins;

        if (result.WinBonus > 0)
        {
            Appear(bonusLine);
            Sfx.Play(Sound.Buy, 1.1f);
            yield return CountReward(reward, reward + result.WinBonus, 0.35f);
            reward += result.WinBonus;
        }

        if (result.RecordBonus > 0)
        {
            Appear(recordLine);
            Sfx.Play(Sound.Joker);
            yield return CountReward(reward, reward + result.RecordBonus, 0.35f);
        }

        yield return new WaitForSecondsRealtime(0.15f);
        Fx.Instance.FlyCoins(rewardIcon.position, balanceIcon, result.Total, 16, share =>
        {
            balance += share;
            balanceText.text = balance.ToString();
            StartCoroutine(Tween.Punch(balanceChip, 0.1f));
        });
    }

    void Update() => rays.Rotate(0f, 0f, -20f * Time.unscaledDeltaTime);

    void Appear(GameObject target)
    {
        target.SetActive(true);
        StartCoroutine(Tween.PopIn(target.transform, 0f));
    }

    IEnumerator CountReward(int from, int to, float duration)
    {
        yield return Tween.Run(duration, t => rewardText.text = Mathf.RoundToInt(Mathf.Lerp(from, to, t)).ToString());
        yield return Tween.Punch(rewardText.transform, 0.18f);
    }

    static bool AnyAffordable()
    {
        for (int i = 0; i < Progress.Levels.Length; i++)
            if (Progress.CanBuy(i)) return true;
        return false;
    }
}
