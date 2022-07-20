using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
public class MAINMENU1 : MonoBehaviour
{
    public static int screenNum = 0;
    public void multiplayer()
    {
        screenNum = 2;
        SceneManager.LoadScene(screenNum);
    }
    public void singleplayer()
    {
        screenNum = 3;
        SceneManager.LoadScene(screenNum);
    }
    public void QuitGame()
    {

        Application.Quit();
    }
}
