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
    public TextMeshProUGUI panelTitleText; // ★ 추가: 현재 어떤 제작소인지 표시용 (예: "제작대", "화로")

    [Header("Detail View UI")]
    public Image resultIcon;
    public TextMeshProUGUI resultNameText;
    public TextMeshProUGUI descriptionText;
    public Button craftButton;
    public TextMeshProUGUI craftButtonText;

    private CraftingRecipe currentRecipe;
    private List<RecipeSlotUI> spawnedSlots = new List<RecipeSlotUI>();
    private bool isOpen = false;

    // ★ 현재 사용 가능한 제작소 타입 (기본은 Player)
    private CraftingType currentStationType = CraftingType.Player;

    void Start()
    {
        craftingPanelRoot.SetActive(false);
        // Start에서는 목록을 만들지 않습니다. 열 때마다 갱신합니다.

        craftButton.onClick.AddListener(OnCraftButtonClicked);
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Tab))
        {
            TogglePanel();
        }

        // UI가 켜져있을 때 범위 밖으로 나가면 자동으로 닫기 (선택사항)
        if (isOpen && currentStationType != CraftingType.Player)
        {
            if (!CheckNearbyStation(currentStationType))
            {
                TogglePanel(); // 닫기
            }
        }
    }

    void TogglePanel()
    {
        isOpen = !isOpen;

        if (isOpen)
        {
            // ★ 열릴 때 주변 스캔!
            DetectStationAndRefresh();
        }

        craftingPanelRoot.SetActive(isOpen);

        if (UIManager.Instance != null)
        {
            UIManager.Instance.SetUIState(isOpen);
        }

        if (isOpen)
        {
            // UI 초기화 (상세창 비우기)
            currentRecipe = null;
            ClearDetailView();
        }
    }

    // ★ 핵심 함수: 주변에 제작대가 있는지 확인하고 리스트 갱신
    void DetectStationAndRefresh()
    {
        // 1. 기본은 플레이어 제작
        currentStationType = CraftingType.Player;
        string title = "제작 (기본)";

        // 2. 주변 제작소 검색 (CraftingStation 스크립트를 가진 모든 오브젝트)
        // (최적화를 위해 LayerMask나 ColliderOverlap을 쓸 수도 있지만, 지금은 Find로 충분)
        CraftingStation[] stations = FindObjectsOfType<CraftingStation>();

        // 우선순위: 화로 > 제작대 > 플레이어 (겹쳐있을 경우)
        foreach (var station in stations)
        {
            if (station.IsPlayerInRange())
            {
                if (station.stationType == CraftingType.Furnace)
                {
                    currentStationType = CraftingType.Furnace;
                    title = "화로";
                    break; // 화로 찾으면 즉시 확정
                }
                else if (station.stationType == CraftingType.Workbench)
                {
                    currentStationType = CraftingType.Workbench;
                    title = "제작대";
                    // 화로가 있을 수 있으니 break 안 하고 계속 검색해볼 수도 있음
                }
            }
        }

        // 3. UI 타이틀 변경 (있으면)
        if (panelTitleText != null) panelTitleText.text = title;

        // 4. 해당 타입의 레시피만 필터링해서 목록 생성
        RefreshRecipeList(currentStationType);
    }

    // 특정 타입의 제작소가 여전히 유효한지 체크 (Update용)
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
        // 기존 목록 삭제
        foreach (Transform child in recipeListContent) Destroy(child.gameObject);
        spawnedSlots.Clear();

        // 조건에 맞는 레시피만 생성
        foreach (var recipe in allRecipes)
        {
            // ★ 내 제작소 타입과 레시피 요구 타입이 같아야 함
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