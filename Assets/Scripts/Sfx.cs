using UnityEngine;

public enum Sound { Shot, Hit, Pop, Merge, Coin, Hurt, Click, Buy, Win, Lose, Joker }

public class Sfx : MonoBehaviour
{
    static Sfx instance;

    [SerializeField] AudioClip[] clips; // порядок как в Sound

    AudioSource[] sources;
    float[] lastPlayed;
    int next;

    void Awake()
    {
        instance = this;
        lastPlayed = new float[clips.Length];
        sources = new AudioSource[12];
        for (int i = 0; i < sources.Length; i++)
        {
            sources[i] = gameObject.AddComponent<AudioSource>();
            sources[i].playOnAwake = false;
        }
    }

    public static void Play(Sound sound, float pitch = 1f, float volume = 1f)
    {
        int id = (int)sound;
        if (Time.unscaledTime - instance.lastPlayed[id] < 0.04f) return;
        instance.lastPlayed[id] = Time.unscaledTime;

        var source = instance.sources[instance.next];
        instance.next = (instance.next + 1) % instance.sources.Length;
        source.clip = instance.clips[id];
        source.pitch = pitch;
        source.volume = volume;
        source.Play();
    }
}
