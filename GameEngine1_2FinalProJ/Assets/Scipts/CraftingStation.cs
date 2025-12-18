using UnityEngine;

// LineRenderer 등의 시각적 요소는 제거해도 됨 (Raycast로 바뀌었으므로)
public class CraftingStation : MonoBehaviour
{
    [Header("Station Settings")]
    public CraftingType stationType = CraftingType.Workbench; // 제작대인지 화로인지 설정
}