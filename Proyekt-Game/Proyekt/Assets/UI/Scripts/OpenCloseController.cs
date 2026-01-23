using UnityEngine;

public class OpenCloseController : MonoBehaviour
{
    private Animator animator;
    [SerializeField] private bool isOpen = false;

    void Awake()
    {
        animator = GetComponent<Animator>();
        
    }

    public void Toggle()
    {
        isOpen = !isOpen;
        animator.SetBool("IsOpen", isOpen);
        if (isOpen) { animator.SetTrigger("Open"); }
        else { animator.SetTrigger("Close"); }
    }



}