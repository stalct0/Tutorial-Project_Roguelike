using System;
using UnityEngine;

public class HackProjectile : MonoBehaviour
{
    [NonSerialized] public float speed = 10f;
    [NonSerialized] public int   damage = 0;
    [NonSerialized] public float stunDuration = 0.7f;
    public LayerMask enemyMask;

    private Vector2 dir = Vector2.right;

    public void Fire(Vector2 direction) => dir = direction.normalized;

    void Update()
    {
        transform.position += (Vector3)(dir * speed * Time.deltaTime);
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        // 적 찾기(IHittable)
        IHittable h = other.GetComponent<IHittable>();
        if (h == null) h = other.GetComponentInParent<IHittable>();
        if (h != null)
        {
            var ev = new DamageEvent
            {
                damage = damage,
                sourcePos = transform.position,
                knockbackForce = 0f,        // ★ 핵심: 넉백 없음
                stunSec = stunDuration,
                instigator = this.gameObject
            };
            h.TakeHit(ev);
            Destroy(gameObject);
        }
        else
        {
            // 지형/벽 등에 맞으면 소멸(필요 시 태그/레이어로 조건 추가)
            if (!other.isTrigger) Destroy(gameObject);
        }
    }
}