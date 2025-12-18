using UnityEngine;
using System.Collections.Generic;
using TMPro;

[System.Serializable]
public class SaplingInfo
{
    public Vector3Int position;
    public MapType plantedTime;
    public bool isGrown;
}

public class GameManager : MonoBehaviour
{
    public static GameManager Instance;

    [Header("Environment")]
    public TimeEnvironmentManager environmentManager;

    [Header("Settings")]
    public ItemData stabilizerItemData;
    public MapGenerator mapGenerator;
    public TextMeshProUGUI timeText;

    [Header("Game State")]
    public MapType currentTime = MapType.Morning;
    public int lanternCount = 0;
    public const int TARGET_LANTERNS = 4;

    [Header("Visuals")]
    public GameObject lanternSuccessEffect; // 등불 성공 이펙트

    // 유적 위치 (Y=15 공중)
    public Vector3Int[] ruinPositions = new Vector3Int[]
    {
        new Vector3Int(10, 0, 10), // Y값은 0으로 해둬도 상관없음 (자동 계산)
        new Vector3Int(10, 0, 40),
        new Vector3Int(40, 0, 10),
        new Vector3Int(40, 0, 40)
    };
    private bool[] ruinActiveStates = new bool[4];

    // 데이터 저장소
    public HashSet<Vector2Int> activeStabilizers = new HashSet<Vector2Int>();
    private Dictionary<MapType, HashSet<Vector3Int>> brokenBlocks = new Dictionary<MapType, HashSet<Vector3Int>>();
    private Dictionary<MapType, Dictionary<Vector3Int, int>> placedBlocks = new Dictionary<MapType, Dictionary<Vector3Int, int>>();
    private Dictionary<MapType, Dictionary<Vector3Int, InventorySlot[]>> storageData = new Dictionary<MapType, Dictionary<Vector3Int, InventorySlot[]>>();
    public List<SaplingInfo> plantedSaplings = new List<SaplingInfo>();

    // ID 상수
    const int SAPLING_ID = 13;
    public const int LANTERN_ID = 16;
    public const int STABILIZER_ID = 11;
    private bool isTimeShifting = false;

    [Header("Debug")]
    public float stabilizerRange = 5f;
    public bool showGizmos = true;

    private void Awake()
    {
        Instance = this;
        brokenBlocks[MapType.Morning] = new HashSet<Vector3Int>();
        brokenBlocks[MapType.Noon] = new HashSet<Vector3Int>();
        brokenBlocks[MapType.Night] = new HashSet<Vector3Int>();

        storageData[MapType.Morning] = new Dictionary<Vector3Int, InventorySlot[]>();
        storageData[MapType.Noon] = new Dictionary<Vector3Int, InventorySlot[]>();
        storageData[MapType.Night] = new Dictionary<Vector3Int, InventorySlot[]>();
    }

    private void Start()
    {
        if (environmentManager != null)
            environmentManager.SetAtmosphere(currentTime);

        // false: 위치 저장 안 함 (맵 중앙 스폰)
        mapGenerator.GenerateMap(currentTime, false);
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.T)) GoToNextTime();
        if (Input.GetKeyDown(KeyCode.F1)) GiveCheatItem();
    }

    // 1. 데이터 가져오기 (없으면 null 리턴)
    public InventorySlot[] GetStorageData(MapType time, Vector3Int pos)
    {
        if (storageData.ContainsKey(time) && storageData[time].ContainsKey(pos))
        {
            return storageData[time][pos];
        }
        return null;
    }

    // 2. 데이터 저장하기 (또는 업데이트)
    public void SaveStorageData(MapType time, Vector3Int pos, InventorySlot[] slots)
    {
        if (!storageData.ContainsKey(time))
            storageData[time] = new Dictionary<Vector3Int, InventorySlot[]>();

        if (storageData[time].ContainsKey(pos))
        {
            storageData[time][pos] = slots;
        }
        else
        {
            storageData[time].Add(pos, slots);
        }
    }

    // 3. 데이터 삭제하기 (보관함을 곡괭이로 캤을 때)
    public void RemoveStorageData(MapType time, Vector3Int pos)
    {
        if (storageData.ContainsKey(time) && storageData[time].ContainsKey(pos))
        {
            storageData[time].Remove(pos);
        }
    }

    // 시간 이동
    public void GoToNextTime()
    {
        // 이미 이동 중이거나 UI가 켜져 있으면 무시
        if (isTimeShifting) return;
        if (UIManager.Instance != null && UIManager.Instance.IsUIOpen) return;

        isTimeShifting = true;

        // ★ 페이드 매니저에게 "화면 가려지면 ActualTimeChange 함수 실행해줘" 라고 위임
        if (SceneTransitionManager.Instance != null)
        {
            SceneTransitionManager.Instance.PlayFadeSequence(ActualTimeChange);
        }
        else
        {
            // 매니저가 없으면(비상시) 그냥 바로 실행
            ActualTimeChange();
        }
    }


    void ActualTimeChange()
    {
        GrowSaplings();

        switch (currentTime)
        {
            case MapType.Morning: currentTime = MapType.Noon; break;
            case MapType.Noon: currentTime = MapType.Night; break;
            case MapType.Night: currentTime = MapType.Morning; break;
        }

        bool shouldSavePos = (currentTime != MapType.Night);

        // 분위기 변경
        if (environmentManager != null)
            environmentManager.SetAtmosphere(currentTime);

        // 맵 재생성 (플레이어 눈에는 안 보임)
        mapGenerator.GenerateMap(currentTime, shouldSavePos);

        if (timeText != null)
            timeText.text = $"Time: {currentTime}";

        Debug.Log($"[시간 이동] {currentTime} 도착!");

        // 작업 끝났으니 플래그 해제
        isTimeShifting = false;
    }

    // 사망 처리도 페이드 효과 적용하면 좋음
    public void HandlePlayerDeath()
    {
        if (isTimeShifting) return;
        isTimeShifting = true;

        Debug.Log("플레이어 사망 -> 아침 리스폰 페이드 시작");

        if (SceneTransitionManager.Instance != null)
        {
            SceneTransitionManager.Instance.PlayFadeSequence(() =>
            {
                // 페이드 중간에 실행될 내용 (람다식)
                currentTime = MapType.Morning;
                if (environmentManager != null) environmentManager.SetAtmosphere(MapType.Morning);
                if (mapGenerator != null) mapGenerator.RespawnAtMorning();
                if (timeText != null) timeText.text = $"Time: {currentTime}";

                isTimeShifting = false;
            });
        }
        else
        {
            currentTime = MapType.Morning;
            if (mapGenerator != null) mapGenerator.RespawnAtMorning();
            isTimeShifting = false;
        }
    }

    // 묘목 성장 및 생존 로직
    void GrowSaplings()
    {
        for (int i = plantedSaplings.Count - 1; i >= 0; i--)
        {
            SaplingInfo sapling = plantedSaplings[i];
            if (!sapling.isGrown)
            {
                if (sapling.plantedTime == MapType.Noon)
                {
                    // 30% 확률로 성장 (0.3 이하)
                    if (Random.value <= 0.3f)
                    {
                        sapling.isGrown = true;
                        Debug.Log("묘목 성장 성공!");
                    }
                    else
                    {
                        if (placedBlocks.ContainsKey(MapType.Noon))
                            placedBlocks[MapType.Noon].Remove(sapling.position);
                        plantedSaplings.RemoveAt(i);
                        Debug.Log("묘목이 말라 죽었습니다.");
                    }
                }
                else
                {
                    sapling.isGrown = true;
                }
            }
        }
    }
    
    // 유적 목표 체크
    public void CheckLanternObjective(Vector3Int pos)
    {
        for (int i = 0; i < ruinPositions.Length; i++)
        {
            if (ruinActiveStates[i]) continue;

            if (pos == ruinPositions[i])
            {
                ruinActiveStates[i] = true;
                lanternCount++;
                Debug.Log($"유적 봉인 해제! ({lanternCount}/{TARGET_LANTERNS})");

                if (lanternSuccessEffect != null)
                    Instantiate(lanternSuccessEffect, new Vector3(pos.x, pos.y, pos.z), Quaternion.identity);

                if (timeText != null) timeText.text = $"Lanterns: {lanternCount}/{TARGET_LANTERNS}";

                if (lanternCount >= TARGET_LANTERNS) GameClear();
                break;
            }
        }
    }

    void GameClear()
    {
        Debug.Log("GAME CLEAR!!");
        if (timeText != null) timeText.text = "GAME CLEAR!!";

        if (UIManager.Instance != null)
        {
            UIManager.Instance.ShowGameClear();
        }
    }


    // 데이터 기록 함수들
    public void RecordPlacedBlock(MapType time, Vector3Int pos, int blockID)
    {
        if (!placedBlocks.ContainsKey(time)) placedBlocks[time] = new Dictionary<Vector3Int, int>();

        if (placedBlocks[time].ContainsKey(pos)) placedBlocks[time][pos] = blockID;
        else placedBlocks[time].Add(pos, blockID);

        if (blockID == SAPLING_ID) RegisterSapling(time, pos);
        if (IsBlockBroken(time, pos.x, pos.y, pos.z)) brokenBlocks[time].Remove(pos);

        // ★★★ [핵심 변경] 유지기라면? -> X, Z 좌표만 리스트에 등록 (다른 시간대 딕셔너리에 복사 X)
        if (blockID == STABILIZER_ID)
        {
            AddStabilizer(pos);
        }
    }

    public void RemovePlacedBlockRecord(MapType time, Vector3Int pos)
    {
        if (placedBlocks.ContainsKey(time) && placedBlocks[time].ContainsKey(pos))
        {
            if (placedBlocks[time][pos] == SAPLING_ID) RemoveSapling(time, pos);
            placedBlocks[time].Remove(pos);
        }
    }

    public int GetPlacedBlockID(MapType time, int x, int y, int z)
    {
        Vector3Int pos = new Vector3Int(x, y, z);
        return (placedBlocks.ContainsKey(time) && placedBlocks[time].ContainsKey(pos)) ? placedBlocks[time][pos] : -1;
    }

    // 유지기 및 파괴 기록 함수들 (단순 호출)
    public void AddStabilizer(Vector3Int pos)
    {
        Vector2Int coord = new Vector2Int(pos.x, pos.z);
        if (!activeStabilizers.Contains(coord)) activeStabilizers.Add(coord);
    }
    public void RemoveStabilizer(Vector3Int pos)
    {
        // X, Z만 추출해서 삭제
        Vector2Int coord = new Vector2Int(pos.x, pos.z);
        if (activeStabilizers.Contains(coord)) activeStabilizers.Remove(coord);

        // 현재 시간대의 기록에서만 삭제하면 됨 (다른 시간대는 자동 생성 로직이므로 기록이 없음)
        RemovePlacedBlockRecord(currentTime, pos);
    }
    public void RecordBrokenBlock(MapType time, Vector3Int pos)
    {
        if (!brokenBlocks.ContainsKey(time)) brokenBlocks[time] = new HashSet<Vector3Int>();
        if (!brokenBlocks[time].Contains(pos)) brokenBlocks[time].Add(pos);
    }
    public bool IsBlockBroken(MapType time, int x, int y, int z) => brokenBlocks.ContainsKey(time) && brokenBlocks[time].Contains(new Vector3Int(x, y, z));

    public bool IsStabilizedZone(int x, int z)
    {
        Vector2 checkPos = new Vector2(x, z);
        foreach (var pos in activeStabilizers)
        {
            // pos는 이제 Vector2Int입니다.
            if (Vector2.Distance(checkPos, pos) <= stabilizerRange) return true;
        }
        return false;
    }

    // 묘목 헬퍼 함수
    public void RegisterSapling(MapType time, Vector3Int pos)
    {
        if (!plantedSaplings.Exists(x => x.position == pos && x.plantedTime == time))
            plantedSaplings.Add(new SaplingInfo { position = pos, plantedTime = time, isGrown = false });
    }
    public void RemoveSapling(MapType time, Vector3Int pos)
    {
        SaplingInfo target = plantedSaplings.Find(x => x.position == pos && x.plantedTime == time);
        if (target != null) plantedSaplings.Remove(target);
    }
    public SaplingInfo GetSaplingInfo(MapType time, Vector3Int pos) => plantedSaplings.Find(x => x.position == pos && x.plantedTime == time);

    void GiveCheatItem()
    {
        if (stabilizerItemData != null && InventoryManager.Instance != null)
            InventoryManager.Instance.AddItem(stabilizerItemData, 1);
    }


    private void OnDrawGizmos()
    {
        if (!showGizmos) return;
        Gizmos.color = Color.cyan;
        // 높이를 모르므로 대충 플레이어 높이나 0으로 그리기
        foreach (var pos in activeStabilizers)
            Gizmos.DrawWireSphere(new Vector3(pos.x, 10, pos.y), stabilizerRange); // pos.y가 Z좌표임
        Gizmos.color = Color.yellow;
        foreach (var pos in ruinPositions) Gizmos.DrawWireCube(pos, Vector3.one);
    }
}