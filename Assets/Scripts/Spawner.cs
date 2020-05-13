using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;
using Random = UnityEngine.Random;

public class Spawner : MonoBehaviour
{
    public PlayerAgent PlayerPrefab;
    
    [Space]
    
    public int CoinCount = 1; 
    public CoinAgent CoinPrefab;
    
    [Space]
    
    public int WallCount = 1;
    public float WallRadius = 1f;
    public float WallSpawnDistance = 13f;
    public GameObject WallPrefab;

    private PlayerAgent _player;
    private List<CoinAgent> _coinList = new List<CoinAgent>();
    private List<GameObject> _wallList = new List<GameObject>();

    public PlayerAgent Player
    {
        get { return _player; }
    }

    private void Awake()
    {
        Init();
    }

    public void Init()
    {
        Vector3 p;
        GameObject obj;
        
        // walls
        for (int i = 0; i < WallCount; i++)
        {
            p = new Vector3(Random.Range(-WallSpawnDistance, WallSpawnDistance), 0f,
                Random.Range(-WallSpawnDistance, WallSpawnDistance));
            obj = GameObject.Instantiate(WallPrefab, transform.position + p, Quaternion.identity, transform);
            _wallList.Add(obj);
        }
        
        // player
        p = GetRandomPosition(PlayerPrefab.Radius, PlayerPrefab.SpawnDistance);
        obj = GameObject.Instantiate(PlayerPrefab.gameObject, p, Quaternion.identity, transform);
        _player = obj.GetComponent<PlayerAgent>();
        _player.Spawner = this;
        
        
        // coins
        for (int i = 0; i < CoinCount; i++)
        {
            p = GetRandomPosition(CoinPrefab.Radius, CoinPrefab.SpawnDistance);
            obj = GameObject.Instantiate(CoinPrefab.gameObject, p, Quaternion.identity, transform);
            var coin = obj.GetComponent<CoinAgent>();
            coin.Spawner = this;
            _coinList.Add(coin);
        }
    }

    public Vector3 GetRandomPosition(float radius, float distance)
    {
        var p = Vector3.zero;
        
        var hasPlace = false;
        while (!hasPlace)
        {
            p = new Vector3(Random.Range(-distance, distance), 0f,
                Random.Range(-distance, distance));

            if (!Physics.CheckBox(transform.position + p - new Vector3(0f, -radius-0.1f, 0f), new Vector3(radius, radius, radius),
                Quaternion.identity))
            {
                hasPlace = true;
            }
        }

        return transform.position + p;
    }

    public CoinAgent GetNearlyCoin(Vector3 pos)
    {
        CoinAgent result = null;
        float dist = float.MaxValue;
        foreach (var coin in _coinList)
        {
            var d = (pos - coin.transform.position).magnitude;
            if (result == null || d < dist)
            {
                result = coin;
                dist = d;
            }
        }
        return result;
    }

    public void ResetWalls()
    {
        foreach(var wall in _wallList)
        {
            var p = GetRandomPosition(WallRadius, WallSpawnDistance);
            wall.transform.position = p;
        }
    }
}
