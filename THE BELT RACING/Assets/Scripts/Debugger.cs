using UnityEngine;

public class Debugger : MonoBehaviour
{
    public static Debugger Instance;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);
    }
    public void OnLoadlevel1() 
    {
        LevelManager.LoadDebug1();
    }
    public void OnLoadlevel2()
    {
        LevelManager.LoadDebug2();
    }
}
