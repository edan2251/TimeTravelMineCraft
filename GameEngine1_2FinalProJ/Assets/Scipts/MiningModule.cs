using UnityEngine;

public class MiningModule : MonoBehaviour
{
    [Header("Settings")]
    public float hitCooldown = 0.2f; // 공격 속도

    private MapGenerator map;
    private float nextHitTime;

    public void Init(MapGenerator mapGen)
    {
        this.map = mapGen;
    }

    // ★ 수정됨: damage 매개변수 추가
    public void TryMine(RaycastHit hit, int damage)
    {
        if (Time.time < nextHitTime) return;

        // 1. 맞은 놈이 '블록 행동' 스크립트를 가지고 있는지 확인
        BlockBehavior block = hit.collider.GetComponent<BlockBehavior>();

        if (block != null)
        {
            // 2. 블록에게 데미지 전달
            // ★ OnHit이 true를 반환하면 "나 죽었어"라는 뜻으로 약속합시다.
            bool isDestroyed = block.OnHit(damage);

            if (isDestroyed)
            {
                // 3. 맵 데이터 갱신 (블록이 파괴된 경우에만)
                Vector3 targetPos = hit.point - (hit.normal * 0.1f);
                map.RemoveBlockAt(targetPos);
            }

            // 타격 효과(파티클/소리)는 죽든 안 죽든 재생
            // EffectManager.PlayHit(hit.point);
        }

        nextHitTime = Time.time + hitCooldown;
    }
}