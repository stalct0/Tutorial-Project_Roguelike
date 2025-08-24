using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class DragonLaser : MonoBehaviour
{
    [Header("Refs")] public RectTransform canvasRoot; // Overlay 캔버스 루트
    public Transform mouthTipWorld; // 용 입(월드)
    public RectTransform target; // 현재 선택 버튼 RectTransform

    [Header("Layers (RawImages)")] public RawImage coreBeam;
    public RawImage outerGlow;
    public RawImage noiseBeam;

    [Header("Impact")] public RawImage hitGlow;
    public RawImage hitSparkA;
    public RawImage hitSparkB;

    [Header("Look & Feel")] public float thicknessCore = 10f;
    public float thicknessGlow = 34f;
    public float thicknessNoise = 14f;

    public float endPadding = 12f;
    public float followLerp = 14f;

    public float scrollCore = 1.4f;
    public float scrollGlow = 0.6f;
    public float scrollNoise = 2.0f;

    public float pulseAmp = 0.18f; // 18%
    public float pulseSpeed = 5.5f;

    public float flickerAmp = 0.15f; // 알파 플리커
    public float wobbleAmp = 6f; // px
    public float wobbleFreq = 3f;

    Vector2 _endSmoothed;
    float _uCore, _uGlow, _uNoise;
    public UINavController navController; // 선택 이벤트 소스(있다면 연결)

    [Header("Sparks")] public UISparkEmitter sparkEmitter; // ★추가
    public float continuousSparksPerSec = 28f; // ★지속 방출
    public int burstOnChange = 16; // ★선택 변경 시 폭발 개수
    public Vector2 sparkSpeedRange = new Vector2(360f, 720f); // ★속도 범위(px/s)
    public float sparkSpreadDeg = 28f; // ★퍼짐 각도
    float _sparkAcc; // ★누적 타이머
    bool _pendingBurst; // ★다음 프레임에서 한 번만 터뜨리기

    void OnEnable()
    {
        if (navController)
        {
            navController.OnSelectionChanged += OnSelectionChanged;
            var rt = navController.GetCurrentRect();
            OnSelectionChanged(rt);
        }
    }

    void OnDisable()
    {
        if (navController)
            navController.OnSelectionChanged -= OnSelectionChanged;
    }

    void Reset()
    {
        var canvas = GetComponentInParent<Canvas>();
        if (canvas) canvasRoot = canvas.transform as RectTransform;
    }

    void Update()
    {
        if (!canvasRoot || !mouthTipWorld || (!coreBeam && !outerGlow && !noiseBeam)) return;

        // 1) 월드/스크린/로컬 변환
        Vector3 mouthScreen = Camera.main
            ? Camera.main.WorldToScreenPoint(mouthTipWorld.position)
            : new Vector3(Screen.width / 2f, Screen.height / 2f, 0f);
        Vector2 mouthLocal;
        RectTransformUtility.ScreenPointToLocalPointInRectangle(canvasRoot, (Vector2)mouthScreen, null, out mouthLocal);

        Vector2 targetLocal = mouthLocal;
        if (target)
        {
            Vector2 targetScreen = RectTransformUtility.WorldToScreenPoint(null, target.position);
            RectTransformUtility.ScreenPointToLocalPointInRectangle(canvasRoot, targetScreen, null, out targetLocal);
        }

        // 2) 부드러운 추적 + 약간의 요동(wobble)
        _endSmoothed = Vector2.Lerp(_endSmoothed, targetLocal, 1f - Mathf.Exp(-followLerp * Time.deltaTime));
        Vector2 dir = _endSmoothed - mouthLocal;
        float dist = dir.magnitude;
        Vector2 n = dist > 0.001f ? dir / dist : Vector2.right;

        float t = Time.time;
        Vector2 wobble = new Vector2(0, Mathf.Sin(t * wobbleFreq) * wobbleAmp); // 수직 미세 흔들림
        // 화면 축 기준으로 회전된 wobble을 레이저 수직방향으로 투영
        var perp = new Vector2(-n.y, n.x);
        Vector2 wobbleWorld = perp * wobble.y;

        Vector2 startP = mouthLocal + n * endPadding;
        Vector2 endP = mouthLocal + n * Mathf.Max(0, dist - endPadding);
        endP += wobbleWorld; // 끝점만 살짝 흔들림

        // 3) 공통 배치 함수
        void PlaceBeam(RawImage img, float thickness, float uSpeed, float pulseFactor)
        {
            if (!img) return;
            var rt = img.rectTransform;

            // 크기/회전
            float len = Vector2.Distance(startP, endP);
            Vector2 mid = (startP + endP) * 0.5f;
            float angle = Mathf.Atan2(n.y, n.x) * Mathf.Rad2Deg;

            float pulse = 1f + Mathf.Sin(t * pulseSpeed + pulseFactor) * pulseAmp;
            rt.anchoredPosition = mid;
            rt.localRotation = Quaternion.Euler(0, 0, angle);
            rt.sizeDelta = new Vector2(len, thickness * pulse);

            // 스크롤
            var uv = img.uvRect;
            uv.x += Time.deltaTime * uSpeed;
            img.uvRect = uv;

            // 알파 플리커(이미 가산합성이므로 subtle)
            var c = img.color;
            float flick = 1f + (Random.value * 2f - 1f) * flickerAmp;
            c.a = Mathf.Clamp01(flick); // 0~1
            img.color = c;

            img.enabled = len > 2f;
        }

        PlaceBeam(coreBeam, thicknessCore, scrollCore, 0f);
        PlaceBeam(outerGlow, thicknessGlow, scrollGlow, 0.7f);
        PlaceBeam(noiseBeam, thicknessNoise, scrollNoise, 1.4f);

        // 4) 히트 VFX 배치
        void PlaceImpact(RawImage img, float size, float spinSpeed)
        {
            if (!img) return;
            var rt = img.rectTransform;
            rt.anchoredPosition = endP;
            rt.sizeDelta = new Vector2(size, size);
            rt.localRotation = Quaternion.Euler(0, 0, spinSpeed != 0 ? t * spinSpeed : 0);
            img.enabled = dist > 6f;
        }

        PlaceImpact(hitGlow, Mathf.Lerp(36f, 52f, Mathf.Abs(Mathf.Sin(t * 5f))), 0);
        PlaceImpact(hitSparkA, 34f, 90f);
        PlaceImpact(hitSparkB, 34f, -90f);
        if (sparkEmitter && dist > 8f)
        {
            _sparkAcc += Time.unscaledDeltaTime * continuousSparksPerSec;
            while (_sparkAcc >= 1f)
            {
                sparkEmitter.EmitDirectionalBurst(endP, n, 1,
                    sparkSpeedRange.x, sparkSpeedRange.y, sparkSpreadDeg,
                    0.22f, 0.45f, -540f, 540f, 1f);
                _sparkAcc -= 1f;
            }

            // 선택 변경 직후 한 번 크게
            if (_pendingBurst)
            {
                sparkEmitter.EmitDirectionalBurst(endP, n, burstOnChange,
                    sparkSpeedRange.x, sparkSpeedRange.y * 1.15f, sparkSpreadDeg + 10f,
                    0.28f, 0.55f, -720f, 720f, 1.1f);
                _pendingBurst = false;
            }
        }
    }


    // 선택 변경시 외부에서 호출
    public void SetTarget(RectTransform newTarget) => target = newTarget;

    void OnSelectionChanged(RectTransform rt)
    {
    SetTarget(rt);
    _pendingBurst = true; // ★추가: 다음 프레임에서 버스트
    }

}