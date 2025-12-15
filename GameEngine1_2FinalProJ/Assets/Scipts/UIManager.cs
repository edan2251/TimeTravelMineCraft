using UnityEngine;

public class UIManager : MonoBehaviour
{
    public static UIManager Instance;

    public bool IsUIOpen { get; private set; }

    private void Awake()
    {
        Instance = this;
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
}