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

    // ★ 캐싱해두기 (매번 FindObject하면 느림)
    private CraftingSystem craftingSystem;
    private StorageSystem storageSystem;

    void Start()
    {
        cam = GetComponentInChildren<Camera>();

        var mapGen = FindObjectOfType<MapGenerator>();
        miner.Init(mapGen);
        builder.Init(mapGen);

        craftingSystem = FindObjectOfType<CraftingSystem>();
        storageSystem = FindObjectOfType<StorageSystem>();
    }

    void Update()
    {
        InventoryManager.Instance.HandleHotbarInput();

        // ★★★ 1. Tab 키 로직을 최우선으로 처리 (UI 상태 무관) ★★★
        if (Input.GetKeyDown(KeyCode.Tab))
        {
            // (1) UI가 이미 열려있다면? -> 닫는다.
            if (UIManager.Instance != null && UIManager.Instance.IsUIOpen)
            {
                // ★ 둘 다 닫기 시도
                if (craftingSystem != null) craftingSystem.ClosePanel();
                if (storageSystem != null) storageSystem.CloseStorage();
                return;
            }

            // (2) UI가 닫혀있다면? -> 상호작용해서 연다.
            else
            {
                OpenCraftingMenuInteraction();
                return;
            }
        }

        // ★★★ 2. UI가 열려있으면 채광/건축 금지 ★★★
        if (UIManager.Instance != null && UIManager.Instance.IsUIOpen) return;


        // --- 아래는 기존 채광/건축 로직 (변동 없음) ---

        Ray ray = cam.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0));
        RaycastHit hit;
        bool hasHit = Physics.Raycast(ray, out hit, range, blockLayer);
        ItemData currentItem = InventoryManager.Instance.GetSelectedBlock();

        // 좌클릭: 채광/공격
        if (Input.GetMouseButton(0) && hasHit)
        {
            int dmg = (currentItem != null && currentItem.isTool) ? currentItem.toolDamage : 1;
            miner.TryMine(hit, dmg);
        }

        // 우클릭: 설치
        if (Input.GetMouseButtonDown(1) && hasHit)
        {
            if (currentItem != null && currentItem.isPlaceable)
            {
                builder.TryBuild(hit, currentItem.blockID);
            }
        }

        // Q키: 버리기
        if (Input.GetKey(KeyCode.Q) && Time.time >= nextDropTime)
        {
            nextDropTime = Time.time + dropRate;
            Vector3 spawnPos = cam.transform.position + (cam.transform.forward * 1f);
            InventoryManager.Instance.DropOneFromSelected(spawnPos, cam.transform.forward);
        }
    }

    // ★ 로직 분리: 제작 메뉴 열기 판별 함수
    void OpenCraftingMenuInteraction() // 함수 내용 수정
    {
        Ray ray = cam.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0));
        RaycastHit hit;
        bool hasHit = Physics.Raycast(ray, out hit, range, blockLayer);

        // 아무것도 안 보고 있으면 리턴 (혹은 기본 제작창 열기? - 기존 로직 유지)
        if (!hasHit)
        {
            if (craftingSystem != null) craftingSystem.OpenCraftingMenu(CraftingType.Player);
            return;
        }

        // 1. 보관함인지 체크
        StorageBlock storage = hit.collider.GetComponent<StorageBlock>();
        if (storage == null) storage = hit.collider.GetComponentInParent<StorageBlock>();

        if (storage != null)
        {
            // 보관함 열기
            if (storageSystem != null) storageSystem.OpenStorage(storage);
            return;
        }

        // 2. 제작대/화로인지 체크 (기존 로직)
        CraftingType targetStation = CraftingType.Player;
        CraftingStation station = hit.collider.GetComponent<CraftingStation>();
        if (station == null) station = hit.collider.GetComponentInParent<CraftingStation>();

        if (station != null) targetStation = station.stationType;

        if (craftingSystem != null) craftingSystem.OpenCraftingMenu(targetStation);
    }
}