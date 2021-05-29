using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Random = UnityEngine.Random;

public class Enemy : MonoBehaviour
{
    [SerializeField]
    private float _speed = 2.0f;
    [SerializeField]
    private GameObject _laserSouthPrefab;
    [SerializeField]
    private GameObject _laserSouthWestPrefab;
    [SerializeField]
    private GameObject _laserSouthEastPrefab;
    [SerializeField]
    private float _fireRate = 1.5f;  //wait for how long before next fire
    private float _canFire = -1f;

    private Player _player;

    private Animator _anim;
    // Start is called before the first frame update
    void Start()
    {
        _player = GameObject.Find("Player").GetComponent<Player>();
        if (_player == null)
        {
            Debug.LogError("The Player is NULL.");
        }

        _anim = GetComponent<Animator>();
        if (_anim == null)
        {
            Debug.LogError("The animator is NULL");
        }
    }

    // Update is called once per frame
    void Update()
    {
        transform.Translate(Vector3.down * _speed * Time.deltaTime);

        if (transform.position.y < -5f)
        {
            float randomX = Random.Range(-8f, 8f);
            transform.position = new Vector3(randomX, 7, 0);
        }
        
        if (Time.time > _canFire)
        {
            FireLaser();
        }
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.tag == "Player")
        {
            Player player = other.transform.GetComponent<Player>();
            if (player != null)
            {
                player.Damage();
            }
            _anim.SetTrigger("OnEnemyDeath");
            _canFire = float.MaxValue;
            _speed = 0;
            Destroy(this.gameObject, 2.6f);
        }else if (other.tag == "Laser")
        {
            Destroy(other.gameObject);
            if (_player != null)
            {
                _player.AddScore(100);
            }
            _anim.SetTrigger("OnEnemyDeath");
            _canFire = float.MaxValue;
            _speed = 0;
            Destroy(this.gameObject, 2.6f);
        }
    }
    
    void FireLaser()
    {
        _canFire = Time.time + _fireRate;
        if (_laserSouthEastPrefab != null && _laserSouthWestPrefab !=null)
        {
            Instantiate(_laserSouthEastPrefab, transform.position + new Vector3(-0.3f, -0.8f, 0), Quaternion.identity);
            Instantiate(_laserSouthWestPrefab, transform.position + new Vector3(0.3f, -0.8f, 0), Quaternion.identity);
        }
        Instantiate(_laserSouthPrefab, transform.position + new Vector3(0, -0.8f, 0), Quaternion.identity);
        
    }
}
