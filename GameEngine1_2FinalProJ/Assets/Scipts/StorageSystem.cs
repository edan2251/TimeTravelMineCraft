using UnityEngine;
using System.Collections.Generic; // List를 쓰기 위해 필수

public class StorageSystem : MonoBehaviour
{
    [Header("Settings")]
    public int storageSize = 40; // 인스펙터에서 40, 50 등 조절 가능

    [Header("UI")]
    public GameObject storagePanelRoot;
    public Transform slotGridParent;
    public GameObject storageSlotPrefab;

    // 현재 열려있는 보관함의 데이터
    private StorageBlock currentStorage;
    // UI 슬롯 리스트
    private List<StorageSlotUI> uiSlots = new List<StorageSlotUI>();

    private int selectedBoxSlotIndex = -1;
    private bool isOpen = false;

    void Start()
    {
        storagePanelRoot.SetActive(false);
        InitializeUISlots();
    }

    // UI 슬롯 초기 생성 (게임 시작 시 한 번만)
    void InitializeUISlots()
    {
        // 기존에 혹시 남아있는 슬롯이 있다면 제거
        foreach (Transform child in slotGridParent) Destroy(child.gameObject);
        uiSlots.Clear();

        // 설정한 크기만큼 슬롯 생성
        for (int i = 0; i < storageSize; i++)
        {
            GameObject go = Instantiate(storageSlotPrefab, slotGridParent);
            StorageSlotUI ui = go.GetComponent<StorageSlotUI>();

            // ★ 여기서 StorageSlotUI의 Setup을 호출하며 'this'(StorageSystem)를 넘겨줍니다.
            ui.Setup(i, this);

            uiSlots.Add(ui);
        }
    }

    void Update()
    {
        if (!isOpen) return;

        // E키로 아이템 이동
        if (Input.GetKeyDown(KeyCode.E)) HandleItemTransfer();

        // ESC로 닫기 (Tab은 PlayerInteraction에서 처리하지만 비상용)
        if (Input.GetKeyDown(KeyCode.Escape)) CloseStorage();
    }

    // 외부에서 호출: 보관함 열기
    public void OpenStorage(StorageBlock storage)
    {
        currentStorage = storage;
        isOpen = true;
        storagePanelRoot.SetActive(true);
        selectedBoxSlotIndex = -1;

        if (UIManager.Instance != null) UIManager.Instance.SetUIState(true);

        RefreshUI();
    }

    // 외부에서 호출: 보관함 닫기
    public void CloseStorage()
    {
        isOpen = false;
        currentStorage = null;
        storagePanelRoot.SetActive(false);
        if (UIManager.Instance != null) UIManager.Instance.SetUIState(false);
    }

    // UI 새로고침
    void RefreshUI()
    {
        if (currentStorage == null) return;

        // UI 개수와 실제 데이터 개수 중 작은 쪽까지만 그림 (에러 방지)
        int count = Mathf.Min(uiSlots.Count, currentStorage.slots.Length);

        for (int i = 0; i < count; i++)
        {
            uiSlots[i].gameObject.SetActive(true); // 활성화
            uiSlots[i].UpdateSlot(currentStorage.slots[i]);
            uiSlots[i].SetSelected(i == selectedBoxSlotIndex);
        }

        // 데이터보다 UI 슬롯이 많으면 남는건 끔
        for (int i = count; i < uiSlots.Count; i++)
        {
            uiSlots[i].gameObject.SetActive(false);
        }
    }

    // ★ StorageSlotUI에서 호출하는 함수
    public void OnSlotClicked(int index)
    {
        // 같은거 또 누르면 취소, 아니면 선택
        if (selectedBoxSlotIndex == index) selectedBoxSlotIndex = -1;
        else selectedBoxSlotIndex = index;

        RefreshUI();
    }

    // 아이템 이동 로직 (E키)
    void HandleItemTransfer()
    {
        // 1. 보관함 -> 플레이어 (보관함 슬롯 선택 시)
        if (selectedBoxSlotIndex != -1)
        {
            if (selectedBoxSlotIndex >= currentStorage.slots.Length) return;

            InventorySlot boxSlot = currentStorage.slots[selectedBoxSlotIndex];
            if (boxSlot.itemData != null)
            {
                bool added = InventoryManager.Instance.AddItem(boxSlot.itemData, boxSlot.count);
                if (added)
                {
                    boxSlot.Clear();
                    selectedBoxSlotIndex = -1;
                    RefreshUI();
                }
            }
        }
        // 2. 플레이어 -> 보관함 (보관함 선택 안함 & 플레이어 핫바 들고있음)
        else
        {
            InventorySlot playerSlot = InventoryManager.Instance.hotbarSlots[InventoryManager.Instance.currentSlotIndex];
            if (playerSlot.itemData != null)
            {
                bool added = currentStorage.AddItemToStorage(playerSlot.itemData, playerSlot.count);
                if (added)
                {
                    playerSlot.Clear();
                    RefreshUI();
                }
            }
        }
    }
}