using UnityEngine;

[RequireComponent(typeof(LineRenderer))]
public class StabilizerRadiusViewer : MonoBehaviour
{
    [Header("Settings")]
    public int segments = 50; // 원을 얼마나 부드럽게 그릴지 (점의 개수)
    public float lineWidth = 0.1f; // 선의 두께
    public Color lineColor = Color.cyan; // 선 색상

    private LineRenderer line;

    void Start()
    {
        line = GetComponent<LineRenderer>();

        // 라인 렌더러 기본 세팅
        line.useWorldSpace = false; // 로컬 좌표 기준 (유지기 따라다님)
        line.startWidth = lineWidth;
        line.endWidth = lineWidth;
        line.loop = true; // 원이 끊기지 않고 이어지게 설정
        line.material = new Material(Shader.Find("Sprites/Default")); // 기본 쉐이더 (반투명 가능)
        line.startColor = lineColor;
        line.endColor = lineColor;

        // 범위 가져오기
        float range = 5f;
        if (GameManager.Instance != null)
        {
            range = GameManager.Instance.stabilizerRange;
        }

        DrawCircle(range);
    }

    void DrawCircle(float radius)
    {
        line.positionCount = segments;

        float angle = 0f;
        float angleStep = 360f / segments;

        for (int i = 0; i < segments; i++)
        {
            // 삼각함수로 원의 좌표 구하기 (X, Z 평면)
            float x = Mathf.Sin(Mathf.Deg2Rad * angle) * radius;
            float z = Mathf.Cos(Mathf.Deg2Rad * angle) * radius;

            // Y축은 바닥보다 살짝 띄워서(0.1f) 겹침 방지
            line.SetPosition(i, new Vector3(x, 0.1f, z));

            angle += angleStep;
        }
    }

    // (선택사항) 게임 실행 중에 범위가 바뀌면 실시간 업데이트하고 싶을 때 사용
    /*
    void Update()
    {
        if (GameManager.Instance != null)
        {
             DrawCircle(GameManager.Instance.stabilizerRange);
        }
    }
    */
}