using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MusicManager : MonoBehaviour
{
    public AudioSource music;

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.M))
        {
            music.mute = !music.mute;
        }
    }
}