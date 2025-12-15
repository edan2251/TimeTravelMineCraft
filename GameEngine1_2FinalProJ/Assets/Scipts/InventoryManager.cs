using UnityEngine;
using System.Collections.Generic;

public class InventoryManager : MonoBehaviour
{
    public static InventoryManager Instance;

    [Header("Hotbar")]
    public InventorySlot[] hotbarSlots = new InventorySlot[10]; 
    public int currentSlotIndex = 0; 

    [Header("Drop Settings")]
    public GameObject droppedItemTemplate;

    private void Awake()
    {
        Instance = this;
        for (int i = 0; i < hotbarSlots.Length; i++) hotbarSlots[i] = new InventorySlot();
    }

    public void SpawnDroppedItem(ItemData data, int count, Vector3 position)
    {
        if (droppedItemTemplate == null || data == null) return;

        GameObject obj = Instantiate(droppedItemTemplate, position + Vector3.up * 0.5f, Quaternion.identity);

        obj.GetComponent<DroppedItem>().Setup(data, count);
    }

    public ItemData GetSelectedBlock()
    {
        return hotbarSlots[currentSlotIndex].itemData;
    }

    public bool AddItem(ItemData data, int count)
    {
        foreach (var slot in hotbarSlots)
        {
            if (slot.itemData == data && slot.count < data.maxStack)
            {
                slot.AddCount(count);
                return true;
            }
        }
        foreach (var slot in hotbarSlots)
        {
            if (slot.itemData == null)
            {
                slot.SetItem(data, count);
                return true;
            }
        }
        return false; 
    }

    public void ConsumeSelectedOne()
    {
        hotbarSlots[currentSlotIndex].RemoveCount(1);
    }

    // 1~0 키 입력 처리
    public void HandleHotbarInput()
    {
        if (Input.GetKeyDown(KeyCode.Alpha1)) currentSlotIndex = 0;
        if (Input.GetKeyDown(KeyCode.Alpha2)) currentSlotIndex = 1;
        if (Input.GetKeyDown(KeyCode.Alpha3)) currentSlotIndex = 2;
        if (Input.GetKeyDown(KeyCode.Alpha4)) currentSlotIndex = 3;
        if (Input.GetKeyDown(KeyCode.Alpha5)) currentSlotIndex = 4;
        if (Input.GetKeyDown(KeyCode.Alpha6)) currentSlotIndex = 5;
        if (Input.GetKeyDown(KeyCode.Alpha7)) currentSlotIndex = 6;
        if (Input.GetKeyDown(KeyCode.Alpha8)) currentSlotIndex = 7;
        if (Input.GetKeyDown(KeyCode.Alpha9)) currentSlotIndex = 8;
        if (Input.GetKeyDown(KeyCode.Alpha0)) currentSlotIndex = 9;

        // 마우스 휠 입력
        float scroll = Input.GetAxis("Mouse ScrollWheel");

        if (scroll > 0f) // 휠 올림 (이전 슬롯 / 왼쪽으로)
        {
            currentSlotIndex--;
            if (currentSlotIndex < 0) currentSlotIndex = hotbarSlots.Length - 1; // 0번에서 뒤로 가면 9번으로
        }
        else if (scroll < 0f) // 휠 내림 (다음 슬롯 / 오른쪽으로)
        {
            currentSlotIndex++;
            if (currentSlotIndex >= hotbarSlots.Length) currentSlotIndex = 0; // 9번에서 앞으로 가면 0번으로
        }

        if (Input.GetKeyDown(KeyCode.LeftArrow)) 
        {
            currentSlotIndex--;
            if (currentSlotIndex < 0) currentSlotIndex = hotbarSlots.Length - 1;
        }
        if (Input.GetKeyDown(KeyCode.RightArrow)) 
        {
            currentSlotIndex++;
            if (currentSlotIndex >= hotbarSlots.Length) currentSlotIndex = 0;
        }
    }

    public int GetItemCount(ItemData data)
    {
        int total = 0;
        foreach (var slot in hotbarSlots)
        {
            if (slot.itemData == data)
            {
                total += slot.count;
            }
        }
        return total;
    }

    public void RemoveItem(ItemData data, int amountToRemove)
    {
        foreach (var slot in hotbarSlots)
        {
            if (amountToRemove <= 0) break; 

            if (slot.itemData == data)
            {
                if (slot.count > amountToRemove)
                {
                    slot.count -= amountToRemove;
                    amountToRemove = 0;
                }
                else
                {
                    amountToRemove -= slot.count;
                    slot.Clear();
                }
            }
        }
    }

    public void DropOneFromSelected(Vector3 dropPosition, Vector3 dropDirection)
    {
        InventorySlot currentSlot = hotbarSlots[currentSlotIndex];

        if (currentSlot.itemData == null || currentSlot.count <= 0) return;

        ItemData dataToDrop = currentSlot.itemData;

        currentSlot.RemoveCount(1);

        if (droppedItemTemplate != null)
        {
            GameObject obj = Instantiate(droppedItemTemplate, dropPosition, Quaternion.identity);

            obj.GetComponent<DroppedItem>().Setup(dataToDrop, 1);

            Rigidbody rb = obj.GetComponent<Rigidbody>();
            if (rb != null)
            {
                rb.AddForce(dropDirection * 5f + Vector3.up * 2f, ForceMode.Impulse);
            }
        }
    }

} // < InventoryManager 클래스 끝


// 인벤토리 슬롯 클래스 (데이터 + 개수)
[System.Serializable]
public class InventorySlot
{
    public ItemData itemData;
    public int count;

    public void SetItem(ItemData data, int amount)
    {
        itemData = data;
        count = amount;
    }

    public void AddCount(int amount)
    {
        count += amount;
    }

    public void RemoveCount(int amount)
    {
        count -= amount;
        if (count <= 0) Clear();
    }

    public void Clear()
    {
        itemData = null;
        count = 0;
    }
}