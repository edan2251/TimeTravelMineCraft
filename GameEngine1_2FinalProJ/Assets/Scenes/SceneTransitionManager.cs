using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using System.Collections;

public class SceneTransitionManager : MonoBehaviour
{
    public static SceneTransitionManager Instance;

    [Header("UI References")]
    public Canvas fadeCanvas; // 페이드용 캔버스
    public Image fadeImage;   // 검은색 이미지

    [Header("Settings")]
    public float fadeDuration = 1.0f;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject); // 씬이 바껴도 파괴되지 않음

            // 캔버스 설정 강제 (최상단 노출)
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
        // 게임 켜지면 페이드 인으로 시작
        StartCoroutine(Fade(0f));
    }

    // 외부에서 호출: "이 씬으로 이동해줘"
    public void LoadScene(string sceneName)
    {
        StartCoroutine(TransitionRoutine(sceneName));
    }

    IEnumerator TransitionRoutine(string sceneName)
    {
        // 1. 페이드 아웃 (화면이 검어짐)
        yield return StartCoroutine(Fade(1f));

        // 2. 씬 로딩
        Time.timeScale = 1f; // 일시정지 상태일 수도 있으니 시간 정상화
        yield return SceneManager.LoadSceneAsync(sceneName);

        // 3. 페이드 인 (화면이 밝아짐)
        yield return StartCoroutine(Fade(0f));
    }

    IEnumerator Fade(float targetAlpha)
    {
        float startAlpha = fadeImage.color.a;
        float time = 0f;

        while (time < fadeDuration)
        {
            time += Time.unscaledDeltaTime; // 일시정지 중에도 페이드 되게 unscaled 사용
            float alpha = Mathf.Lerp(startAlpha, targetAlpha, time / fadeDuration);

            Color c = fadeImage.color;
            c.a = alpha;
            fadeImage.color = c;

            yield return null;
        }

        // 확실하게 목표값 설정
        Color finalColor = fadeImage.color;
        finalColor.a = targetAlpha;
        fadeImage.color = finalColor;

        // 투명해지면 클릭 방해 안 하게 Raycast Target 끄기
        fadeImage.raycastTarget = (targetAlpha > 0.1f);
    }
}