using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class InventoryUI : MonoBehaviour
{
    [Header("UI References")]
    public Transform hotbarPanel;  // Slot들이 모여있는 부모 패널
    // public RectTransform selector; // <-- 더 이상 필요 없음!

    [Header("Color Settings")]
    public Color normalColor = new Color(0.5f, 0.5f, 0.5f, 1f);   // 평소 색상 (회색)
    public Color selectedColor = new Color(1f, 1f, 1f, 1f);       // 선택된 색상 (흰색)

    private Image[] slotBackgrounds; // 슬롯 자체의 배경 이미지들
    private Image[] iconImages;      // 아이템 아이콘들
    private TextMeshProUGUI[] countTexts; // 개수 텍스트들

    void Start()
    {
        int childCount = hotbarPanel.childCount;

        // 배열 초기화
        slotBackgrounds = new Image[childCount];
        iconImages = new Image[childCount];
        countTexts = new TextMeshProUGUI[childCount];

        for (int i = 0; i < childCount; i++)
        {
            Transform slot = hotbarPanel.GetChild(i);

            // 1. 슬롯 배경(자기 자신) 가져오기
            slotBackgrounds[i] = slot.GetComponent<Image>();

            // 2. 아이콘과 텍스트 가져오기
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
        UpdateHotbarUI();      // 아이콘/텍스트 갱신
        UpdateSelectionColor(); // ★ 배경색 갱신 (새로 추가됨)
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

                // 투명도 안전장치
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

    // ★ 선택된 슬롯의 배경색을 바꾸는 함수
    void UpdateSelectionColor()
    {
        int currentIndex = InventoryManager.Instance.currentSlotIndex;

        for (int i = 0; i < slotBackgrounds.Length; i++)
        {
            // 현재 인덱스와 같으면 흰색(Selected), 아니면 회색(Normal)
            if (i == currentIndex)
            {
                slotBackgrounds[i].color = selectedColor;

                // (선택 사항) 선택된 슬롯을 살짝 키우고 싶다면?
                // slotBackgrounds[i].transform.localScale = Vector3.one * 1.1f;
            }
            else
            {
                slotBackgrounds[i].color = normalColor;

                // (선택 사항) 크기 원상복구
                // slotBackgrounds[i].transform.localScale = Vector3.one;
            }
        }
    }
}