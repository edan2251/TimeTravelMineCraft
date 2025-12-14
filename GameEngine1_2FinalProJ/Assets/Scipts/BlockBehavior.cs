using UnityEngine;

public class BlockBehavior : MonoBehaviour
{
    [Header("Data Reference")]
    public ItemData myItemData; // ★ 핵심: 이 블록이 파괴되면 어떤 아이템 데이터인가?

    [Header("Status")]
    public int maxHP = 3;
    public int currentHP;

    private void Start()
    {
        currentHP = maxHP;
    }

    // 채광 모듈(MiningModule)에서 호출
    public bool OnHit(int damage)
    {
        currentHP -= damage;
        // (파티클 재생 로직...)

        if (currentHP <= 0)
        {
            BreakAndDrop();
            return true; // "나 죽었음!" 신호
        }
        return false; // "아직 살아있음"
    }

    void BreakAndDrop()
    {
        if (InventoryManager.Instance != null && myItemData != null)
        {
            ItemData dropTarget = (myItemData.dropItem != null) ? myItemData.dropItem : myItemData;

            // 인벤토리 추가 시도
            bool added = InventoryManager.Instance.AddItem(dropTarget, 1);

            // 실패 시 떨구기
            if (!added)
            {
                InventoryManager.Instance.SpawnDroppedItem(dropTarget, 1, transform.position);
            }
        }
        Destroy(gameObject);
    }
}