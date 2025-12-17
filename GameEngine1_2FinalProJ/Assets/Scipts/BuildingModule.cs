using UnityEngine;

public class BuildingModule : MonoBehaviour
{
    [Header("Settings")]
    public float buildCooldown = 0.25f;

    [Header("Visuals")]
    public GameObject evaporationEffect; // ★ 추가: 밤에 설치 시 나올 증발 파티클

    private MapGenerator map;
    private float nextBuildTime;

    public void Init(MapGenerator mapGen)
    {
        this.map = mapGen;
    }

    public void TryBuild(RaycastHit hit, int blockID)
    {
        if (Time.time < nextBuildTime) return;

        // ★ 추가: 밤(Night)에는 설치 불가!
        if (GameManager.Instance != null && GameManager.Instance.currentTime == MapType.Night)
        {
            Vector3 effectPos = hit.point;

            // 증발 이펙트 생성 (파티클이 있다면)
            if (evaporationEffect != null)
            {
                Instantiate(evaporationEffect, effectPos, Quaternion.LookRotation(hit.normal));
            }

            Debug.Log("밤에는 블록을 설치할 수 없습니다! (증발)");
            return; // 여기서 함수 종료 (설치 안 함)
        }

        Vector3 targetPos = hit.transform.position + hit.normal;

        if (Vector3.Distance(transform.position, targetPos) < 1.2f) return;

        map.PlaceBlockAt(targetPos, blockID);

        if (InventoryManager.Instance != null)
        {
            InventoryManager.Instance.ConsumeSelectedOne();
        }

        nextBuildTime = Time.time + buildCooldown;
    }
}