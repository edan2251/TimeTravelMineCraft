using UnityEngine;

public class BlockBehavior : MonoBehaviour
{
    [Header("Data Reference")]
    public ItemData myItemData;

    [Header("Status")]
    public int maxHP = 3;
    public int currentHP;

    private void Start()
    {
        currentHP = maxHP;
    }

    public bool OnHit(int damage)
    {
        currentHP -= damage;

        if (currentHP <= 0)
        {
            BreakAndDrop();
            return true;
        }
        return false; 
    }

    void BreakAndDrop()
    {
        if (myItemData != null)
        {
            // 1. 아예 드롭 기능이 꺼져있는 경우 (예: 기반암 등)
            if (!myItemData.dropsOnBreak)
            {
                Destroy(gameObject);
                return;
            }

            // 2. ★ 추가: 확률 체크 (꽝이면 드롭 안 함)
            // Random.value는 0.0 ~ 1.0 사이의 랜덤값입니다.
            // 예: dropChance가 0.3인데, 랜덤값이 0.8이 나왔다 -> 드롭 실패
            if (Random.value > myItemData.dropChance)
            {
                Destroy(gameObject);
                return;
            }

            // 3. 드롭 아이템 결정 (설정된 게 없으면 자기 자신)
            ItemData dropTarget = (myItemData.dropItem != null) ? myItemData.dropItem : myItemData;

            if (InventoryManager.Instance != null)
            {
                bool added = InventoryManager.Instance.AddItem(dropTarget, 1);

                if (!added)
                {
                    InventoryManager.Instance.SpawnDroppedItem(dropTarget, 1, transform.position);
                }
            }
        }

        Destroy(gameObject);
    }
}