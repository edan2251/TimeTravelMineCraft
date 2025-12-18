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
            if (map != null) map.RemoveBlockAt(transform.position);

            // ★ 수정: Destroy -> Despawn
            if (ObjectPoolManager.Instance != null)
                ObjectPoolManager.Instance.Despawn(gameObject);
            else
                Destroy(gameObject);
        }
    }
}