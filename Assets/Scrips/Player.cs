using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;

public class Player : MonoBehaviour
{
    [SerializeField]
    private float _speed = 3.5f;
    [SerializeField]
    private GameObject _laserPrefab;
    [SerializeField]
    private GameObject _tripleShotPrefab;
    [SerializeField]
    private GameObject _specialWeaponPrefab;    
    [SerializeField]
    private float _fireRate = 0.15f;
    private float _canFire = -1f;
    [SerializeField]
    private int _lives = 3;

    private SpawnManager _spawnManager;
    
    [SerializeField]
    private bool _isTripleShotActive = false;

    // Start is called before the first frame update
    void Start()
    {
        //set current position to new position (0, 0, 0)
        transform.position = new Vector3(0, 0, 0);
        _spawnManager = GameObject.Find("Spawn_Manager").GetComponent<SpawnManager>();
        if (_spawnManager == null)
        {
            Debug.LogError("The Spawn Manager is NULL");
        }
    }

    // Update is called once per frame
    void Update()
    {
        CalculateMovement();
        if (Input.GetKeyDown(KeyCode.Space) && Time.time > _canFire)
        {
            FireLaser();
        }
        if (Input.GetKeyDown(KeyCode.B))
        {
            FireSpecialWeapon();
        }        
    }

    void CalculateMovement()
    {
        float horizontalInput = Input.GetAxis("Horizontal");
        float vertivalInput = Input.GetAxis("Vertical");
        // new Vector3(-1, 0 ,0) * real time
        // * Time.deltaTime make this statement ran a total of 1 second, instead of every frame (e.g. 60 frames/second) 
        //transform.Translate(Vector3.right * horizontalInput * _speed * Time.deltaTime);
        //transform.Translate(Vector3.up * vertivalInput * _speed * Time.deltaTime);
        
        transform.Translate(new Vector3(horizontalInput, vertivalInput, 0) * _speed * Time.deltaTime);

        // if (transform.position.y > 0)
        // {
        //     transform.position = new Vector3(transform.position.x, 0, 0);
        // }else if (transform.position.y < -3.8f)
        // {
        //     transform.position = new Vector3(transform.position.x, -3.8f, 0);
        // }

        transform.position = new Vector3(transform.position.x, Mathf.Clamp(transform.position.y, -3.8f, 0), 0);
        
        if (transform.position.x > 11.0f)
        {
            transform.position = new Vector3(-11.0f, transform.position.y, 0);
        }else if (transform.position.x < -11.0f)
        {
            transform.position = new Vector3(11.0f, transform.position.y, 0);
        }        
    }

    void FireLaser()
    {
        _canFire = Time.time + _fireRate;
        if (_isTripleShotActive)
        {
            Instantiate(_tripleShotPrefab, transform.position, Quaternion.identity);
        }
        else
        {
            Instantiate(_laserPrefab, transform.position + new Vector3(0, 1.05f, 0), Quaternion.identity); 
        }
        //Instantiate(_laserPrefab, transform.position + new Vector3(-0.21f, 1.35f, 0), Quaternion.identity);
        //Instantiate(_laserPrefab, transform.position + new Vector3(0, 1.05f, 0), Quaternion.identity);
        //Instantiate(_laserPrefab, transform.position + new Vector3(0.21f, 1.35f, 0), Quaternion.identity);
    }

    void FireSpecialWeapon()
    {
        Instantiate(_specialWeaponPrefab, transform.position + new Vector3(0, 0.8f, 0), Quaternion.identity);
    }
    
    public void Damage()
    {
        _lives -= 1;
        if (_lives < 1)
        {
            _spawnManager.onPlayerDeath();
            Destroy(this.gameObject);
        }
    }

    public void TripleShotActive()
    {
        this._isTripleShotActive = true;
        StartCoroutine(TripleShotPowerDownRoutine());
    }

    IEnumerator TripleShotPowerDownRoutine()
    {
        yield return new WaitForSeconds(5.0f);
        this._isTripleShotActive = false;

    }
}
