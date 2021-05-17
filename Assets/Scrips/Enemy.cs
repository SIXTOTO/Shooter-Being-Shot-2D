using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Random = UnityEngine.Random;

public class Enemy : MonoBehaviour
{
    [SerializeField]
    private float _speed = 4.0f;
    [SerializeField]
    private GameObject _laserSouthPrefab;
    [SerializeField]
    private GameObject _laserSouthWestPrefab;
    [SerializeField]
    private GameObject _laserSouthEastPrefab;
    [SerializeField]
    private float _fireRate = 0.5f;
    private float _canFire = -1f;
    // Start is called before the first frame update
    void Start()
    {
        
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

    private void OnTriggerEnter(Collider other)
    {
        if (other.tag == "Player")
        {
            Player player = other.transform.GetComponent<Player>();
            if (player != null)
            {
                player.Damage();
            }
            Destroy(this.gameObject);
        }else if (other.tag == "Laser")
        {
            Destroy(other.gameObject);
            Destroy(this.gameObject);
        }
    }
    
    void FireLaser()
    {
        _canFire = Time.time + _fireRate;
        Instantiate(_laserSouthEastPrefab, transform.position + new Vector3(-0.3f, -0.8f, 0), Quaternion.identity);
        Instantiate(_laserSouthPrefab, transform.position + new Vector3(0, -0.8f, 0), Quaternion.identity);
        Instantiate(_laserSouthWestPrefab, transform.position + new Vector3(0.3f, -0.8f, 0), Quaternion.identity);
    }
}
