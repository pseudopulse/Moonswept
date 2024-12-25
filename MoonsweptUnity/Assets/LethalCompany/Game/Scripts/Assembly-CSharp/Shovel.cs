using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using GameNetcodeStuff;
using Unity.Netcode;
using UnityEngine;

public class Shovel : GrabbableObject
{
	public int shovelHitForce = 1;

	public bool reelingUp;

	public bool isHoldingButton;

	private RaycastHit rayHit;

	private Coroutine reelingUpCoroutine;

	private RaycastHit[] objectsHitByShovel;

	private List<RaycastHit> objectsHitByShovelList = new List<RaycastHit>();

	public AudioClip reelUp;

	public AudioClip swing;

	public AudioClip[] hitSFX;

	public AudioSource shovelAudio;

	private PlayerControllerB previousPlayerHeldBy;

	private int shovelMask = 11012424;

	public override void ItemActivate(bool used, bool buttonDown = true)
	{
		if (playerHeldBy == null)
		{
			return;
		}
		isHoldingButton = buttonDown;
		if (!reelingUp && buttonDown)
		{
			reelingUp = true;
			previousPlayerHeldBy = playerHeldBy;
			if (reelingUpCoroutine != null)
			{
				StopCoroutine(reelingUpCoroutine);
			}
			reelingUpCoroutine = StartCoroutine(reelUpShovel());
		}
	}

	private IEnumerator reelUpShovel()
	{
		playerHeldBy.activatingItem = true;
		playerHeldBy.twoHanded = true;
		playerHeldBy.playerBodyAnimator.ResetTrigger("shovelHit");
		playerHeldBy.playerBodyAnimator.SetBool("reelingUp", value: true);
		shovelAudio.PlayOneShot(reelUp);
		ReelUpSFXServerRpc();
		yield return new WaitForSeconds(0.35f);
		yield return new WaitUntil(() => !isHoldingButton || !isHeld);
		SwingShovel(!isHeld);
		yield return new WaitForSeconds(0.13f);
		yield return new WaitForEndOfFrame();
		HitShovel(!isHeld);
		yield return new WaitForSeconds(0.3f);
		reelingUp = false;
		reelingUpCoroutine = null;
	}

	[ServerRpc]
	public void ReelUpSFXServerRpc()
{		{
			ReelUpSFXClientRpc();
		}
}
	[ClientRpc]
	public void ReelUpSFXClientRpc()
{if(!base.IsOwner)			{
				shovelAudio.PlayOneShot(reelUp);
			}
}
	public override void DiscardItem()
	{
		if (playerHeldBy != null)
		{
			playerHeldBy.activatingItem = false;
		}
		base.DiscardItem();
	}

	public void SwingShovel(bool cancel = false)
	{
		previousPlayerHeldBy.playerBodyAnimator.SetBool("reelingUp", value: false);
		if (!cancel)
		{
			shovelAudio.PlayOneShot(swing);
			previousPlayerHeldBy.UpdateSpecialAnimationValue(specialAnimation: true, (short)previousPlayerHeldBy.transform.localEulerAngles.y, 0.4f);
		}
	}

	public void HitShovel(bool cancel = false)
	{
		if (previousPlayerHeldBy == null)
		{
			Debug.LogError("Previousplayerheldby is null on this client when HitShovel is called.");
			return;
		}
		previousPlayerHeldBy.activatingItem = false;
		bool flag = false;
		bool flag2 = false;
		int num = -1;
		if (!cancel)
		{
			previousPlayerHeldBy.twoHanded = false;
			objectsHitByShovel = Physics.SphereCastAll(previousPlayerHeldBy.gameplayCamera.transform.position + previousPlayerHeldBy.gameplayCamera.transform.right * -0.35f, 0.8f, previousPlayerHeldBy.gameplayCamera.transform.forward, 1.5f, shovelMask, QueryTriggerInteraction.Collide);
			objectsHitByShovelList = objectsHitByShovel.OrderBy((RaycastHit x) => x.distance).ToList();
			for (int i = 0; i < objectsHitByShovelList.Count; i++)
			{
				IHittable component;
				RaycastHit hitInfo;
				if (objectsHitByShovelList[i].transform.gameObject.layer == 8 || objectsHitByShovelList[i].transform.gameObject.layer == 11)
				{
					flag = true;
					string text = objectsHitByShovelList[i].collider.gameObject.tag;
					for (int j = 0; j < StartOfRound.Instance.footstepSurfaces.Length; j++)
					{
						if (StartOfRound.Instance.footstepSurfaces[j].surfaceTag == text)
						{
							num = j;
							break;
						}
					}
				}
				else if (objectsHitByShovelList[i].transform.TryGetComponent<IHittable>(out component) && !(objectsHitByShovelList[i].transform == previousPlayerHeldBy.transform) && (objectsHitByShovelList[i].point == Vector3.zero || !Physics.Linecast(previousPlayerHeldBy.gameplayCamera.transform.position, objectsHitByShovelList[i].point, out hitInfo, StartOfRound.Instance.collidersAndRoomMaskAndDefault)))
				{
					flag = true;
					Vector3 forward = previousPlayerHeldBy.gameplayCamera.transform.forward;
					try
					{
						component.Hit(shovelHitForce, forward, previousPlayerHeldBy, playHitSFX: true, 1);
						flag2 = true;
					}
					catch (Exception arg)
					{
						Debug.Log($"Exception caught when hitting object with shovel from player #{previousPlayerHeldBy.playerClientId}: {arg}");
					}
				}
			}
		}
		if (flag)
		{
			RoundManager.PlayRandomClip(shovelAudio, hitSFX);
			UnityEngine.Object.FindObjectOfType<RoundManager>().PlayAudibleNoise(base.transform.position, 17f, 0.8f);
			if (!flag2 && num != -1)
			{
				shovelAudio.PlayOneShot(StartOfRound.Instance.footstepSurfaces[num].hitSurfaceSFX);
				WalkieTalkie.TransmitOneShotAudio(shovelAudio, StartOfRound.Instance.footstepSurfaces[num].hitSurfaceSFX);
			}
			playerHeldBy.playerBodyAnimator.SetTrigger("shovelHit");
			HitShovelServerRpc(num);
		}
	}

	[ServerRpc]
	public void HitShovelServerRpc(int hitSurfaceID)
{		{
			HitShovelClientRpc(hitSurfaceID);
		}
}
	[ClientRpc]
	public void HitShovelClientRpc(int hitSurfaceID)
{if(!base.IsOwner)		{
			RoundManager.PlayRandomClip(shovelAudio, hitSFX);
			if (hitSurfaceID != -1)
			{
				HitSurfaceWithShovel(hitSurfaceID);
			}
		}
}
	private void HitSurfaceWithShovel(int hitSurfaceID)
	{
		shovelAudio.PlayOneShot(StartOfRound.Instance.footstepSurfaces[hitSurfaceID].hitSurfaceSFX);
		WalkieTalkie.TransmitOneShotAudio(shovelAudio, StartOfRound.Instance.footstepSurfaces[hitSurfaceID].hitSurfaceSFX);
	}
}
