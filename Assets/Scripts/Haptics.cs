using UnityEngine;

// performHapticFeedback вместо Handheld.Vibrate: короткий отклик и без разрешения в манифесте
public static class Haptics
{
    const int LongPress = 0;
    const int VirtualKey = 1;

    public static void Light() => Perform(VirtualKey);
    public static void Heavy() => Perform(LongPress);

#if UNITY_ANDROID && !UNITY_EDITOR
    static AndroidJavaObject view;

    static void Perform(int type)
    {
        if (view == null)
        {
            using var player = new AndroidJavaClass("com.unity3d.player.UnityPlayer");
            using var activity = player.GetStatic<AndroidJavaObject>("currentActivity");
            view = activity.Call<AndroidJavaObject>("getWindow").Call<AndroidJavaObject>("getDecorView");
        }
        view.Call<bool>("performHapticFeedback", type);
    }
#else
    static void Perform(int type) { }
#endif
}
