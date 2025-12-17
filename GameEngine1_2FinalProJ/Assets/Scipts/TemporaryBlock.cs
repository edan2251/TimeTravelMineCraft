using UnityEngine;

public class TemporaryBlock : MonoBehaviour
{
    private MapGenerator map;
    private float lifeTime = 1.0f; // 1초 뒤 파괴
    private float timer = 0f;
    private Renderer[] renderers;

    public void Setup(MapGenerator generator)
    {
        this.map = generator;
        renderers = GetComponentsInChildren<Renderer>();
    }

    void Update()
    {
        timer += Time.deltaTime;
        float progress = timer / lifeTime;

        // 1. 점점 검은색으로 변하는 연출
        if (renderers != null)
        {
            foreach (var r in renderers)
            {
                foreach (var mat in r.materials)
                {
                    mat.color = Color.Lerp(Color.white, Color.black, progress);
                }
            }
        }

        // 2. 시간이 다 되면 파괴
        if (timer >= lifeTime)
        {
            if (map != null)
            {
                // (1) 맵 데이터에서 먼저 지우고 (그래야 그 자리에 또 설치 가능)
                map.RemoveBlockAt(transform.position);
            }

            // (2) ★ 핵심: 눈에 보이는 오브젝트도 확실하게 파괴!
            Destroy(gameObject);
        }
    }
}