using UnityEngine;

public class BuildingModule : MonoBehaviour
{
    [Header("Settings")]
    public float buildCooldown = 0.25f;

    // 이펙트는 이제 TemporaryBlock이나 MapGenerator에서 처리하므로 여기서 제거해도 됩니다.
    // 하지만 나중에 쓸 수도 있으니 변수는 남겨두되, 로직에서는 뺍니다.
    [Header("Visuals")]
    public GameObject evaporationEffect;

    private MapGenerator map;
    private float nextBuildTime;

    public void Init(MapGenerator mapGen)
    {
        this.map = mapGen;
    }

    public void TryBuild(RaycastHit hit, int blockID)
    {
        if (Time.time < nextBuildTime) return;

        // ★ 수정: 밤 설치 제한 로직 완전 삭제! 
        // (이제 밤에도 설치는 됩니다. 유지기 밖이면 MapGenerator가 알아서 태워 없앨 것입니다.)

        Vector3 targetPos = hit.transform.position + hit.normal;

        // 플레이어 끼임 방지
        if (Vector3.Distance(transform.position, targetPos) < 1.2f) return;

        // 맵에 블록 설치 요청 (성공 여부는 MapGenerator 소관)
        map.PlaceBlockAt(targetPos, blockID);

        // 인벤토리 소모
        if (InventoryManager.Instance != null)
        {
            InventoryManager.Instance.ConsumeSelectedOne();
        }

        nextBuildTime = Time.time + buildCooldown;
    }
}