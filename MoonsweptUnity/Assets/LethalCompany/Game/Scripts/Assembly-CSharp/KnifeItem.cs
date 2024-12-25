using System;
using System.Collections.Generic;
using System.Linq;
using GameNetcodeStuff;
using Unity.Netcode;
using UnityEngine;

public class KnifeItem : GrabbableObject
{
	public AudioSource knifeAudio;

	private List<RaycastHit> objectsHitByKnifeList = new List<RaycastHit>();

	public PlayerControllerB previousPlayerHeldBy;

	private RaycastHit[] objectsHitByKnife;

	public int knifeHitForce;

	public AudioClip[] hitSFX;

	public AudioClip[] swingSFX;

	private int knifeMask = 11012424;

	private float timeAtLastDamageDealt;

	public ParticleSystem bloodParticle;

	public override void ItemActivate(bool used, bool buttonDown = true)
	{
		RoundManager.PlayRandomClip(knifeAudio, swingSFX);
		if (playerHeldBy != null)
		{
			previousPlayerHeldBy = playerHeldBy;
			if (playerHeldBy.IsOwner)
			{
				playerHeldBy.playerBodyAnimator.SetTrigger("UseHeldItem1");
			}
		}
		if (base.IsOwner)
		{
			HitKnife();
		}
	}

	public override void PocketItem()
	{
		base.PocketItem();
	}

	public override void DiscardItem()
	{
		base.DiscardItem();
	}

	public override void EquipItem()
	{
		base.EquipItem();
	}

	public void HitKnife(bool cancel = false)
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
			objectsHitByKnife = Physics.SphereCastAll(previousPlayerHeldBy.gameplayCamera.transform.position + previousPlayerHeldBy.gameplayCamera.transform.right * 0.1f, 0.3f, previousPlayerHeldBy.gameplayCamera.transform.forward, 0.75f, knifeMask, QueryTriggerInteraction.Collide);
			objectsHitByKnifeList = objectsHitByKnife.OrderBy((RaycastHit x) => x.distance).ToList();
			for (int i = 0; i < objectsHitByKnifeList.Count; i++)
			{
				if (objectsHitByKnifeList[i].transform.gameObject.layer == 8 || objectsHitByKnifeList[i].transform.gameObject.layer == 11)
				{
					flag = true;
					string text = objectsHitByKnifeList[i].collider.gameObject.tag;
					for (int j = 0; j < StartOfRound.Instance.footstepSurfaces.Length; j++)
					{
						if (StartOfRound.Instance.footstepSurfaces[j].surfaceTag == text)
						{
							num = j;
							break;
						}
					}
				}
				else
				{
					if (!objectsHitByKnifeList[i].transform.TryGetComponent<IHittable>(out var component) || objectsHitByKnifeList[i].transform == previousPlayerHeldBy.transform || (!(objectsHitByKnifeList[i].point == Vector3.zero) && Physics.Linecast(previousPlayerHeldBy.gameplayCamera.transform.position, objectsHitByKnifeList[i].point, out var _, StartOfRound.Instance.collidersAndRoomMaskAndDefault)))
					{
						continue;
					}
					flag = true;
					Vector3 forward = previousPlayerHeldBy.gameplayCamera.transform.forward;
					try
					{
						if (Time.realtimeSinceStartup - timeAtLastDamageDealt > 0.43f)
						{
							timeAtLastDamageDealt = Time.realtimeSinceStartup;
							component.Hit(knifeHitForce, forward, previousPlayerHeldBy, playHitSFX: true, 5);
							bloodParticle.Play(withChildren: true);
						}
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
			RoundManager.PlayRandomClip(knifeAudio, hitSFX);
			UnityEngine.Object.FindObjectOfType<RoundManager>().PlayAudibleNoise(base.transform.position, 17f, 0.8f);
			if (!flag2 && num != -1)
			{
				knifeAudio.PlayOneShot(StartOfRound.Instance.footstepSurfaces[num].hitSurfaceSFX);
				WalkieTalkie.TransmitOneShotAudio(knifeAudio, StartOfRound.Instance.footstepSurfaces[num].hitSurfaceSFX);
			}
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
			RoundManager.PlayRandomClip(knifeAudio, hitSFX);
			if (hitSurfaceID != -1)
			{
				HitSurfaceWithKnife(hitSurfaceID);
			}
		}
}
	private void HitSurfaceWithKnife(int hitSurfaceID)
	{
		knifeAudio.PlayOneShot(StartOfRound.Instance.footstepSurfaces[hitSurfaceID].hitSurfaceSFX);
		WalkieTalkie.TransmitOneShotAudio(knifeAudio, StartOfRound.Instance.footstepSurfaces[hitSurfaceID].hitSurfaceSFX);
	}
}
