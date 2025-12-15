using UnityEngine;

public class MiningModule : MonoBehaviour
{
    [Header("Settings")]
    public float hitCooldown = 0.2f; 

    private MapGenerator map;
    private float nextHitTime;

    public void Init(MapGenerator mapGen)
    {
        this.map = mapGen;
    }

    public void TryMine(RaycastHit hit, int damage)
    {
        if (Time.time < nextHitTime) return;

        BlockBehavior block = hit.collider.GetComponent<BlockBehavior>();

        if (block != null)
        {
            bool isDestroyed = block.OnHit(damage);

            if (isDestroyed)
            {
                Vector3 targetPos = hit.point - (hit.normal * 0.1f);
                map.RemoveBlockAt(targetPos);
            }

        }

        nextHitTime = Time.time + hitCooldown;
    }
}