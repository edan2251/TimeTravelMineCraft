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
    // 0:Dirt, 1:Grass, 2:Stone, 3:Water, 4:Sand, 5:RedRock, 6:VoidStone
    // 7:IronOre, 8:CoalOre, 9:Log, 10:Leaves, 11:Stabilizer
    public List<GameObject> blockPrefabs;

    [Header("References")]
    public Transform playerTransform;

    [Header("Layer Settings")]
    public LayerMask blockLayer;

    private int[,,] mapData;

    // ★ 변경: 공기(빈 공간)를 -1로 정의하여 0번(Dirt)과 구분
    const int AIR_ID = -1;
    const int STABILIZER_ID = 11;
    const int LOG_ID = 9;
    const int LEAVES_ID = 10;

    // 시드값 저장 변수
    private float seedX;
    private float seedZ;

    private Vector3? lastPlayerPos = null;

    void Start()
    {
        seedX = Random.Range(0f, 9999f);
        seedZ = Random.Range(0f, 9999f);

        StartCoroutine(GenerateMapRoutine());
    }

    public void GenerateMap(MapType type)
    {
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
        if (seedX == 0) seedX = Random.Range(0f, 9999f);
        if (seedZ == 0) seedZ = Random.Range(0f, 9999f);
        StartCoroutine(GenerateMapRoutine());
    }

    IEnumerator GenerateMapRoutine()
    {
        if (playerTransform != null)
            playerTransform.gameObject.SetActive(false);

        // 1. 맵 데이터 초기화 (-1로 채우기)
        mapData = new int[width, maxHeight + 1, depth];
        InitializeMapDataWithAir();

        // 2. 기존 오브젝트 삭제
        foreach (Transform child in transform) Destroy(child.gameObject);

        // 3. 지형 데이터 생성
        FillMapData();

        // 4. 나무 심기 (데이터 생성 후, 블록 배치 전)
        GenerateTrees();

        // 5. 파괴된 블록 반영 (구멍 뚫기)
        ApplyBrokenBlocks();

        // 6. 실제 프리팹 생성
        int blockCount = 0;
        for (int x = 0; x < width; x++)
        {
            for (int z = 0; z < depth; z++)
            {
                for (int y = 0; y <= maxHeight; y++)
                {
                    int id = mapData[x, y, z];

                    // 공기(-1)면 패스
                    if (id == AIR_ID) continue;

                    // 최적화: 가려진 블록 생성 안 함 (물은 투명하므로 예외)
                    if (IsHidden(x, y, z) && id != 3) continue;

                    SpawnBlockObj(x, y, z, id);
                    blockCount++;
                }
            }
            if (x % 2 == 0) yield return null;
        }

        // 7. 유지기 주변 갱신
        RefreshStabilizerZones();

        Debug.Log($"맵 생성 완료! 블록 수: {blockCount}");

        if (playerTransform != null)
            playerTransform.gameObject.SetActive(true);

        SpawnPlayer();
    }

    // 맵 전체를 공기(-1)로 초기화하는 함수
    void InitializeMapDataWithAir()
    {
        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y <= maxHeight; y++)
            {
                for (int z = 0; z < depth; z++)
                {
                    mapData[x, y, z] = AIR_ID;
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
                float noiseVal = Mathf.PerlinNoise((x + seedX) / noiseScale, (z + seedZ) / noiseScale);
                if (currentMapType == MapType.Noon) noiseVal *= 1.5f;

                int height = Mathf.FloorToInt(noiseVal * maxHeight);

                // Night: 공허(구멍) 처리
                if (currentMapType == MapType.Night)
                {
                    float voidNoise = Mathf.PerlinNoise((x + seedX) * 0.1f, (z + seedZ) * 0.1f);
                    bool shouldBeHole = (voidNoise < nightVoidThreshold);

                    if (shouldBeHole && GameManager.Instance != null && GameManager.Instance.IsStabilizedZone(x, z))
                    {
                        shouldBeHole = false;
                    }

                    if (shouldBeHole) height = -1; // 높이를 없애버림
                }

                for (int y = 0; y <= maxHeight; y++)
                {
                    // 1. 유지기 우선 배치
                    if (GameManager.Instance != null && GameManager.Instance.activeStabilizers.Contains(new Vector3Int(x, y, z)))
                    {
                        mapData[x, y, z] = STABILIZER_ID;
                        continue;
                    }

                    // 2. 블록 배치 로직
                    if (height >= 0 && y <= height)
                    {
                        // 땅 부분
                        mapData[x, y, z] = GetBlockIDByTheme(y, height);
                    }
                    else if (y <= waterLevel && currentMapType == MapType.Morning)
                    {
                        // 물 부분
                        mapData[x, y, z] = 3; // Water
                    }
                    else
                    {
                        // 공기 부분 (이미 초기화 때 -1로 되어있지만 명시적으로)
                        mapData[x, y, z] = AIR_ID;
                    }
                }
            }
        }
    }

    // ★ 나무 생성 로직 추가
    void GenerateTrees()
    {
        // 아침 맵에서만 생성
        if (currentMapType != MapType.Morning) return;

        for (int x = 2; x < width - 2; x++)
        {
            for (int z = 2; z < depth - 2; z++)
            {
                // 1% 확률로 나무 심기
                if (Random.value > 0.01f) continue;

                // 해당 위치의 지표면 찾기
                int surfaceY = -1;
                for (int y = maxHeight; y >= 0; y--)
                {
                    if (mapData[x, y, z] != AIR_ID && mapData[x, y, z] != 3) // 공기도 아니고 물도 아닌 땅
                    {
                        surfaceY = y;
                        break;
                    }
                }

                // 잔디(1) 위에만 심기
                if (surfaceY > 0 && mapData[x, surfaceY, z] == 1)
                {
                    SpawnSingleTree(x, surfaceY + 1, z);
                }
            }
        }
    }

    void SpawnSingleTree(int rootX, int rootY, int rootZ)
    {
        int treeHeight = Random.Range(4, 6);

        // 1. 기둥 (Log #9)
        for (int i = 0; i < treeHeight; i++)
        {
            int y = rootY + i;
            if (IsIdxValid(rootX, y, rootZ))
            {
                // 이미 다른 블록(유지기 등)이 없으면 설치
                if (mapData[rootX, y, rootZ] == AIR_ID || mapData[rootX, y, rootZ] == 10)
                    mapData[rootX, y, rootZ] = LOG_ID;
            }
        }

        // 2. 나뭇잎 (Leaves #10)
        int leafStart = rootY + treeHeight - 2;
        int leafEnd = rootY + treeHeight + 1;

        for (int y = leafStart; y <= leafEnd; y++)
        {
            for (int x = rootX - 2; x <= rootX + 2; x++)
            {
                for (int z = rootZ - 2; z <= rootZ + 2; z++)
                {
                    if (!IsIdxValid(x, y, z)) continue;

                    // 빈 공간(Air)일 때만 잎 생성
                    if (mapData[x, y, z] != AIR_ID) continue;

                    float dist = Vector3.Distance(new Vector3(rootX, y - 1, rootZ), new Vector3(x, y, z));
                    if (dist <= 2.5f)
                    {
                        mapData[x, y, z] = LEAVES_ID;
                    }
                }
            }
        }
    }

    int GetBlockIDByTheme(int y, int surfaceHeight)
    {
        // 광물 생성 (Morning, Noon, Night 공통)
        if (y < surfaceHeight - 4)
        {
            float rand = Random.value;
            if (rand < 0.05f) return 7; // IronOre
            if (rand < 0.1f) return 8;  // CoalOre
            if (y < 3) return 2;        // Deep Stone -> Stone(2) 그대로 사용 (깊은 돌도 그냥 돌과 같다면)
        }

        switch (currentMapType)
        {
            case MapType.Morning:
                if (y == surfaceHeight) return 1; // Grass
                if (y < surfaceHeight - 3) return 2; // Stone
                return 0; // ★ Dirt (이제 0번이 흙입니다)

            case MapType.Noon:
                if (y == surfaceHeight) return 4; // Sand
                if (y < surfaceHeight - 3) return 2; // Stone
                return 5; // RedRock

            case MapType.Night:
                return 6; // VoidStone

            default: return 1;
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
                    if (GameManager.Instance.IsBlockBroken(currentMapType, x, y, z))
                    {
                        mapData[x, y, z] = AIR_ID; // ★ 공기(-1)로 변경
                    }
                }
            }
        }
    }

    public void PlaceBlockAt(Vector3 worldPos, int blockID)
    {
        Vector3Int coord = WorldToGrid(worldPos);
        if (!IsIdxValid(coord.x, coord.y, coord.z)) return;

        // 이미 블록이 있으면 설치 불가 (공기가 아니면)
        if (mapData[coord.x, coord.y, coord.z] != AIR_ID) return;

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

        // 공기(-1)면 삭제 불가
        if (currentID == AIR_ID) return;

        if (currentID == STABILIZER_ID && GameManager.Instance != null)
        {
            GameManager.Instance.RemoveStabilizer(coord);
        }

        if (GameManager.Instance != null)
        {
            GameManager.Instance.RecordBrokenBlock(currentMapType, coord);
        }

        mapData[coord.x, coord.y, coord.z] = AIR_ID; // ★ 공기(-1)로 변경

        UpdateChunkAt(coord.x, coord.y, coord.z);
    }

    void SpawnBlockObj(int x, int y, int z, int id)
    {
        // ★ ID가 곧 인덱스이므로 그대로 사용
        if (id >= 0 && id < blockPrefabs.Count)
        {
            Instantiate(blockPrefabs[id], new Vector3(x, y, z), Quaternion.identity, transform);
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

        // 공기(-1)면 생성 안 함
        if (id == AIR_ID) return;

        if (Physics.CheckSphere(new Vector3(x, y, z), 0.4f, blockLayer)) return;

        SpawnBlockObj(x, y, z, id);
    }

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

    // Helper functions
    public Vector3Int WorldToGrid(Vector3 pos) => new Vector3Int(Mathf.RoundToInt(pos.x), Mathf.RoundToInt(pos.y), Mathf.RoundToInt(pos.z));

    bool IsIdxValid(int x, int y, int z) => x >= 0 && x < width && y >= 0 && y <= maxHeight && z >= 0 && z < depth;

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
        // 공기(-1)이거나 물(3)이면 투명함
        return (id == AIR_ID || id == 3);
    }

    void SpawnPlayer()
    {
        if (playerTransform == null) return;
        int targetX, targetZ;

        if (lastPlayerPos.HasValue)
        {
            targetX = Mathf.Clamp(Mathf.RoundToInt(lastPlayerPos.Value.x), 0, width - 1);
            targetZ = Mathf.Clamp(Mathf.RoundToInt(lastPlayerPos.Value.z), 0, depth - 1);
        }
        else
        {
            targetX = width / 2;
            targetZ = depth / 2;
        }

        int spawnY = maxHeight + 5;
        for (int y = maxHeight; y >= 0; y--)
        {
            if (mapData[targetX, y, targetZ] != AIR_ID)
            {
                spawnY = y + 2;
                break;
            }
        }

        PlayerController pc = playerTransform.GetComponent<PlayerController>();
        if (pc != null) pc.Teleport(new Vector3(targetX, spawnY, targetZ));
        else playerTransform.position = new Vector3(targetX, spawnY, targetZ);
    }
}