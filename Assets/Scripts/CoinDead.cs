using System;
using UnityEngine;

public class CoinDead : MonoBehaviour
{
	public float LifetimeMax = 5f;

	private float _lifetime;

	private void OnEnable()
	{
		_lifetime = LifetimeMax;
	}

	private void Update()
	{
		if (_lifetime <= 0f)
			return;
		
		_lifetime -= Time.deltaTime;
		if (_lifetime <= 0f)
		{
			Destroy(gameObject);
		}
	}
}