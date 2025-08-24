using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class UISparkEmitter : MonoBehaviour
{
    [Header("Canvas / Prefab")]
    public RectTransform canvasRoot;   // Overlay 캔버스 루트
    public RawImage sparkPrefab;       // 비활성화된 RawImage 프리팹
    public int poolSize = 64;

    [Header("Physics (UI px/sec)")]
    public Vector2 gravity = new Vector2(0f, -1200f);
    public float drag = 2.5f;          // 속도 감쇠(선형)

    [Header("Rendering")]
    public Gradient colorOverLife;     // 알파/색 변화
    public Vector2 sizeOverLife = new Vector2(1f, 0.4f); // 시작/끝 크기 배율

    class P
    {
        public bool active;
        public RectTransform rt;
        public RawImage img;
        public Vector2 pos, vel;
        public float life, lifeMax;
        public float ang, angVel;
        public float sizeBase;
    }

    readonly List<P> pool = new List<P>();

    void Awake()
    {
        if (!canvasRoot)
        {
            var c = GetComponentInParent<Canvas>();
            if (c) canvasRoot = c.transform as RectTransform;
        }
        // 풀 생성
        for (int i = 0; i < poolSize; i++)
        {
            var inst = Instantiate(sparkPrefab, canvasRoot);
            inst.gameObject.SetActive(false);
            inst.transform.SetAsLastSibling(); // UI 최상단
            pool.Add(new P {
                active = false,
                rt = inst.rectTransform,
                img = inst
            });
        }
        if (colorOverLife == null)
        {
            colorOverLife = new Gradient
            {
                colorKeys = new[] {
                    new GradientColorKey(Color.white, 0f),
                    new GradientColorKey(Color.cyan, 0.6f),
                    new GradientColorKey(Color.white, 1f),
                },
                alphaKeys = new[] {
                    new GradientAlphaKey(1f, 0f),
                    new GradientAlphaKey(0.8f, 0.2f),
                    new GradientAlphaKey(0f, 1f)
                }
            };
        }
    }

    void Update()
    {
        float dt = Time.unscaledDeltaTime; // 메뉴 UI라면 unscaled 추천
        for (int i = 0; i < pool.Count; i++)
        {
            var p = pool[i];
            if (!p.active) continue;

            p.life -= dt;
            if (p.life <= 0f)
            {
                p.active = false;
                p.img.gameObject.SetActive(false);
                continue;
            }

            // 물리
            p.vel += gravity * dt;
            p.vel *= (1f - Mathf.Clamp01(drag * dt));
            p.pos += p.vel * dt;
            p.ang += p.angVel * dt;

            // 렌더
            float t = 1f - (p.life / p.lifeMax);
            var col = colorOverLife.Evaluate(t);
            p.img.color = col;

            float sz = Mathf.Lerp(sizeOverLife.x, sizeOverLife.y, t) * p.sizeBase;
            p.rt.anchoredPosition = p.pos;
            p.rt.localRotation = Quaternion.Euler(0, 0, p.ang);
            p.rt.sizeDelta = new Vector2(sz * 18f, sz * 6f); // 기본 종횡비 유지
        }
    }

    P Rent()
    {
        for (int i = 0; i < pool.Count; i++)
        {
            if (!pool[i].active) return pool[i];
        }
        return null; // 풀 부족하면 그냥 스킵
    }

    // dir 기준으로 퍼지는 방향성 방출
    public void EmitDirectionalBurst(Vector2 originCanvasLocal, Vector2 dirNorm,
                                     int count, float speedMin, float speedMax,
                                     float spreadDeg = 30f,
                                     float lifeMin = 0.25f, float lifeMax = 0.5f,
                                     float angVelMin = -360f, float angVelMax = 360f,
                                     float sizeBase = 1f)
    {
        if (dirNorm.sqrMagnitude < 0.0001f) dirNorm = Vector2.right;
        float half = spreadDeg * 0.5f;
        for (int i = 0; i < count; i++)
        {
            var p = Rent();
            if (p == null) return;

            float a = Random.Range(-half, half) * Mathf.Deg2Rad;
            Vector2 d = new Vector2(
                dirNorm.x * Mathf.Cos(a) - dirNorm.y * Mathf.Sin(a),
                dirNorm.x * Mathf.Sin(a) + dirNorm.y * Mathf.Cos(a)
            );
            float spd = Random.Range(speedMin, speedMax);

            p.active = true;
            p.pos = originCanvasLocal + d * Random.Range(0f, 4f); // 살짝 랜덤 오프셋
            p.vel = d * spd + new Vector2( Random.Range(-60f,60f), Random.Range(-40f,40f) );
            p.lifeMax = p.life = Random.Range(lifeMin, lifeMax);
            p.ang = Random.Range(0f, 360f);
            p.angVel = Random.Range(angVelMin, angVelMax);
            p.sizeBase = sizeBase;

            p.img.gameObject.SetActive(true);
            p.img.transform.SetAsLastSibling();
        }
    }
}