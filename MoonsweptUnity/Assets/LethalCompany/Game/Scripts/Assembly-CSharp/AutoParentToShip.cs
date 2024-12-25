using System.Collections;
using Unity.Netcode;
using UnityEngine;

public class AutoParentToShip : NetworkBehaviour
{
	public bool disableObject;

	public Vector3 positionOffset;

	public Vector3 rotationOffset;

	[HideInInspector]
	public Vector3 startingPosition;

	[HideInInspector]
	public Vector3 startingRotation;

	public bool overrideOffset;

	private void Awake()
	{
		if (!overrideOffset)
		{
			positionOffset = StartOfRound.Instance.elevatorTransform.InverseTransformPoint(base.transform.position);
			rotationOffset = StartOfRound.Instance.elevatorTransform.InverseTransformDirection(base.transform.eulerAngles);
		}
		MoveToOffset();
		PlaceableShipObject component = base.gameObject.GetComponent<PlaceableShipObject>();
		if (component != null && component.parentObjectSecondary != null)
		{
			startingPosition = component.parentObjectSecondary.position;
			startingRotation = component.parentObjectSecondary.eulerAngles;
		}
		else
		{
			startingPosition = positionOffset;
			startingRotation = rotationOffset;
		}
	}

	private void LateUpdate()
	{
		if (!StartOfRound.Instance.suckingFurnitureOutOfShip)
		{
			if (disableObject)
			{
				base.transform.position = new Vector3(800f, -100f, 0f);
			}
			else
			{
				MoveToOffset();
			}
		}
	}

	public void StartSuckingOutOfShip()
	{
		StartCoroutine(SuckObjectOutOfShip());
	}

	private IEnumerator SuckObjectOutOfShip()
	{
		Vector3 dir = Vector3.Normalize((StartOfRound.Instance.middleOfSpaceNode.position - base.transform.position) * 10000f);
		Debug.Log(dir);
		Quaternion randomRotation = Random.rotation;
		while (StartOfRound.Instance.suckingFurnitureOutOfShip)
		{
			yield return null;
			base.transform.position = base.transform.position + dir * (Time.deltaTime * Mathf.Clamp(StartOfRound.Instance.suckingPower, 1.1f, 100f) * 17f);
			base.transform.rotation = Quaternion.Lerp(base.transform.rotation, base.transform.rotation * randomRotation, Time.deltaTime * StartOfRound.Instance.suckingPower);
			Debug.DrawRay(base.transform.position + Vector3.up * 0.2f, StartOfRound.Instance.middleOfSpaceNode.position - base.transform.position, Color.blue);
			Debug.DrawRay(base.transform.position, dir, Color.green);
		}
	}

	public void MoveToOffset()
	{
		base.transform.rotation = StartOfRound.Instance.elevatorTransform.rotation;
		base.transform.Rotate(rotationOffset);
		base.transform.position = StartOfRound.Instance.elevatorTransform.position;
		Vector3 vector = positionOffset;
		vector = StartOfRound.Instance.elevatorTransform.rotation * vector;
		base.transform.position += vector;
	}
}
