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
            if (!myItemData.dropsOnBreak)
            {
                Destroy(gameObject);
                return; 
            }

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