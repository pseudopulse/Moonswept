using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

public class InitializeGame : MonoBehaviour
{
	public bool runBootUpScreen = true;

	public Animator bootUpAnimation;

	public AudioSource bootUpAudio;

	public PlayerActions playerActions;

	private bool canSkip;

	private bool hasSkipped;

	public bool playColdOpenCinematic;

	private void OnEnable()
	{
		playerActions.Movement.OpenMenu.performed += OpenMenu_performed;
		playerActions.Movement.Enable();
	}

	private void OnDisable()
	{
		playerActions.Movement.OpenMenu.performed -= OpenMenu_performed;
		playerActions.Movement.Disable();
	}

	private void Awake()
	{
		playerActions = new PlayerActions();
		Application.backgroundLoadingPriority = ThreadPriority.Normal;
		bool flag = ES3.Load("LastVerPlayed", "LCGeneralSaveData", GameNetworkManager.Instance.gameVersionNum) < 50;
		playColdOpenCinematic = flag || ES3.Load("TimesLoadedGame", "LCGeneralSaveData", 0) == 7;
		if (flag)
		{
			ES3.Save("TimesLoadedGame", 8, "LCGeneralSaveData");
		}
	}

	public void OpenMenu_performed(InputAction.CallbackContext context)
	{
		canSkip = !playColdOpenCinematic;
		if (context.performed && canSkip && !hasSkipped)
		{
			hasSkipped = true;
			SceneManager.LoadScene("MainMenu");
		}
	}

	private IEnumerator SendToNextScene()
	{
		if (runBootUpScreen)
		{
			bootUpAudio.Play();
			yield return new WaitForSeconds(0.2f);
			canSkip = true;
			bootUpAnimation.SetTrigger("playAnim");
			if (playColdOpenCinematic)
			{
				yield return new WaitForSeconds(1.5f);
				SceneManager.LoadScene("ColdOpen1");
				yield break;
			}
			yield return new WaitForSeconds(3f);
		}
		yield return new WaitForSeconds(0.2f);
		SceneManager.LoadScene("MainMenu");
	}

	private void Start()
	{
		StartCoroutine(SendToNextScene());
	}
}
