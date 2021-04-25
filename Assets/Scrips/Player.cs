using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Player : MonoBehaviour
{
    // Start is called before the first frame update
    void Start()
    {
        //set current position to new position (0, 0, 0)
        transform.position = new Vector3(0, 0, 0);
    }

    // Update is called once per frame
    void Update()
    {
        // new Vector3(-1, 0 ,0) * real time
        // * Time.deltaTime make this statement ran a total of 1 second, instead of every frame (e.g. 60 frames/second) 
        transform.Translate(Vector3.left * Time.deltaTime);
    }
}
