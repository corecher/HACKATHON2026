using UnityEngine;
using System.Collections.Generic;

public class SoundManager : Singleton<SoundManager>
{
    private AudioSource bgmSource;
    private List<AudioSource> sfxSources = new List<AudioSource>();
    
    [Header("Audio Clips")]
    public AudioClip defaultBgm;
    
    // 해커톤용 딕셔너리 (인스펙터 등록용 대신 이름으로 빠르게 찾기용)
    public List<AudioClip> sfxClips = new List<AudioClip>();
    private Dictionary<string, AudioClip> sfxDic = new Dictionary<string, AudioClip>();
    private void Start()
    {
        PlayBGM(defaultBgm);
    }

    protected override void Awake()
    {
        base.Awake();
        
        // BGM 소스 초기화
        bgmSource = gameObject.AddComponent<AudioSource>();
        bgmSource.loop = true;

        // 디렉토리 대신 리스트에 넣은 클립들 딕셔너리에 세팅
        foreach (var clip in sfxClips)
        {
            if (clip != null && !sfxDic.ContainsKey(clip.name))
                sfxDic.Add(clip.name, clip);
        }
    }

    public void PlayBGM(AudioClip clip, float volume = 0.5f)
    {
        if (clip == null) return;
        bgmSource.clip = clip;
        bgmSource.volume = volume;
        bgmSource.Play();
    }

    public void StopBGM() => bgmSource.Stop();

    // 사용법: SoundManager.Instance.PlaySFX("Explosion");
    public void PlaySFX(string clipName, float volume = 1f)
    {
        if (!sfxDic.ContainsKey(clipName))
        {
            Debug.LogWarning($"SFX 없음: {clipName}");
            return;
        }
        
        PlaySFX(sfxDic[clipName], volume);
    }

    public void PlaySFX(AudioClip clip, float volume = 1f)
    {
        if (clip == null) return;

        // 비어있는 오디오 소스 재활용
        AudioSource source = sfxSources.Find(s => !s.isPlaying);
        if (source == null)
        {
            source = gameObject.AddComponent<AudioSource>();
            sfxSources.Add(source);
        }

        source.clip = clip;
        source.volume = volume;
        source.Play();
    }
}
