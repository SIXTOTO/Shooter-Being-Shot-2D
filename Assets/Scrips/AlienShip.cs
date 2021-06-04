using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Random = UnityEngine.Random;

public class AlienShip : MonoBehaviour
{
    [SerializeField]
    private float _speed = 2.0f;
    [SerializeField]
    private GameObject _laserPrefab;
    [SerializeField]
    private float _fireRate = 1.5f;  //wait for how long before next fire
    private float _canFire = -1f;

    private Player _player;

    private Animator _anim;

    private AudioSource _audioSource;

    private SpawnManager _spawnManager;
    
    [SerializeField]
    private GameObject _explosionPrefab;

    [SerializeField]
    private int alienShipId;
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
            Debug.LogError("The animator is NULL.");
        }

        _audioSource = GetComponent<AudioSource>();
        if (_audioSource == null)
        {
            Debug.LogError("Audio Source is NULL.");
        }
        
        _spawnManager = GameObject.Find("Spawn_Manager").GetComponent<SpawnManager>();
        if (_spawnManager == null)
        {
            Debug.LogError("Spawn Manager is NULL.");
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
            
            _canFire = float.MaxValue;
            _speed = 0;
            Destroy(this.GetComponent<PolygonCollider2D>());
            
            this.GetComponent<SpriteRenderer>().enabled = false;
            Instantiate(_explosionPrefab, transform.position, Quaternion.identity);
            _audioSource.Play();
            
            _spawnManager.onAlienShipHit(this.alienShipId);
            Destroy(this.gameObject, 2.6f);
        }else if (other.tag == "Laser")
        {
            if (other.transform.GetComponent<Laser>().isLaserGoingUp())  //this laser is coming from Player
            {
                Destroy(other.gameObject);
                _player.AddScore(100);
            
                _canFire = float.MaxValue;
                _speed = 0;
                Destroy(this.GetComponent<PolygonCollider2D>());
            
                this.GetComponent<SpriteRenderer>().enabled = false;
                Instantiate(_explosionPrefab, transform.position, Quaternion.identity);
                _audioSource.Play();
            
                _spawnManager.onAlienShipHit(this.alienShipId);
                Destroy(this.gameObject, 2.6f);
            }
        }
    }
    
    void FireLaser()
    {
        _canFire = Time.time + _fireRate;
        if (_laserPrefab != null )
        {
            Instantiate(_laserPrefab, transform.position + new Vector3(0, -3.42f, 0), Quaternion.identity);
        }
    }
}
