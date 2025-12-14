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
    public float dropRate = 0.1f; // 0.15초마다 1개씩 버림 (숫자가 작을수록 빠름)
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
        // UI 열려있으면 중지
        if (UIManager.Instance != null && UIManager.Instance.IsUIOpen) return;

        InventoryManager.Instance.HandleHotbarInput();

        // 2. 현재 들고 있는 아이템 정보 가져오기
        ItemData currentItem = InventoryManager.Instance.GetSelectedBlock();

        // 3. 레이캐스트
        Ray ray = cam.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0));
        RaycastHit hit;
        bool hasHit = Physics.Raycast(ray, out hit, range, blockLayer);

        // --- 좌클릭 (채광/공격) ---
        if (Input.GetMouseButton(0) && hasHit)
        {
            int dmg = (currentItem != null && currentItem.isTool) ? currentItem.toolDamage : 1;
            miner.TryMine(hit, dmg);
        }

        // --- 우클릭 (설치) ---
        if (Input.GetMouseButtonDown(1) && hasHit)
        {
            if (currentItem != null && currentItem.isPlaceable)
            {
                builder.TryBuild(hit, currentItem.blockID);
            }
        }

        // --- ★ 수정된 부분: 'O' 키 꾹 누르면 연속 버리기 ---
        // GetKeyDown -> GetKey로 변경
        // Time.time 체크를 통해 광속으로 사라지는 것 방지
        if (Input.GetKey(KeyCode.Q) && Time.time >= nextDropTime)
        {
            nextDropTime = Time.time + dropRate; // 다음 버리기 시간 설정

            Vector3 spawnPos = cam.transform.position + (cam.transform.forward * 0.5f);
            Vector3 throwDir = cam.transform.forward;

            InventoryManager.Instance.DropOneFromSelected(spawnPos, throwDir);
        }
    }
}