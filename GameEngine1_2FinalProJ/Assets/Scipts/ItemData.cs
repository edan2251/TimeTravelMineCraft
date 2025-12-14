using UnityEngine;

// 아이템의 종류 (블록, 도구, 기타)
public enum ItemType { Block, Tool, Resource }

[CreateAssetMenu(fileName = "New Item", menuName = "Chrono/Item Data")]
public class ItemData : ScriptableObject
{
    [Header("Basic Info")]
    public string itemName;
    public Sprite icon;
    public ItemType itemType;
    public int maxStack = 64;

    [Header("Visuals")]
    // ★ 여기가 핵심: 드롭되거나 손에 들었을 때 보일 프리팹
    // 블록이면 BlockPrefab을, 도구면 ToolPrefab을 여기에 넣으세요.
    public GameObject dropModel;

    [Header("Action Settings")]
    public bool isPlaceable;
    public int blockID; // MapGenerator용 ID

    public bool isTool;
    public int toolDamage = 1;

    public ItemData dropItem;
}