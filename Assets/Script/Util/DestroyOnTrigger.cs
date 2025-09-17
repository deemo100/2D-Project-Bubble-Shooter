
using UnityEngine;

// 이 스크립트가 부착된 오브젝트의 트리거에 다른 Collider2D가 들어오면 그 오브젝트를 파괴합니다.
[RequireComponent(typeof(Collider2D))]
public class DestroyOnTrigger : MonoBehaviour
{
    private void OnTriggerEnter2D(Collider2D other)
    {
        // 들어온 오브젝트를 파괴합니다.
        Destroy(other.gameObject);
    }
}
