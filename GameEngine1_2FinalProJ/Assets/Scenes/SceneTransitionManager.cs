using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using System.Collections;
using System; // Action을 쓰기 위해 필요

public class SceneTransitionManager : MonoBehaviour
{
    public static SceneTransitionManager Instance;

    [Header("UI References")]
    public Canvas fadeCanvas;
    public Image fadeImage;

    [Header("Settings")]
    public float fadeDuration = 1.0f;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject); // 씬이 바껴도 파괴되지 않음

            fadeCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
            fadeCanvas.sortingOrder = 9999;
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void Start()
    {
        // 게임 시작 시 페이드 인
        StartCoroutine(Fade(0f));
    }

    public void LoadScene(string sceneName)
    {
        StartCoroutine(TransitionRoutine(sceneName));
    }

    // ★★★ [신규 기능] 씬 이동 없이 페이드 효과만 주고, 중간에 할 일(함수)을 실행
    public void PlayFadeSequence(Action onMiddle)
    {
        StartCoroutine(FadeSequenceRoutine(onMiddle));
    }

    IEnumerator FadeSequenceRoutine(Action onMiddle)
    {
        // 1. 화면을 검게 만듦 (Fade Out)
        yield return StartCoroutine(Fade(1f));

        // 2. 중간에 할 일 실행 (여기서 맵이 바뀜)
        // 화면이 깜깜해서 플레이어는 맵이 바뀌는 과정을 못 봄
        if (onMiddle != null) onMiddle.Invoke();

        // 맵 생성 렉이 페이드 중에 튀지 않게 한 프레임 대기
        yield return null;

        // 3. 다시 화면을 밝게 만듦 (Fade In)
        yield return StartCoroutine(Fade(0f));
    }

    IEnumerator TransitionRoutine(string sceneName)
    {
        yield return StartCoroutine(Fade(1f));
        Time.timeScale = 1f;
        yield return SceneManager.LoadSceneAsync(sceneName);
        yield return StartCoroutine(Fade(0f));
    }

    IEnumerator Fade(float targetAlpha)
    {
        float startAlpha = fadeImage.color.a;
        float time = 0f;

        while (time < fadeDuration)
        {
            time += Time.unscaledDeltaTime;
            float alpha = Mathf.Lerp(startAlpha, targetAlpha, time / fadeDuration);

            Color c = fadeImage.color;
            c.a = alpha;
            fadeImage.color = c;

            yield return null;
        }

        Color finalColor = fadeImage.color;
        finalColor.a = targetAlpha;
        fadeImage.color = finalColor;
        fadeImage.raycastTarget = (targetAlpha > 0.1f); // 투명할 땐 클릭 통과
    }
}