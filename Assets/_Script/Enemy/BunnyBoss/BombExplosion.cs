using UnityEngine;

public class BombExplosion : MonoBehaviour
{
    private Animator animator;
    void Start()
    {
        animator = GetComponent<Animator>();
    }

    void EndExplosion()
    {
        Destroy(gameObject);
    }

}
