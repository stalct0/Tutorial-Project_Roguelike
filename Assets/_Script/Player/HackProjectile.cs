using System;
using UnityEngine;

public class HackProjectile : MonoBehaviour
{
    [NonSerialized] public float speed = 10f;
    [NonSerialized] public int   damage = 0;
    [NonSerialized] public float stunDuration = 0.7f;
    public LayerMask enemyMask;
    
    [Header("Stun Indicator")]
    [SerializeField] private GameObject stunMarkerPrefab;     // 스턴 아이콘 프리팹
    private float   markerAboveOffsetY = -0.3f; // 콜라이더 위로 띄울 높이
    private Vector2 fallbackLocalOffset = new Vector2(0f, 0.0f);
    
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
            
            AttachOrRefreshStunMarker(other, h, stunDuration);
            
            Destroy(gameObject);
        }
        else
        {
            // 지형/벽 등에 맞으면 소멸(필요 시 태그/레이어로 조건 추가)
            if (!other.isTrigger) Destroy(gameObject);
        }
    }
    
    private void AttachOrRefreshStunMarker(Collider2D hitCol, IHittable h, float duration)
    {
        if (!stunMarkerPrefab) return;

        // IHittable이 붙어있는 실제 Transform을 우선 사용
        var enemyComp = h as Component;
        Transform enemyT = enemyComp ? enemyComp.transform : hitCol.transform;

        // 이미 달려 있는 스턴 마커가 있으면 재사용(시간 갱신)
        var existing = enemyT.GetComponentInChildren<StunMarker>(true);
        if (existing == null)
        {
            // 새로 생성해서 자식으로 부착
            var go = Instantiate(stunMarkerPrefab, enemyT);
            go.name = "StunMarker";
            existing = go.GetComponent<StunMarker>();
        }

        // 위치 잡기: 가능하면 콜라이더 상단 바로 위, 없으면 예비 오프셋
        Vector3 targetWorldPos;
        if (hitCol != null)
            targetWorldPos = new Vector3(hitCol.bounds.center.x,
                hitCol.bounds.max.y + markerAboveOffsetY,
                enemyT.position.z);
        else
            targetWorldPos = enemyT.TransformPoint((Vector3)fallbackLocalOffset);

        existing.transform.position = targetWorldPos;

        // 스턴 시간만큼 보이게
        existing.Show(duration);
    }
    
    
    
}