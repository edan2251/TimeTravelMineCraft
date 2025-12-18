using System.Collections;
using UnityEngine;

public class HandAction : MonoBehaviour
{
    [Header("References")]
    public Transform handMount; // 아까 만든 HandMount 연결

    [Header("Settings")]
    public float swingSpeed = 15f; // 휘두르는 속도
    public float swingAngle = 80f; // 휘두르는 각도 (내려찍는 정도)

    private bool isSwinging = false;
    private Quaternion initialLocalRotation;

    void Start()
    {
        // 시작할 때 원래 손 각도를 기억해둠
        if (handMount != null)
            initialLocalRotation = handMount.localRotation;
    }

    void Update()
    {
        // UI가 열려있지 않을 때만 클릭 감지
        if (UIManager.Instance != null && UIManager.Instance.IsUIOpen) return;
        if (handMount == null) return;

        // 좌클릭(채광) 또는 우클릭(설치) 시 애니메이션 실행
        if (!isSwinging && (Input.GetMouseButtonDown(0) || Input.GetMouseButtonDown(1)))
        {
            StartCoroutine(Swing());
        }
    }

    IEnumerator Swing()
    {
        isSwinging = true;

        // 목표 각도: 원래 각도에서 X축으로 swingAngle만큼 더 회전 (내려찍기)
        Quaternion targetRot = initialLocalRotation * Quaternion.Euler(swingAngle, 0, 0);

        float t = 0f;

        // 1. 내려치기 (Slerp로 부드럽게)
        while (t < 1f)
        {
            t += Time.deltaTime * swingSpeed;
            handMount.localRotation = Quaternion.Slerp(initialLocalRotation, targetRot, t);
            yield return null;
        }

        t = 0f;

        // 2. 다시 올라오기
        while (t < 1f)
        {
            t += Time.deltaTime * swingSpeed;
            handMount.localRotation = Quaternion.Slerp(targetRot, initialLocalRotation, t);
            yield return null;
        }

        // 오차 보정: 확실하게 원위치
        handMount.localRotation = initialLocalRotation;
        isSwinging = false;
    }
}