using UnityEngine;

public class UIOpenClose : MonoBehaviour
{
    [SerializeField] private GameObject UIToOpen;
    [SerializeField] private GameObject UIToClose;
    [SerializeField] private GameObject UIToClose2;

    public void Start()
    {
        Time.timeScale = 0f;
    }
    public void OpenUI()
    {
        if (UIToOpen.activeSelf == false)
        {
            UIToOpen.SetActive(true);
            
        }
        else
        {
            UIToOpen.SetActive(false);
        }
    }

    public void CloseUI()
    {
        
        if (UIToClose != null)
            UIToClose.SetActive(false);
            Time.timeScale = 1f;
            if (UIToClose2 != null)
            UIToClose2.SetActive(false);
            Time.timeScale = 1f;
    
    }
}
