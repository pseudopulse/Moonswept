using GameNetcodeStuff;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.AI;

[RequireComponent(typeof(InteractTrigger))]
public class DoorLock : NetworkBehaviour
{
	private InteractTrigger doorTrigger;

	public float maxTimeLeft = 60f;

	public float lockPickTimeLeft = 60f;

	public bool isLocked;

	public bool isPickingLock;

	[Space(5f)]
	public DoorLock twinDoor;

	public Transform lockPickerPosition;

	public Transform lockPickerPosition2;

	private float enemyDoorMeter;

	private bool isDoorOpened;

	private NavMeshObstacle navMeshObstacle;

	public AudioClip pickingLockSFX;

	public AudioClip unlockSFX;

	public AudioSource doorLockSFX;

	private bool displayedLockTip;

	private bool localPlayerPickingLock;

	private int playersPickingDoor;

	private float playerPickingLockProgress;

	public void Awake()
	{
		doorTrigger = base.gameObject.GetComponent<InteractTrigger>();
		lockPickTimeLeft = maxTimeLeft;
		navMeshObstacle = GetComponent<NavMeshObstacle>();
	}

	public void OnHoldInteract()
	{
		if (isLocked && !displayedLockTip && HUDManager.Instance.holdFillAmount / doorTrigger.timeToHold > 0.3f)
		{
			displayedLockTip = true;
			HUDManager.Instance.DisplayTip("TIP:", "To get through locked doors efficiently, order a <u>lock-picker</u> from the ship terminal.", isWarning: false, useSave: true, "LCTip_Autopicker");
		}
	}

	public void LockDoor(float timeToLockPick = 30f)
	{
		doorTrigger.interactable = false;
		doorTrigger.timeToHold = timeToLockPick;
		doorTrigger.hoverTip = "Locked (pickable)";
		doorTrigger.holdTip = "Picking lock";
		isLocked = true;
		navMeshObstacle.carving = true;
		navMeshObstacle.carveOnlyStationary = true;
		if (twinDoor != null)
		{
			twinDoor.doorTrigger.interactable = false;
			twinDoor.doorTrigger.timeToHold = 35f;
			twinDoor.doorTrigger.hoverTip = "Locked (pickable)";
			twinDoor.doorTrigger.holdTip = "Picking lock";
			twinDoor.isLocked = true;
		}
	}

	public void UnlockDoor()
	{
		doorLockSFX.Stop();
		doorLockSFX.PlayOneShot(unlockSFX);
		navMeshObstacle.carving = false;
		if (isLocked)
		{
			doorTrigger.interactable = true;
			doorTrigger.hoverTip = "Use door : [LMB]";
			doorTrigger.holdTip = "";
			isPickingLock = false;
			isLocked = false;
			doorTrigger.timeToHoldSpeedMultiplier = 1f;
			navMeshObstacle.carving = false;
			Debug.Log("Unlocking door");
			doorTrigger.timeToHold = 0.3f;
		}
	}

	public void UnlockDoorSyncWithServer()
	{
		if (isLocked)
		{
			UnlockDoor();
			UnlockDoorServerRpc();
		}
	}

	[ServerRpc(RequireOwnership = false)]
	public void UnlockDoorServerRpc()
			{
				UnlockDoorClientRpc();
			}

	[ClientRpc]
	public void UnlockDoorClientRpc()
			{
				UnlockDoor();
			}

	private void Update()
	{
		if (isLocked)
		{
			if (GameNetworkManager.Instance == null || GameNetworkManager.Instance.localPlayerController == null)
			{
				return;
			}
			if (GameNetworkManager.Instance.localPlayerController.currentlyHeldObjectServer != null && GameNetworkManager.Instance.localPlayerController.currentlyHeldObjectServer.itemProperties.itemId == 14)
			{
				if (StartOfRound.Instance.localPlayerUsingController)
				{
					doorTrigger.disabledHoverTip = "Use key: [R-trigger]";
				}
				else
				{
					doorTrigger.disabledHoverTip = "Use key: [ LMB ]";
				}
			}
			else
			{
				doorTrigger.disabledHoverTip = "Locked";
			}
			if (playersPickingDoor > 0)
			{
				playerPickingLockProgress = Mathf.Clamp(playerPickingLockProgress + (float)playersPickingDoor * 0.85f * Time.deltaTime, 1f, 3.5f);
			}
			doorTrigger.timeToHoldSpeedMultiplier = Mathf.Clamp((float)playersPickingDoor * 0.85f, 1f, 3.5f);
		}
		else
		{
			navMeshObstacle.carving = false;
		}
		if (isLocked && isPickingLock)
		{
			lockPickTimeLeft -= Time.deltaTime;
			doorTrigger.disabledHoverTip = $"Picking lock: {(int)lockPickTimeLeft} sec.";
			if (base.IsServer && lockPickTimeLeft < 0f)
			{
				UnlockDoor();
				UnlockDoorServerRpc();
			}
		}
	}

	private void OnTriggerStay(Collider other)
	{
		if (NetworkManager.Singleton == null || !base.IsServer || isLocked || isDoorOpened || !other.CompareTag("Enemy"))
		{
			return;
		}
		EnemyAICollisionDetect component = other.GetComponent<EnemyAICollisionDetect>();
		if (!(component == null))
		{
			enemyDoorMeter += Time.deltaTime * component.mainScript.openDoorSpeedMultiplier;
			if (enemyDoorMeter > 1f)
			{
				enemyDoorMeter = 0f;
				base.gameObject.GetComponent<AnimatedObjectTrigger>().TriggerAnimationNonPlayer(component.mainScript.useSecondaryAudiosOnAnimatedObjects, overrideBool: true);
				OpenDoorAsEnemyServerRpc();
			}
		}
	}

	public void OpenOrCloseDoor(PlayerControllerB playerWhoTriggered)
	{
		AnimatedObjectTrigger component = base.gameObject.GetComponent<AnimatedObjectTrigger>();
		component.TriggerAnimation(playerWhoTriggered);
		isDoorOpened = component.boolValue;
		navMeshObstacle.enabled = !component.boolValue;
	}

	public void SetDoorAsOpen(bool isOpen)
	{
		isDoorOpened = isOpen;
		navMeshObstacle.enabled = !isOpen;
	}

	public void OpenDoorAsEnemy()
	{
		isDoorOpened = true;
		navMeshObstacle.enabled = false;
	}

	[ServerRpc(RequireOwnership = false)]
	public void OpenDoorAsEnemyServerRpc()
			{
				OpenDoorAsEnemyClientRpc();
			}

	[ClientRpc]
	public void OpenDoorAsEnemyClientRpc()
			{
				OpenDoorAsEnemy();
			}

	public void TryPickingLock()
	{
		if (isLocked)
		{
			HUDManager.Instance.holdFillAmount = playerPickingLockProgress;
			if (!localPlayerPickingLock)
			{
				localPlayerPickingLock = true;
				PlayerPickLockServerRpc();
			}
		}
	}

	public void StopPickingLock()
	{
		if (localPlayerPickingLock)
		{
			localPlayerPickingLock = false;
			if (playersPickingDoor == 1)
			{
				playerPickingLockProgress = Mathf.Clamp(playerPickingLockProgress - 1f, 0f, 45f);
			}
			PlayerStopPickingLockServerRpc();
		}
	}

	[ServerRpc(RequireOwnership = false)]
	public void PlayerStopPickingLockServerRpc()
			{
				PlayerStopPickingLockClientRpc();
			}

	[ClientRpc]
	public void PlayerStopPickingLockClientRpc()
			{
				doorLockSFX.Stop();
				playersPickingDoor = Mathf.Clamp(playersPickingDoor - 1, 0, 4);
			}

	[ServerRpc(RequireOwnership = false)]
	public void PlayerPickLockServerRpc()
			{
				PlayerPickLockClientRpc();
			}

	[ClientRpc]
	public void PlayerPickLockClientRpc()
			{
				doorLockSFX.clip = pickingLockSFX;
				doorLockSFX.Play();
				playersPickingDoor = Mathf.Clamp(playersPickingDoor + 1, 0, 4);
			}
}
