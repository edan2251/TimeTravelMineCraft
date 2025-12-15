using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public enum MapType
{
    Morning, // 숲, 물 있음, 평화로움
    Noon,    // 사막, 물 없음, 거친 지형
    Night    // 공허, 물 없음, 부유섬(구멍 뚫림)
}

public class MapGenerator : MonoBehaviour
{
    [Header("Map Settings")]
    public MapType currentMapType = MapType.Morning;
    public int width = 50;
    public int depth = 50;
    public int maxHeight = 20;

    [Header("Theme Settings")]
    public int waterLevel = 4;
    public float noiseScale = 20f;
    [Range(0f, 1f)] public float nightVoidThreshold = 0.3f;

    [Header("Block Prefabs")]
    // 주의: 인덱스 번호가 Block ID와 일치해야 합니다. (예: 11번 요소에 유지기 프리팹)
    public List<GameObject> blockPrefabs;

    [Header("References")]
    public Transform playerTransform;

    [Header("Layer Settings")]
    public LayerMask blockLayer;

    private int[,,] mapData;
    const int STABILIZER_ID = 11;

    // 시드값 저장 변수
    private float seedX;
    private float seedZ;

    private Vector3? lastPlayerPos = null;

    void Start()
    {
        // 게임 시작 시 시드값 고정
        seedX = Random.Range(0f, 9999f);
        seedZ = Random.Range(0f, 9999f);

        StartCoroutine(GenerateMapRoutine());
    }

    public void GenerateMap(MapType type)
    {
        // 플레이어 위치 저장
        if (playerTransform != null)
        {
            lastPlayerPos = playerTransform.position;
        }

        currentMapType = type;
        StopAllCoroutines();
        StartCoroutine(GenerateMapRoutine());
    }

    [ContextMenu("Generate Map")]
    public void GenerateMap()
    {
        // 에디터 테스트용
        if (seedX == 0) seedX = Random.Range(0f, 9999f);
        if (seedZ == 0) seedZ = Random.Range(0f, 9999f);
        StartCoroutine(GenerateMapRoutine());
    }

    IEnumerator GenerateMapRoutine()
    {
        // 1. 플레이어 비활성화 (낙사 방지)
        if (playerTransform != null)
            playerTransform.gameObject.SetActive(false);

        mapData = new int[width, maxHeight + 1, depth];

        // 2. 기존 맵 삭제
        foreach (Transform child in transform) Destroy(child.gameObject);

        // 3. 데이터 채우기 (시드 기반)
        FillMapData();

        // 4. 파괴된 블록 정보 반영 (구멍 뚫기)
        ApplyBrokenBlocks();

        // 5. 실제 블록 생성 루프
        int blockCount = 0;
        for (int x = 0; x < width; x++)
        {
            for (int z = 0; z < depth; z++)
            {
                for (int y = 0; y <= maxHeight; y++)
                {
                    int id = mapData[x, y, z];

                    if (id == 0) continue;
                    // 최적화: 가려진 블록은 생성 안 함 (물은 예외)
                    if (IsHidden(x, y, z) && id != 3) continue;

                    SpawnBlockObj(x, y, z, id);
                    blockCount++;
                }
            }
            // 부하 분산
            if (x % 2 == 0) yield return null;
        }

        // 6. 유지기 주변 강제 갱신 (빈 공간 메꾸기)
        RefreshStabilizerZones();

        Debug.Log($"맵 생성 완료! 블록 수: {blockCount}");

        // 7. 플레이어 활성화 및 이동
        if (playerTransform != null)
            playerTransform.gameObject.SetActive(true);

        SpawnPlayer();
    }

    // ★ 수정된 부분: 메서드 범위가 꼬여있던 것을 수정했습니다.
    void RefreshStabilizerZones()
    {
        if (GameManager.Instance == null) return;

        float range = GameManager.Instance.stabilizerRange;

        foreach (var pos in GameManager.Instance.activeStabilizers)
        {
            int startX = Mathf.Max(0, pos.x - (int)range - 2);
            int endX = Mathf.Min(width - 1, pos.x + (int)range + 2);
            int startZ = Mathf.Max(0, pos.z - (int)range - 2);
            int endZ = Mathf.Min(depth - 1, pos.z + (int)range + 2);

            for (int x = startX; x <= endX; x++)
            {
                for (int z = startZ; z <= endZ; z++)
                {
                    for (int y = 0; y <= maxHeight; y++)
                    {
                        CheckAndSpawn(x, y, z);
                    }
                }
            }
        }
    }

    void ApplyBrokenBlocks()
    {
        if (GameManager.Instance == null) return;

        for (int x = 0; x < width; x++)
        {
            for (int z = 0; z < depth; z++)
            {
                for (int y = 0; y <= maxHeight; y++)
                {
                    // GameManager에게 "이 시간대, 이 좌표가 부서진 적 있니?" 물어봄
                    if (GameManager.Instance.IsBlockBroken(currentMapType, x, y, z))
                    {
                        mapData[x, y, z] = 0; // 공기로 변경
                    }
                }
            }
        }
    }

    void FillMapData()
    {
        for (int x = 0; x < width; x++)
        {
            for (int z = 0; z < depth; z++)
            {
                // 지형 높이 계산 (고정된 seedX, seedZ 사용)
                float noiseVal = Mathf.PerlinNoise((x + seedX) / noiseScale, (z + seedZ) / noiseScale);
                if (currentMapType == MapType.Noon) noiseVal *= 1.5f;

                int height = Mathf.FloorToInt(noiseVal * maxHeight);

                // 밤(Night) 맵 공허 처리
                if (currentMapType == MapType.Night)
                {
                    float voidNoise = Mathf.PerlinNoise((x + seedX) * 0.1f, (z + seedZ) * 0.1f);
                    bool shouldBeHole = (voidNoise < nightVoidThreshold);

                    // 유지기 주변은 구멍 뚫지 않음
                    if (shouldBeHole && GameManager.Instance != null && GameManager.Instance.IsStabilizedZone(x, z))
                    {
                        shouldBeHole = false;
                    }

                    if (shouldBeHole) height = 0;
                }

                for (int y = 0; y <= maxHeight; y++)
                {
                    // 1. 유지기(Stabilizer) 우선 배치
                    if (GameManager.Instance != null && GameManager.Instance.activeStabilizers.Contains(new Vector3Int(x, y, z)))
                    {
                        mapData[x, y, z] = STABILIZER_ID;
                        continue;
                    }

                    // 2. 일반 블록 배치
                    if (y <= height && height > 0)
                        mapData[x, y, z] = GetBlockIDByTheme(y, height);
                    else if (y <= waterLevel && currentMapType == MapType.Morning)
                        mapData[x, y, z] = 3; // 물
                    else
                        mapData[x, y, z] = 0; // 공기
                }
            }
        }
    }

    int GetBlockIDByTheme(int y, int surfaceHeight)
    {
        // 광물 생성 (랜덤)
        if (y < surfaceHeight - 4)
        {
            float rand = Random.value;
            if (rand < 0.05f) return 7; // Iron
            if (rand < 0.1f) return 8;  // Coal
            if (y < 3) return 2;        // Deep Stone
        }

        switch (currentMapType)
        {
            case MapType.Morning:
                if (y == surfaceHeight) return 1; // Grass
                if (y < surfaceHeight - 3) return 2; // Stone
                return 10; // Dirt
            case MapType.Noon:
                if (y == surfaceHeight) return 4; // Sand
                if (y < surfaceHeight - 3) return 2; // Stone
                return 5; // RedRock
            case MapType.Night:
                return 6; // VoidStone
            default: return 1;
        }
    }

    public void PlaceBlockAt(Vector3 worldPos, int blockID)
    {
        Vector3Int coord = WorldToGrid(worldPos);
        if (!IsIdxValid(coord.x, coord.y, coord.z)) return;
        if (mapData[coord.x, coord.y, coord.z] != 0) return;

        mapData[coord.x, coord.y, coord.z] = blockID;
        SpawnBlockObj(coord.x, coord.y, coord.z, blockID);

        if (blockID == STABILIZER_ID && GameManager.Instance != null)
        {
            GameManager.Instance.AddStabilizer(coord);
        }
    }

    public void RemoveBlockAt(Vector3 worldPos)
    {
        Vector3Int coord = WorldToGrid(worldPos);
        if (!IsIdxValid(coord.x, coord.y, coord.z)) return;

        int currentID = mapData[coord.x, coord.y, coord.z];
        if (currentID == 0) return;

        // 1. 유지기였다면 해제
        if (currentID == STABILIZER_ID && GameManager.Instance != null)
        {
            GameManager.Instance.RemoveStabilizer(coord);
        }

        // 2. 파괴 기록 저장
        if (GameManager.Instance != null)
        {
            GameManager.Instance.RecordBrokenBlock(currentMapType, coord);
        }

        // 3. 데이터 삭제 및 갱신
        mapData[coord.x, coord.y, coord.z] = 0;

        // 주의: 여기서 실제 오브젝트(GameObject)를 파괴하는 코드는 없습니다.
        // PlayerController나 상호작용 스크립트에서 Destroy(hit.collider.gameObject)를 호출해야 합니다.

        UpdateChunkAt(coord.x, coord.y, coord.z);
    }

    void SpawnBlockObj(int x, int y, int z, int id)
    {
        // 흙(10) 예외처리 및 인덱스 매핑
        int prefabIdx = id;
        if (id == 10) prefabIdx = 0; // Dirt를 0번 프리팹으로 쓴다는 가정 (필요시 수정)

        if (prefabIdx >= 0 && prefabIdx < blockPrefabs.Count)
        {
            Instantiate(blockPrefabs[prefabIdx], new Vector3(x, y, z), Quaternion.identity, transform);
        }
    }

    void UpdateChunkAt(int x, int y, int z)
    {
        CheckAndSpawn(x + 1, y, z);
        CheckAndSpawn(x - 1, y, z);
        CheckAndSpawn(x, y + 1, z);
        CheckAndSpawn(x, y - 1, z);
        CheckAndSpawn(x, y, z + 1);
        CheckAndSpawn(x, y, z - 1);
    }

    void CheckAndSpawn(int x, int y, int z)
    {
        if (!IsIdxValid(x, y, z)) return;

        int id = mapData[x, y, z];
        if (id == 0) return;

        // 이미 오브젝트가 있는지 확인 (중복 생성 방지)
        if (Physics.CheckSphere(new Vector3(x, y, z), 0.4f, blockLayer)) return;

        SpawnBlockObj(x, y, z, id);
    }

    public Vector3Int WorldToGrid(Vector3 pos)
    {
        return new Vector3Int(Mathf.RoundToInt(pos.x), Mathf.RoundToInt(pos.y), Mathf.RoundToInt(pos.z));
    }

    bool IsIdxValid(int x, int y, int z)
    {
        return x >= 0 && x < width && y >= 0 && y <= maxHeight && z >= 0 && z < depth;
    }

    bool IsHidden(int x, int y, int z)
    {
        if (x <= 0 || x >= width - 1 || y <= 0 || y >= maxHeight - 1 || z <= 0 || z >= depth - 1) return false;

        if (IsTransparent(x + 1, y, z)) return false;
        if (IsTransparent(x - 1, y, z)) return false;
        if (IsTransparent(x, y + 1, z)) return false;
        if (IsTransparent(x, y - 1, z)) return false;
        if (IsTransparent(x, y, z + 1)) return false;
        if (IsTransparent(x, y, z - 1)) return false;

        return true;
    }

    bool IsTransparent(int x, int y, int z)
    {
        int id = mapData[x, y, z];
        return (id == 0 || id == 3);
    }

    void SpawnPlayer()
    {
        if (playerTransform == null) return;

        int targetX, targetZ;

        if (lastPlayerPos.HasValue)
        {
            targetX = Mathf.RoundToInt(lastPlayerPos.Value.x);
            targetZ = Mathf.RoundToInt(lastPlayerPos.Value.z);

            targetX = Mathf.Clamp(targetX, 0, width - 1);
            targetZ = Mathf.Clamp(targetZ, 0, depth - 1);
        }
        else
        {
            targetX = width / 2;
            targetZ = depth / 2;
        }

        int spawnY = maxHeight + 5;

        for (int y = maxHeight; y >= 0; y--)
        {
            if (mapData[targetX, y, targetZ] != 0)
            {
                spawnY = y + 2;
                break;
            }
        }

        PlayerController pc = playerTransform.GetComponent<PlayerController>();
        if (pc != null)
        {
            pc.Teleport(new Vector3(targetX, spawnY, targetZ));
        }
        else
        {
            playerTransform.position = new Vector3(targetX, spawnY, targetZ);
        }
    }
}