using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;

public class SessionUI : UIBehaviour
{
    public static SessionUI Instance;
    [SerializeField] TMP_InputField joinCodeInput;
    public string JoinCode => joinCodeInput.text.Trim().ToUpperInvariant();

    protected override void Awake()
    {
        base.Awake();
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);
    }
    protected override void OnEnable()
    {
        base.OnEnable();
        SessionManager.Instance.OnHostCodeSet += UpdateHostCode;
    }
    public void StartGame()
    {
        SessionManager.StartGame();
    }
    public void JoinGame()
    {
        SessionManager.Instance.JoinGame();
    }
    public void HostGame()
    {
        SessionManager.Instance.HostGame();
    }
    public void LeaveGame()
    {
        SessionManager.Instance.LeaveGame();
    }
    private void UpdateHostCode(string code) 
    {
        joinCodeInput.text = code;
    }
}
