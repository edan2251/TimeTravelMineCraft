using UnityEngine;
using TMPro;

public class TitleSceneManager : MonoBehaviour
{
    [Header("UI")]
    public TextMeshProUGUI blinkText; // "- 클릭해서 시작하세요 -"
    public float blinkSpeed = 2f;

    [Header("Scene Name")]
    public string gameSceneName = "GameScene"; // 이동할 게임 씬 이름

    private bool isLoading = false;

    void Update()
    {
        // 1. 텍스트 깜빡임 (Mathf.Sin 활용)
        float alpha = (Mathf.Sin(Time.time * blinkSpeed) + 1f) / 2f; // 0~1 사이 반복
        blinkText.color = new Color(blinkText.color.r, blinkText.color.g, blinkText.color.b, alpha);

        // 2. 클릭 감지 -> 게임 시작
        if (!isLoading && Input.GetMouseButtonDown(0))
        {
            isLoading = true;
            // 씬 전환 매니저에게 요청
            SceneTransitionManager.Instance.LoadScene(gameSceneName);
        }
    }
}