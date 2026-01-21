using UnityEngine;
using UnityEngine.Events;

public class EventOnStart : MonoBehaviour
{
    public UnityEvent OnStartExecute = new();

    private void Start()
    {
        OnStartExecute?.Invoke();
    }
}
