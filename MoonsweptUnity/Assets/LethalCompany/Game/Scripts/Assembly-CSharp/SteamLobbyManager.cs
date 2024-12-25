using System.Collections;
using System.Linq;
using Steamworks;
using Steamworks.Data;
using Steamworks.ServerList;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class SteamLobbyManager : MonoBehaviour
{
	private Internet Request;

	private Lobby[] currentLobbyList;

	public TextMeshProUGUI serverListBlankText;

	public Transform levelListContainer;

	public GameObject LobbySlotPrefab;

	public GameObject LobbySlotPrefabChallenge;

	private float lobbySlotPositionOffset;

	public int sortByDistanceSetting = 2;

	private float refreshServerListTimer;

	public bool censorOffensiveLobbyNames = true;

	private Coroutine loadLobbyListCoroutine;

	public UnityEngine.UI.Image sortWithChallengeMoonsCheckbox;

	private bool sortWithChallengeMoons = true;

	public TMP_InputField serverTagInputField;

	public void ToggleSortWithChallengeMoons()
	{
		sortWithChallengeMoons = !sortWithChallengeMoons;
		sortWithChallengeMoonsCheckbox.enabled = sortWithChallengeMoons;
	}

	public void ChangeDistanceSort(int newValue)
	{
		sortByDistanceSetting = newValue;
	}

	private void OnEnable()
	{
		serverTagInputField.text = string.Empty;
	}

	private void DebugLogServerList()
	{
		if (currentLobbyList != null)
		{
			for (int i = 0; i < currentLobbyList.Length; i++)
			{
				Debug.Log($"Lobby #{i} id: {currentLobbyList[i].Id}; members: {currentLobbyList[i].MemberCount}");
				uint ip = 0u;
				ushort port = 0;
				SteamId serverId = default(SteamId);
				Debug.Log($"Is lobby #{i} valid?: {currentLobbyList[i].GetGameServer(ref ip, ref port, ref serverId)}");
			}
		}
		else
		{
			Debug.Log("Server list null");
		}
	}

	public void RefreshServerListButton()
	{
		if (!(refreshServerListTimer < 0.5f))
		{
			LoadServerList();
		}
	}

	public async void LoadServerList()
	{
		if (GameNetworkManager.Instance.waitingForLobbyDataRefresh)
		{
			return;
		}
		if (loadLobbyListCoroutine != null)
		{
			StopCoroutine(loadLobbyListCoroutine);
		}
		refreshServerListTimer = 0f;
		serverListBlankText.text = "Loading server list...";
		currentLobbyList = null;
		LobbySlot[] array = Object.FindObjectsOfType<LobbySlot>();
		for (int i = 0; i < array.Length; i++)
		{
			Object.Destroy(array[i].gameObject);
		}
		switch (sortByDistanceSetting)
		{
		case 0:
			SteamMatchmaking.LobbyList.FilterDistanceClose();
			break;
		case 1:
			SteamMatchmaking.LobbyList.FilterDistanceFar();
			break;
		case 2:
			SteamMatchmaking.LobbyList.FilterDistanceWorldwide();
			break;
		}
		currentLobbyList = null;
		Debug.Log("Requested server list");
		GameNetworkManager.Instance.waitingForLobbyDataRefresh = true;
		LobbyQuery lobbyQuery = sortByDistanceSetting switch
		{
			0 => SteamMatchmaking.LobbyList.FilterDistanceClose().WithSlotsAvailable(1).WithKeyValue("vers", GameNetworkManager.Instance.gameVersionNum.ToString()), 
			1 => SteamMatchmaking.LobbyList.FilterDistanceFar().WithSlotsAvailable(1).WithKeyValue("vers", GameNetworkManager.Instance.gameVersionNum.ToString()), 
			_ => SteamMatchmaking.LobbyList.FilterDistanceWorldwide().WithSlotsAvailable(1).WithKeyValue("vers", GameNetworkManager.Instance.gameVersionNum.ToString()), 
		};
		if (!sortWithChallengeMoons)
		{
			lobbyQuery = lobbyQuery.WithKeyValue("chal", "f");
		}
		if (serverTagInputField.text != string.Empty)
		{
			lobbyQuery = lobbyQuery.WithKeyValue("tag", serverTagInputField.text.Substring(0, Mathf.Min(19, serverTagInputField.text.Length)).ToLower());
		}
		currentLobbyList = await lobbyQuery.RequestAsync();
		GameNetworkManager.Instance.waitingForLobbyDataRefresh = false;
		if (currentLobbyList != null)
		{
			Debug.Log("Got lobby list!");
			DebugLogServerList();
			if (currentLobbyList.Length == 0)
			{
				serverListBlankText.text = "No available servers to join.";
			}
			else
			{
				serverListBlankText.text = "";
			}
			lobbySlotPositionOffset = 0f;
			loadLobbyListCoroutine = StartCoroutine(loadLobbyListAndFilter(currentLobbyList));
		}
		else
		{
			Debug.Log("Lobby list is null after request.");
			serverListBlankText.text = "No available servers to join.";
		}
	}

	private IEnumerator loadLobbyListAndFilter(Lobby[] lobbyList)
	{
		string[] offensiveWords = new string[23]
		{
			"nigger", "faggot", "n1g", "nigers", "cunt", "pussies", "pussy", "minors", "chink", "buttrape",
			"molest", "rape", "coon", "negro", "beastiality", "cocks", "cumshot", "ejaculate", "pedophile", "furfag",
			"necrophilia", "yiff", "sex"
		};
		for (int i = 0; i < currentLobbyList.Length; i++)
		{
			Friend[] array = SteamFriends.GetBlocked().ToArray();
			if (array != null)
			{
				for (int j = 0; j < array.Length; j++)
				{
					Debug.Log($"blocked user: {array[j].Name}; id: {array[j].Id}");
					if (currentLobbyList[i].IsOwnedBy(array[j].Id))
					{
						Debug.Log("Hiding lobby by blocked user: " + array[j].Name);
					}
				}
			}
			else
			{
				Debug.Log("Blocked users list is null");
			}
			string lobbyName = currentLobbyList[i].GetData("name");
			if (lobbyName.Length == 0)
			{
				continue;
			}
			string lobbyNameNoCapitals = lobbyName.ToLower();
			if (censorOffensiveLobbyNames)
			{
				bool nameIsOffensive = false;
				for (int b = 0; b < offensiveWords.Length; b++)
				{
					if (lobbyNameNoCapitals.Contains(offensiveWords[b]))
					{
						nameIsOffensive = true;
						break;
					}
					if (b % 5 == 0)
					{
						yield return null;
					}
				}
				if (nameIsOffensive)
				{
					continue;
				}
			}
			GameObject original = ((!(lobbyList[i].GetData("chal") == "t")) ? LobbySlotPrefab : LobbySlotPrefabChallenge);
			GameObject obj = Object.Instantiate(original, levelListContainer);
			obj.GetComponent<RectTransform>().anchoredPosition = new Vector2(0f, 0f + lobbySlotPositionOffset);
			lobbySlotPositionOffset -= 42f;
			LobbySlot componentInChildren = obj.GetComponentInChildren<LobbySlot>();
			componentInChildren.LobbyName.text = lobbyName.Substring(0, Mathf.Min(lobbyName.Length, 40));
			componentInChildren.playerCount.text = $"{currentLobbyList[i].MemberCount} / 4";
			componentInChildren.lobbyId = currentLobbyList[i].Id;
			componentInChildren.thisLobby = currentLobbyList[i];
		}
	}

	private void Update()
	{
		refreshServerListTimer += Time.deltaTime;
	}
}
