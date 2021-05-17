using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EnemyLaserSouthWest : MonoBehaviour
{
    [SerializeField]
    private float _speed = 8.0f;
    private float moveX = 1f;
    
    // Start is called before the first frame update
    void Start()
    {
    }

    // Update is called once per frame
    void Update()
    {
        transform.Translate(new Vector3(moveX, -4f, 0) * _speed * Time.deltaTime);
        if (transform.position.y > 8.0f)
        {
            Destroy(this.gameObject);
        }
    }

}
