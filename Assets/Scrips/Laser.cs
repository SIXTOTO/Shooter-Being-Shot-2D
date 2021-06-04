using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Laser : MonoBehaviour
{
    [SerializeField]
    private float _speed = 8.0f; 
    // Start is called before the first frame update

    [SerializeField]
    private int _direction = 0; //0 = down, 1 = up 
    private AudioSource _audioSource;
    void Start()
    {
        _audioSource = GetComponent<AudioSource>();
        if (_audioSource == null)
        {
            Debug.LogError("Audio Source is NULL.");
        }
    }

    // Update is called once per frame
    void Update()
    {
        if (_direction == 0)
        {
            moveDown();
        }else if (_direction == 1)
        {
            moveUp();
        }
    }

    private void moveUp()
    {
        transform.Translate(Vector3.up * _speed * Time.deltaTime);
        if (transform.position.y > 8.0f)
        {
            if (this.transform.parent != null)
            {
                Destroy(this.transform.parent.gameObject);
                return;
            }
            Destroy(this.gameObject);
        }
    }
    
    private void moveDown()
    {
        transform.Translate(Vector3.down * _speed * Time.deltaTime);
        if (transform.position.y < -8.0f)
        {
            if (this.transform.parent != null)
            {
                Destroy(this.transform.parent.gameObject);
                return;
            }
            Destroy(this.gameObject);
        }
    }
    
    private void OnTriggerEnter2D(Collider2D other)
    {
        
        if (this._direction == 0 && other.tag == "Player")   //laser direction going down = enemy laser
        {
            Player player = other.transform.GetComponent<Player>();
            if (player != null)
            {
                player.Damage();
            }
            _speed = 0;
            Destroy(this.GetComponent<BoxCollider2D>());
            
            this.GetComponent<SpriteRenderer>().enabled = false;
            _audioSource.Play();
            if (this.transform.parent != null)
            {
                Destroy(this.transform.parent.gameObject);
            }
            else
            {
                Destroy(this.gameObject, 2.6f);
            }
        }
    }

    public void setDirection(int direction)
    {
        _direction = direction;
    }

    public bool isLaserGoingUp()
    {
        return _direction == 1;
    }
}
