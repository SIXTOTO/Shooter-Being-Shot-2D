using System;
using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;
using Random = UnityEngine.Random;

public class SpawnManager : MonoBehaviour
{
    [SerializeField]
    private GameObject[] _enemyPrefab; // hold the prefab of all the different type of enemy
    [SerializeField]
    private int[] _numberOfEnemyAllowedOnScreen; //for each type of enemy, how many can appear on screen at any moment

    private int[] _numberOfEnemyOnScreenNow;
    [SerializeField]
    private int _totalEnemyAllowedOnScreen = 5;

    private int _totalNumEnemyOnScreenNow = 0;  //how many enemy on screen at the moment
    [SerializeField]
    private GameObject _enemyContainer;
    [SerializeField]
    private GameObject[] _powerups;

    private bool _stopSpawning = false;
    // Start is called before the first frame update

    public void StartSpawning()
    {
        StartCoroutine(SpawnEnemyRoutine());
        StartCoroutine(SpawnPowerupRoutine());        
    }

    void Start()
    {
        _numberOfEnemyOnScreenNow = new int[_enemyPrefab.Length];
        for (int i=0; i<_enemyPrefab.Length; i++)
        {
            _numberOfEnemyOnScreenNow[i] = 0;
        }
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    IEnumerator SpawnEnemyRoutine()
    {
        yield return new WaitForSeconds(3.0f);
        while (!_stopSpawning)
        {
            if (_totalNumEnemyOnScreenNow < _totalEnemyAllowedOnScreen)
            {
                Vector3 posToSpawn = new Vector3(Random.Range(-8f, 8f),7f,  0);
                int enemyTypeToSpawn = Random.Range(0, _enemyPrefab.Length);
                while (_numberOfEnemyOnScreenNow[enemyTypeToSpawn] >= _numberOfEnemyAllowedOnScreen[enemyTypeToSpawn])
                {
                    enemyTypeToSpawn = Random.Range(0, _enemyPrefab.Length);
                }
                GameObject newEnemy = Instantiate(_enemyPrefab[enemyTypeToSpawn], posToSpawn, Quaternion.identity);
                _numberOfEnemyOnScreenNow[enemyTypeToSpawn]++;
                _totalNumEnemyOnScreenNow++;
            
                newEnemy.transform.parent = _enemyContainer.transform;
                yield return new WaitForSeconds(5.0f);
            }
        }
    }
    
    IEnumerator SpawnPowerupRoutine()
    {
        yield return new WaitForSeconds(3.0f);
        while (! _stopSpawning)
        {
            Vector3 posToSpawn = new Vector3(Random.Range(-8f, 8f),7f,  0);
            int randomPowerUp = Random.Range(0, 3);
            Instantiate(_powerups[randomPowerUp], posToSpawn, Quaternion.identity);
            yield return new WaitForSeconds(Random.Range(3,8));
        }
        
    }

    public void onPlayerDeath()
    {
        _stopSpawning = true;
    }

    public void onAlienShipHit(int enemyId)
    {
        _numberOfEnemyOnScreenNow[enemyId]--;
        _totalNumEnemyOnScreenNow--;
    }
}
