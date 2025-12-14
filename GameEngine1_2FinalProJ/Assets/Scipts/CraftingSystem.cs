using System.Collections.Generic;
using System.Text;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class CraftingSystem : MonoBehaviour
{
    [Header("Data")]
    public List<CraftingRecipe> allRecipes; // 인스펙터에서 레시피들을 등록

    [Header("UI References")]
    public GameObject craftingPanelRoot; // 패널 전체 (Tab으로 껐다 킬 대상)
    public Transform recipeListContent;  // Scroll View의 Content
    public GameObject recipeSlotPrefab;  // RecipeSlotUI 프리팹

    [Header("Detail View UI")]
    public Image resultIcon;
    public TextMeshProUGUI resultNameText;
    public TextMeshProUGUI descriptionText; // "재료: 나무 3/5" 같은 내용 표시
    public Button craftButton;
    public TextMeshProUGUI craftButtonText;

    private CraftingRecipe currentRecipe;
    private List<RecipeSlotUI> spawnedSlots = new List<RecipeSlotUI>();
    private bool isOpen = false;

    void Start()
    {
        // 시작 시 패널 닫기 및 초기화
        craftingPanelRoot.SetActive(false);
        InitializeRecipeList();

        craftButton.onClick.AddListener(OnCraftButtonClicked);
    }

    void Update()
    {
        // Tab 키로 열고 닫기
        if (Input.GetKeyDown(KeyCode.Tab))
        {
            TogglePanel();
        }
    }

    void TogglePanel()
    {
        isOpen = !isOpen;
        craftingPanelRoot.SetActive(isOpen);

        // ★ 추가된 부분: UI 매니저에게 상태 알림
        if (UIManager.Instance != null)
        {
            UIManager.Instance.SetUIState(isOpen);
        }

        if (isOpen)
        {
            if (currentRecipe != null) UpdateDetailView();
            else ClearDetailView();
        }
    }

    // 1. 레시피 리스트 생성 (게임 시작 시 한 번만 실행)
    void InitializeRecipeList()
    {
        // 기존 슬롯 제거
        foreach (Transform child in recipeListContent) Destroy(child.gameObject);
        spawnedSlots.Clear();

        foreach (var recipe in allRecipes)
        {
            GameObject go = Instantiate(recipeSlotPrefab, recipeListContent);
            RecipeSlotUI slot = go.GetComponent<RecipeSlotUI>();
            slot.Setup(recipe, this);
            spawnedSlots.Add(slot);
        }
    }

    // 2. 레시피 선택 시 호출됨 (RecipeSlotUI에서 호출)
    public void SelectRecipe(CraftingRecipe recipe)
    {
        currentRecipe = recipe;
        UpdateDetailView();
    }

    // 3. 선택된 슬롯 시각적 강조 (노란 테두리 등)
    public void UpdateSelectionVisuals(RecipeSlotUI selectedSlot)
    {
        foreach (var slot in spawnedSlots)
        {
            slot.SetSelected(slot == selectedSlot);
        }
    }

    // 4. 우측 상세 정보창 갱신 (핵심 로직)
    void UpdateDetailView()
    {
        if (currentRecipe == null) return;

        // 결과물 정보 표시
        resultIcon.sprite = currentRecipe.resultItem.icon;
        resultIcon.enabled = true;
        resultNameText.text = currentRecipe.resultItem.itemName;

        // 재료 확인 및 텍스트 표시
        StringBuilder sb = new StringBuilder();
        bool canCraft = true;

        foreach (var ing in currentRecipe.ingredients)
        {
            // 인벤토리에 있는 개수 확인 (InventoryManager에 헬퍼 함수 필요)
            int currentCount = InventoryManager.Instance.GetItemCount(ing.item);
            int requiredCount = ing.count;

            // 텍스트 색상 설정 (충분하면 초록, 부족하면 빨강)
            string colorHex = (currentCount >= requiredCount) ? "#00FF00" : "#FF0000";

            sb.AppendLine($"{ing.item.itemName}: <color={colorHex}>{currentCount} / {requiredCount}</color>");

            if (currentCount < requiredCount)
            {
                canCraft = false;
            }
        }

        descriptionText.text = sb.ToString();

        // 버튼 상태 갱신
        craftButton.interactable = canCraft;
        craftButtonText.text = canCraft ? "제작하기" : "재료 부족";
    }

    void ClearDetailView()
    {
        resultIcon.enabled = false;
        resultNameText.text = "레시피를 선택하세요";
        descriptionText.text = "";
        craftButton.interactable = false;
        craftButtonText.text = "-";
    }

    // 5. 제작 버튼 클릭
    void OnCraftButtonClicked()
    {
        if (currentRecipe == null) return;

        // 1. 재료 소모
        foreach (var ing in currentRecipe.ingredients)
        {
            InventoryManager.Instance.RemoveItem(ing.item, ing.count);
        }

        // 2. 결과물 지급 시도
        bool added = InventoryManager.Instance.AddItem(currentRecipe.resultItem, currentRecipe.resultCount);

        // 3. ★ 실패 시(꽉 참) 플레이어 앞에 떨구기
        if (!added)
        {
            // 플레이어 위치 찾기 (PlayerInteraction이 붙은 객체 등)
            Transform playerPos = GameObject.FindGameObjectWithTag("Player").transform;

            InventoryManager.Instance.SpawnDroppedItem(
                currentRecipe.resultItem,
                currentRecipe.resultCount,
                playerPos.position + playerPos.forward // 플레이어 살짝 앞
            );

            Debug.Log("인벤토리가 꽉 차서 바닥에 버려졌습니다!");
        }

        UpdateDetailView();
    }
}