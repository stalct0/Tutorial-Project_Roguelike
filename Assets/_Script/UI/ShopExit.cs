    using UnityEngine;

public class ShopExit : MonoBehaviour
{
    public void OnNextLevelClicked()
    {
        GameManager.Instance.NextStage();
    }
}
