using UnityEngine;
using TMPro;
using UnityEngine.UI;
using UnityEngine.SceneManagement; // 씬 이름 가져오기 위해 필요

public class UIManager : MonoBehaviour
{
    public static UIManager Instance;
    public bool IsUIOpen { get; private set; }

    [Header("Panels")]
    public GameObject pausePanel;
    public GameObject gameClearPanel;

    [Header("Pause Buttons")]
    public Button pauseRestartButton; // 일시정지 -> 재시작
    public Button pauseMainButton;    // 일시정지 -> 메인으로

    [Header("Clear Buttons")]
    public Button clearRestartButton; // 클리어 -> 재시작
    public Button clearMainButton;    // 클리어 -> 메인으로

    private bool isPaused = false;

    private void Awake()
    {
        Instance = this;

        // 패널 초기화 (꺼두기)
        if (pausePanel) pausePanel.SetActive(false);
        if (gameClearPanel) gameClearPanel.SetActive(false);

        // --- 버튼 기능 연결 ---

        // 1. 일시정지 패널 버튼
        if (pauseRestartButton) pauseRestartButton.onClick.AddListener(RestartGame);
        if (pauseMainButton) pauseMainButton.onClick.AddListener(GoToTitle);

        // 2. 게임 클리어 패널 버튼
        if (clearRestartButton) clearRestartButton.onClick.AddListener(RestartGame);
        if (clearMainButton) clearMainButton.onClick.AddListener(GoToTitle);
    }

    void Update()
    {
        // ESC 키로 일시정지 토글 (게임 클리어 상태가 아닐 때만)
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            if (!gameClearPanel.activeSelf)
            {
                TogglePause();
            }
        }
    }

    public void TogglePause()
    {
        isPaused = !isPaused;

        // 패널 활성/비활성
        if (pausePanel != null) pausePanel.SetActive(isPaused);

        // 시간 정지/재개
        Time.timeScale = isPaused ? 0f : 1f;

        // 마우스 커서 상태 동기화
        SetUIState(isPaused);
    }

    public void ShowGameClear()
    {
        if (gameClearPanel != null) gameClearPanel.SetActive(true);
        SetUIState(true);    // 마우스 보이기
        Time.timeScale = 0f; // 게임 정지
    }

    public void SetUIState(bool isOpen)
    {
        IsUIOpen = isOpen;

        if (isOpen)
        {
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }
        else
        {
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }
    }

    // ★ 기능: 게임 재시작
    void RestartGame()
    {
        Time.timeScale = 1f; // 시간 정상화 필수
        // 현재 떠있는 씬을 다시 로드
        string currentSceneName = SceneManager.GetActiveScene().name;
        SceneTransitionManager.Instance.LoadScene(currentSceneName);
    }

    // ★ 기능: 메인으로 이동
    void GoToTitle()
    {
        Time.timeScale = 1f; // 시간 정상화 필수
        SceneTransitionManager.Instance.LoadScene("TitleScene");
    }
}