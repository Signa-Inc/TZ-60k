using UnityEngine;

[RequireComponent(typeof(Camera))]
public class CameraRig : MonoBehaviour
{
    const float GroundHeight = 2.4f;
    const float MinSize = 8.6f;
    const float BackgroundBottom = -1.1f;
    const float SkylineTop = 4.5f;

    [SerializeField] SpriteRenderer background;

    public float Top => cam.transform.position.y + cam.orthographicSize;

    Camera cam;
    float shake;

    void Awake()
    {
        cam = GetComponent<Camera>();
        Fit();
    }

    public void Shake(float amount) => shake = Mathf.Min(Mathf.Max(shake, amount), 0.6f);

    void LateUpdate()
    {
        Fit();
        if (shake <= 0f) return;

        transform.position += (Vector3)(Random.insideUnitCircle * shake);
        shake = Mathf.MoveTowards(shake, 0f, Time.unscaledDeltaTime * 2.2f);
    }

    void Fit()
    {
        cam.orthographicSize = Mathf.Max(Battle.HalfWidth / cam.aspect, MinSize);
        transform.position = new Vector3(0f, cam.orthographicSize - GroundHeight, -20f);

        // низ фона чуть под землёй, чтобы силуэты на картинке были видны над ней
        var size = background.sprite.bounds.size;
        float height = Top - BackgroundBottom + 0.7f;
        float width = cam.orthographicSize * cam.aspect * 2f + 1.4f;
        float scale = Mathf.Max(width / size.x, height / size.y);
        background.transform.localScale = new Vector3(scale, scale, 1f);
        // на широком экране картинка растягивается, поэтому опускаем её, чтобы силуэты не залезали на поле
        float skyline = size.y * 0.19f * scale;
        float bottom = Mathf.Min(BackgroundBottom, SkylineTop - skyline);
        background.transform.position = new Vector3(0f, bottom + size.y * scale * 0.5f, 10f);
    }
}
