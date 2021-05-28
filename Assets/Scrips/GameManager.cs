using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class GameManager : MonoBehaviour
{
    [SerializeField]
    private bool _isGameOver;

    // Update is called once per frame
    void Update()
    {
        if (Input.GetKeyDown(KeyCode.A) && _isGameOver)
        {
            SceneManager.LoadScene(1); // 0 is the Current Game Scene
        }
    }
    
    public void GameOver()
    {
        _isGameOver = true;
    }
    
    
}
