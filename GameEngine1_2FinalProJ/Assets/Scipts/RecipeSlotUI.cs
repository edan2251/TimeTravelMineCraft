using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class RecipeSlotUI : MonoBehaviour
{
    [Header("UI References")]
    public Image iconImage;
    public TextMeshProUGUI nameText;
    public Button button;

    [Header("Background Settings")]
    public Image backgroundImage; // ★ 슬롯의 배경 이미지 (색을 바꿀 대상)

    public Color normalColor = new Color(1f, 1f, 1f, 0f);    // 평소 색상 (투명)
    public Color selectedColor = new Color(0f, 0f, 0f, 0.7f); // 선택된 색상 (반투명 검정)

    private CraftingRecipe myRecipe;
    private CraftingSystem system;

    public void Setup(CraftingRecipe recipe, CraftingSystem craftingSystem)
    {
        myRecipe = recipe;
        system = craftingSystem;

        iconImage.sprite = recipe.resultItem.icon;
        nameText.text = recipe.resultItem.itemName;

        // 처음엔 선택 안 된 상태로 초기화
        SetSelected(false);

        // 버튼 클릭 이벤트
        button.onClick.RemoveAllListeners(); // 재사용 시 중복 방지
        button.onClick.AddListener(() => {
            system.SelectRecipe(myRecipe);
            system.UpdateSelectionVisuals(this);
        });
    }

    // ★ 배경색 변경 로직
    public void SetSelected(bool isSelected)
    {
        if (backgroundImage != null)
        {
            backgroundImage.color = isSelected ? selectedColor : normalColor;
        }
    }
}