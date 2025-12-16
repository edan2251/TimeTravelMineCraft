using System;
using System.Collections.Generic;
using UnityEngine;

// 제작소 타입 정의 (이 스크립트 안에 같이 두거나 별도 파일로 관리)
public enum CraftingType
{
    Player,     // 기본 제작 (플레이어 손)
    Workbench,  // 제작대
    Furnace     // 화로
}

[CreateAssetMenu(fileName = "New Recipe", menuName = "Chrono/Crafting Recipe")]
public class CraftingRecipe : ScriptableObject
{
    [Header("Crafting Setting")]
    public CraftingType requiredStation = CraftingType.Player; 

    [Header("Result")]
    public ItemData resultItem;
    public int resultCount = 1;

    [Header("Ingredients")]
    public List<Ingredient> ingredients = new List<Ingredient>();

    [Serializable]
    public struct Ingredient
    {
        public ItemData item;
        public int count;
    }
}