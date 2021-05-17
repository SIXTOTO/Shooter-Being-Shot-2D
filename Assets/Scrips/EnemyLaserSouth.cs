using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EnemyLaserSouth : MonoBehaviour
{
    [SerializeField]
    private float _speed = 9.5f;
    private float moveX = 0f;

    // Start is called before the first frame update
    void Start()
    {
    }

    // Update is called once per frame
    void Update()
    {
        transform.Translate(new Vector3(moveX, -1f, 0) * _speed * Time.deltaTime);
        if (transform.position.y > 8.0f)
        {
            Destroy(this.gameObject);
        }
    }
}
