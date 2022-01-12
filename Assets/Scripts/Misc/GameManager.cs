using DG.Tweening;
using System;
using System.Collections;
using System.Collections.Generic;

using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

[System.Serializable]
public struct EndGameInfo
{
    public string missionStatusText;
    [TextArea]
    public string flavourText;
}

public class GameManager : MonoBehaviour
{
    public CanvasGroup gameOverCanvas;
    public Text missionStatus;
    public Text flavourText;

    public EndGameInfo escapeInfo;
    public EndGameInfo capturedInfo;

    internal bool _gameOver;

    public void EndGame(bool victoryFlag)
    {
        missionStatus.text = victoryFlag ? escapeInfo.missionStatusText : capturedInfo.missionStatusText;
        flavourText.text = victoryFlag ? string.Format(escapeInfo.flavourText, DateTime.Now.TimeOfDay.Hours.ToString() + DateTime.Now.TimeOfDay.Minutes.ToString()) : capturedInfo.flavourText;

        gameOverCanvas.DOFade(1f, 1f);
        _gameOver = true;
    }

    public void Restart()
    {
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }

    public void Quit()
    {
        Application.Quit();
    }
}
