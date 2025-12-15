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

        if (Vector3.Distance(transform.position, targetPos) < 1.2f) return;

        map.PlaceBlockAt(targetPos, blockID);

        nextBuildTime = Time.time + buildCooldown;

    }
}