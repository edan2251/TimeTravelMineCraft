using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public enum MapType
{
    Morning, // 숲
    Noon,    // 사막
    Night    // 공허
}

public class MapGenerator : MonoBehaviour
{
    [Header("Map Dimensions")]
    public int width = 50;
    public int depth = 50;
    public int worldHeight = 64;
    public int terrainHeight = 20;

    [Header("Settings")]
    public MapType currentMapType = MapType.Morning;
    public int waterLevel = 4;
    public float noiseScale = 20f;
    [Range(0f, 1f)] public float nightVoidThreshold = 0.3f;

    [Header("Night Ring Settings")]
    public float centerSafeRadius = 8f;   // 중앙 안전지대 크기
    public float ringWidth = 8f;          // 링 하나의 두께 (공허 링 or 땅 링)
    public float ruinIslandRadius = 6f;   // 유적 주변 안전지대 크기
    public int ruinPadding = 10;          // 맵 끝에서 얼마나 떨어져서 유적을 지을지

    [Header("References")]
    public List<GameObject> blockPrefabs;
    public Transform playerTransform;
    public LayerMask blockLayer;

    private int[,,] mapData;
    private float seedX, seedZ;
    private Vector3? lastPlayerPos = null;
    private HashSet<Vector3Int> ruinBlockCoords = new HashSet<Vector3Int>();

    const int AIR_ID = -1;
    const int STABILIZER_ID = 11;
    const int LOG_ID = 9;
    const int LEAVES_ID = 10;
    const int SAPLING_ID = 13;
    const int SUN_FRUIT_ID = 12;
    const int RUIN_BLOCK_ID = 6;

    // 외부에서 호출: 맵 생성 시작
    public void GenerateMap(MapType type, bool savePosition = true)
    {
        if (savePosition && playerTransform != null) lastPlayerPos = playerTransform.position;
        else lastPlayerPos = null;

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

    // 맵 생성 코루틴
    IEnumerator GenerateMapRoutine()
    {
        if (playerTransform != null) playerTransform.gameObject.SetActive(false);

        // 0. 유적 위치 재계산 (맵 크기가 변했을 수 있으므로)
        CalculateRuinPositions();

        // 1. 초기화
        mapData = new int[width, worldHeight, depth];
        InitializeMapDataWithAir();
        ruinBlockCoords.Clear();
        foreach (Transform child in transform) Destroy(child.gameObject);

        // 2. 데이터 생성
        FillMapData();
        GenerateTrees();
        GenerateRuins();
        ApplyBrokenBlocks();
        ApplyPlacedBlocks();

        // 3. 오브젝트 소환
        int blockCount = 0;
        for (int x = 0; x < width; x++)
        {
            for (int z = 0; z < depth; z++)
            {
                for (int y = 0; y < worldHeight; y++)
                {
                    int id = mapData[x, y, z];
                    if (id == AIR_ID) continue;
                    if (IsHidden(x, y, z) && id != 3) continue;

                    SpawnBlockObj(x, y, z, id);
                    blockCount++;
                }
            }
            if (x % 2 == 0) yield return null;
        }

        RefreshStabilizerZones();
        Debug.Log($"맵 생성 완료! 블록 수: {blockCount}");

        if (playerTransform != null) playerTransform.gameObject.SetActive(true);
        SpawnPlayer();
    }

    void CalculateRuinPositions()
    {
        if (GameManager.Instance == null) return;

        // 맵의 4귀퉁이에서 padding만큼 안쪽으로 들어온 위치 계산
        // 왼쪽 아래, 왼쪽 위, 오른쪽 아래, 오른쪽 위
        GameManager.Instance.ruinPositions[0] = new Vector3Int(ruinPadding, 0, ruinPadding);
        GameManager.Instance.ruinPositions[1] = new Vector3Int(ruinPadding, 0, depth - ruinPadding - 1);
        GameManager.Instance.ruinPositions[2] = new Vector3Int(width - ruinPadding - 1, 0, ruinPadding);
        GameManager.Instance.ruinPositions[3] = new Vector3Int(width - ruinPadding - 1, 0, depth - ruinPadding - 1);
    }

    void InitializeMapDataWithAir()
    {
        for (int x = 0; x < width; x++)
            for (int y = 0; y < worldHeight; y++)
                for (int z = 0; z < depth; z++)
                    mapData[x, y, z] = AIR_ID;
    }

    // 펄린 노이즈 기반 지형 데이터 생성
    void FillMapData()
    {
        Vector2 centerPos = new Vector2(width / 2f, depth / 2f);

        for (int x = 0; x < width; x++)
        {
            for (int z = 0; z < depth; z++)
            {
                float noiseVal = Mathf.PerlinNoise((x + seedX) / noiseScale, (z + seedZ) / noiseScale);
                if (currentMapType == MapType.Noon) noiseVal *= 1.5f;

                int height = Mathf.FloorToInt(noiseVal * terrainHeight);

                // ★★★ 밤(Night) 패턴 로직 수정: 다중 링(Ripple) ★★★
                if (currentMapType == MapType.Night)
                {
                    float distFromCenter = Vector2.Distance(new Vector2(x, z), centerPos);
                    bool shouldBeHole = false;

                    // 1. 중앙 안전지대 체크
                    if (distFromCenter < centerSafeRadius)
                    {
                        shouldBeHole = false; // 안전
                    }
                    else
                    {
                        // 2. 링 패턴 계산 (거리 / 링두께)
                        // 0번 링(중앙 직후): 공허
                        // 1번 링: 땅
                        // 2번 링: 공허 ...
                        float ringIndex = Mathf.Floor((distFromCenter - centerSafeRadius) / ringWidth);

                        // 짝수 번째 링은 '공허', 홀수 번째 링은 '땅'
                        if (ringIndex % 2 == 0)
                        {
                            shouldBeHole = true; // 공허 링
                        }
                        else
                        {
                            // 땅 링이지만, 작은 구멍(치즈) 뚫기
                            float voidNoise = Mathf.PerlinNoise((x + seedX) * 0.15f, (z + seedZ) * 0.15f);
                            shouldBeHole = (voidNoise < nightVoidThreshold);
                        }
                    }

                    // 3. 유적 주변 보호 (Override)
                    if (GameManager.Instance != null)
                    {
                        foreach (Vector3Int ruinPos in GameManager.Instance.ruinPositions)
                        {
                            // 유적 위치는 아직 Y가 0일 수 있으므로 X, Z 거리만 비교
                            if (Vector2.Distance(new Vector2(x, z), new Vector2(ruinPos.x, ruinPos.z)) < ruinIslandRadius)
                            {
                                shouldBeHole = false; // 강제 땅
                                break;
                            }
                        }
                    }

                    // 4. 유지기 보호
                    if (shouldBeHole && GameManager.Instance != null && GameManager.Instance.IsStabilizedZone(x, z))
                    {
                        shouldBeHole = false;
                    }

                    if (shouldBeHole) height = -1;
                }
                // ★★★ 밤 로직 끝 ★★★

                for (int y = 0; y < worldHeight; y++)
                {
                    if (GameManager.Instance != null && GameManager.Instance.activeStabilizers.Contains(new Vector3Int(x, y, z)))
                    {
                        mapData[x, y, z] = STABILIZER_ID;
                        continue;
                    }

                    if (height >= 0 && y <= height) mapData[x, y, z] = GetBlockIDByTheme(y, height);
                    else if (y <= waterLevel && currentMapType == MapType.Morning) mapData[x, y, z] = 3;
                    else mapData[x, y, z] = AIR_ID;
                }
            }
        }
    }

    // 나무 생성 (시드 기반 고정)
    void GenerateTrees()
    {
        if (currentMapType != MapType.Morning) return;

        int treeSeed = Mathf.FloorToInt(seedX + seedZ);
        System.Random prng = new System.Random(treeSeed);

        for (int x = 2; x < width - 2; x++)
        {
            for (int z = 2; z < depth - 2; z++)
            {
                if (prng.NextDouble() > 0.01f) continue;

                int surfaceY = -1;
                for (int y = worldHeight - 1; y >= 0; y--) // 위에서부터 검색
                {
                    if (mapData[x, y, z] != AIR_ID && mapData[x, y, z] != 3)
                    {
                        surfaceY = y;
                        break;
                    }
                }

                if (surfaceY > 0 && mapData[x, surfaceY, z] == 1)
                {
                    SpawnSingleTree(x, surfaceY + 1, z, prng.Next(4, 7), false, false);
                }
            }
        }
    }

    // 단일 나무 생성 로직
    void SpawnSingleTree(int rootX, int rootY, int rootZ, int treeHeight, bool hasFruit = false, bool record = false)
    {
        // 1. 기둥
        for (int i = 0; i < treeHeight; i++)
        {
            if (IsIdxValid(rootX, rootY + i, rootZ))
            {
                int current = mapData[rootX, rootY + i, rootZ];
                if (current == AIR_ID || current == LEAVES_ID || current == SAPLING_ID)
                    PlaceTreeBlock(rootX, rootY + i, rootZ, LOG_ID, record);
            }
        }

        // 2. 잎
        int leafStart = rootY + treeHeight - 2;
        int leafEnd = rootY + treeHeight + 1;

        for (int y = leafStart; y <= leafEnd; y++)
        {
            for (int x = rootX - 2; x <= rootX + 2; x++)
            {
                for (int z = rootZ - 2; z <= rootZ + 2; z++)
                {
                    if (!IsIdxValid(x, y, z)) continue;
                    if (mapData[x, y, z] == AIR_ID)
                    {
                        if (Vector3.Distance(new Vector3(rootX, y - 1, rootZ), new Vector3(x, y, z)) <= 2.5f)
                            PlaceTreeBlock(x, y, z, LEAVES_ID, record);
                    }
                }
            }
        }

        // 3. 열매
        if (hasFruit)
        {
            int fruitCount = Random.Range(1, 3);
            int attempts = 0;
            while (fruitCount > 0 && attempts < 10)
            {
                int fx = Random.Range(rootX - 1, rootX + 2);
                int fz = Random.Range(rootZ - 1, rootZ + 2);
                int fy = Random.Range(leafStart, leafEnd);

                if (IsIdxValid(fx, fy, fz) && mapData[fx, fy, fz] == LEAVES_ID)
                {
                    PlaceTreeBlock(fx, fy, fz, SUN_FRUIT_ID, record);
                    fruitCount--;
                }
                attempts++;
            }
        }
    }

    void PlaceTreeBlock(int x, int y, int z, int id, bool record)
    {
        mapData[x, y, z] = id;
        if (record && GameManager.Instance != null)
            GameManager.Instance.RecordPlacedBlock(currentMapType, new Vector3Int(x, y, z), id);
    }

    int GetBlockIDByTheme(int y, int surfaceHeight)
    {
        if (y < surfaceHeight - 4) // 지하 자원
        {
            float rand = Random.value;
            if (rand < 0.05f) return 7; // Iron
            if (rand < 0.1f) return 8;  // Coal
            if (y < 3) return 2;        // Bedrock/DeepStone
        }

        switch (currentMapType)
        {
            case MapType.Morning: return (y == surfaceHeight) ? 1 : (y < surfaceHeight - 3 ? 2 : 0);
            case MapType.Noon: return (y == surfaceHeight) ? 4 : (y < surfaceHeight - 3 ? 2 : 5);
            case MapType.Night: return 6;
            default: return 1;
        }
    }

    // 설치/파괴 데이터 반영
    void ApplyBrokenBlocks()
    {
        if (GameManager.Instance == null) return;
        for (int x = 0; x < width; x++)
            for (int z = 0; z < depth; z++)
                for (int y = 0; y < worldHeight; y++)
                    if (GameManager.Instance.IsBlockBroken(currentMapType, x, y, z)) mapData[x, y, z] = AIR_ID;
    }

    void ApplyPlacedBlocks()
    {
        if (GameManager.Instance == null) return;
        for (int x = 0; x < width; x++)
        {
            for (int z = 0; z < depth; z++)
            {
                for (int y = 0; y < worldHeight; y++)
                {
                    int placedID = GameManager.Instance.GetPlacedBlockID(currentMapType, x, y, z);
                    if (placedID != AIR_ID)
                    {
                        // 묘목 성장 체크
                        if (placedID == SAPLING_ID)
                        {
                            SaplingInfo info = GameManager.Instance.GetSaplingInfo(currentMapType, new Vector3Int(x, y, z));
                            if (info != null && info.isGrown)
                            {
                                GameManager.Instance.RemovePlacedBlockRecord(currentMapType, new Vector3Int(x, y, z));
                                SpawnSingleTree(x, y, z, Random.Range(4, 7), (info.plantedTime == MapType.Noon), true);
                                continue;
                            }
                        }
                        mapData[x, y, z] = placedID;
                    }
                }
            }
        }
    }

    // 유적 생성 (정자 형태)
    void GenerateRuins()
    {
        if (currentMapType != MapType.Night || GameManager.Instance == null) return;
        const int FLOOR = 2, PILLAR = 9, ROOF = 5;

        // CalculateRuinPositions에서 이미 X, Z는 세팅됨. 여기선 Y(높이)만 찾아서 건설.
        for (int i = 0; i < GameManager.Instance.ruinPositions.Length; i++)
        {
            Vector3Int targetPos = GameManager.Instance.ruinPositions[i];

            // 땅 찾기
            int groundY = -1;
            for (int y = worldHeight - 1; y >= 0; y--)
            {
                if (mapData[targetPos.x, y, targetPos.z] != AIR_ID)
                {
                    groundY = y;
                    break;
                }
            }
            if (groundY == -1) groundY = 10;

            int altarY = groundY + 2;
            GameManager.Instance.ruinPositions[i] = new Vector3Int(targetPos.x, altarY, targetPos.z);
            Vector3Int pos = GameManager.Instance.ruinPositions[i];

            for (int x = -1; x <= 1; x++)
            {
                for (int z = -1; z <= 1; z++)
                {
                    int cx = pos.x + x, cz = pos.z + z;
                    if (!IsIdxValid(cx, pos.y, cz)) continue;

                    SetRuinBlock(cx, pos.y - 1, cz, FLOOR);
                    SetRuinBlock(cx, pos.y + 2, cz, ROOF);

                    if (Mathf.Abs(x) == 1 && Mathf.Abs(z) == 1)
                    {
                        SetRuinBlock(cx, pos.y, cz, PILLAR);
                        SetRuinBlock(cx, pos.y + 1, cz, PILLAR);
                    }
                    else
                    {
                        mapData[cx, pos.y, cz] = AIR_ID;
                        mapData[cx, pos.y + 1, cz] = AIR_ID;
                    }
                }
            }
            mapData[pos.x, pos.y, pos.z] = AIR_ID;
        }
    }

    void SetRuinBlock(int x, int y, int z, int id)
    {
        mapData[x, y, z] = id;
        ruinBlockCoords.Add(new Vector3Int(x, y, z));
    }

    // 블록 설치 (외부 호출)
    public void PlaceBlockAt(Vector3 worldPos, int blockID)
    {
        Vector3Int coord = WorldToGrid(worldPos);
        if (!IsIdxValid(coord.x, coord.y, coord.z)) return;
        if (mapData[coord.x, coord.y, coord.z] != AIR_ID && mapData[coord.x, coord.y, coord.z] != 3) return;

        mapData[coord.x, coord.y, coord.z] = blockID;
        GameObject newBlock = SpawnBlockObj(coord.x, coord.y, coord.z, blockID);

        if (blockID == STABILIZER_ID && GameManager.Instance != null)
            GameManager.Instance.AddStabilizer(coord);

        if (GameManager.Instance != null)
        {
            GameManager.Instance.RecordPlacedBlock(currentMapType, coord, blockID);
            if (blockID == GameManager.LANTERN_ID) GameManager.Instance.CheckLanternObjective(coord);

            // 밤: 유지기 밖이면 소멸 저주
            if (currentMapType == MapType.Night && blockID != STABILIZER_ID && blockID != GameManager.LANTERN_ID)
            {
                if (!GameManager.Instance.IsStabilizedZone(coord.x, coord.z))
                {
                    if (newBlock != null)
                    {
                        TemporaryBlock tb = newBlock.AddComponent<TemporaryBlock>();
                        tb.Setup(this);
                    }
                }
            }
        }
    }

    // 블록 제거 (외부 호출)
    public void RemoveBlockAt(Vector3 worldPos)
    {
        Vector3Int coord = WorldToGrid(worldPos);
        if (!IsIdxValid(coord.x, coord.y, coord.z)) return;

        int currentID = mapData[coord.x, coord.y, coord.z];
        if (currentID == AIR_ID) return;

        if (currentID == STABILIZER_ID && GameManager.Instance != null)
            GameManager.Instance.RemoveStabilizer(coord);

        if (GameManager.Instance != null)
        {
            if (GameManager.Instance.GetPlacedBlockID(currentMapType, coord.x, coord.y, coord.z) != AIR_ID)
                GameManager.Instance.RemovePlacedBlockRecord(currentMapType, coord);
            else
                GameManager.Instance.RecordBrokenBlock(currentMapType, coord);
        }

        mapData[coord.x, coord.y, coord.z] = AIR_ID;
        UpdateChunkAt(coord.x, coord.y, coord.z);
    }

    // 프리팹 인스턴스화
    GameObject SpawnBlockObj(int x, int y, int z, int id)
    {
        if (id >= 0 && id < blockPrefabs.Count)
        {
            GameObject obj = Instantiate(blockPrefabs[id], new Vector3(x, y, z), Quaternion.identity, transform);

            // 유적 블록 무적 설정
            if (ruinBlockCoords.Contains(new Vector3Int(x, y, z)))
            {
                var block = obj.GetComponent<BlockBehavior>();
                if (block != null) block.isUnbreakable = true;
            }
            return obj;
        }
        return null;
    }

    // 주변 블록 갱신 (컬링 체크용)
    void UpdateChunkAt(int x, int y, int z)
    {
        CheckAndSpawn(x + 1, y, z); CheckAndSpawn(x - 1, y, z);
        CheckAndSpawn(x, y + 1, z); CheckAndSpawn(x, y - 1, z);
        CheckAndSpawn(x, y, z + 1); CheckAndSpawn(x, y, z - 1);
    }

    void CheckAndSpawn(int x, int y, int z)
    {
        if (!IsIdxValid(x, y, z)) return;
        int id = mapData[x, y, z];
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
                for (int z = startZ; z <= endZ; z++)
                    for (int y = 0; y < worldHeight; y++)
                        CheckAndSpawn(x, y, z);
        }
    }

    // 유틸리티
    public Vector3Int WorldToGrid(Vector3 pos) => new Vector3Int(Mathf.RoundToInt(pos.x), Mathf.RoundToInt(pos.y), Mathf.RoundToInt(pos.z));
    bool IsIdxValid(int x, int y, int z) => x >= 0 && x < width && y >= 0 && y < worldHeight && z >= 0 && z < depth; // worldHeight 사용

    bool IsHidden(int x, int y, int z)
    {
        if (x <= 0 || x >= width - 1 || y <= 0 || y >= worldHeight - 1 || z <= 0 || z >= depth - 1) return false;

        if (IsTransparent(x + 1, y, z)) return false;
        if (IsTransparent(x - 1, y, z)) return false;
        if (IsTransparent(x, y + 1, z)) return false;
        if (IsTransparent(x, y - 1, z)) return false;
        if (IsTransparent(x, y, z + 1)) return false;
        if (IsTransparent(x, y, z - 1)) return false;
        return true;
    }

    bool IsTransparent(int x, int y, int z) => (mapData[x, y, z] == AIR_ID || mapData[x, y, z] == 3);

    // 플레이어 스폰 위치 계산 및 이동
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

        int spawnY = worldHeight - 1; // 하늘에서부터 땅 찾기
        for (int y = worldHeight - 1; y >= 0; y--)
        {
            if (mapData[targetX, y, targetZ] != AIR_ID)
            {
                spawnY = y + 2;
                break;
            }
        }

        PlayerController pc = playerTransform.GetComponent<PlayerController>();
        if (pc != null)
        {
            pc.enabled = true; // 컨트롤러 활성화
            pc.Teleport(new Vector3(targetX, spawnY, targetZ));
        }
        else
        {
            playerTransform.position = new Vector3(targetX, spawnY, targetZ);
        }

        Debug.Log($"플레이어 스폰: ({targetX}, {spawnY}, {targetZ})");
    }

    public void RespawnAtMorning()
    {
        lastPlayerPos = null; // 위치 초기화
        currentMapType = MapType.Morning;
        StopAllCoroutines();
        StartCoroutine(GenerateMapRoutine());
    }
}