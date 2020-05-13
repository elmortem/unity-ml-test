using UnityEngine;

public class TriggerItem : MonoBehaviour
{
	public CoinAgent Coin;
	private void OnTriggerEnter(Collider other)
	{
		var agent = other.transform.GetComponentInParent<PlayerAgent>();
		if (agent == null)
			return;
		
		if (tag == "Coin")
		{
			agent.AddReward(25f);
			Coin.Die();
		}
	}
}