using UnityEngine;
using System.Collections.Generic;
using TMPro;

// 묘목 정보를 저장할 간단한 클래스
[System.Serializable]
public class SaplingInfo
{
    public Vector3Int position; // 심은 위치
    public MapType plantedTime; // 언제 심었는지 (Morning? Noon?)
    public bool isGrown;        // 다 자랐는지 여부
}

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

    private Dictionary<MapType, Dictionary<Vector3Int, int>> placedBlocks = new Dictionary<MapType, Dictionary<Vector3Int, int>>();

    public List<SaplingInfo> plantedSaplings = new List<SaplingInfo>();

    // 상수 (묘목 ID) - 13번을 묘목으로 쓰기로 했죠?
    const int SAPLING_ID = 13;

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
        mapGenerator.GenerateMap(currentTime, false);
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

    public void HandlePlayerDeath()
    {
        Debug.Log("플레이어 사망! 아침 시간대의 안전한 곳으로 리스폰합니다.");

        // 1. 시간대를 아침으로 강제 변경
        currentTime = MapType.Morning;

        // 2. 맵 생성기에게 "리스폰 모드"로 맵을 만들라고 명령
        // (그냥 GenerateMap을 부르면 플레이어가 떨어진 위치(지하)를 기억해버리므로, 
        //  리스폰 전용 함수를 MapGenerator에 새로 만들어서 호출합니다.)
        if (mapGenerator != null)
        {
            mapGenerator.RespawnAtMorning();
        }

        if (timeText != null) timeText.text = $"Time: {currentTime}";
    }

    public void RecordPlacedBlock(MapType time, Vector3Int pos, int blockID)
    {
        if (!placedBlocks.ContainsKey(time)) placedBlocks[time] = new Dictionary<Vector3Int, int>();

        // 이미 있으면 덮어쓰기, 없으면 추가
        if (placedBlocks[time].ContainsKey(pos))
            placedBlocks[time][pos] = blockID;
        else
            placedBlocks[time].Add(pos, blockID);

        if (blockID == SAPLING_ID)
        {
            RegisterSapling(time, pos);
        }

        // 중요: 만약 이 자리가 '파괴된 기록'이 있었다면, 다시 설치했으니 파괴 기록은 지워줌
        if (IsBlockBroken(time, pos.x, pos.y, pos.z))
        {
            brokenBlocks[time].Remove(pos);
        }
    }

    public void RemovePlacedBlockRecord(MapType time, Vector3Int pos)
    {
        if (placedBlocks.ContainsKey(time) && placedBlocks[time].ContainsKey(pos))
        {
            // ★ 추가: 블록을 지울 때 그게 묘목이었다면 묘목 리스트에서도 삭제
            int id = placedBlocks[time][pos];
            if (id == SAPLING_ID)
            {
                RemoveSapling(time, pos);
            }

            placedBlocks[time].Remove(pos);
        }
    }

    // ★ 추가: 맵 생성기가 "여기에 플레이어가 뭐 설치했나요?" 물어볼 때
    public int GetPlacedBlockID(MapType time, int x, int y, int z)
    {
        Vector3Int pos = new Vector3Int(x, y, z);
        if (placedBlocks.ContainsKey(time) && placedBlocks[time].ContainsKey(pos))
        {
            return placedBlocks[time][pos];
        }
        return -1; // -1은 "설치된 거 없음" 의미 (MapGenerator의 AIR_ID와 맞춤)
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
        GrowSaplings();

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
        mapGenerator.GenerateMap(currentTime, true);

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

    void GrowSaplings()
    {
        // 리스트를 거꾸로 돌면서 삭제가 가능하게 함
        for (int i = plantedSaplings.Count - 1; i >= 0; i--)
        {
            SaplingInfo sapling = plantedSaplings[i];

            if (!sapling.isGrown)
            {
                // ★ 추가: 낮(Noon)에 심은 묘목은 30% 확률로만 성공
                if (sapling.plantedTime == MapType.Noon)
                {
                    float chance = Random.value; // 0.0 ~ 1.0

                    if (chance <= 0.7f)
                    {
                        // 30% 성공: 성장!
                        sapling.isGrown = true;
                        Debug.Log($"[성공] 묘목이 혹독한 태양을 견뎌냈습니다! ({sapling.position})");
                    }
                    else
                    {
                        // 70% 실패: 묘목 파괴 (증발)
                        // 1. 설치된 블록 기록(PlacedBlocks)에서 지우기
                        if (placedBlocks.ContainsKey(MapType.Noon))
                        {
                            placedBlocks[MapType.Noon].Remove(sapling.position);
                        }

                        // 2. 묘목 리스트에서 지우기
                        plantedSaplings.RemoveAt(i);

                        Debug.Log($"[실패] 묘목이 말라 죽었습니다... ({sapling.position})");
                    }
                }
                else
                {
                    // 아침이나 다른 시간대는 100% 성장 (기존 로직)
                    sapling.isGrown = true;
                }
            }
        }
    }

    // ★ 묘목 등록 (MapGenerator에서 호출)
    public void RegisterSapling(MapType time, Vector3Int pos)
    {
        // 중복 등록 방지
        if (plantedSaplings.Exists(x => x.position == pos && x.plantedTime == time)) return;

        SaplingInfo newSapling = new SaplingInfo();
        newSapling.position = pos;
        newSapling.plantedTime = time;
        newSapling.isGrown = false; // 처음 심으면 안 자란 상태

        plantedSaplings.Add(newSapling);
    }

    // ★ 묘목 제거 (자라서 나무가 되거나, 플레이어가 캐버렸을 때)
    public void RemoveSapling(MapType time, Vector3Int pos)
    {
        SaplingInfo target = plantedSaplings.Find(x => x.position == pos && x.plantedTime == time);
        if (target != null)
        {
            plantedSaplings.Remove(target);
        }
    }

    // ★ 묘목 상태 확인 (MapGenerator가 맵 그릴 때 물어봄)
    public SaplingInfo GetSaplingInfo(MapType time, Vector3Int pos)
    {
        return plantedSaplings.Find(x => x.position == pos && x.plantedTime == time);
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