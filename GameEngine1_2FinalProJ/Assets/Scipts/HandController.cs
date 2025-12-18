using UnityEngine;

public class HandController : MonoBehaviour
{
    [Header("References")]
    public Transform handMount; // 아까 만든 HandMount 연결

    [Header("Settings")]
    public Vector3 defaultScale = Vector3.one; // 아이템 크기 조절 필요시 사용

    private int lastSlotIndex = -1;
    private ItemData lastItemData = null;
    private GameObject currentModelObj;

    void Update()
    {
        if (InventoryManager.Instance == null) return;

        // 현재 선택된 슬롯 번호와 아이템 데이터 가져오기
        int currentIndex = InventoryManager.Instance.currentSlotIndex;
        ItemData currentItem = InventoryManager.Instance.GetSelectedBlock();

        // 슬롯이 바뀌었거나, 같은 슬롯인데 아이템이 바뀌었는지(소모 등) 체크
        if (currentIndex != lastSlotIndex || currentItem != lastItemData)
        {
            UpdateHandModel(currentItem);

            lastSlotIndex = currentIndex;
            lastItemData = currentItem;
        }
    }

    void UpdateHandModel(ItemData item)
    {
        // 1. 기존에 들고 있던 모델 삭제
        if (currentModelObj != null)
        {
            Destroy(currentModelObj);
            currentModelObj = null;
        }

        // 2. 새 아이템이 없으면(빈손) 리턴
        if (item == null || item.dropModel == null) return;

        // 3. 새 모델 생성
        currentModelObj = Instantiate(item.dropModel, handMount);

        // 4. 위치/회전 초기화
        currentModelObj.transform.localPosition = item.handPositionOffset;
        currentModelObj.transform.localRotation = Quaternion.Euler(item.handRotationOffset);
        currentModelObj.transform.localScale = Vector3.one * item.handScale;

        // ★★★ 5. 물리/충돌 제거 (가장 중요!) ★★★
        // 손에 들고 있는데 물리엔진이 적용되면 플레이어가 튕겨 나감

        // 리지드바디 제거
        Rigidbody rb = currentModelObj.GetComponent<Rigidbody>();
        if (rb != null) Destroy(rb);

        // 콜라이더 제거 (모든 자식 포함)
        foreach (Collider col in currentModelObj.GetComponentsInChildren<Collider>())
        {
            Destroy(col);
        }

        // 스크립트 제거 (BlockBehavior 등 상호작용 스크립트가 있다면 오작동 방지)
        foreach (BlockBehavior script in currentModelObj.GetComponentsInChildren<BlockBehavior>())
        {
            Destroy(script);
        }

        // (선택) 만약 DroppedItem 스크립트가 붙어있다면 그것도 제거
        DroppedItem droppedItem = currentModelObj.GetComponent<DroppedItem>();
        if (droppedItem != null) Destroy(droppedItem);
    }
}