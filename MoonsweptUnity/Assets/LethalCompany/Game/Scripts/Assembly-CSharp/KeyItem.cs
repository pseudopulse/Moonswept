using UnityEngine;

public class KeyItem : GrabbableObject
{
	public override void ItemActivate(bool used, bool buttonDown = true)
	{
		if (!(playerHeldBy == null) && base.IsOwner && Physics.Raycast(new Ray(playerHeldBy.gameplayCamera.transform.position, playerHeldBy.gameplayCamera.transform.forward), out var hitInfo, 3f, 2816))
		{
			DoorLock component = hitInfo.transform.GetComponent<DoorLock>();
			if (component != null && component.isLocked && !component.isPickingLock)
			{
				component.UnlockDoorSyncWithServer();
				playerHeldBy.DespawnHeldObject();
			}
		}
	}
}
