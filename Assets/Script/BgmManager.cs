using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

public class BgmManager : MonoBehaviour
{
    public static BgmManager I { get; private set; }

    [Header("Scene Names")]
    [SerializeField] private string titleSceneName = "TitleScene";
    [SerializeField] private string gameSceneName = "GameScene";

    [Header("BGM Clips")]
    [SerializeField] private AudioClip titleBgm;
    [SerializeField] private AudioClip gameBgm;

    [Header("Settings")]
    [Range(0f, 1f)]
    [SerializeField] private float volume = 0.5f;
    [SerializeField] private float fadeDuration = 0.5f;

    private AudioSource audioSource;
    private Coroutine fadeCoroutine;

    private void Awake()
    {
        if (I != null && I != this)
        {
            Destroy(gameObject);
            return;
        }

        I = this;
        DontDestroyOnLoad(gameObject);

        audioSource = GetComponent<AudioSource>();

        if (audioSource == null)
            audioSource = gameObject.AddComponent<AudioSource>();

        audioSource.loop = true;
        audioSource.playOnAwake = false;
        audioSource.volume = volume;
    }

    private void OnEnable()
    {
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private void OnDisable()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    private void Start()
    {
        PlayBgmForScene(SceneManager.GetActiveScene().name);
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        PlayBgmForScene(scene.name);
    }

    private void PlayBgmForScene(string sceneName)
    {
        if (sceneName == titleSceneName)
        {
            PlayBgm(titleBgm);
            return;
        }

        if (sceneName == gameSceneName)
        {
            PlayBgm(gameBgm);
            return;
        }

        // シーン名が違う場合でも、ゲーム画面側で使いたい場合はここで調整
        // 例：SampleScene を使っている場合
        if (sceneName == "SampleScene")
        {
            PlayBgm(gameBgm);
        }
    }

    private void PlayBgm(AudioClip clip)
    {
        if (clip == null)
            return;

        // 同じBGMがすでに流れているなら、再生し直さない
        if (audioSource.clip == clip && audioSource.isPlaying)
            return;

        if (fadeCoroutine != null)
            StopCoroutine(fadeCoroutine);

        fadeCoroutine = StartCoroutine(ChangeBgmRoutine(clip));
    }

    private IEnumerator ChangeBgmRoutine(AudioClip nextClip)
    {
        float startVolume = audioSource.volume;

        // 今のBGMをフェードアウト
        if (audioSource.isPlaying)
        {
            float timer = 0f;

            while (timer < fadeDuration)
            {
                timer += Time.unscaledDeltaTime;
                float t = Mathf.Clamp01(timer / fadeDuration);

                audioSource.volume = Mathf.Lerp(startVolume, 0f, t);

                yield return null;
            }
        }

        audioSource.Stop();
        audioSource.clip = nextClip;
        audioSource.volume = 0f;
        audioSource.Play();

        // 新しいBGMをフェードイン
        {
            float timer = 0f;

            while (timer < fadeDuration)
            {
                timer += Time.unscaledDeltaTime;
                float t = Mathf.Clamp01(timer / fadeDuration);

                audioSource.volume = Mathf.Lerp(0f, volume, t);

                yield return null;
            }
        }

        audioSource.volume = volume;
        fadeCoroutine = null;
    }

    public void SetVolume(float newVolume)
    {
        volume = Mathf.Clamp01(newVolume);

        if (audioSource != null)
            audioSource.volume = volume;
    }

    public void StopBgm()
    {
        if (fadeCoroutine != null)
            StopCoroutine(fadeCoroutine);

        audioSource.Stop();
    }
}