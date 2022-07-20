using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class radio : MonoBehaviour
{
   public   AudioSource[] Radio;
    public  static int i = 0;
    private bool play = true;
    private bool start = false;
    // Update is called once per frame
    private void Awake()
    {
        start = true;
    }
    void Update()
    {
        if (start && play == true)
        {
            
                Radio[i].Play();
                play = false;
            
        }
        if (Input.GetKeyDown(KeyCode.T))
        {
            Radio[i].Stop();
            i++;
            if (i == 6)
                i = 0;
            Radio[i].Play();
        }
        if (Input.GetKeyDown(KeyCode.E))
        {
            Radio[i].Stop();
            i--;
            if (i == -1)
                i = 5;
            Radio[i].Play();
        }
    }

    public void pause()
    {
        Radio[i].Pause();
    }
    public void unpause()
    {
        Radio[i].UnPause();
    }
}
