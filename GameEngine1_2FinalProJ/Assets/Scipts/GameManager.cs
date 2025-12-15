using UnityEngine;
using System.Collections.Generic;
using TMPro;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance;

    [Header("Cheat Settings")]
    // ★ 여기에 에디터에서 만든 'Stabilizer' ItemData를 드래그해서 넣으세요!
    public ItemData stabilizerItemData;

    [Header("References")]
    public MapGenerator mapGenerator;
    public TextMeshProUGUI timeText; // 현재 시간 표시용 UI

    [Header("Status")]
    public MapType currentTime = MapType.Morning;

    // ★ 핵심: 유지기가 설치된 좌표들을 기억하는 저장소 (중복 방지를 위해 HashSet 사용)
    // 낮(Noon)에 설치하면 여기에 저장되고, 밤(Night)에 맵을 만들 때 이걸 참고합니다.
    public HashSet<Vector3Int> activeStabilizers = new HashSet<Vector3Int>();

    private Dictionary<MapType, HashSet<Vector3Int>> brokenBlocks = new Dictionary<MapType, HashSet<Vector3Int>>();

    [Header("Debug")]
    public float stabilizerRange = 5f; // 범위값 변수화 (나중에 조절하기 편하게)
    public bool showGizmos = true;

    private void Awake()
    {
        Instance = this;

        brokenBlocks[MapType.Morning] = new HashSet<Vector3Int>();
        brokenBlocks[MapType.Noon] = new HashSet<Vector3Int>();
        brokenBlocks[MapType.Night] = new HashSet<Vector3Int>();
    }

    private void Start()
    {
        UpdateMap();
    }

    void Update()
    {
        // T키로 시간 여행 테스트
        if (Input.GetKeyDown(KeyCode.T))
        {
            GoToNextTime();
        }

        if (Input.GetKeyDown(KeyCode.F1))
        {
            GiveCheatItem();
        }
    }
    void GiveCheatItem()
    {
        if (stabilizerItemData != null && InventoryManager.Instance != null)
        {
            // 아이템 1개 추가
            InventoryManager.Instance.AddItem(stabilizerItemData, 1);
            Debug.Log("치트 사용: 지형 유지기 획득!");
        }
        else
        {
            Debug.LogWarning("GameManager에 'Stabilizer Item Data'가 연결되지 않았거나 인벤토리가 없습니다.");
        }
    }

    public void GoToNextTime()
    {
        switch (currentTime)
        {
            case MapType.Morning: currentTime = MapType.Noon; break;
            case MapType.Noon: currentTime = MapType.Night; break;
            case MapType.Night: currentTime = MapType.Morning; break;
        }
        UpdateMap();
    }

    void UpdateMap()
    {
        // 맵 생성기에게 "이 시간대로 만들어줘"라고 명령
        mapGenerator.GenerateMap(currentTime);

        if (timeText != null)
            timeText.text = $"Time: {currentTime}";

        Debug.Log($"[시간 이동] {currentTime} 도착!");
    }

    // ★ 유지기 설치 시 호출 (MapGenerator에서 부름)
    public void AddStabilizer(Vector3Int pos)
    {
        if (!activeStabilizers.Contains(pos))
        {
            activeStabilizers.Add(pos);
            Debug.Log($"유지기 가동됨! 좌표: {pos}");
        }
    }

    // ★ 유지기 파괴 시 호출
    public void RemoveStabilizer(Vector3Int pos)
    {
        if (activeStabilizers.Contains(pos))
        {
            activeStabilizers.Remove(pos);
            Debug.Log($"유지기 비활성화. 좌표: {pos}");
        }
    }

    public bool IsStabilizedZone(int x, int z)
    {
        foreach (var pos in activeStabilizers)
        {
            float dist = Vector2.Distance(new Vector2(x, z), new Vector2(pos.x, pos.z));
            if (dist <= stabilizerRange) return true;
        }
        return false;
    }

    public void RecordBrokenBlock(MapType time, Vector3Int pos)
    {
        if (!brokenBlocks.ContainsKey(time))
        {
            brokenBlocks[time] = new HashSet<Vector3Int>();
        }

        if (!brokenBlocks[time].Contains(pos))
        {
            brokenBlocks[time].Add(pos);
            Debug.Log($"[{time}] 블록 파괴 기록됨: {pos}");
        }
    }

    // ★ 추가: 맵 생성기가 "여기 부서진 곳인가요?" 물어볼 때 대답하는 함수
    public bool IsBlockBroken(MapType time, int x, int y, int z)
    {
        if (brokenBlocks.ContainsKey(time))
        {
            return brokenBlocks[time].Contains(new Vector3Int(x, y, z));
        }
        return false;
    }

    private void OnDrawGizmos()
    {
        if (!showGizmos) return;

        Gizmos.color = Color.cyan; // 하늘색으로 표시

        // 활성화된 모든 유지기 위치에 동그라미 그리기
        foreach (var pos in activeStabilizers)
        {
            // 블록 중심에 맞추기 위해 +0.5f
            Vector3 center = new Vector3(pos.x, pos.y, pos.z);
            Gizmos.DrawWireSphere(center, stabilizerRange);
        }
    }
}