using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerController : MonoBehaviour
{
    [Header("Movement Settings")]
    public float moveSpeed = 6f;
    public float jumpPower = 6f;
    public float gravity = -20.0f; 
    public float mouseSensitivity = 2f;

    [Header("Status")]
    float xRotation = 0f;
    Vector3 velocity;
    bool isGrounded;
    private bool isCursorLocked = true;

    [Header("Safety")]
    public float voidHeightThreshold = -20f; // ★ 추가: 이 높이 아래로 떨어지면 사망

    CharacterController controller;
    Transform cam;

    void Awake()
    {
        controller = GetComponent<CharacterController>();
        cam = GetComponentInChildren<Camera>()?.transform;

        SetCursorLock(true);
    }

    void Update()
    {
        if (UIManager.Instance != null && UIManager.Instance.IsUIOpen) return;

        if (transform.position.y < voidHeightThreshold)
        {
            if (GameManager.Instance != null)
            {
                // 계속 호출되지 않게 위치를 살짝 위로 옮기거나, 컨트롤러를 끄고 호출
                // 여기서는 GameManager가 맵을 재생성하며 플레이어를 비활성화할 것이므로 호출만 함
                GameManager.Instance.HandlePlayerDeath();
                this.enabled = false; // 중복 사망 방지를 위해 잠시 끔 (리스폰 시 다시 켜짐)
                return;
            }
        }

        if (!isCursorLocked)
        {
            HandleCursorLock();
            return;
        }

        HandleCursorLock();
        HandleLook();
        HandleMove();
    }

    void HandleMove()
    {
        isGrounded = controller.isGrounded;

        if (isGrounded && velocity.y < 0)
        {
            velocity.y = -2f;
        }

        float h = Input.GetAxis("Horizontal");
        float v = Input.GetAxis("Vertical");

        Vector3 move = transform.right * h + transform.forward * v;
        controller.Move(move * moveSpeed * Time.deltaTime);

        if (Input.GetButtonDown("Jump") && isGrounded)
        {
            velocity.y = Mathf.Sqrt(jumpPower * -2f * gravity);
        }

        velocity.y += gravity * Time.deltaTime;
        controller.Move(velocity * Time.deltaTime);
    }

    void HandleLook()
    {
        float mouseX = Input.GetAxis("Mouse X") * mouseSensitivity;
        float mouseY = Input.GetAxis("Mouse Y") * mouseSensitivity;

        transform.Rotate(Vector3.up * mouseX);

        xRotation -= mouseY;
        xRotation = Mathf.Clamp(xRotation, -80f, 80f);

        if (cam != null)
        {
            cam.localRotation = Quaternion.Euler(xRotation, 0f, 0f);
        }
    }

    private void HandleCursorLock()
    {
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            SetCursorLock(false);
        }
        else if (!isCursorLocked && Input.GetMouseButtonDown(0))
        {
            SetCursorLock(true);
        }
    }

    private void SetCursorLock(bool lockState)
    {
        isCursorLocked = lockState;
        Cursor.lockState = lockState ? CursorLockMode.Locked : CursorLockMode.None;
        Cursor.visible = !lockState;
    }

    public void Teleport(Vector3 position)
    {
        controller.enabled = false;
        transform.position = position;
        controller.enabled = true;
    }
}
