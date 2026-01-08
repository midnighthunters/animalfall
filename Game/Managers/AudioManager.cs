using UnityEngine;

public class AudioManager : MonoBehaviour
{
    public static AudioManager Instance;
    public enum SfxType { Collect, WrongTap, Explosion, LevelWin, LevelLose, ShieldBreak }

    public AudioClip collect;
    public AudioClip wrong;
    public AudioClip explosion;
    public AudioClip levelWin;
    public AudioClip levelLose;
    public AudioClip shieldBreak;

    private AudioSource sfx;

    private void Awake()
    {
        if (Instance == null) Instance = this; else Destroy(gameObject);
        sfx = gameObject.AddComponent<AudioSource>();
    }

    public void PlaySFX(SfxType type)
    {
        switch (type)
        {
            case SfxType.Collect: sfx.PlayOneShot(collect); break;
            case SfxType.WrongTap: sfx.PlayOneShot(wrong); break;
            case SfxType.Explosion: sfx.PlayOneShot(explosion); break;
            case SfxType.LevelWin: sfx.PlayOneShot(levelWin); break;
            case SfxType.LevelLose: sfx.PlayOneShot(levelLose); break;
            case SfxType.ShieldBreak: sfx.PlayOneShot(shieldBreak); break;
        }
    }
}
