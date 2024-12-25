using System.Collections;
using System.Collections.Generic;
using System.Linq;
using GameNetcodeStuff;
using Unity.Netcode;
using UnityEngine;

public class ManualCameraRenderer : NetworkBehaviour
{
	public Camera cam;

	public CameraView[] cameraViews;

	public int cameraViewIndex;

	public bool currentCameraDisabled;

	[Space(5f)]
	public MeshRenderer mesh;

	public Material offScreenMat;

	public Material onScreenMat;

	public int materialIndex;

	private bool isScreenOn;

	public bool overrideCameraForOtherUse;

	public bool renderAtLowerFramerate;

	public float fps = 60f;

	private float elapsed;

	public PlayerControllerB targetedPlayer;

	public List<TransformAndName> radarTargets = new List<TransformAndName>();

	public int targetTransformIndex;

	public Camera mapCamera;

	public Light mapCameraLight;

	public Animator mapCameraAnimator;

	private bool mapCameraMaxFramerate;

	private Coroutine updateMapCameraCoroutine;

	private bool syncingTargetPlayer;

	private bool syncingSwitchScreen;

	private bool screenEnabledOnLocalClient;

	private Vector3 targetDeathPosition;

	public Transform mapCameraStationaryUI;

	public Transform shipArrowPointer;

	public GameObject shipArrowUI;

	private void Start()
	{
		if (cam == null)
		{
			cam = GetComponent<Camera>();
		}
		if (!isScreenOn)
		{
			cam.enabled = false;
		}
		targetDeathPosition = new Vector3(0f, -100f, 0f);
	}

	private void Awake()
	{
		for (int i = 0; i < StartOfRound.Instance.allPlayerScripts.Length; i++)
		{
			radarTargets.Add(new TransformAndName(StartOfRound.Instance.allPlayerScripts[i].transform, StartOfRound.Instance.allPlayerScripts[i].playerUsername));
		}
		targetTransformIndex = 0;
		targetedPlayer = StartOfRound.Instance.allPlayerScripts[0];
	}

	public void SwitchScreenButton()
	{
		bool on = !isScreenOn;
		SwitchScreenOn(on);
		syncingSwitchScreen = true;
		SwitchScreenOnServerRpc(on);
	}

	public void SwitchScreenOn(bool on = true)
	{
		isScreenOn = on;
		currentCameraDisabled = !on;
		Material[] sharedMaterials = mesh.sharedMaterials;
		if (on)
		{
			sharedMaterials[materialIndex] = onScreenMat;
			mapCameraAnimator.SetTrigger("Transition");
		}
		else
		{
			sharedMaterials[materialIndex] = offScreenMat;
		}
		mesh.sharedMaterials = sharedMaterials;
	}

	[ServerRpc(RequireOwnership = false)]
	public void SwitchScreenOnServerRpc(bool on)
			{
				SwitchScreenOnClientRpc(on);
			}

	[ClientRpc]
	public void SwitchScreenOnClientRpc(bool on)
{		{
			if (syncingSwitchScreen)
			{
				syncingSwitchScreen = false;
			}
			else
			{
				SwitchScreenOn(on);
			}
		}
}
	public void SwitchCameraView(bool switchForward = true, int switchToView = -1)
	{
		cam.enabled = false;
		cameraViewIndex = (cameraViewIndex + 1) % cameraViews.Length;
		cam = cameraViews[cameraViewIndex].camera;
		onScreenMat = cameraViews[cameraViewIndex].cameraMaterial;
	}

	public string AddTransformAsTargetToRadar(Transform newTargetTransform, string targetName, bool isNonPlayer = false)
	{
		int num = 0;
		for (int i = 0; i < radarTargets.Count; i++)
		{
			if (radarTargets[i].transform == newTargetTransform)
			{
				return null;
			}
			if (radarTargets[i].name == targetName)
			{
				num++;
			}
		}
		if (num != 0)
		{
			targetName += num + 1;
		}
		if (!newTargetTransform.GetComponent<NetworkObject>())
		{
			return null;
		}
		radarTargets.Add(new TransformAndName(newTargetTransform, targetName, isNonPlayer));
		return targetName;
	}

	public void ChangeNameOfTargetTransform(Transform t, string newName)
	{
		for (int i = 0; i < radarTargets.Count; i++)
		{
			if (radarTargets[i].transform == t)
			{
				radarTargets[i].name = newName;
			}
		}
	}

	public void SyncOrderOfRadarBoostersInList()
	{
		radarTargets = radarTargets.OrderBy((TransformAndName x) => x.transform.gameObject.GetComponent<NetworkObject>().NetworkObjectId).ToList();
	}

	public void FlashRadarBooster(int targetId)
	{
		if (targetId < radarTargets.Count && radarTargets[targetId].isNonPlayer)
		{
			RadarBoosterItem component = radarTargets[targetId].transform.gameObject.GetComponent<RadarBoosterItem>();
			if (component != null)
			{
				component.FlashAndSync();
			}
		}
	}

	public void PingRadarBooster(int targetId)
	{
		if (targetId < radarTargets.Count && radarTargets[targetId].isNonPlayer)
		{
			RadarBoosterItem component = radarTargets[targetId].transform.gameObject.GetComponent<RadarBoosterItem>();
			if (component != null)
			{
				component.PlayPingAudioAndSync();
			}
		}
	}

	public void RemoveTargetFromRadar(Transform removeTransform)
	{
		for (int i = 0; i < radarTargets.Count; i++)
		{
			if (radarTargets[i].transform == removeTransform)
			{
				radarTargets.RemoveAt(i);
				if (targetTransformIndex >= radarTargets.Count)
				{
					targetTransformIndex--;
					SwitchRadarTargetForward(callRPC: false);
				}
			}
		}
	}

	public void SwitchRadarTargetForward(bool callRPC)
	{
		if (updateMapCameraCoroutine != null)
		{
			StopCoroutine(updateMapCameraCoroutine);
		}
		updateMapCameraCoroutine = StartCoroutine(updateMapTarget(GetRadarTargetIndexPlusOne(targetTransformIndex), !callRPC));
	}

	public void SwitchRadarTargetAndSync(int switchToIndex)
	{
		if (radarTargets.Count > switchToIndex)
		{
			if (updateMapCameraCoroutine != null)
			{
				StopCoroutine(updateMapCameraCoroutine);
			}
			updateMapCameraCoroutine = StartCoroutine(updateMapTarget(switchToIndex, calledFromRPC: false));
		}
	}

	private int GetRadarTargetIndexPlusOne(int index)
	{
		return (index + 1) % radarTargets.Count;
	}

	private int GetRadarTargetIndexMinusOne(int index)
	{
		if (index - 1 < 0)
		{
			return radarTargets.Count - 1;
		}
		return index - 1;
	}

	private IEnumerator updateMapTarget(int setRadarTargetIndex, bool calledFromRPC = true)
	{
		if (screenEnabledOnLocalClient)
		{
			mapCameraMaxFramerate = true;
			mapCameraAnimator.SetTrigger("Transition");
		}
		yield return new WaitForSeconds(0.035f);
		if (radarTargets.Count <= setRadarTargetIndex)
		{
			setRadarTargetIndex = radarTargets.Count - 1;
		}
		PlayerControllerB component = radarTargets[setRadarTargetIndex].transform.gameObject.GetComponent<PlayerControllerB>();
		if (!calledFromRPC)
		{
			for (int i = 0; i < radarTargets.Count; i++)
			{
				Debug.Log($"radar target index {i}");
				if (radarTargets[setRadarTargetIndex] == null)
				{
					setRadarTargetIndex = GetRadarTargetIndexPlusOne(setRadarTargetIndex);
					continue;
				}
				component = radarTargets[setRadarTargetIndex].transform.gameObject.GetComponent<PlayerControllerB>();
				if (!(component != null) || component.isPlayerControlled || component.isPlayerDead || !(component.redirectToEnemy == null))
				{
					break;
				}
				setRadarTargetIndex = GetRadarTargetIndexPlusOne(setRadarTargetIndex);
			}
		}
		if (radarTargets[setRadarTargetIndex] == null)
		{
			Debug.Log($"Radar attempted to target object which doesn't exist; index {setRadarTargetIndex}");
			yield break;
		}
		targetTransformIndex = setRadarTargetIndex;
		targetedPlayer = component;
		StartOfRound.Instance.mapScreenPlayerName.text = "MONITORING: " + radarTargets[targetTransformIndex].name;
		mapCameraMaxFramerate = false;
		if (!calledFromRPC)
		{
			SwitchRadarTargetServerRpc(targetTransformIndex);
		}
	}

	[ServerRpc(RequireOwnership = false)]
	public void SwitchRadarTargetServerRpc(int targetIndex)
			{
				SwitchRadarTargetClientRpc(targetIndex);
			}

	[ClientRpc]
	public void SwitchRadarTargetClientRpc(int switchToIndex)
{		if (syncingTargetPlayer)
		{
			syncingTargetPlayer = false;
		}
		else
		{
			if (radarTargets.Count <= switchToIndex)
			{
				return;
			}
			if (!isScreenOn)
			{
				if (switchToIndex == -1)
				{
					return;
				}
				SwitchScreenOn();
			}
			if (updateMapCameraCoroutine != null)
			{
				StopCoroutine(updateMapCameraCoroutine);
			}
			updateMapCameraCoroutine = StartCoroutine(updateMapTarget(switchToIndex));
		}
}
	private void MapCameraFocusOnPosition(Vector3 pos)
	{
		if (!(GameNetworkManager.Instance.localPlayerController == null))
		{
			bool flag = radarTargets[targetTransformIndex].transform.position.y < -80f;
			if (mapCameraLight != null)
			{
				mapCameraLight.enabled = flag && !GameNetworkManager.Instance.localPlayerController.isPlayerDead && GameNetworkManager.Instance.localPlayerController.isInHangarShipRoom;
			}
			if (targetedPlayer != null && targetedPlayer.isInHangarShipRoom)
			{
				mapCamera.nearClipPlane = -0.96f;
				StartOfRound.Instance.radarCanvas.planeDistance = -0.93f;
			}
			else
			{
				mapCamera.nearClipPlane = -2.47f;
				StartOfRound.Instance.radarCanvas.planeDistance = -2.4f;
			}
			mapCamera.transform.position = new Vector3(pos.x, pos.y + 3.636f, pos.z);
		}
	}

	private void Update()
	{
		if (GameNetworkManager.Instance.localPlayerController == null || NetworkManager.Singleton == null)
		{
			return;
		}
		if (overrideCameraForOtherUse)
		{
			if (shipArrowUI != null)
			{
				shipArrowUI.SetActive(value: false);
			}
			return;
		}
		if (cam == mapCamera)
		{
			if (radarTargets[targetTransformIndex].transform == null)
			{
				mapCameraLight.enabled = false;
			}
			if (targetedPlayer != null)
			{
				if (targetedPlayer.isPlayerDead)
				{
					if ((bool)targetedPlayer.redirectToEnemy)
					{
						MapCameraFocusOnPosition(targetedPlayer.redirectToEnemy.transform.position);
					}
					else if (targetedPlayer.deadBody != null)
					{
						MapCameraFocusOnPosition(targetedPlayer.deadBody.transform.position);
						targetDeathPosition = targetedPlayer.deadBody.spawnPosition;
					}
					else
					{
						MapCameraFocusOnPosition(targetedPlayer.placeOfDeath);
					}
				}
				else
				{
					MapCameraFocusOnPosition(targetedPlayer.transform.position);
				}
			}
			else
			{
				MapCameraFocusOnPosition(radarTargets[targetTransformIndex].transform.position);
			}
			if (mapCameraLight != null && mapCameraLight.transform.position.y > -80f)
			{
				mapCameraLight.enabled = false;
			}
			if (mapCameraMaxFramerate)
			{
				mapCamera.enabled = true;
				return;
			}
		}
		PlayerControllerB player = ((!GameNetworkManager.Instance.localPlayerController.isPlayerDead || !(GameNetworkManager.Instance.localPlayerController.spectatedPlayerScript != null)) ? GameNetworkManager.Instance.localPlayerController : GameNetworkManager.Instance.localPlayerController.spectatedPlayerScript);
		if (!MeetsCameraEnabledConditions(player))
		{
			screenEnabledOnLocalClient = false;
			cam.enabled = false;
			return;
		}
		if (cam == mapCamera && radarTargets[targetTransformIndex].transform != null)
		{
			if (!(radarTargets[targetTransformIndex].transform.position.y < -80f) && Vector3.Distance(radarTargets[targetTransformIndex].transform.position, StartOfRound.Instance.elevatorTransform.transform.position) > 16f)
			{
				shipArrowPointer.LookAt(StartOfRound.Instance.elevatorTransform);
				shipArrowPointer.eulerAngles = new Vector3(0f, shipArrowPointer.eulerAngles.y, 0f);
				shipArrowUI.SetActive(value: true);
			}
			else
			{
				shipArrowUI.SetActive(value: false);
			}
		}
		screenEnabledOnLocalClient = true;
		if (renderAtLowerFramerate)
		{
			cam.enabled = false;
			elapsed += Time.deltaTime;
			if (elapsed > 1f / fps)
			{
				elapsed = 0f;
				cam.Render();
			}
		}
		else
		{
			cam.enabled = true;
		}
	}

	private bool MeetsCameraEnabledConditions(PlayerControllerB player)
	{
		if (currentCameraDisabled)
		{
			return false;
		}
		if (mesh != null && !mesh.isVisible)
		{
			return false;
		}
		if (!StartOfRound.Instance.inShipPhase && (!player.isInHangarShipRoom || (!StartOfRound.Instance.shipDoorsEnabled && (StartOfRound.Instance.currentPlanetPrefab == null || !StartOfRound.Instance.currentPlanetPrefab.activeSelf))))
		{
			return false;
		}
		return true;
	}
}
