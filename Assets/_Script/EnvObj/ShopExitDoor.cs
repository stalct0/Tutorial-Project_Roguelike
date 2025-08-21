using UnityEngine;

public class ShopExitDoor : MonoBehaviour
{
    private bool playerInRange = false;

    void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
            playerInRange = true;
            
    }

    void OnTriggerExit2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            playerInRange = false;
        }
        
    }

    void Update()
    {
        if (playerInRange && Input.GetKeyDown(KeyCode.I)) // 
        {
            GameManager.Instance.NextStage();
            GameManager.Instance.LoadScene("Level");
        }
    }

}
