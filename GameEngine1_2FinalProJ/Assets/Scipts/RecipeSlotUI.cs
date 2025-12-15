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
    public Image backgroundImage; 

    public Color normalColor = new Color(1f, 1f, 1f, 0f);  
    public Color selectedColor = new Color(0f, 0f, 0f, 0.7f); 

    private CraftingRecipe myRecipe;
    private CraftingSystem system;

    public void Setup(CraftingRecipe recipe, CraftingSystem craftingSystem)
    {
        myRecipe = recipe;
        system = craftingSystem;

        iconImage.sprite = recipe.resultItem.icon;
        nameText.text = recipe.resultItem.itemName;

        SetSelected(false);

        button.onClick.RemoveAllListeners();
        button.onClick.AddListener(() => {
            system.SelectRecipe(myRecipe);
            system.UpdateSelectionVisuals(this);
        });
    }

    public void SetSelected(bool isSelected)
    {
        if (backgroundImage != null)
        {
            backgroundImage.color = isSelected ? selectedColor : normalColor;
        }
    }
}