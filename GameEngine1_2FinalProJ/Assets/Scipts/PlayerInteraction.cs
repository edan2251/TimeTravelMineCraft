using UnityEngine;

public class PlayerInteraction : MonoBehaviour
{
    [Header("Modules")]
    public MiningModule miner;
    public BuildingModule builder;

    [Header("Settings")]
    public float range = 5f;
    public LayerMask blockLayer;

    [Header("Drop Settings")]
    public float dropRate = 0.1f; 
    private float nextDropTime;

    private Camera cam;

    void Start()
    {
        cam = GetComponentInChildren<Camera>();

        var mapGen = FindObjectOfType<MapGenerator>();
        miner.Init(mapGen);
        builder.Init(mapGen);
    }

    void Update()
    {
        if (UIManager.Instance != null && UIManager.Instance.IsUIOpen) return;

        InventoryManager.Instance.HandleHotbarInput();

        ItemData currentItem = InventoryManager.Instance.GetSelectedBlock();

        Ray ray = cam.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0));
        RaycastHit hit;
        bool hasHit = Physics.Raycast(ray, out hit, range, blockLayer);

        if (Input.GetMouseButton(0) && hasHit)
        {
            int dmg = (currentItem != null && currentItem.isTool) ? currentItem.toolDamage : 1;
            miner.TryMine(hit, dmg);
        }

        if (Input.GetMouseButtonDown(1) && hasHit)
        {
            if (currentItem != null && currentItem.isPlaceable)
            {
                builder.TryBuild(hit, currentItem.blockID);
            }
        }

        if (Input.GetKey(KeyCode.Q) && Time.time >= nextDropTime)
        {
            nextDropTime = Time.time + dropRate;

            // ★ 수정 전: 0.5f -> 너무 가까움 (몸 속에서 생성됨)
            // Vector3 spawnPos = cam.transform.position + (cam.transform.forward * 0.5f);

            // ★ 수정 후: 1.5f -> 팔을 뻗은 정도 거리에서 생성
            Vector3 spawnPos = cam.transform.position + (cam.transform.forward * 1f);

            Vector3 throwDir = cam.transform.forward;

            InventoryManager.Instance.DropOneFromSelected(spawnPos, throwDir);
        }
    }
}