using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using TMPro;

public class GameManager : MonoBehaviour
{
    [Header("UI")]
    public GameObject endScreenPanel;
    public TMP_Text endScreenText;
    public Image endScreenBackground;
    public Color endScreenBackgroundColor = new Color(0f, 0f, 0f, 0.85f);

    private bool gameEnded = false;
    public bool GameEnded => gameEnded;

    void Start()
    {
        Time.timeScale = 1f;

        if (endScreenPanel != null)
        {
            endScreenPanel.SetActive(false);

            if (endScreenBackground == null)
                endScreenBackground = endScreenPanel.GetComponent<Image>();

            if (endScreenBackground != null)
                endScreenBackground.color = endScreenBackgroundColor;
        }

        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    void Update()
    {
        if (gameEnded && Input.GetKeyDown(KeyCode.R))
        {
            Time.timeScale = 1f;
            SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
        }
    }

    public void WinGame()
    {
        if (gameEnded) return;

        gameEnded = true;
        ShowEndScreen("YOU WIN");
    }

    public void LoseGame()
    {
        if (gameEnded) return;

        gameEnded = true;
        ShowEndScreen("GAME OVER");
    }

    private void ShowEndScreen(string message)
    {
        if (endScreenPanel != null)
        {
            endScreenPanel.SetActive(true);

            if (endScreenBackground == null)
                endScreenBackground = endScreenPanel.GetComponent<Image>();

            if (endScreenBackground != null)
                endScreenBackground.color = endScreenBackgroundColor;
        }

        if (endScreenText != null)
            endScreenText.text = message + "\n\nPress R to Restart";

        Time.timeScale = 0f;

        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }
}
