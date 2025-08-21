using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

/// 화살표 키로 UINavItem을 이동 선택하고, Enter로 Submit.
/// - children을 자동 스캔하여 항목 목록 구성(계층 순서)
/// - 1D/그리드 지원: columns>1이면 그리드 이동, 아니면 좌우(또는 상하) 선형 이동
/// - wrap 이동 옵션, 마우스 호버로 선택 동기화(선택)
[DisallowMultipleComponent]
public class UINavController : MonoBehaviour
{
    public enum AxisMode { Horizontal, Vertical }
    [Header("Layout")]
    [Tooltip("1이면 선형(행/열 한 줄). 그 이상이면 그리드로 해석")]
    public int columns = 1;
    [Tooltip("선형 모드일 때 이동 축")]
    public AxisMode linearAxis = AxisMode.Horizontal;
    [Tooltip("끝에서 반대쪽으로 이어질지")]
    public bool wrap = true;

    [Header("Build")]
    [Tooltip("Start에서 자식에서 자동으로 항목을 모읍니다.")]
    public bool autoCollectChildren = true;
    [Tooltip("수동 목록(자동 수집 끄는 경우 사용)")]
    public List<UINavItem> items = new List<UINavItem>();

    [Header("Input")]
    public KeyCode leftKey  = KeyCode.LeftArrow;
    public KeyCode rightKey = KeyCode.RightArrow;
    public KeyCode upKey    = KeyCode.UpArrow;
    public KeyCode downKey  = KeyCode.DownArrow;
    public KeyCode submitKey= KeyCode.Return; // Enter

    [Header("Mouse Sync (optional)")]
    public bool selectOnPointerEnter = true;

    int index = 0; // 현재 선택 인덱스

    void Start()
    {
        if (autoCollectChildren)
        {
            items.Clear();
            GetComponentsInChildren(true, items);
        }
        // 빈 항목 제거
        items.RemoveAll(it => it == null || !it.gameObject.activeInHierarchy);

        ClampIndex();
        RefreshSelection();
    }

    void Update()
    {
        // 방향 입력 처리
        bool moved = false;

        if (columns <= 1)
        {
            // 선형 이동
            if (linearAxis == AxisMode.Horizontal)
            {
                if (Input.GetKeyDown(leftKey))  { Move(-1); moved = true; }
                if (Input.GetKeyDown(rightKey)) { Move(+1); moved = true; }
            }
            else
            {
                if (Input.GetKeyDown(upKey))   { Move(-1); moved = true; }
                if (Input.GetKeyDown(downKey)) { Move(+1); moved = true; }
            }
        }
        else
        {
            // 그리드 이동
            if (Input.GetKeyDown(leftKey))  { MoveGrid(-1, 0); moved = true; }
            if (Input.GetKeyDown(rightKey)) { MoveGrid(+1, 0); moved = true; }
            if (Input.GetKeyDown(upKey))    { MoveGrid(0, -1); moved = true; }
            if (Input.GetKeyDown(downKey))  { MoveGrid(0, +1); moved = true; }
        }

        if (moved) RefreshSelection();

        // Submit
        if (Input.GetKeyDown(submitKey))
            SubmitCurrent();
    }

    // ── 이동 로직 ─────────────────────────────────────────

    void Move(int delta)
    {
        int count = items.Count;
        if (count == 0) return;

        int next = index + delta;
        if (wrap)
        {
            if (next < 0) next = count - 1;
            if (next >= count) next = 0;
        }
        else
        {
            next = Mathf.Clamp(next, 0, count - 1);
        }

        if (next != index)
        {
            index = next;
        }
    }

    void MoveGrid(int dx, int dy)
    {
        int count = items.Count;
        if (count == 0 || columns <= 0) return;

        int rows = Mathf.CeilToInt(count / (float)columns);
        int row = index / columns;
        int col = index % columns;

        int nextRow = row + dy;
        int nextCol = col + dx;

        if (wrap)
        {
            nextCol = (nextCol + columns) % columns;
            nextRow = (nextRow + rows) % rows;
        }
        else
        {
            nextCol = Mathf.Clamp(nextCol, 0, columns - 1);
            nextRow = Mathf.Clamp(nextRow, 0, rows - 1);
        }

        int next = nextRow * columns + nextCol;

        // 빈 칸(범위 밖) 보정
        if (next >= count)
        {
            if (wrap) next = next % count;
            else next = Mathf.Min(count - 1, row * columns + (columns - 1));
        }

        index = Mathf.Clamp(next, 0, count - 1);
    }

    // ── 선택/실행 ─────────────────────────────────────────

    void ClampIndex()
    {
        if (items.Count == 0) { index = 0; return; }
        index = Mathf.Clamp(index, 0, items.Count - 1);
    }

    void RefreshSelection()
    {
        for (int i = 0; i < items.Count; i++)
        {
            if (!items[i]) continue;
            items[i].SetSelected(i == index);
        }
    }

    public void SubmitCurrent()
    {
        if (items.Count == 0) return;
        var it = items[index];
        if (it) it.Submit();
    }

    // ── 마우스 호버로 선택 동기화(선택 기능) ───────────────
    // UINavItem과 같은 오브젝트에 EventTrigger(PointerEnter)를 달아
    // 이 공개 메서드를 바인딩하면 마우스로 올렸을 때도 선택이 따라갑니다.
    public void SelectByPointer(UINavItem item)
    {
        if (!selectOnPointerEnter || item == null) return;
        int idx = items.IndexOf(item);
        if (idx >= 0 && idx < items.Count)
        {
            index = idx;
            RefreshSelection();
        }
    }

    // 외부에서 초기 선택 인덱스 설정하고 싶을 때
    public void SetIndex(int i)
    {
        index = i;
        ClampIndex();
        RefreshSelection();
    }
}