using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class InventoryUI : MonoBehaviour
{
    [Header("UI References")]
    public Transform hotbarPanel; 

    [Header("Color Settings")]
    public Color normalColor = new Color(0.5f, 0.5f, 0.5f, 1f);   
    public Color selectedColor = new Color(1f, 1f, 1f, 1f);       

    private Image[] slotBackgrounds;
    private Image[] iconImages;      
    private TextMeshProUGUI[] countTexts;

    void Start()
    {
        int childCount = hotbarPanel.childCount;

        slotBackgrounds = new Image[childCount];
        iconImages = new Image[childCount];
        countTexts = new TextMeshProUGUI[childCount];

        for (int i = 0; i < childCount; i++)
        {
            Transform slot = hotbarPanel.GetChild(i);

            slotBackgrounds[i] = slot.GetComponent<Image>();

            Transform iconTr = slot.Find("Icon");
            Transform textTr = slot.Find("CountText");

            if (iconTr == null || textTr == null)
            {
                Debug.LogError($"[UI 오류] {slot.name}의 자식 이름이 틀렸습니다.");
                continue;
            }

            iconImages[i] = iconTr.GetComponent<Image>();
            countTexts[i] = textTr.GetComponent<TextMeshProUGUI>();
        }

        UpdateHotbarUI();
    }

    void Update()
    {
        UpdateHotbarUI();     
        UpdateSelectionColor();
    }

    void UpdateHotbarUI()
    {
        var slots = InventoryManager.Instance.hotbarSlots;

        for (int i = 0; i < slots.Length; i++)
        {
            if (i >= iconImages.Length) break;

            if (slots[i].itemData != null)
            {
                iconImages[i].sprite = slots[i].itemData.icon;

                Color c = iconImages[i].color; c.a = 1f; iconImages[i].color = c;

                iconImages[i].gameObject.SetActive(true);

                if (slots[i].count > 1)
                {
                    countTexts[i].text = slots[i].count.ToString();
                    countTexts[i].gameObject.SetActive(true);
                }
                else
                {
                    countTexts[i].gameObject.SetActive(false);
                }
            }
            else
            {
                iconImages[i].gameObject.SetActive(false);
                countTexts[i].gameObject.SetActive(false);
            }
        }
    }

    void UpdateSelectionColor()
    {
        int currentIndex = InventoryManager.Instance.currentSlotIndex;

        for (int i = 0; i < slotBackgrounds.Length; i++)
        {
            if (i == currentIndex)
            {
                slotBackgrounds[i].color = selectedColor;

            }
            else
            {
                slotBackgrounds[i].color = normalColor;

            }
        }
    }
}