using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class MetaScreen : MonoBehaviour
{
    [SerializeField] Camera cam;
    [SerializeField] Cannon cannon;
    [SerializeField] RectTransform cannonSlot;
    [SerializeField] TMP_Text balanceText;
    [SerializeField] RectTransform balanceChip;
    [SerializeField] UpgradeCard[] cards;
    [SerializeField] Image[] medals;
    [SerializeField] Sprite[] medalSprites;
    [SerializeField] Sprite medalLocked;
    [SerializeField] TMP_Text goalText, playText;

    const float DesignHeight = 1920f;

    Transform nextMedal;
    RectTransform[] bottomBlock;
    float[] restY;

    void Update()
    {
        // следующая цель в лестнице рекордов слегка покачивается
        foreach (var medal in medals)
        {
            bool target = medal.transform == nextMedal;
            medal.transform.localScale = Vector3.one * (target ? 1.08f + Mathf.Sin(Time.unscaledTime * 5f) * 0.08f : 1f);
        }
    }

    void Awake()
    {
        foreach (var card in cards)
        {
            var c = card;
            c.Button.onClick.AddListener(() => Buy(c));
        }
    }

    public void Show()
    {
        gameObject.SetActive(true);
        Refresh();
        playText.text = "В БОЙ · ВОЛНА " + Progress.Wave;
        for (int i = 0; i < cards.Length; i++)
            StartCoroutine(Tween.PopIn(cards[i].transform, 0.06f * i));

        Canvas.ForceUpdateCanvases();
        LiftBottomBlock();
        Vector3 point = cam.ScreenToWorldPoint(cannonSlot.position);
        cannon.Showcase(new Vector3(point.x, point.y - 1.15f, 0f));
    }

    // на высоких экранах карточки, рекорды и кнопка поднимаются, чтобы под пушкой не было дыры
    void LiftBottomBlock()
    {
        var safe = (RectTransform)balanceChip.parent;
        float lift = Mathf.Max(0f, safe.rect.height - DesignHeight) * 0.75f;

        if (bottomBlock == null)
        {
            var list = new List<RectTransform>();
            foreach (var card in cards) list.Add((RectTransform)card.transform);
            list.Add((RectTransform)medals[0].transform.parent);
            list.Add((RectTransform)playText.transform.parent);
            bottomBlock = list.ToArray();
            restY = Array.ConvertAll(bottomBlock, r => r.anchoredPosition.y);
        }

        for (int i = 0; i < bottomBlock.Length; i++)
            bottomBlock[i].anchoredPosition = new Vector2(bottomBlock[i].anchoredPosition.x, restY[i] + lift);
    }

    void Buy(UpgradeCard card)
    {
        if (!Progress.TryBuy(card.Id)) return;

        Sfx.Play(Sound.Buy, 1f + Progress.Levels[card.Id] * 0.04f);
        Refresh();
        StartCoroutine(Tween.Punch(card.transform, 0.1f));
        StartCoroutine(Tween.Punch(balanceChip, 0.15f));
        cannon.Celebrate();

        Vector3 at = cannon.transform.position + Vector3.up * 1.4f;
        Fx.Instance.Burst(at, new Color(1f, 0.85f, 0.3f), 30, 8f);
        Fx.Instance.Ring(at, 2.5f, new Color(1f, 0.85f, 0.3f));
    }

    void Refresh()
    {
        balanceText.text = Progress.Coins.ToString();
        foreach (var card in cards) card.Refresh();

        // медали идут с числа 8 (tier 3)
        for (int i = 0; i < medals.Length; i++)
            medals[i].sprite = Progress.BestTier >= i + 3 ? medalSprites[i] : medalLocked;

        int next = Mathf.Max(Progress.BestTier + 1, 3);
        nextMedal = next - 3 < medals.Length ? medals[next - 3].transform : null;
        goalText.text = next < medals.Length + 3
            ? "Слей шар " + (1 << next) + ": награда +" + Progress.RecordBonus(next)
            : "Все рекорды слияния собраны!";
    }
}
