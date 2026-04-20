using System;
using System.Collections;
using DG.Tweening;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.SceneManagement;

/*
 *
 * do LoadingManager.LoadScene("AnySceneName") to load a scene with doing fade out -> fade in
 * 
 * to force a fade out just do LoadingManager.ForceFadeOut()
 * and then, you need to manually fade back in using LoadingManager.ForceFadeIn() whenever you're ready
 * 
 */
public class LoadingManager : MonoBehaviour
{
    
    CanvasGroup canvasGroup;

    public static UnityAction<string> OnLoadingExpected;
    public static UnityAction OnLoadingComplete;
    public static float fadeDuration = 0.5f;
    public bool fadeInOnStart = true;
    
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void Init()
    {
        string persistentSceneName = "LoadingScene";
        if (!SceneManager.GetSceneByName(persistentSceneName).isLoaded)
        {
            SceneManager.LoadScene(persistentSceneName, LoadSceneMode.Additive);
        }
    }
    
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Awake()
    {
        DontDestroyOnLoad(gameObject);
        canvasGroup = GetComponent<CanvasGroup>();
        OnLoadingExpected += LoadSceneInternal;
        OnLoadingComplete += HideDarkener;
        if (fadeInOnStart)
        {
            Invoke("HideDarkener",0.25f);
        }
    }

    private void OnDestroy()
    {
        OnLoadingExpected -= LoadSceneInternal;
        OnLoadingComplete -= HideDarkener;
    }

    public static void LoadScene(string sceneName)
    {
        OnLoadingExpected?.Invoke(sceneName);
    }

    public static void ForceFadeOut()
    {
        OnLoadingExpected?.Invoke("");
    }

    public static void ForceFadeIn()
    {
        OnLoadingComplete?.Invoke();
    }
    
    void LoadSceneInternal(string sceneName)
    {
        ShowDarkener();
        if(sceneName != "")
            StartCoroutine(PlayLoadSceneInternal(sceneName));

    }

    IEnumerator PlayLoadSceneInternal(string sceneName)
    {
        yield return new WaitForSeconds(fadeDuration);
        AsyncOperation op = SceneManager.LoadSceneAsync(sceneName);
        if (op != null)
        {
            while (!op.isDone)
            {
                yield return new WaitForEndOfFrame();
            }    
        }
        OnLoadingComplete.Invoke();
    }
    
    void ShowDarkener()
    {
        canvasGroup.DOFade(1, fadeDuration);
    }

    void HideDarkener()
    {
        canvasGroup.DOFade(0, fadeDuration);
    }
    
    
    // Update is called once per frame
    void Update()
    {
        
    }
}
