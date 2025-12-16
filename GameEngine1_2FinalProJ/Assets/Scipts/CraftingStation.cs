using UnityEngine;

[RequireComponent(typeof(LineRenderer))]
public class CraftingStation : MonoBehaviour
{
    [Header("Station Settings")]
    public CraftingType stationType = CraftingType.Workbench; // 제작대인지 화로인지 설정
    public float interactionRange = 3.0f; // 감지 범위

    [Header("Visuals")]
    public Color rangeColor = Color.green;
    public int segments = 50;

    private LineRenderer line;
    private Transform player;

    void Start()
    {
        // 라인 렌더러 초기화 (범위 표시용)
        line = GetComponent<LineRenderer>();
        line.useWorldSpace = false;
        line.startWidth = 0.1f;
        line.endWidth = 0.1f;
        line.loop = true;
        line.material = new Material(Shader.Find("Sprites/Default"));
        line.startColor = rangeColor;
        line.endColor = rangeColor;

        DrawCircle();

        GameObject p = GameObject.FindGameObjectWithTag("Player");
        if (p != null) player = p.transform;
    }

    void DrawCircle()
    {
        line.positionCount = segments;
        float angle = 0f;
        float angleStep = 360f / segments;

        for (int i = 0; i < segments; i++)
        {
            float x = Mathf.Sin(Mathf.Deg2Rad * angle) * interactionRange;
            float z = Mathf.Cos(Mathf.Deg2Rad * angle) * interactionRange;
            line.SetPosition(i, new Vector3(x, 0.1f, z));
            angle += angleStep;
        }
    }

    // 플레이어가 범위 안에 있는지 확인
    public bool IsPlayerInRange()
    {
        if (player == null) return false;
        return Vector3.Distance(transform.position, player.position) <= interactionRange;
    }
}