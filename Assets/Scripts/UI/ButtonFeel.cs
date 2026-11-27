using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class ButtonFeel : MonoBehaviour, IPointerDownHandler, IPointerUpHandler
{
    [SerializeField] bool breathe;

    Button button;
    bool pressed;
    float scale = 1f;

    void Awake() => button = GetComponent<Button>();

    void OnEnable()
    {
        pressed = false;
        scale = 1f;
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        if (!button.interactable) return;
        pressed = true;
        Sfx.Play(Sound.Click);
    }

    public void OnPointerUp(PointerEventData eventData) => pressed = false;

    void Update()
    {
        scale = Mathf.MoveTowards(scale, pressed ? 0.92f : 1f, Time.unscaledDeltaTime * 1.5f);
        float pulse = breathe && !pressed ? Mathf.Sin(Time.unscaledTime * 4f) * 0.025f : 0f;
        transform.localScale = Vector3.one * (scale + pulse);
    }
}
