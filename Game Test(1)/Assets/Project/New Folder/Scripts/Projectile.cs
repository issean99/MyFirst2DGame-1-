using UnityEngine;

public class Projectile : MonoBehaviour
{
    [Header("Projectile Settings")]
    public float lifeTime = 5f;           // 투사체 생존 시간
    public int damage = 10;               // 데미지
    public bool destroyOnHit = true;      // 적에게 맞으면 파괴
    public GameObject hitEffect;          // 충돌 이펙트 (선택)

    void Start()
    {
        // 일정 시간 후 자동 파괴
        Destroy(gameObject, lifeTime);
    }

    void OnTriggerEnter2D(Collider2D collision)
    {
        // 플레이어는 무시
        if (collision.CompareTag("Player"))
        {
            return;
        }

        // 적에게 충돌
        if (collision.CompareTag("Enemy"))
        {
            // 충돌 이펙트 생성 (있다면)
            if (hitEffect != null)
            {
                Instantiate(hitEffect, transform.position, Quaternion.identity);
            }

            // 나중에 적 Health 시스템 추가 시 사용
            // collision.GetComponent<EnemyHealth>()?.TakeDamage(damage);
            Debug.Log("적에게 " + damage + " 데미지!");

            // 투사체 파괴
            if (destroyOnHit)
            {
                Destroy(gameObject);
            }
        }
        // 벽이나 장애물에 충돌 (Tag 체크 없이)
        else if (collision.gameObject.layer != LayerMask.NameToLayer("Default"))
        {
            // 충돌 이펙트 생성 (있다면)
            if (hitEffect != null)
            {
                Instantiate(hitEffect, transform.position, Quaternion.identity);
            }

            // 투사체 파괴
            if (destroyOnHit)
            {
                Destroy(gameObject);
            }
        }
    }

    void OnCollisionEnter2D(Collision2D collision)
    {
        // Collider가 아닌 Collision으로도 충돌 감지
        // 플레이어는 무시
        if (collision.gameObject.CompareTag("Player"))
        {
            return;
        }

        // 충돌 이펙트 생성
        if (hitEffect != null)
        {
            Instantiate(hitEffect, transform.position, Quaternion.identity);
        }

        // 투사체 파괴
        if (destroyOnHit)
        {
            Destroy(gameObject);
        }
    }
}
