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
    public GameObject dropModel;

    [Header("Action Settings")]
    public bool isPlaceable;
    public int blockID;
    public bool isDestructible = true;

    [Header("Drop Settings")]
    public bool dropsOnBreak = true;
    [Range(0f, 1f)]
    public float dropChance = 1.0f;

    [Header("Tool Settings")]
    public bool isTool;
    public int toolDamage = 1;

    public ItemData dropItem;
}