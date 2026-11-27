using UnityEngine;

[RequireComponent(typeof(RectTransform))]
public class SafeArea : MonoBehaviour
{
    Rect applied;

    // сразу при включении: экраны считают позиции элементов в тот же кадр
    void OnEnable() => Apply();

    void Update() => Apply();

    void Apply()
    {
        var area = Screen.safeArea;
        if (area == applied) return;
        applied = area;

        var rect = (RectTransform)transform;
        var screen = new Vector2(Screen.width, Screen.height);
        rect.anchorMin = area.min / screen;
        rect.anchorMax = area.max / screen;
        rect.offsetMin = rect.offsetMax = Vector2.zero;
    }
}
