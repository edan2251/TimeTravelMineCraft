using UnityEngine;
using System.Collections.Generic;

public class InventoryManager : MonoBehaviour
{
    // 싱글톤 (어디서든 접근 가능하게)
    public static InventoryManager Instance;

    [Header("Hotbar")]
    public InventorySlot[] hotbarSlots = new InventorySlot[10]; // 0~9번 슬롯
    public int currentSlotIndex = 0; // 현재 선택된 슬롯 (0~9)

    [Header("Drop Settings")]
    public GameObject droppedItemTemplate;

    private void Awake()
    {
        Instance = this;
        // 슬롯 초기화
        for (int i = 0; i < hotbarSlots.Length; i++) hotbarSlots[i] = new InventorySlot();
    }

    public void SpawnDroppedItem(ItemData data, int count, Vector3 position)
    {
        if (droppedItemTemplate == null || data == null) return;

        // 1. 껍데기 생성 (위치는 바닥보다 살짝 위)
        GameObject obj = Instantiate(droppedItemTemplate, position + Vector3.up * 0.5f, Quaternion.identity);

        // 2. 데이터 주입 (이때 내부에서 모델 생성 및 스케일 조정 일어남)
        obj.GetComponent<DroppedItem>().Setup(data, count);
    }

    // 현재 선택된 아이템 데이터 반환 (없으면 null)
    public ItemData GetSelectedBlock()
    {
        return hotbarSlots[currentSlotIndex].itemData;
    }

    // 아이템 획득 (채집 시 호출)
    public bool AddItem(ItemData data, int count)
    {
        // 1. 이미 있는 슬롯에 합치기
        foreach (var slot in hotbarSlots)
        {
            if (slot.itemData == data && slot.count < data.maxStack)
            {
                slot.AddCount(count);
                return true;
            }
        }
        // 2. 빈 슬롯에 넣기
        foreach (var slot in hotbarSlots)
        {
            if (slot.itemData == null)
            {
                slot.SetItem(data, count);
                return true;
            }
        }
        return false; // 인벤토리 꽉 참
    }

    // 아이템 사용/설치 시 1개 감소
    public void ConsumeSelectedOne()
    {
        hotbarSlots[currentSlotIndex].RemoveCount(1);
    }

    // 1~0 키 입력 처리
    public void HandleHotbarInput()
    {
        // --- 1. 숫자 키 입력 (기존 유지) ---
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

        // --- 2. 마우스 휠 입력 ---
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

        // --- 3. 화살표 키 입력 ---
        if (Input.GetKeyDown(KeyCode.LeftArrow)) // 왼쪽 화살표 (이전 슬롯)
        {
            currentSlotIndex--;
            if (currentSlotIndex < 0) currentSlotIndex = hotbarSlots.Length - 1;
        }
        if (Input.GetKeyDown(KeyCode.RightArrow)) // 오른쪽 화살표 (다음 슬롯)
        {
            currentSlotIndex++;
            if (currentSlotIndex >= hotbarSlots.Length) currentSlotIndex = 0;
        }
    }

    // =========================================================
    // ★ 여기가 중요합니다! 함수들을 InventoryManager 클래스 안으로 옮겼습니다.
    // =========================================================

    // 특정 아이템 데이터(ItemData)가 총 몇 개 있는지 확인
    public int GetItemCount(ItemData data)
    {
        int total = 0;
        foreach (var slot in hotbarSlots)
        {
            // slot.itemData가 null일 수 있으므로 체크
            if (slot.itemData == data)
            {
                total += slot.count;
            }
        }
        return total;
    }

    // 특정 아이템을 개수만큼 제거 (앞에서부터 제거)
    public void RemoveItem(ItemData data, int amountToRemove)
    {
        foreach (var slot in hotbarSlots)
        {
            if (amountToRemove <= 0) break; // 다 지웠으면 종료

            if (slot.itemData == data)
            {
                if (slot.count > amountToRemove)
                {
                    // 이 슬롯에 충분히 있으면 차감하고 종료
                    slot.count -= amountToRemove;
                    amountToRemove = 0;
                }
                else
                {
                    // 이 슬롯을 다 비워도 모자라면
                    amountToRemove -= slot.count;
                    slot.Clear();
                }
            }
        }
    }

    public void DropOneFromSelected(Vector3 dropPosition, Vector3 dropDirection)
    {
        // 1. 현재 슬롯 가져오기
        InventorySlot currentSlot = hotbarSlots[currentSlotIndex];

        // 2. 아이템이 없으면 리턴
        if (currentSlot.itemData == null || currentSlot.count <= 0) return;

        // 3. 떨어뜨릴 데이터 백업 (RemoveCount 하면 데이터가 사라질 수도 있어서 미리 복사)
        ItemData dataToDrop = currentSlot.itemData;

        // 4. 인벤토리에서 1개 차감
        currentSlot.RemoveCount(1);

        // 5. 바닥에 생성 (기존에 만든 SpawnDroppedItem 활용)
        // 약간 앞쪽 + 위쪽에 생성
        if (droppedItemTemplate != null)
        {
            GameObject obj = Instantiate(droppedItemTemplate, dropPosition, Quaternion.identity);

            // 데이터 주입
            obj.GetComponent<DroppedItem>().Setup(dataToDrop, 1);

            // 6. 던지는 힘 추가 (앞으로 툭 던지기)
            Rigidbody rb = obj.GetComponent<Rigidbody>();
            if (rb != null)
            {
                // dropDirection은 플레이어가 바라보는 방향
                rb.AddForce(dropDirection * 5f + Vector3.up * 2f, ForceMode.Impulse);
            }
        }
    }

} // <--- InventoryManager 클래스 끝 (여기가 진짜 끝!)


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