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

    [Header("Detail View UI")]
    public Image resultIcon;
    public TextMeshProUGUI resultNameText;
    public TextMeshProUGUI descriptionText;
    public Button craftButton;
    public TextMeshProUGUI craftButtonText;

    private CraftingRecipe currentRecipe;
    private List<RecipeSlotUI> spawnedSlots = new List<RecipeSlotUI>();
    private bool isOpen = false;

    void Start()
    {
        craftingPanelRoot.SetActive(false);
        InitializeRecipeList();

        craftButton.onClick.AddListener(OnCraftButtonClicked);
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Tab))
        {
            TogglePanel();
        }
    }

    void TogglePanel()
    {
        isOpen = !isOpen;
        craftingPanelRoot.SetActive(isOpen);

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

    void InitializeRecipeList()
    {
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

            string colorHex = (currentCount >= requiredCount) ? "#00FF00" : "#FF0000";

            sb.AppendLine($"{ing.item.itemName}: <color={colorHex}>{currentCount} / {requiredCount}</color>");

            if (currentCount < requiredCount)
            {
                canCraft = false;
            }
        }

        descriptionText.text = sb.ToString();

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

    void OnCraftButtonClicked()
    {
        if (currentRecipe == null) return;

        foreach (var ing in currentRecipe.ingredients)
        {
            InventoryManager.Instance.RemoveItem(ing.item, ing.count);
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