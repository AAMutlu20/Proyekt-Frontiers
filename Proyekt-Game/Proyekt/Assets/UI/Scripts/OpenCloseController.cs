using UnityEngine;

public class OpenCloseController : MonoBehaviour
{
    private Animator animator;
    private bool isOpen = true;

    void Awake()
    {
        animator = GetComponent<Animator>();
        
    }

    public void Toggle()
    {
        isOpen = !isOpen;
        animator.SetBool("IsOpen", isOpen);
    }



}