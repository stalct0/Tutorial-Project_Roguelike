using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI; 
using TMPro;          


[RequireComponent(typeof(PlayerInventory))]
public class PlayerItemInteractor : MonoBehaviour
{
    [Header("입력")]
    public KeyCode pickupKey = KeyCode.I;

    [Header("OverlapBox 설정")]
    private Vector2 boxSize = new Vector2(0.65f, 0.65f);   // 플레이어 주위 범위
    private Vector2 boxOffset = new Vector2(0f, 0f);     // 플레이어 기준 오프셋
    public LayerMask itemLayer;                         // "Item" 레이어만 포함
    public LayerMask exitLayer;
    
    [Header("선택")]
    private float maxPickupDistance = 1.0f; // 너무 먼 건 제외
    private float maxExitDistance = 1.0f;

    [Header("프롬프트 UI(고정 위치)")]
    public InteractUI promptUI; 
    private PlayerInventory inv;
    
    [Header("프롬프트 UI(화면 공간)")]
    public RectTransform screenPrompt;         // Canvas(Overlay) 아래의 프롬프트 오브젝트
    public Vector3 worldOffset = new Vector3(0f, 0.8f, 0f); // 대상 위로 띄우기

    void Awake()
    {
        inv = GetComponent<PlayerInventory>();
    }

    void Start()
    {
    }
    
    void Update()
    {
        // 1) 주변 대상 탐지
        ItemPickup item = GetClosestItem();
        Transform exitTf = GetClosestExit();

        // 2) 표시 대상 결정 (아이템 우선 → 없으면 출구)
        Transform targetTf = null;
        string promptText = string.Empty;

        if (item != null)
        {
            targetTf = item.transform;
            if (promptUI) promptUI.gameObject.SetActive(true);
        }
        else if (exitTf != null)
        {
            targetTf = exitTf;
            if (promptUI) promptUI.gameObject.SetActive(true);
        }

        // 3) 프롬프트 토글 + 위치 갱신
        if (screenPrompt != null)
        {
            bool visible = (targetTf != null);
            if (screenPrompt.gameObject.activeSelf != visible)
                screenPrompt.gameObject.SetActive(visible);

            if (visible)
            {
                Camera cam = Camera.main;
                if (cam != null)
                {
                    Vector3 worldPos = targetTf.position + worldOffset;
                    Vector3 screenPos = cam.WorldToScreenPoint(worldPos);
                    screenPrompt.position = screenPos;
                }
            }
        }

        // 4) 입력 처리
        if (item != null && Input.GetKeyDown(pickupKey))
        {
            bool ok = item.TryPickup(inv);
        }
    }
    
    ItemPickup GetClosestItem()
    {
        Vector2 center = (Vector2)transform.position + boxOffset;
        Collider2D[] hits = Physics2D.OverlapBoxAll(center, boxSize, 0f, itemLayer);

        ItemPickup best = null;
        float bestSqr = float.MaxValue;
        Vector2 me = transform.position;
        float maxSqr = maxPickupDistance * maxPickupDistance;

        for (int i = 0; i < hits.Length; i++)
        {
            if (hits[i] == null) continue;
            var p = hits[i].GetComponent<ItemPickup>();
            if (p == null) continue;

            float d2 = ((Vector2)p.transform.position - me).sqrMagnitude;
            if (d2 > maxSqr) continue;
            if (d2 < bestSqr) { bestSqr = d2; best = p; }
        }
        return best;
    }

    Transform GetClosestExit()
    {
        Vector2 center = (Vector2)transform.position + boxOffset;
        Collider2D[] hits = Physics2D.OverlapBoxAll(center, boxSize, 0f, exitLayer);

        if (hits == null || hits.Length == 0) return null;

        Transform best = null;
        float bestSqr = float.MaxValue;
        Vector2 me = transform.position;
        float maxSqr = maxExitDistance * maxExitDistance;

        for (int i = 0; i < hits.Length; i++)
        {
            if (hits[i] == null) continue;
            float d2 = ((Vector2)hits[i].transform.position - me).sqrMagnitude;
            if (d2 > maxSqr) continue;
            if (d2 < bestSqr) { bestSqr = d2; best = hits[i].transform; }
        }
        return best;
    }

    // 에디터에서 범위 확인용
    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Vector2 center = (Vector2)transform.position + boxOffset;
        Gizmos.DrawWireCube(center, boxSize);
    }
}