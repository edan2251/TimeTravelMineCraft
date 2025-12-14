using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerController : MonoBehaviour
{
    [Header("Movement Settings")]
    public float moveSpeed = 6f;
    public float jumpPower = 6f;
    public float gravity = -20.0f; // 중력을 조금 더 강하게 줘야 점프가 쫀득합니다.
    public float mouseSensitivity = 2f;

    [Header("Status")]
    float xRotation = 0f;
    Vector3 velocity;
    bool isGrounded;
    private bool isCursorLocked = true;

    CharacterController controller;
    Transform cam;

    void Awake()
    {
        controller = GetComponent<CharacterController>();
        // 자식 오브젝트 중 카메라 찾기
        cam = GetComponentInChildren<Camera>()?.transform;

        // 시작하자마자 커서 잠금
        SetCursorLock(true);
    }

    void Update()
    {
        // ★ 추가된 부분: UI가 열려있으면 시점 회전 중단
        if (UIManager.Instance != null && UIManager.Instance.IsUIOpen) return;

        // (기존 코드) UI가 닫혀있을 때만 커서 락 체크
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

        // 땅에 닿아있을 때 y속도 초기화 (0으로 하면 가끔 붕 뜨는 버그가 있어서 -2 정도로 누름)
        if (isGrounded && velocity.y < 0)
        {
            velocity.y = -2f;
        }

        float h = Input.GetAxis("Horizontal");
        float v = Input.GetAxis("Vertical");

        // 로컬 좌표 기준 이동 방향 계산
        Vector3 move = transform.right * h + transform.forward * v;
        controller.Move(move * moveSpeed * Time.deltaTime);

        // 점프
        if (Input.GetButtonDown("Jump") && isGrounded)
        {
            velocity.y = Mathf.Sqrt(jumpPower * -2f * gravity);
        }

        // 중력 적용
        velocity.y += gravity * Time.deltaTime;
        controller.Move(velocity * Time.deltaTime);
    }

    void HandleLook()
    {
        float mouseX = Input.GetAxis("Mouse X") * mouseSensitivity;
        float mouseY = Input.GetAxis("Mouse Y") * mouseSensitivity;

        // 좌우 회전 (몸통)
        transform.Rotate(Vector3.up * mouseX);

        // 상하 회전 (카메라)
        xRotation -= mouseY;
        xRotation = Mathf.Clamp(xRotation, -80f, 80f);

        if (cam != null)
        {
            cam.localRotation = Quaternion.Euler(xRotation, 0f, 0f);
        }
    }

    private void HandleCursorLock()
    {
        // ESC: 커서 풀기
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            SetCursorLock(false);
        }
        // 클릭: 커서 다시 잠그기 (커서가 풀려있을 때만)
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

    // ★ 맵 생성 후 플레이어를 이동시키기 위한 함수 ★
    public void Teleport(Vector3 position)
    {
        // CharacterController가 켜져 있으면 transform.position 변경이 무시될 수 있음
        controller.enabled = false;
        transform.position = position;
        controller.enabled = true;
    }
}
