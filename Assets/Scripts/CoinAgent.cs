using System;
using System.Collections.Generic;
using Unity.MLAgents;
using Unity.MLAgents.Sensors;
using UnityEngine;
using Random = UnityEngine.Random;

public class CoinAgent : Agent
{
	public float Radius = 10f;
	public float SpawnDistance = 10f;
	public Vector3[] Rays = new Vector3[0];
	public Vector3 RayOffset = new Vector3(0f, 0.2f, 0f);
	public float RayDistanceMin = 3f;
	public float RayDistanceMax = 30f;
	public List<string> Targets = new List<string>();
	public float SpeedMin = 0.5f;
	public float SpeedMax = 0.5f;
	public float RotSpeedMin = 0.5f;
	public float RotSpeedMax = 50f;
	
	[Space]
	
	public Rigidbody Rigidbody;
	
	[Space]
	
	public GameObject DeadPrefab;
	public GameObject SelfDeadPrefab;
	
	private Vector3 _direction = Vector3.zero;
	private Vector3 _look = Vector3.zero;
	private float _speed;
	private float _rotSpeed;
	private float _rayDistance;
	private float _lifetime;
	
	public Spawner Spawner { get; set; }

	public override void OnEpisodeBegin()
	{
		var p = Spawner.GetRandomPosition(Radius, SpawnDistance);
		transform.position = p;
		transform.rotation = Quaternion.Euler(0f, Random.value * 360f, 0f);
		_direction = Vector3.zero;
		_look = Vector3.zero;

		_speed = Random.Range(SpeedMin, SpeedMax);
		_rotSpeed = Random.Range(RotSpeedMin, RotSpeedMax);
		_rayDistance = Random.Range(RayDistanceMin, RayDistanceMax);

		_lifetime = 0f;
	}
	
	public override void CollectObservations(VectorSensor sensor)
	{
		sensor.AddObservation(_speed);
		sensor.AddObservation(_rotSpeed);

		if (Spawner.Player != null)
		{
			var p = transform.position - Spawner.Player.transform.position;
			var a = Vector3.SignedAngle(p.normalized, Spawner.Player.transform.forward, Vector3.up);
			sensor.AddObservation(a / 180f);
			p = p / _rayDistance;
			sensor.AddObservation(p.x);
			sensor.AddObservation(p.z);
			p = Spawner.Player.transform.position + Spawner.Player.transform.forward * 15f;
			p = (transform.position - p) / _rayDistance;
			sensor.AddObservation(p.x);
			sensor.AddObservation(p.z);
		}
		else
		{
			Debug.LogError("Player not found.");
			sensor.AddObservation(1f);
			sensor.AddObservation(1f);
			sensor.AddObservation(1f);
			sensor.AddObservation(1f);
			sensor.AddObservation(1f);
		}

		for (int i = 0; i < Rays.Length; i++)
		{
			var d = _rayDistance;
			Color rc = Color.white;
			
			var baseDir = Rays[i].normalized;
			var dir = Quaternion.LookRotation(baseDir) * transform.forward;
			var r = new Ray(transform.position + RayOffset, dir);
			if (Physics.Raycast(r, out var hitInfo, _rayDistance) && hitInfo.transform != null)
			{
				var idx = Targets.IndexOf(hitInfo.transform.tag);
				sensor.AddObservation(hitInfo.distance / _rayDistance);
				
				d = hitInfo.distance;
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
	}
	
	public override void OnActionReceived(float[] actions)
	{
		_lifetime += Time.fixedDeltaTime;
		
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

		var dist = (transform.position - Spawner.Player.transform.position).magnitude;
		var a = Quaternion.LookRotation(transform.position - Spawner.Player.transform.position);
		var angle = Quaternion.Angle(a, Spawner.Player.transform.rotation);

		//Step1(dist, angle);
		//Step2(dist, angle);
		//Step3(dist, angle);
		Step4(dist, angle);
		//Step5(dist, angle);
		//Step6(dist, angle);
	}
	
	private void Step1(float dist, float angle)
	{
		if (angle < 10f)
		{
			SelfDie(-50f);
		}
		if (angle > 120f && _lifetime > 3f)
		{
			AddReward(50f);
			EndEpisode();
		}
		
		AddReward(50f / MaxStep);
	}
	
	private void Step2(float dist, float angle)
	{
		if (angle < 15f || dist < 5f)
		{
			SelfDie(-50f);
		}
		if (angle > 140f && _lifetime > 10f)
		{
			AddReward(50f);
			EndEpisode();
		}
		
		AddReward(30f / MaxStep);
	}
	
	private void Step3(float dist, float angle)
	{
		if (angle < 20f || dist < 5f)
		{
			SelfDie(-50f);
		}
		if (angle > 160f && _lifetime > 15f)
		{
			AddReward(50f);
			EndEpisode();
		}
		
		AddReward(10f / MaxStep);
	}
	
	private void Step4(float dist, float angle)
	{
		/*if (dist < 2f)
		{
			SelfDie(-30f);
		}*/
		if (angle > 100f)
		{
			AddReward(100f / MaxStep);
		}
	}

	public override void Heuristic(float[] actionsOut)
	{
		var dir = new Vector2(Input.GetAxis("Horizontal"), Input.GetAxis("Vertical"));
		dir.Normalize();
		actionsOut[0] = dir.x;
		actionsOut[1] = dir.y;
		actionsOut[2] = dir.x;
		actionsOut[3] = dir.y;
	}
	
	/*private void OnCollisionEnter(Collision other)
	{
		if (other.transform.CompareTag("Wall"))
		{
			SelfDie(-50f);
		}
	}*/
	
	public void Die()
	{
		GameObject.Instantiate(DeadPrefab, transform.position, transform.rotation);
		AddReward(-50f);
		EndEpisode();
	}
	
	public void SelfDie(float reward)
	{
		GameObject.Instantiate(SelfDeadPrefab, transform.position, transform.rotation);
		AddReward(reward);
		EndEpisode();
	}
	
	public void Looked()
	{
		var d = (40f - (Spawner.Player.transform.position - transform.position).magnitude) * 0.01f;
		AddReward(-d);
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