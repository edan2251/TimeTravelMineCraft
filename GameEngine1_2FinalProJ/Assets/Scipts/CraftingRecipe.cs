using System;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "New Recipe", menuName = "Chrono/Crafting Recipe")]
public class CraftingRecipe : ScriptableObject
{
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