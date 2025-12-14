using UnityEngine;

public class BuildingModule : MonoBehaviour
{
    [Header("Settings")]
    public float buildCooldown = 0.25f;

    private MapGenerator map;
    private float nextBuildTime;

    public void Init(MapGenerator mapGen)
    {
        this.map = mapGen;
    }

    // blockID: 인벤토리에서 선택된 블록 ID (예: 1=돌, 2=흙...)
    public void TryBuild(RaycastHit hit, int blockID)
    {
        if (Time.time < nextBuildTime) return;

        // 1. 설치할 위치 계산 (맞은 면의 바깥쪽)
        // 큐브가 정수 좌표계라면 Normal을 더하면 정확히 옆 칸이 됨
        Vector3 targetPos = hit.transform.position + hit.normal;

        // 2. 플레이어랑 겹치는지 확인 (선택 사항)
        if (Vector3.Distance(transform.position, targetPos) < 1.2f) return;

        // 3. 맵에 생성 요청
        map.PlaceBlockAt(targetPos, blockID);

        nextBuildTime = Time.time + buildCooldown;

        // (선택) 인벤토리 소모 로직 호출
        // Inventory.Consume(1);
    }
}