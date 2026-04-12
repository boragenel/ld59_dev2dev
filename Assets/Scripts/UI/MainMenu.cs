using System.Collections;
using TMPro;
using UnityEngine;

public class MainMenu : MonoBehaviour
{
    public GameObject settings;
    public GameObject credits;
    public TextMeshProUGUI quitT;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        settings.GetComponentInChildren<SettingsPanel>().LoadSettings();
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    public void StartGame()
    {
        LoadingManager.LoadScene("GameScene");
    }
    
    public void SetSettingsVisible(bool value)
    {
        settings.SetActive(value);
    }

    public void SetCreditsVisible(bool value)
    {
        credits.SetActive(value);
    }

    public void QuitGame()
    {
        #if UNITY_WEBGL
            StartCoroutine(PlayWebGLQuit());
        #else
            Application.Quit();
        #endif
    }

    IEnumerator PlayWebGLQuit()
    {
        string text = quitT.text;
        quitT.text = "You're in browser, silly!";
        yield return new WaitForSeconds(1.5f);
        quitT.text = text;
    }
    
}
