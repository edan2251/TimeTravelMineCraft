using System.Collections.Generic;
using System.Text;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class CraftingSystem : MonoBehaviour
{
    [Header("Data")]
    public List<CraftingRecipe> allRecipes;

    [Header("UI References")]
    public GameObject craftingPanelRoot;
    public Transform recipeListContent;
    public GameObject recipeSlotPrefab;
    public TextMeshProUGUI panelTitleText;

    [Header("Detail View UI")]
    public Image resultIcon;
    public TextMeshProUGUI resultNameText;
    public TextMeshProUGUI descriptionText;
    public Button craftButton;
    public TextMeshProUGUI craftButtonText;

    private CraftingRecipe currentRecipe;
    private List<RecipeSlotUI> spawnedSlots = new List<RecipeSlotUI>();
    private bool isOpen = false;

    private CraftingType currentStationType = CraftingType.Player;
    private bool isCreativeMode = false;

    void Start()
    {
        craftingPanelRoot.SetActive(false);
        craftButton.onClick.AddListener(OnCraftButtonClicked);
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.P))
        {
            isCreativeMode = !isCreativeMode;
            if (isOpen && currentRecipe != null) UpdateDetailView();
        }
    }

    // ★ 외부(PlayerInteraction)에서 호출하는 열기 함수
    public void OpenCraftingMenu(CraftingType type)
    {
        if (isOpen) return; // 이미 열려있으면 무시

        currentStationType = type;
        isOpen = true;
        craftingPanelRoot.SetActive(true);

        if (UIManager.Instance != null) UIManager.Instance.SetUIState(true);

        // 타이틀 설정
        string title = "제작 (기본)";
        if (type == CraftingType.Workbench) title = "제작대";
        else if (type == CraftingType.Furnace) title = "화로";

        if (panelTitleText != null) panelTitleText.text = title;

        currentRecipe = null;
        ClearDetailView();
        RefreshRecipeList(currentStationType);
    }

    // 패널 닫기
    public void ClosePanel()
    {
        isOpen = false;
        craftingPanelRoot.SetActive(false);
        if (UIManager.Instance != null) UIManager.Instance.SetUIState(false);
    }

    // ★ TogglePanel, DetectStationAndRefresh, CheckNearbyStation 함수들은 더 이상 필요 없으므로 삭제하거나 안 씀

    void RefreshRecipeList(CraftingType type)
    {
        foreach (Transform child in recipeListContent) Destroy(child.gameObject);
        spawnedSlots.Clear();

        foreach (var recipe in allRecipes)
        {
            if (recipe.requiredStation == type)
            {
                GameObject go = Instantiate(recipeSlotPrefab, recipeListContent);
                RecipeSlotUI slot = go.GetComponent<RecipeSlotUI>();
                slot.Setup(recipe, this);
                spawnedSlots.Add(slot);
            }
        }
    }

    public void SelectRecipe(CraftingRecipe recipe)
    {
        currentRecipe = recipe;
        UpdateDetailView();
    }

    public void UpdateSelectionVisuals(RecipeSlotUI selectedSlot)
    {
        foreach (var slot in spawnedSlots)
        {
            slot.SetSelected(slot == selectedSlot);
        }
    }

    void UpdateDetailView()
    {
        if (currentRecipe == null) return;

        resultIcon.sprite = currentRecipe.resultItem.icon;
        resultIcon.enabled = true;
        resultNameText.text = currentRecipe.resultItem.itemName;

        StringBuilder sb = new StringBuilder();
        bool canCraft = true;

        foreach (var ing in currentRecipe.ingredients)
        {
            int currentCount = InventoryManager.Instance.GetItemCount(ing.item);
            int requiredCount = ing.count;

            string colorHex;
            if (isCreativeMode) colorHex = "#00FFFF";
            else colorHex = (currentCount >= requiredCount) ? "#00FF00" : "#FF0000";

            sb.AppendLine($"{ing.item.itemName}: <color={colorHex}>{currentCount} / {requiredCount}</color>");

            if (!isCreativeMode && currentCount < requiredCount) canCraft = false;
        }

        descriptionText.text = sb.ToString();
        craftButton.interactable = canCraft;

        if (isCreativeMode) craftButtonText.text = "무료 제작 (Cheat)";
        else craftButtonText.text = canCraft ? "제작하기" : "재료 부족";
    }

    void ClearDetailView()
    {
        resultIcon.enabled = false;
        resultNameText.text = "레시피를 선택하세요";
        descriptionText.text = "";
        craftButton.interactable = false;
        craftButtonText.text = "-";
    }

    void OnCraftButtonClicked()
    {
        if (currentRecipe == null) return;

        if (!isCreativeMode)
        {
            foreach (var ing in currentRecipe.ingredients)
            {
                InventoryManager.Instance.RemoveItem(ing.item, ing.count);
            }
        }

        bool added = InventoryManager.Instance.AddItem(currentRecipe.resultItem, currentRecipe.resultCount);
        if (!added)
        {
            Transform playerPos = GameObject.FindGameObjectWithTag("Player").transform;
            InventoryManager.Instance.SpawnDroppedItem(
                currentRecipe.resultItem,
                currentRecipe.resultCount,
                playerPos.position + playerPos.forward
            );
            Debug.Log("인벤토리가 꽉 차서 바닥에 버려졌습니다!");
        }

        UpdateDetailView();
    }
}