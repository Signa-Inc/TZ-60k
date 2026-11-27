using System;
using System.Collections;
using UnityEngine;

public static class Tween
{
    public static IEnumerator Run(float duration, Action<float> step)
    {
        for (float t = 0f; t < duration; t += Time.unscaledDeltaTime)
        {
            step(t / duration);
            yield return null;
        }
        step(1f);
    }

    public static float OutCubic(float t) => 1f - Mathf.Pow(1f - t, 3f);

    public static float OutBack(float t)
    {
        float u = t - 1f;
        return 1f + u * u * (2.7f * u + 1.7f);
    }

    public static IEnumerator PopIn(Transform target, float delay)
    {
        target.localScale = Vector3.zero;
        if (delay > 0f) yield return new WaitForSecondsRealtime(delay);
        yield return Run(0.32f, t => target.localScale = Vector3.one * OutBack(t));
    }

    public static IEnumerator Punch(Transform target, float strength = 0.2f)
    {
        yield return Run(0.22f, t => target.localScale = Vector3.one * (1f + Mathf.Sin(t * Mathf.PI) * strength));
    }
}
