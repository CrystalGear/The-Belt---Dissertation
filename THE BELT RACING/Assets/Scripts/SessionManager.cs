using System;
using Unity.Services.Authentication;
using Unity.Services.Core;
using UnityEngine;
using System.Threading.Tasks;
using Unity.Services.Multiplayer;

public class SessionManager : MonoBehaviour
{
    public static SessionManager Instance;
    static bool gameStarted = false;
    public event Action<string> OnHostCodeSet;
    bool servicesReady;
    bool busy;
    ISession activeSession;
    string hostJoinCode = "";
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
    private async void Start()
    {
        //OnStatusChanged?.Invoke(status);
        //OnNetcodeStateChanged?.Invoke(GetNetcodeState());
        await InitialiseServicesAsync();
    }
    private async Task InitialiseServicesAsync()
    {
        //OnNetcodeStateChanged?.Invoke(GetNetcodeState());
        try
        {
            await UnityServices.InitializeAsync();

            if (!AuthenticationService.Instance.IsSignedIn)
            {
                await AuthenticationService.Instance.SignInAnonymouslyAsync();
            }

            servicesReady = true;
            // TODO this should be a status object, not an arbitrary string.
            //status = "Ready. Host a game or enter a join code.";
            //OnStatusChanged?.Invoke(status);
        }
        catch (Exception exception)
        {
            RecordFailure("Service initialisation", exception);
        }
        //OnNetcodeStateChanged?.Invoke(GetNetcodeState());
    }
    private void RecordFailure(string operation, Exception exception)
    {
        //OnNetcodeStateChanged?.Invoke(GetNetcodeState());
        //status = $"{operation} failed: {exception.Message}";
        //OnStatusChanged?.Invoke(status);
        Debug.LogException(exception);
    }

    public static void StartGame()
    {

    }
    public async void JoinGame()
    {
        if (!servicesReady || busy || activeSession != null) return;
        string code = SessionUI.Instance.JoinCode;
        if (string.IsNullOrWhiteSpace(code)) return;
        busy = true;
        try
        {
            activeSession = await MultiplayerService.Instance.JoinSessionByCodeAsync(code);
        }
        catch (Exception exception) { RecordFailure("Joining", exception); }
        finally { busy = false; }
    }
    public async void HostGame()
    {
        if (!servicesReady || busy || activeSession != null) return;
        busy = true;
        try 
        {
            SessionOptions sessionOptions = new SessionOptions
            {
                MaxPlayers = 4
            }.WithRelayNetwork();
            activeSession = await MultiplayerService.Instance.CreateSessionAsync(sessionOptions);
            hostJoinCode = activeSession.Code;
            OnHostCodeSet?.Invoke(hostJoinCode);
        } 
        catch (Exception exception) { RecordFailure("Hosting", exception); }
        finally { busy = false; }
        
    }
    public async void LeaveGame()
    {

    }
}
