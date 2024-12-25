using UnityEngine;

public class EnemyAnimationEvent : MonoBehaviour
{
	public EnemyAI mainScript;

	public void PlayEventA()
	{
		mainScript.AnimationEventA();
	}

	public void PlayEventB()
	{
		mainScript.AnimationEventB();
	}
}
