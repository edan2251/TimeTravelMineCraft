using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class StorageSlotUI : MonoBehaviour
{
    [Header("UI References")]
    public Image iconImage;
    public TextMeshProUGUI countText;
    public Image backgroundImage;
    public Button btn;

    [Header("Colors")]
    public Color normalColor = new Color(0.5f, 0.5f, 0.5f, 0.5f);
    public Color selectedColor = new Color(1f, 1f, 0f, 0.8f); // 선택되면 노란색

    private int myIndex;
    private StorageSystem system;
    private InventorySlot mySlotData;

    public void Setup(int index, StorageSystem sys)
    {
        myIndex = index;
        system = sys;
        btn.onClick.AddListener(OnClick);
        SetSelected(false);
    }

    public void UpdateSlot(InventorySlot slotData)
    {
        mySlotData = slotData;

        if (slotData != null && slotData.itemData != null && slotData.count > 0)
        {
            iconImage.sprite = slotData.itemData.icon;
            iconImage.enabled = true;
            countText.text = slotData.count.ToString();
            countText.gameObject.SetActive(slotData.count > 1);
        }
        else
        {
            iconImage.enabled = false;
            countText.gameObject.SetActive(false);
        }
    }

    public void SetSelected(bool isSelected)
    {
        backgroundImage.color = isSelected ? selectedColor : normalColor;
    }

    void OnClick()
    {
        // 시스템에게 "나 클릭됐어!" 하고 알림
        system.OnSlotClicked(myIndex);
    }
}