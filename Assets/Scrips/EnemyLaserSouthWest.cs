using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EnemyLaserSouthWest : MonoBehaviour
{
    [SerializeField]
    private float _enemyLaserSouthWestSpeed = 3f;
    private float moveX = 1f;
    private float moveY = -1f;
    
    // Start is called before the first frame update
    void Start()
    {
    }

    // Update is called once per frame
    void Update()
    {
        transform.Translate(new Vector3(moveX, moveY, 0) * _enemyLaserSouthWestSpeed * Time.deltaTime);
        if (transform.position.y > 8.0f)
        {
            Destroy(this.gameObject);
        }
    }

}
