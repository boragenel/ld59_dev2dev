using System;
using DG.Tweening;
using TMPro;
using UnityEngine;

public class Ending : MonoBehaviour
{
    public GameObject guy;
    public TextMeshProUGUI scoreNowT;
    public TextMeshProUGUI highScoreT;
    public CanvasGroup canvasGroup;
    public Camera cam;
    public GameObject explanation;
    public GameObject timePanel;
    public GameObject signalPanel;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        GameManager.Instance.gameWon = true;
        canvasGroup.gameObject.SetActive(true);
        TimeSpan timeSpan = TimeSpan.FromSeconds(GameManager.Instance.gameTimer);
        scoreNowT.text = timeSpan.ToString(@"mm\:ss\:ff");
        float highscore = 9999999999;
        if (WebGLPrefs.HasKey("HighScore"))
        {
            highscore =  WebGLPrefs.GetFloat("HighScore");
            if (GameManager.Instance.gameTimer < highscore)
            {
                highscore = GameManager.Instance.gameTimer;
                WebGLPrefs.SetFloat("HighScore", highscore);
            }
        }
        else
        {
            highscore = GameManager.Instance.gameTimer;
        }
        
        TimeSpan timeSpanHighscore = TimeSpan.FromSeconds(highscore);
        highScoreT.text = timeSpanHighscore.ToString(@"mm\:ss\:ff");
        
        
        explanation.SetActive(false);
        transform.localScale = Vector3.one * 0.001f;
        transform.DOScale(1f, 4f).SetEase(Ease.Linear);
        timePanel.SetActive(false);
        signalPanel.SetActive(false);
        cam =  Camera.main;
        guy.transform.DOScale(3.52f, 7f).SetEase(Ease.Linear).SetDelay(4f).OnComplete(() =>
        {
            explanation.SetActive(true);
        });
        cam.DOOrthoSize(2.4f, 10f);
        canvasGroup.DOFade(1f, 5f).SetDelay(2f).SetEase(Ease.Linear);
    }

    public void GotoMainMenu()
    {
        LoadingManager.OnLoadingExpected?.Invoke("MainMenuScene");
    }
    
    // Update is called once per frame
    void Update()
    {
        
    }
}
