using UnityEngine;

public class HandSway : MonoBehaviour
{
    [Header("Settings")]
    public float amount = 0.02f;   // 흔들림 강도 (0.02 ~ 0.05 추천)
    public float maxAmount = 0.06f; // 최대 흔들림 범위
    public float smoothAmount = 6f; // 부드러움 정도 (높을수록 빠릿함)

    private Vector3 initialPosition;

    void Start()
    {
        // 원래 위치(0,0,0 등)를 기억해둠
        initialPosition = transform.localPosition;
    }

    void Update()
    {
        // UI가 열려있을 땐 흔들림 방지 (마우스 커서 움직임에 반응하니까)
        if (UIManager.Instance != null && UIManager.Instance.IsUIOpen) return;

        // 마우스 입력값 가져오기 (좌우, 상하 반전)
        float movementX = -Input.GetAxis("Mouse X") * amount;
        float movementY = -Input.GetAxis("Mouse Y") * amount;

        // 너무 멀리 가지 않게 범위 제한 (Clamp)
        movementX = Mathf.Clamp(movementX, -maxAmount, maxAmount);
        movementY = Mathf.Clamp(movementY, -maxAmount, maxAmount);

        // 목표 위치 계산
        Vector3 finalPosition = new Vector3(movementX, movementY, 0);

        // 부드럽게 이동 (Lerp)
        transform.localPosition = Vector3.Lerp(
            transform.localPosition,
            finalPosition + initialPosition,
            Time.deltaTime * smoothAmount
        );
    }
}