using UnityEngine;

public class StorageBlock : MonoBehaviour
{
    [Header("Settings")]
    public int slotCount = 40; // ★ 여기서 이 상자의 크기 결정

    // public으로 열어두되, 크기는 Awake에서 결정됨
    [HideInInspector]
    public InventorySlot[] slots;

    private Vector3Int myGridPos;

    void Awake()
    {
        // ★ 설정된 slotCount 크기로 배열 생성
        slots = new InventorySlot[slotCount];

        for (int i = 0; i < slots.Length; i++)
        {
            slots[i] = new InventorySlot();
        }
    }

    void Start()
    {
        // 1. 내 그리드 좌표 계산
        myGridPos = new Vector3Int(
            Mathf.RoundToInt(transform.position.x),
            Mathf.RoundToInt(transform.position.y),
            Mathf.RoundToInt(transform.position.z)
        );

        if (GameManager.Instance != null)
        {
            MapType currentTime = GameManager.Instance.currentTime;

            // 2. GameManager에 저장된 내 데이터가 있는지 확인
            InventorySlot[] savedSlots = GameManager.Instance.GetStorageData(currentTime, myGridPos);

            if (savedSlots != null)
            {
                // ★ 데이터가 있으면 그걸 내 걸로 씀 (불러오기)
                // 참조(Reference)를 가져오는 거라, 내가 수정하면 GameManager 것도 수정됨
                this.slots = savedSlots;
            }
            else
            {
                // ★ 데이터가 없으면(처음 설치), 내 빈 슬롯을 GameManager에 등록
                GameManager.Instance.SaveStorageData(currentTime, myGridPos, this.slots);
            }
        }
    }

    // ... (AddItemToStorage 등 나머지 코드는 동일)
    public bool AddItemToStorage(ItemData data, int count)
    {
        foreach (var slot in slots)
        {
            if (slot.itemData == data && slot.count < data.maxStack)
            {
                slot.AddCount(count);
                return true;
            }
        }
        foreach (var slot in slots)
        {
            if (slot.itemData == null)
            {
                slot.SetItem(data, count);
                return true;
            }
        }
        return false;
    }
}