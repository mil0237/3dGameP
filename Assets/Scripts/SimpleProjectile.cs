using UnityEngine;

public class SimpleProjectile : MonoBehaviour
{
    private Vector3 _direction;
    private float _speed;
    private float _lifeTime;
    private float _elapsed;

    public void Init(Vector3 direction, float speed, float lifeTime)
    {
        _direction = direction.normalized;
        _speed = speed;
        _lifeTime = lifeTime;
        _elapsed = 0f;
    }

    void Update()
    {
        transform.position += _direction * _speed * Time.deltaTime;
        _elapsed += Time.deltaTime;

        if (_elapsed >= _lifeTime)
        {
            Destroy(gameObject);
        }
    }

    void OnTriggerEnter(Collider other)
    {
        // 충돌 처리 추가 (예: 플레이어, 지형 등)
        // 예시:
        // if (other.CompareTag("Player"))
        // {
        //     other.GetComponent<PlayerHealth>()?.TakeDamage(1);
        //     Destroy(gameObject);
        // }
    }
}
