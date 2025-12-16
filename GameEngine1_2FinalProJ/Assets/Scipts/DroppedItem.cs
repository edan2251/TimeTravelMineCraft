using UnityEngine;

public class DroppedItem : MonoBehaviour
{
    private float spawnTime;

    public ItemData itemData;
    public int count = 1;

    [Header("Settings")]
    public float rotateSpeed = 50f;

    public void Setup(ItemData data, int amount)
    {
        itemData = data;
        count = amount;

        spawnTime = Time.time;

        if (data.dropModel != null)
        {
            GameObject model = Instantiate(data.dropModel, transform);

            model.transform.localPosition = Vector3.zero;
            model.transform.localRotation = Quaternion.identity;
            model.transform.localScale = Vector3.one * 0.3f;

            foreach (var col in model.GetComponentsInChildren<Collider>())
            {
                Destroy(col); 
            }

            foreach (var script in model.GetComponentsInChildren<BlockBehavior>())
            {
                Destroy(script);
            }
        }

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
        transform.Rotate(Vector3.up * rotateSpeed * Time.deltaTime);
    }

    private void OnTriggerEnter(Collider other)
    {
        if (Time.time - spawnTime < 1.0f) return;

        if (other.CompareTag("Player"))
        {
            bool added = InventoryManager.Instance.AddItem(itemData, count);

            if (added)
            {
                Destroy(gameObject);
            }
        }
    }
}