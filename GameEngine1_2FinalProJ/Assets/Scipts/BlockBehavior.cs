using UnityEngine;

public class BlockBehavior : MonoBehaviour
{
    [Header("Data Reference")]
    public ItemData myItemData;

    [Header("Status")]
    public int maxHP = 3;
    public int currentHP;

    public bool isUnbreakable = false;

    // ★ 수정: 렌더러가 하나가 아니라 여러 개일 수 있으니 배열로 변경
    private Renderer[] myRenderers;
    private Color[] originalColors; // 원래 색상들도 배열로 저장

    private void Start()
    {
        currentHP = maxHP;

        // ★ 수정: 자식 오브젝트(GFX)들에 있는 모든 렌더러를 다 찾아옴
        myRenderers = GetComponentsInChildren<Renderer>();

        // 찾은 렌더러 개수만큼 원래 색상 저장소 생성
        originalColors = new Color[myRenderers.Length];

        for (int i = 0; i < myRenderers.Length; i++)
        {
            // 각 파츠의 원래 색깔을 기억해둠
            originalColors[i] = myRenderers[i].material.color;
        }
    }

    public bool OnHit(int damage)
    {
        if (isUnbreakable) return false;

        if (myItemData != null && !myItemData.isDestructible)
        {
            return false;
        }

        currentHP -= damage;

        // 색깔 업데이트
        UpdateDamageColor();

        if (currentHP <= 0)
        {
            BreakAndDrop();
            return true;
        }
        return false;
    }

    void UpdateDamageColor()
    {
        // 렌더러가 없으면 패스
        if (myRenderers == null || myRenderers.Length == 0) return;

        // 체력 비율 계산
        float ratio = (float)currentHP / (float)maxHP;

        // ★ 수정: 모든 자식 렌더러들을 순회하며 색깔 변경
        for (int i = 0; i < myRenderers.Length; i++)
        {
            if (myRenderers[i] != null)
            {
                // 각각 원래 자기 색깔을 기준으로 검게 변함
                myRenderers[i].material.color = Color.Lerp(Color.black, originalColors[i], ratio);
            }
        }
    }

    void BreakAndDrop()
    {
        if (myItemData != null)
        {
            if (!myItemData.dropsOnBreak)
            {
                Destroy(gameObject);
                return;
            }

            if (Random.value > myItemData.dropChance)
            {
                Destroy(gameObject);
                return;
            }

            ItemData dropTarget = (myItemData.dropItem != null) ? myItemData.dropItem : myItemData;

            if (InventoryManager.Instance != null)
            {
                bool added = InventoryManager.Instance.AddItem(dropTarget, 1);

                if (!added)
                {
                    InventoryManager.Instance.SpawnDroppedItem(dropTarget, 1, transform.position);
                }
            }
        }

        Destroy(gameObject);
    }
}