using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Events;

/// 씬 내에서 “선택 가능한 UI 항목”에 붙이는 컴포넌트.
/// - highlightGO: 선택 중일 때 켜질 하이라이트(테두리 등)
/// - onSubmit: Enter를 눌렀을 때 실행(없으면 Button.onClick 시도)
[DisallowMultipleComponent]
public class UINavItem : MonoBehaviour
{
    [Header("Main Highlight (기존)")]
    [Tooltip("선택 시 켜질 하이라이트(글로우, 배경 등). 비워도 됨")]
    public GameObject highlightGO;

    [Header("Border Frame (추가)")]
    [Tooltip("선택 시 켜질 테두리 프레임 오브젝트(예: Image). 비워도 됨")]
    public GameObject borderGO;                 // 테두리 자체를 GameObject로 토글
    [Tooltip("테두리 색 제어를 원하면 넣기(없으면 단순 on/off만)")]
    public Image borderImage;                   // 선택: 색/알파 제어용
    public Color borderOn  = Color.white;       // 선택 상태의 테두리 색
    public Color borderOff = new Color(1,1,1,0.2f); // 비선택 상태의 테두리 색

    [Header("Submit (Enter)")]
    public UnityEvent onSubmit;                 // 없으면 Button.onClick 호출

    [HideInInspector] public bool IsSelected { get; private set; }

    Button cachedButton;

    void Awake()
    {
        cachedButton = GetComponent<Button>();
        // 에디터에서 연결 안했으면 이름으로 자동 탐색(선택 사항)
        if (!highlightGO) highlightGO = transform.Find("Highlight")?.gameObject;
        if (!borderGO)    borderGO    = transform.Find("Border")?.gameObject;
        if (!borderImage && borderGO) borderImage = borderGO.GetComponent<Image>();

        SetSelected(false);
    }

#if UNITY_EDITOR
    void OnValidate()
    {
        // 에디터에서 값 바뀔 때 테두리 색 미리 반영
        if (borderImage)
            borderImage.color = IsSelected ? borderOn : borderOff;
        if (highlightGO) highlightGO.SetActive(IsSelected);
        if (borderGO)    borderGO.SetActive(IsSelected);
    }
#endif

    public void SetSelected(bool sel)
    {
        IsSelected = sel;

        // 1) 기존 하이라이트 토글
        if (highlightGO) highlightGO.SetActive(sel);

        // 2) 테두리 프레임 토글 + 색상
        if (borderGO) borderGO.SetActive(sel);
        if (borderImage) borderImage.color = sel ? borderOn : borderOff;
    }

    public void Submit()
    {
        if (onSubmit != null && onSubmit.GetPersistentEventCount() > 0)
        {
            onSubmit.Invoke();
            return;
        }
        if (cachedButton) cachedButton.onClick?.Invoke();
    }
}