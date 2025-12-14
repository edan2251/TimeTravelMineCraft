using System;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "New Recipe", menuName = "Chrono/Crafting Recipe")]
public class CraftingRecipe : ScriptableObject
{
    [Header("Result")]
    public ItemData resultItem; // 결과물 아이템
    public int resultCount = 1; // 결과물 개수

    [Header("Ingredients")]
    public List<Ingredient> ingredients = new List<Ingredient>();

    [Serializable]
    public struct Ingredient
    {
        public ItemData item; // 필요 재료
        public int count;     // 필요 개수
    }
}