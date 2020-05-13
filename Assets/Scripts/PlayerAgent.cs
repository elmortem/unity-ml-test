using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.MLAgents;
using Unity.MLAgents.Sensors;
using UnityEngine.Serialization;
using Random = UnityEngine.Random;

public class PlayerAgent : Agent
{
	public float Radius = 0.5f;
	public float SpawnDistance = 10f;
	public Vector3[] Rays = new Vector3[0];
	public Vector3 RayOffset = new Vector3(0f, 0.2f, 0f);
	public float RayDistanceMin = 3f;
	public float RayDistanceMax = 3f;
	public List<string> Targets = new List<string>();
	public float SpeedMin = 0.5f;
	public float SpeedMax = 0.5f;
	public float RotSpeedMin = 0.5f;
	public float RotSpeedMax = 50f;
	public float FireTimeMin = 0.1f;
	public float FireTimeMax = 3f;
	
	public Material NormalMaterial;
	public Material FireMaterial;

	[Space]
	
	public Rigidbody Rigidbody;
	public Renderer Renderer;

	public float LearnValue = 0f;
	
	[FormerlySerializedAs("CoinSpawner")] [Space]

	private Vector3 _direction = Vector3.zero;
	private Vector3 _look = Vector3.zero;
	private CoinAgent _lookedCoin;
	private float _lookedCoinDist;
	private float _lookedCoinTimer;
	private Coroutine _fireCoroutine;
	private float _fireTime;
	private bool _fired;
	private float _speed;
	private float _rotSpeed;
	private float _rayDistance;
	private int _coinCount;
	
	public Spawner Spawner { get; set; }
	public bool CanFire { get; private set; }

	public override void OnEpisodeBegin()
	{
		if (_fireCoroutine != null)
		{
			StopCoroutine(_fireCoroutine);
			_fireCoroutine = null;
		}
		Renderer.material = NormalMaterial;
		CanFire = true;
		_fireTime = Random.Range(FireTimeMin, FireTimeMax);

		var p = Spawner.GetRandomPosition(Radius, SpawnDistance);
		transform.position = p;
		transform.rotation = Quaternion.Euler(0f, Random.value * 360f, 0f);
		_direction = Vector3.zero;
		_look = Vector3.zero;
		_lookedCoin = null;

		_speed = Random.Range(SpeedMin, SpeedMax);
		_rotSpeed = Random.Range(RotSpeedMin, RotSpeedMax);
		_rayDistance = Random.Range(RayDistanceMin, RayDistanceMax);

		_coinCount = 0;
		
		Spawner.ResetWalls();
	}

	public override void CollectObservations(VectorSensor sensor)
	{
		_lookedCoin = null;
		
		var rotAngle = transform.rotation.eulerAngles.y;
		sensor.AddObservation(rotAngle / 360f);
		sensor.AddObservation(_speed);
		sensor.AddObservation(_rotSpeed);
		sensor.AddObservation(CanFire);

		var nearCoin = Spawner.GetNearlyCoin(transform.position);
		if (nearCoin != null)
		{
			//var coinAngle = Vector3.SignedAngle(transform.position, nearCoin.transform.position, Vector3.up);
			//sensor.AddObservation(coinAngle / 360f);
			var p = (transform.position - nearCoin.transform.position) / _rayDistance;
			sensor.AddObservation(p.x);
			sensor.AddObservation(p.z);
		}
		else
		{
			sensor.AddObservation(1f);
			sensor.AddObservation(1f);
		}

		for (int i = 0; i < Rays.Length; i++)
		{
			/*if(i > 0)
			{
				sensor.AddObservation(0.5f);
				continue;
			}*/
			
			var d = _rayDistance;
			Color rc = Color.white;
			
			var baseDir = Rays[i].normalized;
			var dir = Quaternion.LookRotation(baseDir) * transform.forward;
			var r = new Ray(transform.position + RayOffset, dir);
			if (Physics.Raycast(r, out var hitInfo, _rayDistance) && hitInfo.transform != null)
			{
				var idx = Targets.IndexOf(hitInfo.transform.tag);
				var dist = hitInfo.distance / _rayDistance;
				sensor.AddObservation(dist);
				
				d = hitInfo.distance;

				if (i == 0)
				{
					if (hitInfo.transform.CompareTag("Coin"))
					{
						_lookedCoin = hitInfo.transform.GetComponentInParent<CoinAgent>();
						_lookedCoinDist = dist;
						_lookedCoinTimer += Time.fixedDeltaTime;
						
						rc = Color.red;
					}
					else
					{
						_lookedCoinTimer = 0f;
					}
				}
			}
			else
			{
				sensor.AddObservation(1f);
			}

			if (transform.position.y > 20 && i == 0)
			{
				Debug.DrawRay(transform.position + RayOffset, dir * d, rc);
			}
		}
		
		sensor.AddObservation(_lookedCoin != null ? 1f : 0f);
	}

	public override void OnActionReceived(float[] actions)
	{
		Rigidbody.angularVelocity = Vector3.zero;
		if (transform.position.y != Spawner.transform.position.y)
		{
			var p = transform.position;
			p.y = Spawner.transform.position.y;
			transform.position = p;
		}
		
		// movement
		_direction.x = Mathf.Clamp(actions[0], -1f, 1f);
		_direction.y = 0f;
		_direction.z = Mathf.Clamp(actions[1], -1f, 1f);
		if(_direction.sqrMagnitude > 1f)
			_direction.Normalize();
		Rigidbody.velocity = _direction * _speed;

		// look
		_look.x = Mathf.Clamp(actions[2], -1f, 1f);
		_look.y = 0f;
		_look.z = Mathf.Clamp(actions[3], -1f, 1f);
		if(_look.sqrMagnitude > 1f)
			_look.Normalize();
		if (_look.sqrMagnitude > 0.1f)
		{
			var newRotation = Quaternion.LookRotation(_look);
			//var rotAngle = newRotation.eulerAngles.y;
			var rotAngle = Mathf.LerpAngle(Rigidbody.rotation.eulerAngles.y, newRotation.eulerAngles.y, Time.deltaTime * _rotSpeed);
			newRotation = Quaternion.Euler(0f, rotAngle, 0f);
			Rigidbody.MoveRotation(newRotation);
		}
		
		//Rigidbody.velocity = (_look + _direction).normalized * _direction.magnitude * Speed;

		_fired = actions[4] > 0.5f;

		//Step1();
		//Step2();
		//Step3();
		//Step4();
		//Step5();
		StepFinal();
	}

	private void Step1()
	{
		if (_lookedCoin != null)
		{
			AddReward(50f);
			EndEpisode();
		}
	}
	
	private void Step2()
	{
		if (_lookedCoin != null && _lookedCoinTimer >= 0.2f)
		{
			AddReward(50f);
			EndEpisode();
		}
	}
	
	private void Step3()
	{
		if (_lookedCoin != null && _lookedCoinTimer >= 0.5f)
		{
			AddReward(50f);
			EndEpisode();
		}
	}
	
	private void Step4()
	{
		if (_lookedCoin != null)
		{
			_lookedCoin.Looked();
			AddReward(10f / MaxStep);
			
			if (_lookedCoinTimer >= 5f)
			{
				AddReward(50f);
				EndEpisode();
			}
		}
		else
		{
			AddReward(-50f / MaxStep);
		}

		// fire
		if (CanFire && _fired)
		{
			if (_lookedCoin != null)
			{
				TakeCoin(_lookedCoin, true);
				_lookedCoin = null;
				_lookedCoinTimer = 0f;
				return;
			}
			else
			{
				AddReward(-1f);
				_fireCoroutine = StartCoroutine(Fire());
			}
		}
	}
	
	private void Step5()
	{
		if (_lookedCoin != null)
		{
			_lookedCoin.Looked();
			AddReward(10f / MaxStep);
			
			if (_lookedCoinTimer >= 5f)
			{
				AddReward(50f);
				EndEpisode();
			}
		}
		else
		{
			AddReward(-50f / MaxStep);
		}

		// fire
		if (CanFire && _fired)
		{
			if (_lookedCoin != null && _lookedCoinTimer >= 0.5f)
			{
				TakeCoin(_lookedCoin, true);
				_lookedCoin = null;
				_lookedCoinTimer = 0f;
				return;
			}
			else
			{
				AddReward(-1f);
				_fireCoroutine = StartCoroutine(Fire());
			}
		}
	}
	
	private void StepFinal()
	{
		// fire
		if (CanFire && _fired)
		{
			if (_lookedCoin != null && _lookedCoinTimer >= 0.2f)
			{
				TakeCoin(_lookedCoin, true);
				_lookedCoin = null;
				_lookedCoinTimer = 0f;
			}
		}
	}

	private void StepN()
	{
		if (_lookedCoin != null)
		{
			_lookedCoin.Looked();
			AddReward(10f / MaxStep);
			
			if (_lookedCoinTimer >= 0.1f)
			{
				AddReward(50f);
				EndEpisode();
			}
		}
		else
		{
			AddReward(-50f / MaxStep);
		}

		// fire
		if (CanFire && _fired)
		{
			if (_lookedCoin != null && _lookedCoinDist > 0.5f && _lookedCoinDist < 0.8f && _lookedCoinTimer >= 5f)
			{
				TakeCoin(_lookedCoin, true);
				_lookedCoin = null;
				_lookedCoinTimer = 0f;
			}
			else
			{
				AddReward(-1f);
				_fireCoroutine = StartCoroutine(Fire());
			}
		}
	}

	public override void Heuristic(float[] actionsOut)
	{
		/*var dir = new Vector2(Input.GetAxis("Horizontal"), Input.GetAxis("Vertical"));
		dir.Normalize();
		actionsOut[0] = dir.x;
		actionsOut[1] = dir.y;
		actionsOut[2] = dir.x;
		actionsOut[3] = dir.y;
		actionsOut[4] = Input.GetKey(KeyCode.Space)?1f:0;*/
	}

	/*private void OnCollisionEnter(Collision other)
	{
		if (other.transform.CompareTag("Wall"))
		{
			AddReward(-50f);
			EndEpisode();
		}
	}*/

	public IEnumerator Fire()
	{
		CanFire = false;
		Renderer.material = FireMaterial;
		yield return new WaitForSeconds(_fireTime);
		CanFire = true;
		Renderer.material = NormalMaterial;
		_fireCoroutine = null;
	}
	
	public void TakeCoin(CoinAgent coin, bool victory)
	{
		coin.Die();
		if (victory)
		{
			AddReward(50f);
			EndEpisode();
		}
	}
	
	#if UNITY_EDITOR

	private void OnDrawGizmos()
	{
		if (transform.position.y > 20)
		{
			Debug.DrawRay(transform.position + RayOffset, _look * 5f, Color.blue);
			Debug.DrawRay(transform.position + RayOffset, _direction * 5f, Color.black);
		}
	}
#endif
}
