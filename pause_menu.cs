using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class pause_menu : MonoBehaviour
{
    public bool gamepaused = false;
    public GameObject pauseMenu;
    

    void Update()
    {

        if (Input.GetButtonDown("Cancel"))
        {
            if (gamepaused == false)
            {
                Time.timeScale = 0;
                gamepaused = true;
                Cursor.visible = true;
                this.GetComponent<AudioSource>().Pause();
                pauseMenu.SetActive(true);
            }
            else
            {
                Time.timeScale = 1;
                gamepaused = false;
                Cursor.visible = false;
                this.GetComponent<AudioSource>().UnPause();
                pauseMenu.SetActive(false);

            }
        }
    }
    public void UnpauseGame()
    {
        Time.timeScale = 1;
        gamepaused = false;
        Cursor.visible = false;
        this.GetComponent<AudioSource>().UnPause();
        pauseMenu.SetActive(false);

    }

    public void QuitLevel()
    {
        Time.timeScale = 1;
        SceneManager.LoadScene(1);
    }
}
