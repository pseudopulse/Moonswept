using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

public class ColdOpenCinematicCutscene : MonoBehaviour
{
	public Camera cam;

	public Transform camContainer;

	public Transform camTarget;

	private InputActionAsset inputAsset;

	public float cameraUp;

	private float startInputTimer;

	public Animator cameraAnimator;

	private void TurnCamera(Vector2 input)
	{
		input = input * 0.008f * IngamePlayerSettings.Instance.settings.lookSensitivity;
		camTarget.Rotate(Vector3.up, input.x);
		cameraUp -= input.y;
		cameraUp = Mathf.Clamp(cameraUp, -60f, 40f);
		camTarget.transform.localEulerAngles = new Vector3(cameraUp, camTarget.transform.localEulerAngles.y, camTarget.transform.localEulerAngles.z);
		camTarget.eulerAngles = new Vector3(camTarget.eulerAngles.x, camTarget.eulerAngles.y, 0f);
		camContainer.transform.rotation = Quaternion.Lerp(camContainer.transform.rotation, camTarget.rotation, 12f * Time.deltaTime);
	}

	public void Start()
	{
		inputAsset = IngamePlayerSettings.Instance.playerInput.actions;
		Cursor.visible = false;
		Cursor.lockState = CursorLockMode.Locked;
	}

	public void Update()
	{
		if (inputAsset == null)
		{
			Debug.LogError("Input asset not found!");
			return;
		}
		startInputTimer += Time.deltaTime;
		if (startInputTimer > 0.5f)
		{
			TurnCamera(inputAsset.FindAction("Look").ReadValue<Vector2>());
		}
	}

	public void ShakeCameraSmall()
	{
		cameraAnimator.SetTrigger("shake");
	}

	public void ShakeCameraLong()
	{
		cameraAnimator.SetTrigger("vibrateLong");
	}

	public void EndColdOpenCutscene()
	{
		Cursor.visible = true;
		Cursor.lockState = CursorLockMode.None;
		SceneManager.LoadScene("MainMenu");
	}
}
