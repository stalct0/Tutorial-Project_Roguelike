using UnityEngine;
using UnityEngine.InputSystem;

public class LobbyExit : MonoBehaviour
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
            GameManager.Instance.DestroyPlayer();
            GameManager.Instance.LoadScene("Level");
        }
    }


}
