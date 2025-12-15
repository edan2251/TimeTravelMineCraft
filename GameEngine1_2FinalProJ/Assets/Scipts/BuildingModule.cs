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

    public void TryBuild(RaycastHit hit, int blockID)
    {
        if (Time.time < nextBuildTime) return;

        Vector3 targetPos = hit.transform.position + hit.normal;

        // 플레이어와 너무 가까우면 설치 불가 (끼임 방지)
        if (Vector3.Distance(transform.position, targetPos) < 1.2f) return;

        // 1. 맵에 블록 설치
        map.PlaceBlockAt(targetPos, blockID);

        // 2. ★ 추가된 부분: 인벤토리에서 현재 선택된 아이템 1개 소비
        if (InventoryManager.Instance != null)
        {
            InventoryManager.Instance.ConsumeSelectedOne();
        }

        nextBuildTime = Time.time + buildCooldown;
    }
}