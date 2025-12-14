using UnityEngine;

public class UIManager : MonoBehaviour
{
    public static UIManager Instance;

    // 현재 UI가 열려있는지 확인하는 변수
    public bool IsUIOpen { get; private set; }

    private void Awake()
    {
        Instance = this;
    }

    // UI 상태를 변경하는 함수 (다른 스크립트에서 호출)
    public void SetUIState(bool isOpen)
    {
        IsUIOpen = isOpen;

        if (isOpen)
        {
            // UI 열림: 커서 보이기 & 잠금 해제
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }
        else
        {
            // UI 닫힘: 커서 숨기기 & 중앙 고정
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }
    }
}