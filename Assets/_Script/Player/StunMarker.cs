using UnityEngine;

public class StunMarker : MonoBehaviour
{
    float remain;
    bool running;

    /// 스턴 아이콘을 seconds 동안 보이게. 재호출하면 시간이 갱신됩니다.
    public void Show(float seconds)
    {
        remain = Mathf.Max(0.01f, seconds);
        if (!running) StartCoroutine(Run());
    }

    System.Collections.IEnumerator Run()
    {
        running = true;
        while (remain > 0f)
        {
            remain -= Time.deltaTime;
            yield return null;
        }
        Destroy(gameObject);
    }
}