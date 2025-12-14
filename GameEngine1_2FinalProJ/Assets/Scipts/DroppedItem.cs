using UnityEngine;

public class DroppedItem : MonoBehaviour
{
    private float spawnTime;

    public ItemData itemData;
    public int count = 1;

    [Header("Settings")]
    public float rotateSpeed = 50f;

    // 아이템 생성 시 호출 (데이터 주입)
    public void Setup(ItemData data, int amount)
    {
        itemData = data;
        count = amount;

        spawnTime = Time.time;

        // 1. 시각적 모델 생성 (껍데기 안에 자식으로 생성)
        if (data.dropModel != null)
        {
            GameObject model = Instantiate(data.dropModel, transform);

            // 2. 위치 및 크기 조정 (0.3배)
            model.transform.localPosition = Vector3.zero;
            model.transform.localRotation = Quaternion.identity;
            model.transform.localScale = Vector3.one * 0.3f;

            // 3. ★ 중요: 자식 모델의 물리 기능 제거 ★
            // 블록 프리팹에는 BoxCollider가 붙어있으므로, 이걸 안 끄면 플레이어가 아이템을 밟고 올라섭니다.
            foreach (var col in model.GetComponentsInChildren<Collider>())
            {
                Destroy(col); // 혹은 col.enabled = false;
            }

            // 혹시 BlockBehavior 같은 스크립트가 붙어있다면 제거 (안전장치)
            foreach (var script in model.GetComponentsInChildren<BlockBehavior>())
            {
                Destroy(script);
            }

            // (선택) 도구 같은 경우 위치가 이상하면 여기서 오프셋 조정
            // model.transform.localPosition = new Vector3(0, 0.2f, 0); 
        }

        // 4. 퐁! 튀어오르는 연출
        Rigidbody rb = GetComponent<Rigidbody>();
        if (rb != null)
        {
            float randomX = Random.Range(-2f, 2f);
            float randomZ = Random.Range(-2f, 2f);
            rb.AddForce(new Vector3(randomX, 5f, randomZ), ForceMode.Impulse);
        }
    }

    void Update()
    {
        // 둥둥 떠다니며 회전
        transform.Rotate(Vector3.up * rotateSpeed * Time.deltaTime);
    }

    private void OnTriggerEnter(Collider other)
    {
        if (Time.time - spawnTime < 1.0f) return;

        if (other.CompareTag("Player"))
        {
            // 인벤토리 추가 시도
            bool added = InventoryManager.Instance.AddItem(itemData, count);

            if (added)
            {
                // 성공하면 삭제
                Destroy(gameObject);
            }
            // 실패하면(꽉 참) 그냥 바닥에 유지
        }
    }
}