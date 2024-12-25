using UnityEngine;

public class WhoopieCushionTrigger : MonoBehaviour
{
	public WhoopieCushionItem itemScript;

	private void OnTriggerEnter(Collider other)
	{
		if (!itemScript.isHeld && (other.gameObject.CompareTag("Player") || other.gameObject.CompareTag("Enemy")))
		{
			Debug.Log("Collided with whoopie cushion");
			itemScript.FartWithDebounce();
		}
	}
}
