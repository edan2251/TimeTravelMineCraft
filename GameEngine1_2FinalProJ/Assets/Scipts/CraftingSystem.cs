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

    // ★ 치트 모드 변수 추가
    private bool isCreativeMode = false;

    void Start()
    {
        craftingPanelRoot.SetActive(false);
        craftButton.onClick.AddListener(OnCraftButtonClicked);
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Tab))
        {
            TogglePanel();
        }

        // ★ P키로 무료 제작 모드 토글
        if (Input.GetKeyDown(KeyCode.P))
        {
            isCreativeMode = !isCreativeMode;
            Debug.Log($"무료 제작 치트: {(isCreativeMode ? "ON" : "OFF")}");

            // UI가 열려있다면 즉시 갱신해서 버튼 활성화 상태 보여주기
            if (isOpen && currentRecipe != null)
            {
                UpdateDetailView();
            }
        }

        if (isOpen && currentStationType != CraftingType.Player)
        {
            if (!CheckNearbyStation(currentStationType))
            {
                TogglePanel();
            }
        }
    }

    void TogglePanel()
    {
        isOpen = !isOpen;

        if (isOpen)
        {
            DetectStationAndRefresh();
        }

        craftingPanelRoot.SetActive(isOpen);

        if (UIManager.Instance != null)
        {
            UIManager.Instance.SetUIState(isOpen);
        }

        if (isOpen)
        {
            currentRecipe = null;
            ClearDetailView();
        }
    }

    void DetectStationAndRefresh()
    {
        currentStationType = CraftingType.Player;
        string title = "제작 (기본)";

        CraftingStation[] stations = FindObjectsOfType<CraftingStation>();

        foreach (var station in stations)
        {
            if (station.IsPlayerInRange())
            {
                if (station.stationType == CraftingType.Furnace)
                {
                    currentStationType = CraftingType.Furnace;
                    title = "화로";
                    break;
                }
                else if (station.stationType == CraftingType.Workbench)
                {
                    currentStationType = CraftingType.Workbench;
                    title = "제작대";
                }
            }
        }

        if (panelTitleText != null) panelTitleText.text = title;
        RefreshRecipeList(currentStationType);
    }

    bool CheckNearbyStation(CraftingType type)
    {
        CraftingStation[] stations = FindObjectsOfType<CraftingStation>();
        foreach (var station in stations)
        {
            if (station.stationType == type && station.IsPlayerInRange()) return true;
        }
        return false;
    }

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

            // ★ 치트 모드일 때는 항상 Cyan(하늘색), 아니면 초록/빨강
            string colorHex;
            if (isCreativeMode)
            {
                colorHex = "#00FFFF"; // 치트 활성화 색상
            }
            else
            {
                colorHex = (currentCount >= requiredCount) ? "#00FF00" : "#FF0000";
            }

            sb.AppendLine($"{ing.item.itemName}: <color={colorHex}>{currentCount} / {requiredCount}</color>");

            // ★ 치트가 꺼져있을 때만 재료 부족 검사
            if (!isCreativeMode && currentCount < requiredCount)
            {
                canCraft = false;
            }
        }

        descriptionText.text = sb.ToString();

        craftButton.interactable = canCraft;

        // 버튼 텍스트 변경
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

        // ★ 치트가 꺼져있을 때만 재료 소모
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