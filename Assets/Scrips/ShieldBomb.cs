using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ShieldBomb : MonoBehaviour
{
    private float _speed = 3.0f;

    private bool isExposed = false;
    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        if (!isExposed)
        {
            transform.Translate(Vector3.up * _speed * Time.deltaTime);
            if (transform.position.y > 1.5f )
            {
                isExposed = true;
                transform.localScale += new Vector3(16,0,0);
                Destroy(this.gameObject, 5f );
            }
        }
    }
}
