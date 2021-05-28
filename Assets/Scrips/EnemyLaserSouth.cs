using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EnemyLaserSouth : MonoBehaviour
{
    [SerializeField]
    private float _enemyLaserSouthSpeed = 3f;
    private float moveX = 0f;
    private float moveY = -1f;

    // Start is called before the first frame update
    void Start()
    {
    }

    // Update is called once per frame
    void Update()
    {
        transform.Translate(new Vector3(moveX, moveY, 0) * _enemyLaserSouthSpeed * Time.deltaTime);
        if (transform.position.y > 8.0f)
        {
            Destroy(this.gameObject);
        }
    }
}
