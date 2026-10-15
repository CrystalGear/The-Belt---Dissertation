using System.Collections;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.SceneManagement;

public class LevelManager : MonoBehaviour
{
    public static LevelManager Instance;
    const string LEVEL1 = "Debug 1";
    const string LEVEL2 = "Debug 2";

    string currentScene;

    private void Awake()
    {
        if(Instance!= null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    public IEnumerator LoadSceneAsync(string sceneName) 
    {
        yield return UnLoadSceneAsync(currentScene);
        if (string.IsNullOrWhiteSpace(sceneName)) yield break;
        Scene scene = SceneManager.GetSceneByName(sceneName);
        if (scene.isLoaded) yield break;

        AsyncOperation operation = SceneManager.LoadSceneAsync(sceneName,LoadSceneMode.Additive);
        if (operation == null) 
        {
            Debug.LogError($"Failed to start loading scene {sceneName}");
            yield break;
        }
        while (!operation.isDone) 
        {
            yield return null;
        }

        currentScene = sceneName;
    }

    public IEnumerator UnLoadSceneAsync(string sceneName) 
    {
        if (string.IsNullOrWhiteSpace(sceneName)) yield break;
        Scene scene = SceneManager.GetSceneByName(sceneName);

        if(!scene.IsValid() || ! scene.isLoaded)yield break;

        AsyncOperation operation = SceneManager.UnloadSceneAsync(sceneName);
        if (operation == null)yield break;

        while (!operation.isDone) 
            yield return null;
        currentScene = "";

    }

    public static void LoadDebug1() 
    {
        Instance.StartCoroutine(Instance.LoadSceneAsync(LEVEL1));
    }

    public static void LoadDebug2()
    {
        Instance.StartCoroutine(Instance.LoadSceneAsync(LEVEL2));
    }
}
