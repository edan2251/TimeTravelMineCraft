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
    public List<GameObject> blockPrefabs;

    [Header("References")]
    public Transform playerTransform; 

    [Header("Layer Settings")]
    public LayerMask blockLayer;

    private int[,,] mapData;

    void Start()
    {
        StartCoroutine(GenerateMapRoutine());
    }

    public void GenerateMap(MapType type)
    {
        currentMapType = type;
        StopAllCoroutines();
        StartCoroutine(GenerateMapRoutine());
    }

    [ContextMenu("Generate Map")]
    public void GenerateMap()
    {
        StartCoroutine(GenerateMapRoutine());
    }

    IEnumerator GenerateMapRoutine()
    {
        mapData = new int[width, maxHeight + 1, depth];

        foreach (Transform child in transform) Destroy(child.gameObject);

        FillMapData();

        int blockCount = 0;
        for (int x = 0; x < width; x++)
        {
            for (int z = 0; z < depth; z++)
            {
                for (int y = 0; y <= maxHeight; y++)
                {
                    int id = mapData[x, y, z];
                    if (id == 0) continue;

                    if (IsHidden(x, y, z) && id != 3) continue;

                    SpawnBlockObj(x, y, z, id);
                    blockCount++;
                }
            }
            if (x % 2 == 0) yield return null; 
        }

        Debug.Log($"맵 생성 완료! 블록 수: {blockCount}");
        SpawnPlayer();
    }

    void FillMapData()
    {
        float offsetX = Random.Range(0f, 9999f);
        float offsetZ = Random.Range(0f, 9999f);

        for (int x = 0; x < width; x++)
        {
            for (int z = 0; z < depth; z++)
            {
                float noiseVal = Mathf.PerlinNoise((x + offsetX) / noiseScale, (z + offsetZ) / noiseScale);
                if (currentMapType == MapType.Noon) noiseVal *= 1.5f;

                int height = Mathf.FloorToInt(noiseVal * maxHeight);

                if (currentMapType == MapType.Night)
                {
                    float voidNoise = Mathf.PerlinNoise((x + offsetX) * 0.1f, (z + offsetZ) * 0.1f);
                    if (voidNoise < nightVoidThreshold) height = 0;
                }

                for (int y = 0; y <= maxHeight; y++)
                {
                    if (y <= height && height > 0)
                        mapData[x, y, z] = GetBlockIDByTheme(y, height);
                    else if (y <= waterLevel && currentMapType == MapType.Morning)
                        mapData[x, y, z] = 3;
                    else
                        mapData[x, y, z] = 0; 
                }
            }
        }
    }

    int GetBlockIDByTheme(int y, int surfaceHeight)
    {
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
                return 10; // Dirt (ID 10)

            case MapType.Noon:
                if (y == surfaceHeight) return 4; // Sand
                if (y < surfaceHeight - 3) return 2; // Stone
                return 5; // RedRock

            case MapType.Night:
                return 6; // VoidStone

            default: return 1;
        }
    }


    public void RemoveBlockAt(Vector3 worldPos)
    {
        Vector3Int coord = WorldToGrid(worldPos);
        if (!IsIdxValid(coord.x, coord.y, coord.z)) return;

        mapData[coord.x, coord.y, coord.z] = 0;

        UpdateChunkAt(coord.x, coord.y, coord.z);
    }

    public void PlaceBlockAt(Vector3 worldPos, int blockID)
    {
        Vector3Int coord = WorldToGrid(worldPos);
        if (!IsIdxValid(coord.x, coord.y, coord.z)) return;

        if (mapData[coord.x, coord.y, coord.z] != 0) return;

        mapData[coord.x, coord.y, coord.z] = blockID;

        SpawnBlockObj(coord.x, coord.y, coord.z, blockID);
    }

    void SpawnBlockObj(int x, int y, int z, int id)
    {
        int prefabIdx = (id == 10) ? 0 : id; 
        Instantiate(blockPrefabs[prefabIdx], new Vector3(x, y, z), Quaternion.identity, transform);
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

        int centerX = width / 2;
        int centerZ = depth / 2;
        int spawnY = maxHeight + 5;

        for (int y = maxHeight; y >= 0; y--)
        {
            if (mapData[centerX, y, centerZ] != 0)
            {
                spawnY = y + 2;
                break;
            }
        }

        PlayerController pc = playerTransform.GetComponent<PlayerController>();
        if (pc != null) pc.Teleport(new Vector3(centerX, spawnY, centerZ));
    }
}