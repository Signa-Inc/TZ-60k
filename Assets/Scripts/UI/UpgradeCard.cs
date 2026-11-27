using System.Globalization;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class UpgradeCard : MonoBehaviour
{
    [SerializeField] int id;
    [SerializeField] TMP_Text levelText, effectText, costText;
    [SerializeField] Button button;
    [SerializeField] Image buttonImage;
    [SerializeField] GameObject coinIcon;
    [SerializeField] Sprite affordable, expensive;
    [SerializeField] string unit;

    public Button Button => button;
    public int Id => id;

    public void Refresh()
    {
        int level = Progress.Levels[id];
        bool maxed = Progress.IsMaxed(id);

        levelText.text = "ур. " + (level + 1);
        string next = maxed ? "" : "  »  <color=#1E9A32>" + Value(level + 1) + "</color>";
        effectText.text = Value(level) + next + " <size=70%>" + unit + "</size>";
        costText.text = maxed ? "МАКС" : Progress.Cost(id).ToString();
        coinIcon.SetActive(!maxed);
        button.interactable = Progress.CanBuy(id);
        buttonImage.sprite = button.interactable ? affordable : expensive;
    }

    string Value(int level)
    {
        switch (id)
        {
            case Progress.Rate: return Format(Progress.FireRateAt(level));
            case Progress.Power: return Format(Progress.DamageAt(level));
            case Progress.Blast: return Mathf.RoundToInt(Progress.BlastAt(level) * 100f) + "%";
            default: return Progress.ShieldsAt(level).ToString();
        }
    }

    static string Format(float value) => value.ToString("0.#", CultureInfo.InvariantCulture);
}
