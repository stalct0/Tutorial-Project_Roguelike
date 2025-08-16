using UnityEngine;

public struct DamageEvent
{
    public int damage;
    public Vector2 sourcePos;     // 공격자(또는 충돌 유발자) 위치
    public float knockbackForce;  // 기본 넉백 힘
    public float stunSec;         // 기절 시간
    public GameObject instigator; // 피해를 유발한 주체(플레이어 or 날아가는 적)
}

public interface IHittable
{
    void TakeHit(DamageEvent e);
}