using System.Collections.Generic;
using UnityEngine;

public class ObjectPoolManager : MonoBehaviour
{
    public static ObjectPoolManager Instance;

    // 프리팹 이름(Key)으로 대기열(Queue)을 관리
    private Dictionary<string, Queue<GameObject>> poolDictionary = new Dictionary<string, Queue<GameObject>>();

    // 오브젝트가 어느 프리팹 출신인지 기억하기 위한 딕셔너리 (반납할 때 필요)
    private Dictionary<GameObject, string> activeObjectsMap = new Dictionary<GameObject, string>();

    private void Awake()
    {
        Instance = this;
    }

    // ★ 꺼내기 (Instantiate 대체)
    public GameObject Spawn(GameObject prefab, Vector3 position, Quaternion rotation, Transform parent = null)
    {
        string key = prefab.name;

        // 1. 창고가 없으면 새로 만듦
        if (!poolDictionary.ContainsKey(key))
        {
            poolDictionary[key] = new Queue<GameObject>();
        }

        GameObject obj = null;

        // 2. 창고에 재고가 있으면 꺼내 씀 (★ 수정된 부분)
        // 큐가 비어있지 않은 동안 계속 반복
        while (poolDictionary[key].Count > 0)
        {
            // 하나 꺼냄
            obj = poolDictionary[key].Dequeue();

            // ★ 중요: 꺼낸 오브젝트가 실제로 존재하는지(파괴되지 않았는지) 확인
            if (obj != null)
            {
                // 살아있다면 이 오브젝트를 사용하기로 하고 루프 종료
                break;
            }
            // 만약 obj == null 이라면(이미 파괴된 유령이라면), 
            // 그냥 버리고(Dequeue했으니 사라짐) 다음 루프를 돌며 다른 걸 찾음
        }

        // 3. 재고가 없거나(큐가 비었거나), 꺼낸 게 다 죽어있으면 새로 만듦
        if (obj == null)
        {
            obj = Instantiate(prefab);
            obj.name = key;
        }

        // 4. 상태 설정 및 활성화
        obj.transform.position = position; // 여기서 에러가 났던 것임 (obj가 null이라서)
        obj.transform.rotation = rotation;
        obj.transform.SetParent(parent);
        obj.SetActive(true);

        // 출신 성분 기록
        if (!activeObjectsMap.ContainsKey(obj))
        {
            activeObjectsMap.Add(obj, key);
        }

        return obj;
    }

    // ★ 반납하기 (Destroy 대체)
    public void Despawn(GameObject obj)
    {
        if (activeObjectsMap.TryGetValue(obj, out string key))
        {
            obj.SetActive(false); // 끄기만 함
            poolDictionary[key].Enqueue(obj); // 장부상으로만 반납

            // ★ 삭제: 부모 옮기는 코드를 지워! 이게 렉의 주범임.
            // obj.transform.SetParent(transform); 
        }
        else
        {
            Destroy(obj);
        }
    }
}