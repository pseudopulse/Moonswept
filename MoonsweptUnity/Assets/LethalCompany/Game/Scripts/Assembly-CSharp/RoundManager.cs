using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using DunGen;
using TMPro;
using Unity.AI.Navigation;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.SceneManagement;

public class RoundManager : NetworkBehaviour
{
	public StartOfRound playersManager;

	public Transform syncedPropsContainer;

	public Transform itemPooledObjectsContainer;

	[Header("Global Game Variables / Balancing")]
	public float scrapValueMultiplier = 1f;

	public float scrapAmountMultiplier = 1f;

	public float mapSizeMultiplier = 1f;

	[Space(3f)]
	public int increasedInsideEnemySpawnRateIndex = -1;

	public int increasedOutsideEnemySpawnRateIndex = -1;

	public int increasedMapPropSpawnRateIndex = -1;

	public int increasedScrapSpawnRateIndex = -1;

	public int increasedMapHazardSpawnRateIndex = -1;

	[Space(5f)]
	[Space(5f)]
	public float currentMaxInsidePower;

	public float currentMaxOutsidePower;

	public float currentEnemyPower;

	public float currentOutsideEnemyPower;

	public float currentDaytimeEnemyPower;

	public TimeOfDay timeScript;

	private int currentHour;

	public float currentHourTime;

	[Header("Gameplay events")]
	public List<int> enemySpawnTimes = new List<int>();

	public int currentEnemySpawnIndex;

	public bool isSpawningEnemies;

	public bool begunSpawningEnemies;

	[Header("Elevator Properties")]
	public bool ElevatorCharging;

	public float elevatorCharge;

	public bool ElevatorPowered;

	public bool elevatorUp;

	public bool ElevatorLowering;

	public bool ElevatorRunning;

	public bool ReturnToSurface;

	[Header("Elevator Variables")]
	public Animator ElevatorAnimator;

	public Animator ElevatorLightAnimator;

	public AudioSource elevatorMotorAudio;

	public AudioClip startMotor;

	public Animator PanelButtons;

	public Animator PanelLights;

	public AudioSource elevatorButtonsAudio;

	public AudioClip PressButtonSFX1;

	public AudioClip PressButtonSFX2;

	public TextMeshProUGUI PanelScreenText;

	public Canvas PanelScreen;

	public NetworkObject lungPlacePosition;

	public InteractTrigger elevatorSocketTrigger;

	private Coroutine loadLevelCoroutine;

	private Coroutine flickerLightsCoroutine;

	private Coroutine powerLightsCoroutine;

	[Header("Enemies")]
	public EnemyVent[] allEnemyVents;

	public List<Anomaly> SpawnedAnomalies = new List<Anomaly>();

	public List<EnemyAI> SpawnedEnemies = new List<EnemyAI>();

	private List<int> SpawnProbabilities = new List<int>();

	public int hourTimeBetweenEnemySpawnBatches = 2;

	public int numberOfEnemiesInScene;

	public int minEnemiesToSpawn;

	public int minOutsideEnemiesToSpawn;

	[Header("Hazards")]
	public SpawnableMapObject[] spawnableMapObjects;

	public GameObject mapPropsContainer;

	public Transform[] shipSpawnPathPoints;

	public GameObject[] spawnDenialPoints;

	public string[] possibleCodesForBigDoors;

	public GameObject quicksandPrefab;

	public GameObject keyPrefab;

	[Space(5f)]
	public GameObject[] outsideAINodes;

	public GameObject[] insideAINodes;

	[Header("Dungeon generation")]
	public IndoorMapType[] dungeonFlowTypes;

	public RuntimeDungeon dungeonGenerator;

	public bool dungeonCompletedGenerating;

	public bool bakedNavMesh;

	public bool dungeonFinishedGeneratingForAllPlayers;

	public AudioClip[] firstTimeDungeonAudios;

	[Header("Scrap-collection")]
	public Transform spawnedScrapContainer;

	public int scrapCollectedInLevel;

	public float totalScrapValueInLevel;

	public int valueOfFoundScrapItems;

	public List<GrabbableObject> scrapCollectedThisRound = new List<GrabbableObject>();

	public SelectableLevel currentLevel;

	public System.Random LevelRandom;

	public System.Random AnomalyRandom;

	public System.Random EnemySpawnRandom;

	public System.Random OutsideEnemySpawnRandom;

	public System.Random BreakerBoxRandom;

	public System.Random ScrapValuesRandom;

	public System.Random ChallengeMoonRandom;

	public bool powerOffPermanently;

	public bool hasInitializedLevelRandomSeed;

	public List<ulong> playersFinishedGeneratingFloor = new List<ulong>(4);

	public PowerSwitchEvent onPowerSwitch = new PowerSwitchEvent();

	public List<Animator> allPoweredLightsAnimators = new List<Animator>();

	public List<Light> allPoweredLights = new List<Light>();

	public List<GameObject> spawnedSyncedObjects = new List<GameObject>();

	public float stabilityMeter;

	private Coroutine elevatorRunningCoroutine;

	public int collisionsMask = 2305;

	public bool cannotSpawnMoreInsideEnemies;

	public Collider[] tempColliderResults = new Collider[20];

	public Transform tempTransform;

	public bool GotNavMeshPositionResult;

	public NavMeshHit navHit;

	private bool firstTimeSpawningEnemies;

	private bool firstTimeSpawningOutsideEnemies;

	private bool firstTimeSpawningDaytimeEnemies;

	public List<EnemyAINestSpawnObject> enemyNestSpawnObjects = new List<EnemyAINestSpawnObject>();

	public static RoundManager Instance { get; private set; }

	private void Awake()
	{
		if (Instance == null)
		{
			Instance = this;
		}
		else
		{
			UnityEngine.Object.Destroy(Instance.gameObject);
		}
	}

	public void SpawnScrapInLevel()
	{
		int num = (int)((float)AnomalyRandom.Next(currentLevel.minScrap, currentLevel.maxScrap) * scrapAmountMultiplier);
		if (StartOfRound.Instance.isChallengeFile)
		{
			int num2 = AnomalyRandom.Next(10, 30);
			num += num2;
			Debug.Log($"Anomaly random 0b: {num2}");
		}
		List<Item> ScrapToSpawn = new List<Item>();
		List<int> list = new List<int>();
		int num3 = 0;
		List<int> list2 = new List<int>(currentLevel.spawnableScrap.Count);
		for (int j = 0; j < currentLevel.spawnableScrap.Count; j++)
		{
			if (j == increasedScrapSpawnRateIndex)
			{
				list2.Add(100);
			}
			else
			{
				list2.Add(currentLevel.spawnableScrap[j].rarity);
			}
		}
		int[] weights = list2.ToArray();
		for (int k = 0; k < num; k++)
		{
			ScrapToSpawn.Add(currentLevel.spawnableScrap[GetRandomWeightedIndex(weights)].spawnableItem);
		}
		Debug.Log($"Number of scrap to spawn: {ScrapToSpawn.Count}. minTotalScrapValue: {currentLevel.minTotalScrapValue}. Total value of items: {num3}.");
		RandomScrapSpawn randomScrapSpawn = null;
		RandomScrapSpawn[] source = UnityEngine.Object.FindObjectsOfType<RandomScrapSpawn>();
		List<NetworkObjectReference> list3 = new List<NetworkObjectReference>();
		List<RandomScrapSpawn> usedSpawns = new List<RandomScrapSpawn>();
		int i;
		for (i = 0; i < ScrapToSpawn.Count; i++)
		{
			if (ScrapToSpawn[i] == null)
			{
				Debug.Log("Error!!!!! Found null element in list ScrapToSpawn. Skipping it.");
				continue;
			}
			List<RandomScrapSpawn> list4 = ((ScrapToSpawn[i].spawnPositionTypes != null && ScrapToSpawn[i].spawnPositionTypes.Count != 0) ? source.Where((RandomScrapSpawn x) => ScrapToSpawn[i].spawnPositionTypes.Contains(x.spawnableItems) && !x.spawnUsed).ToList() : source.ToList());
			if (list4.Count <= 0)
			{
				Debug.Log("No tiles containing a scrap spawn with item type: " + ScrapToSpawn[i].itemName);
				continue;
			}
			if (usedSpawns.Count > 0 && list4.Contains(randomScrapSpawn))
			{
				list4.RemoveAll((RandomScrapSpawn x) => usedSpawns.Contains(x));
				if (list4.Count <= 0)
				{
					usedSpawns.Clear();
					i--;
					continue;
				}
			}
			randomScrapSpawn = list4[AnomalyRandom.Next(0, list4.Count)];
			usedSpawns.Add(randomScrapSpawn);
			Vector3 position;
			if (randomScrapSpawn.spawnedItemsCopyPosition)
			{
				randomScrapSpawn.spawnUsed = true;
				position = randomScrapSpawn.transform.position;
			}
			else
			{
				position = GetRandomNavMeshPositionInBoxPredictable(randomScrapSpawn.transform.position, randomScrapSpawn.itemSpawnRange, navHit, AnomalyRandom) + Vector3.up * ScrapToSpawn[i].verticalOffset;
			}
			GameObject obj = UnityEngine.Object.Instantiate(ScrapToSpawn[i].spawnPrefab, position, Quaternion.identity, spawnedScrapContainer);
			GrabbableObject component = obj.GetComponent<GrabbableObject>();
			component.transform.rotation = Quaternion.Euler(component.itemProperties.restingRotation);
			component.fallTime = 0f;
			list.Add((int)((float)AnomalyRandom.Next(ScrapToSpawn[i].minValue, ScrapToSpawn[i].maxValue) * scrapValueMultiplier));
			num3 += list[list.Count - 1];
			component.scrapValue = list[list.Count - 1];
			NetworkObject component2 = obj.GetComponent<NetworkObject>();
			component2.Spawn();
			list3.Add(component2);
		}
		StartCoroutine(waitForScrapToSpawnToSync(list3.ToArray(), list.ToArray()));
	}

	private IEnumerator waitForScrapToSpawnToSync(NetworkObjectReference[] spawnedScrap, int[] scrapValues)
	{
		yield return new WaitForSeconds(11f);
		SyncScrapValuesClientRpc(spawnedScrap, scrapValues);
	}

	[ClientRpc]
	public void SyncScrapValuesClientRpc(NetworkObjectReference[] spawnedScrap, int[] allScrapValue)
{		Debug.Log($"clientRPC scrap values length: {allScrapValue.Length}");
		ScrapValuesRandom = new System.Random(StartOfRound.Instance.randomMapSeed + 210);
		int num = 0;
		for (int i = 0; i < spawnedScrap.Length; i++)
		{
			if (spawnedScrap[i].TryGet(out var networkObject))
			{
				GrabbableObject component = networkObject.GetComponent<GrabbableObject>();
				if (component != null)
				{
					if (i >= allScrapValue.Length)
					{
						Debug.LogError($"spawnedScrap amount exceeded allScrapValue!: {spawnedScrap.Length}");
						break;
					}
					component.SetScrapValue(allScrapValue[i]);
					num += allScrapValue[i];
					if (component.itemProperties.meshVariants.Length != 0)
					{
						component.gameObject.GetComponent<MeshFilter>().mesh = component.itemProperties.meshVariants[ScrapValuesRandom.Next(0, component.itemProperties.meshVariants.Length)];
					}
					try
					{
						if (component.itemProperties.materialVariants.Length != 0)
						{
							component.gameObject.GetComponent<MeshRenderer>().sharedMaterial = component.itemProperties.materialVariants[ScrapValuesRandom.Next(0, component.itemProperties.materialVariants.Length)];
						}
					}
					catch (Exception arg)
					{
						Debug.Log($"Item name: {component.gameObject.name}; {arg}");
					}
				}
				else
				{
					Debug.LogError("Scrap networkobject object did not contain grabbable object!: " + networkObject.gameObject.name);
				}
			}
			else
			{
				Debug.LogError($"Failed to get networkobject reference for scrap. id: {spawnedScrap[i].NetworkObjectId}");
			}
		}
		totalScrapValueInLevel = num;
		scrapCollectedInLevel = 0;
		valueOfFoundScrapItems = 0;
}
	public void SpawnSyncedProps()
	{
		try
		{
			spawnedSyncedObjects.Clear();
			SpawnSyncedObject[] array = UnityEngine.Object.FindObjectsOfType<SpawnSyncedObject>();
			if (array == null)
			{
				return;
			}
			mapPropsContainer = GameObject.FindGameObjectWithTag("MapPropsContainer");
			Debug.Log($"Spawning synced props on server. Length: {array.Length}");
			for (int i = 0; i < array.Length; i++)
			{
				GameObject gameObject = UnityEngine.Object.Instantiate(array[i].spawnPrefab, array[i].transform.position, array[i].transform.rotation, mapPropsContainer.transform);
				if (gameObject != null)
				{
					gameObject.GetComponent<NetworkObject>().Spawn(destroyWithScene: true);
					spawnedSyncedObjects.Add(gameObject);
				}
			}
		}
		catch (Exception arg)
		{
			Debug.Log($"Exception! Unable to sync spawned objects on host; {arg}");
		}
	}

	public void SpawnMapObjects()
	{
		if (currentLevel.spawnableMapObjects.Length == 0)
		{
			return;
		}
		System.Random random = new System.Random(StartOfRound.Instance.randomMapSeed + 587);
		mapPropsContainer = GameObject.FindGameObjectWithTag("MapPropsContainer");
		RandomMapObject[] array = UnityEngine.Object.FindObjectsOfType<RandomMapObject>();
		EntranceTeleport[] array2 = UnityEngine.Object.FindObjectsByType<EntranceTeleport>(FindObjectsSortMode.None);
		List<Vector3> list = new List<Vector3>();
		List<RandomMapObject> list2 = new List<RandomMapObject>();
		for (int i = 0; i < currentLevel.spawnableMapObjects.Length; i++)
		{
			list2.Clear();
			int num = (int)currentLevel.spawnableMapObjects[i].numberToSpawn.Evaluate((float)random.NextDouble());
			if (increasedMapHazardSpawnRateIndex == i)
			{
				num = Mathf.Min(num * 2, 150);
			}
			if (num <= 0)
			{
				continue;
			}
			for (int j = 0; j < array.Length; j++)
			{
				if (array[j].spawnablePrefabs.Contains(currentLevel.spawnableMapObjects[i].prefabToSpawn))
				{
					list2.Add(array[j]);
				}
			}
			if (list2.Count == 0)
			{
				Debug.Log("NO SPAWNERS WERE COMPATIBLE WITH THE SPAWNABLE MAP OBJECT: '" + currentLevel.spawnableMapObjects[i].prefabToSpawn.gameObject.name + "'");
				continue;
			}
			list.Clear();
			for (int k = 0; k < num; k++)
			{
				RandomMapObject randomMapObject = list2[random.Next(0, list2.Count)];
				Vector3 position = randomMapObject.transform.position;
				position = GetRandomNavMeshPositionInBoxPredictable(position, randomMapObject.spawnRange, default(NavMeshHit), random);
				if (currentLevel.spawnableMapObjects[i].disallowSpawningNearEntrances)
				{
					for (int l = 0; l < array2.Length; l++)
					{
						if (!array2[l].isEntranceToBuilding)
						{
							Vector3.Distance(array2[l].entrancePoint.transform.position, position);
							_ = 5.5f;
						}
					}
				}
				if (currentLevel.spawnableMapObjects[i].requireDistanceBetweenSpawns)
				{
					bool flag = false;
					for (int m = 0; m < list.Count; m++)
					{
						if (Vector3.Distance(position, list[m]) < 5f)
						{
							flag = true;
							break;
						}
					}
					if (flag)
					{
						continue;
					}
				}
				GameObject gameObject = UnityEngine.Object.Instantiate(currentLevel.spawnableMapObjects[i].prefabToSpawn, position, Quaternion.identity, mapPropsContainer.transform);
				if (currentLevel.spawnableMapObjects[i].spawnFacingAwayFromWall)
				{
					gameObject.transform.eulerAngles = new Vector3(0f, YRotationThatFacesTheFarthestFromPosition(position + Vector3.up * 0.2f), 0f);
				}
				else if (currentLevel.spawnableMapObjects[i].spawnFacingWall)
				{
					gameObject.transform.eulerAngles = new Vector3(0f, YRotationThatFacesTheNearestFromPosition(position + Vector3.up * 0.2f), 0f);
				}
				else
				{
					gameObject.transform.eulerAngles = new Vector3(gameObject.transform.eulerAngles.x, random.Next(0, 360), gameObject.transform.eulerAngles.z);
				}
				if (currentLevel.spawnableMapObjects[i].spawnWithBackToWall && Physics.Raycast(gameObject.transform.position, -gameObject.transform.forward, out var hitInfo, 100f, StartOfRound.Instance.collidersAndRoomMaskAndDefault, QueryTriggerInteraction.Ignore))
				{
					gameObject.transform.position = hitInfo.point;
					if (currentLevel.spawnableMapObjects[i].spawnWithBackFlushAgainstWall)
					{
						gameObject.transform.forward = hitInfo.normal;
						gameObject.transform.eulerAngles = new Vector3(0f, gameObject.transform.eulerAngles.y, 0f);
					}
				}
				gameObject.GetComponent<NetworkObject>().Spawn(destroyWithScene: true);
			}
		}
		for (int n = 0; n < array.Length; n++)
		{
			UnityEngine.Object.Destroy(array[n].gameObject);
		}
	}

	public float YRotationThatFacesTheFarthestFromPosition(Vector3 pos, float maxDistance = 25f, int resolution = 6)
	{
		int num = 0;
		float num2 = 0f;
		for (int i = 0; i < 360; i += 360 / resolution)
		{
			tempTransform.eulerAngles = new Vector3(0f, i, 0f);
			if (Physics.Raycast(pos, tempTransform.forward, out var hitInfo, maxDistance, StartOfRound.Instance.collidersAndRoomMask, QueryTriggerInteraction.Ignore))
			{
				if (hitInfo.distance > num2)
				{
					num2 = hitInfo.distance;
					num = i;
				}
				continue;
			}
			num = i;
			break;
		}
		if (!hasInitializedLevelRandomSeed)
		{
			return UnityEngine.Random.Range(num - 15, num + 15);
		}
		int num3 = AnomalyRandom.Next(num - 15, num + 15);
		Debug.Log($"Anomaly random yrotation farthest: {num3}");
		return num3;
	}

	public float YRotationThatFacesTheNearestFromPosition(Vector3 pos, float maxDistance = 25f, int resolution = 6)
	{
		int num = 0;
		float num2 = 100f;
		bool flag = false;
		for (int i = 0; i < 360; i += 360 / resolution)
		{
			tempTransform.eulerAngles = new Vector3(0f, i, 0f);
			if (Physics.Raycast(pos, tempTransform.forward, out var hitInfo, maxDistance, StartOfRound.Instance.collidersAndRoomMask, QueryTriggerInteraction.Ignore))
			{
				flag = true;
				if (hitInfo.distance < num2)
				{
					num2 = hitInfo.distance;
					num = i;
				}
			}
		}
		if (!flag)
		{
			return -777f;
		}
		if (!hasInitializedLevelRandomSeed)
		{
			return UnityEngine.Random.Range(num - 15, num + 15);
		}
		int num3 = AnomalyRandom.Next(num - 15, num + 15);
		Debug.Log($"Anomaly random yrotation nearest: {num3}");
		return num3;
	}

	public void GenerateNewFloor()
	{
		int num = -1;
		if (currentLevel.dungeonFlowTypes != null && currentLevel.dungeonFlowTypes.Length != 0)
		{
			List<int> list = new List<int>();
			for (int i = 0; i < currentLevel.dungeonFlowTypes.Length; i++)
			{
				list.Add(currentLevel.dungeonFlowTypes[i].rarity);
			}
			num = currentLevel.dungeonFlowTypes[GetRandomWeightedIndex(list.ToArray(), LevelRandom)].id;
			dungeonGenerator.Generator.DungeonFlow = dungeonFlowTypes[num].dungeonFlow;
			if (num < firstTimeDungeonAudios.Length && firstTimeDungeonAudios[num] != null)
			{
				EntranceTeleport[] array = UnityEngine.Object.FindObjectsOfType<EntranceTeleport>();
				if (array != null && array.Length != 0)
				{
					for (int j = 0; j < array.Length; j++)
					{
						if (array[j].isEntranceToBuilding)
						{
							array[j].firstTimeAudio = firstTimeDungeonAudios[num];
							array[j].dungeonFlowId = num;
						}
					}
				}
			}
		}
		dungeonGenerator.Generator.ShouldRandomizeSeed = false;
		dungeonGenerator.Generator.Seed = LevelRandom.Next();
		float num2;
		if (num != -1)
		{
			num2 = currentLevel.factorySizeMultiplier / dungeonFlowTypes[num].MapTileSize * mapSizeMultiplier;
			num2 = (float)((double)Mathf.Round(num2 * 100f) / 100.0);
		}
		else
		{
			num2 = currentLevel.factorySizeMultiplier * mapSizeMultiplier;
		}
		dungeonGenerator.Generator.LengthMultiplier = num2;
		dungeonGenerator.Generate();
	}

	public void GeneratedFloorPostProcessing()
	{
		if (base.IsServer)
		{
			SpawnScrapInLevel();
			SpawnMapObjects();
		}
	}

	public void TurnBreakerSwitchesOff()
	{
		BreakerBox breakerBox = UnityEngine.Object.FindObjectOfType<BreakerBox>();
		if (breakerBox != null)
		{
			Debug.Log("Switching breaker switches off");
			breakerBox.SetSwitchesOff();
			SwitchPower(on: false);
			onPowerSwitch.Invoke(arg0: false);
		}
	}

	public void LoadNewLevel(int randomSeed, SelectableLevel newLevel)
	{
		if (base.IsServer)
		{
			currentLevel = newLevel;
			dungeonFinishedGeneratingForAllPlayers = false;
			playersManager.fullyLoadedPlayers.Clear();
			if (dungeonGenerator != null)
			{
				dungeonGenerator.Generator.OnGenerationStatusChanged -= Generator_OnGenerationStatusChanged;
			}
			if (loadLevelCoroutine != null)
			{
				loadLevelCoroutine = null;
			}
			loadLevelCoroutine = StartCoroutine(LoadNewLevelWait(randomSeed));
		}
	}

	private void SetChallengeFileRandomModifiers()
	{
		if (!StartOfRound.Instance.isChallengeFile)
		{
			return;
		}
		int[] array = new int[5];
		for (int i = 0; i < array.Length; i++)
		{
			array[i] = AnomalyRandom.Next(0, 100);
		}
		if (array[0] < 45)
		{
			increasedInsideEnemySpawnRateIndex = AnomalyRandom.Next(0, currentLevel.Enemies.Count);
			if (currentLevel.Enemies[increasedInsideEnemySpawnRateIndex].enemyType.spawningDisabled)
			{
				increasedInsideEnemySpawnRateIndex = AnomalyRandom.Next(0, currentLevel.Enemies.Count);
			}
		}
		if (array[1] < 45)
		{
			increasedOutsideEnemySpawnRateIndex = AnomalyRandom.Next(0, currentLevel.OutsideEnemies.Count);
		}
		if (array[2] < 45)
		{
			increasedMapHazardSpawnRateIndex = AnomalyRandom.Next(0, currentLevel.spawnableMapObjects.Length);
		}
		if (array[3] < 45)
		{
			increasedMapPropSpawnRateIndex = AnomalyRandom.Next(0, currentLevel.spawnableOutsideObjects.Length);
		}
		if (array[4] < 45)
		{
			increasedScrapSpawnRateIndex = AnomalyRandom.Next(0, currentLevel.spawnableScrap.Count);
		}
	}

	private IEnumerator LoadNewLevelWait(int randomSeed)
	{
		yield return null;
		yield return null;
		playersFinishedGeneratingFloor.Clear();
		GenerateNewLevelClientRpc(randomSeed, currentLevel.levelID);
		if (currentLevel.spawnEnemiesAndScrap)
		{
			yield return new WaitUntil(() => dungeonCompletedGenerating);
			yield return null;
			yield return new WaitUntil(() => playersFinishedGeneratingFloor.Count >= GameNetworkManager.Instance.connectedPlayers);
			Debug.Log("Players finished generating the new floor");
		}
		yield return new WaitForSeconds(0.3f);
		SpawnSyncedProps();
		if (currentLevel.spawnEnemiesAndScrap)
		{
			GeneratedFloorPostProcessing();
		}
		yield return null;
		playersFinishedGeneratingFloor.Clear();
		dungeonFinishedGeneratingForAllPlayers = true;
		RefreshEnemyVents();
		FinishGeneratingNewLevelClientRpc();
	}

	[ClientRpc]
	public void GenerateNewLevelClientRpc(int randomSeed, int levelID)
{		playersManager.randomMapSeed = randomSeed;
		currentLevel = playersManager.levels[levelID];
		InitializeRandomNumberGenerators();
		SetChallengeFileRandomModifiers();
		HUDManager.Instance.loadingText.text = $"Random seed: {randomSeed}";
		HUDManager.Instance.loadingDarkenScreen.enabled = true;
		dungeonCompletedGenerating = false;
		mapPropsContainer = GameObject.FindGameObjectWithTag("MapPropsContainer");
		if (!currentLevel.spawnEnemiesAndScrap)
		{
			return;
		}
		dungeonGenerator = UnityEngine.Object.FindObjectOfType<RuntimeDungeon>(includeInactive: false);
		if (dungeonGenerator != null)
		{
			GenerateNewFloor();
			if (dungeonGenerator.Generator.Status == GenerationStatus.Complete)
			{
				FinishGeneratingLevel();
				Debug.Log("Dungeon finished generating in one frame.");
			}
			else
			{
				dungeonGenerator.Generator.OnGenerationStatusChanged += Generator_OnGenerationStatusChanged;
				Debug.Log("Now listening to dungeon generator status.");
			}
		}
		else
		{
			Debug.LogError($"This client could not find dungeon generator! scene count: {SceneManager.sceneCount}");
		}
}
	private void FinishGeneratingLevel()
	{
		insideAINodes = GameObject.FindGameObjectsWithTag("AINode");
		outsideAINodes = GameObject.FindGameObjectsWithTag("OutsideAINode");
		dungeonCompletedGenerating = true;
		SetToCurrentLevelWeather();
		SpawnOutsideHazards();
		FinishedGeneratingLevelServerRpc(NetworkManager.Singleton.LocalClientId);
	}

	private void Generator_OnGenerationStatusChanged(DungeonGenerator generator, GenerationStatus status)
	{
		if (status == GenerationStatus.Complete && !dungeonCompletedGenerating)
		{
			FinishGeneratingLevel();
			Debug.Log("Dungeon has finished generating on this client after multiple frames");
		}
		dungeonGenerator.Generator.OnGenerationStatusChanged -= Generator_OnGenerationStatusChanged;
	}

	[ServerRpc(RequireOwnership = false)]
	public void FinishedGeneratingLevelServerRpc(ulong clientId)
			{
				playersFinishedGeneratingFloor.Add(clientId);
			}

	public void DespawnPropsAtEndOfRound(bool despawnAllItems = false)
	{
		if (!base.IsServer)
		{
			return;
		}
		GrabbableObject[] array = UnityEngine.Object.FindObjectsOfType<GrabbableObject>();
		for (int i = 0; i < array.Length; i++)
		{
			if (despawnAllItems || (!array[i].isHeld && !array[i].isInShipRoom) || array[i].deactivated || (StartOfRound.Instance.allPlayersDead && array[i].itemProperties.isScrap))
			{
				if (array[i].isHeld && array[i].playerHeldBy != null)
				{
					array[i].playerHeldBy.DropAllHeldItemsAndSync();
				}
				NetworkObject component = array[i].gameObject.GetComponent<NetworkObject>();
				if (component != null && component.IsSpawned)
				{
					array[i].gameObject.GetComponent<NetworkObject>().Despawn();
				}
				else
				{
					Debug.Log("Error/warning: prop '" + array[i].gameObject.name + "' was not spawned or did not have a NetworkObject component! Skipped despawning and destroyed it instead.");
					UnityEngine.Object.Destroy(array[i].gameObject);
				}
			}
			else
			{
				array[i].scrapPersistedThroughRounds = true;
			}
			if (spawnedSyncedObjects.Contains(array[i].gameObject))
			{
				spawnedSyncedObjects.Remove(array[i].gameObject);
			}
		}
		GameObject[] array2 = GameObject.FindGameObjectsWithTag("TemporaryEffect");
		for (int j = 0; j < array2.Length; j++)
		{
			UnityEngine.Object.Destroy(array2[j]);
		}
		bakedNavMesh = false;
	}

	public void UnloadSceneObjectsEarly()
	{
		if (!base.IsServer)
		{
			return;
		}
		Debug.Log("Despawning props and enemies #3");
		isSpawningEnemies = false;
		EnemyAI[] array = UnityEngine.Object.FindObjectsOfType<EnemyAI>();
		Debug.Log($"Enemies on map: {array.Length}");
		for (int i = 0; i < array.Length; i++)
		{
			if (array[i].thisNetworkObject.IsSpawned)
			{
				array[i].thisNetworkObject.Despawn();
			}
			else
			{
				Debug.Log($"{array[i].thisNetworkObject} was not spawned on network, so it could not be removed.");
			}
		}
		SpawnedEnemies.Clear();
		EnemyAINestSpawnObject[] array2 = UnityEngine.Object.FindObjectsByType<EnemyAINestSpawnObject>(FindObjectsSortMode.None);
		for (int j = 0; j < array2.Length; j++)
		{
			NetworkObject component = array2[j].gameObject.GetComponent<NetworkObject>();
			if (component != null && component.IsSpawned)
			{
				component.Despawn();
			}
			else
			{
				UnityEngine.Object.Destroy(array2[j].gameObject);
			}
		}
		currentEnemyPower = 0f;
		currentDaytimeEnemyPower = 0f;
		currentOutsideEnemyPower = 0f;
	}

	public override void OnDestroy()
	{
		if (dungeonGenerator != null)
		{
			dungeonGenerator.Generator.OnGenerationStatusChanged -= Generator_OnGenerationStatusChanged;
		}
		base.OnDestroy();
	}

	[ServerRpc]
	public void FinishGeneratingNewLevelServerRpc()
{		{
			FinishGeneratingNewLevelClientRpc();
		}
}
	private void SetToCurrentLevelWeather()
	{
		TimeOfDay.Instance.currentLevelWeather = currentLevel.currentWeather;
		if (TimeOfDay.Instance.currentLevelWeather == LevelWeatherType.None || currentLevel.randomWeathers == null)
		{
			return;
		}
		for (int i = 0; i < currentLevel.randomWeathers.Length; i++)
		{
			if (currentLevel.randomWeathers[i].weatherType != currentLevel.currentWeather)
			{
				continue;
			}
			TimeOfDay.Instance.currentWeatherVariable = currentLevel.randomWeathers[i].weatherVariable;
			TimeOfDay.Instance.currentWeatherVariable2 = currentLevel.randomWeathers[i].weatherVariable2;
			if (StartOfRound.Instance.isChallengeFile)
			{
				System.Random random = new System.Random(StartOfRound.Instance.randomMapSeed);
				if (random.Next(0, 100) < 20)
				{
					TimeOfDay.Instance.currentWeatherVariable *= (float)random.Next(20, 80) * 0.02f;
				}
				if (random.Next(0, 100) < 20)
				{
					TimeOfDay.Instance.currentWeatherVariable2 *= (float)random.Next(20, 80) * 0.02f;
				}
			}
			if (TimeOfDay.Instance.currentLevelWeather == LevelWeatherType.Foggy)
			{
				TimeOfDay.Instance.currentWeatherVariable = Mathf.Max(4f, TimeOfDay.Instance.currentWeatherVariable);
			}
			TimeOfDay.Instance.SetWeatherBasedOnVariables();
		}
	}

	[ClientRpc]
	public void FinishGeneratingNewLevelClientRpc()
{		{
			HUDManager.Instance.loadingText.enabled = false;
			HUDManager.Instance.loadingDarkenScreen.enabled = false;
			RefreshLightsList();
			StartOfRound.Instance.StartCoroutine(playersManager.openingDoorsSequence());
			if (currentLevel.spawnEnemiesAndScrap)
			{
				SetLevelObjectVariables();
			}
			ResetEnemySpawningVariables();
			ResetEnemyTypesSpawnedCounts();
			playersManager.newGameIsLoading = false;
			FlashlightItem.globalFlashlightInterferenceLevel = 0;
			powerOffPermanently = false;
			RefreshEnemiesList();
			try
			{
				PredictAllOutsideEnemies();
			}
			catch (Exception arg)
			{
				Debug.LogError($"Error while predicting outside enemies: {arg}");
			}
			if (StartOfRound.Instance.currentLevel.levelIncludesSnowFootprints)
			{
				StartOfRound.Instance.InstantiateFootprintsPooledObjects();
			}
		}
}
	public void PredictAllOutsideEnemies()
	{
		if (!base.IsServer)
		{
			return;
		}
		int num = 0;
		float num2 = 0f;
		bool flag = true;
		System.Random random = new System.Random(playersManager.randomMapSeed + 41);
		System.Random randomSeed = new System.Random(playersManager.randomMapSeed + 21);
		while (num < TimeOfDay.Instance.numberOfHours)
		{
			num += hourTimeBetweenEnemySpawnBatches;
			float num3 = timeScript.lengthOfHours * (float)num;
			float num4 = currentLevel.outsideEnemySpawnChanceThroughDay.Evaluate(num3 / timeScript.totalTime);
			if (StartOfRound.Instance.isChallengeFile)
			{
				num4 += 1f;
			}
			float num5 = num4 + (float)Mathf.Abs(TimeOfDay.Instance.daysUntilDeadline - 3) / 1.6f;
			int num6 = Mathf.Clamp(random.Next((int)(num5 - 3f), (int)(num4 + 3f)), minOutsideEnemiesToSpawn, 20);
			for (int i = 0; i < num6; i++)
			{
				SpawnProbabilities.Clear();
				int num7 = 0;
				for (int j = 0; j < currentLevel.OutsideEnemies.Count; j++)
				{
					EnemyType enemyType = currentLevel.OutsideEnemies[j].enemyType;
					if (flag)
					{
						enemyType.numberSpawned = 0;
					}
					if (enemyType.PowerLevel > currentMaxOutsidePower - num2 || enemyType.numberSpawned >= enemyType.MaxCount || enemyType.spawningDisabled)
					{
						SpawnProbabilities.Add(0);
						continue;
					}
					int num8 = ((increasedOutsideEnemySpawnRateIndex == j) ? 100 : ((!enemyType.useNumberSpawnedFalloff) ? ((int)((float)currentLevel.OutsideEnemies[j].rarity * enemyType.probabilityCurve.Evaluate(num3 / timeScript.totalTime))) : ((int)((float)currentLevel.OutsideEnemies[j].rarity * (enemyType.probabilityCurve.Evaluate(num3 / timeScript.totalTime) * enemyType.numberSpawnedFalloff.Evaluate((float)enemyType.numberSpawned / 10f))))));
					SpawnProbabilities.Add(num8);
					num7 += num8;
				}
				flag = false;
				if (num7 <= 0)
				{
					if (num2 >= currentMaxOutsidePower)
					{
						Debug.Log($"Round manager: No more spawnable outside enemies. Power count: {currentOutsideEnemyPower} ; max : {currentLevel.maxOutsideEnemyPowerCount}");
					}
					continue;
				}
				int randomWeightedIndex = GetRandomWeightedIndex(SpawnProbabilities.ToArray(), random);
				EnemyType enemyType2 = currentLevel.OutsideEnemies[randomWeightedIndex].enemyType;
				num2 += enemyType2.PowerLevel;
				enemyType2.numberSpawned++;
				if (enemyType2.nestSpawnPrefab != null)
				{
					if (!enemyType2.useMinEnemyThresholdForNest)
					{
						SpawnNestObjectForOutsideEnemy(enemyType2, randomSeed);
					}
					else if (enemyType2.nestsSpawned < 1 && enemyType2.numberSpawned >= enemyType2.minEnemiesToSpawnNest)
					{
						SpawnNestObjectForOutsideEnemy(enemyType2, randomSeed);
					}
				}
			}
		}
		enemyNestSpawnObjects.TrimExcess();
		List<NetworkObjectReference> list = new List<NetworkObjectReference>();
		for (int k = 0; k < enemyNestSpawnObjects.Count; k++)
		{
			NetworkObject component = enemyNestSpawnObjects[k].GetComponent<NetworkObject>();
			if (component != null)
			{
				list.Add(component);
			}
		}
		if (list.Count > 0)
		{
			SyncNestSpawnObjectsOrderServerRpc(list.ToArray());
		}
	}

	public void SpawnNestObjectForOutsideEnemy(EnemyType enemyType, System.Random randomSeed)
	{
		GameObject[] array = GameObject.FindGameObjectsWithTag("OutsideAINode");
		int num = randomSeed.Next(0, array.Length);
		Vector3 position = Vector3.zero;
		for (int i = 0; i < array.Length; i++)
		{
			position = array[num].transform.position;
			position = GetRandomNavMeshPositionInBoxPredictable(position, 15f, default(NavMeshHit), randomSeed, GetLayermaskForEnemySizeLimit(enemyType));
			position = PositionWithDenialPointsChecked(position, array, enemyType);
			Vector3 vector = PositionEdgeCheck(position, enemyType.nestSpawnPrefabWidth);
			if (vector == Vector3.zero)
			{
				num = (num + 1) % array.Length;
			}
			else
			{
				position = vector;
			}
		}
		GameObject gameObject = UnityEngine.Object.Instantiate(enemyType.nestSpawnPrefab, position, Quaternion.Euler(Vector3.zero));
		gameObject.transform.Rotate(Vector3.up, randomSeed.Next(-180, 180), Space.World);
		if (!gameObject.gameObject.GetComponentInChildren<NetworkObject>())
		{
			Debug.LogError("Error: No NetworkObject found in enemy nest spawn prefab that was just spawned on the host: '" + gameObject.name + "'");
		}
		else
		{
			gameObject.gameObject.GetComponentInChildren<NetworkObject>().Spawn(destroyWithScene: true);
		}
		if (!gameObject.GetComponent<EnemyAINestSpawnObject>())
		{
			Debug.LogError("Error: No EnemyAINestSpawnObject component in nest object prefab that was just spawned on the host: '" + gameObject.name + "'");
		}
		else
		{
			enemyNestSpawnObjects.Add(gameObject.GetComponent<EnemyAINestSpawnObject>());
		}
		enemyType.nestsSpawned++;
	}

	[ServerRpc]
	public void SyncNestSpawnObjectsOrderServerRpc(NetworkObjectReference[] nestObjects)
{		{
			SyncNestSpawnPositionsClientRpc(nestObjects);
		}
}
	[ClientRpc]
	public void SyncNestSpawnPositionsClientRpc(NetworkObjectReference[] nestObjects)
{if(base.IsServer)		{
			return;
		}
		enemyNestSpawnObjects.Clear();
		for (int i = 0; i < nestObjects.Length; i++)
		{
			if (nestObjects[i].TryGet(out var networkObject))
			{
				EnemyAINestSpawnObject component = networkObject.gameObject.GetComponent<EnemyAINestSpawnObject>();
				if (component != null)
				{
					enemyNestSpawnObjects.Add(component);
				}
			}
		}
}
	private void ResetEnemySpawningVariables()
	{
		begunSpawningEnemies = false;
		currentHour = 0;
		cannotSpawnMoreInsideEnemies = false;
		minEnemiesToSpawn = 0;
		minOutsideEnemiesToSpawn = 0;
		for (int i = 0; i < currentLevel.OutsideEnemies.Count; i++)
		{
			currentLevel.OutsideEnemies[i].enemyType.nestsSpawned = 0;
		}
	}

	public void ResetEnemyVariables()
	{
		HoarderBugAI.grabbableObjectsInMap.Clear();
		HoarderBugAI.HoarderBugItems.Clear();
		BaboonBirdAI.baboonCampPosition = Vector3.zero;
		FlowerSnakeEnemy.mainSnakes = null;
	}

	public void CollectNewScrapForThisRound(GrabbableObject scrapObject)
	{
		if (scrapObject.itemProperties.isScrap && !scrapCollectedThisRound.Contains(scrapObject) && !scrapObject.scrapPersistedThroughRounds)
		{
			scrapCollectedThisRound.Add(scrapObject);
			HUDManager.Instance.AddNewScrapFoundToDisplay(scrapObject);
		}
	}

	public void DetectElevatorIsRunning()
	{
		if (base.IsServer)
		{
			Debug.Log("Ship is leaving. Despawning props and enemies.");
			if (elevatorRunningCoroutine != null)
			{
				StopCoroutine(elevatorRunningCoroutine);
			}
			elevatorRunningCoroutine = StartCoroutine(DetectElevatorRunning());
		}
	}

	private IEnumerator DetectElevatorRunning()
	{
		isSpawningEnemies = false;
		yield return new WaitForSeconds(1.5f);
		Debug.Log("Despawning props and enemies #2");
		UnloadSceneObjectsEarly();
	}

	public void BeginEnemySpawning()
	{
		if (base.IsServer)
		{
			if (allEnemyVents.Length != 0 && currentLevel.maxEnemyPowerCount > 0)
			{
				currentEnemySpawnIndex = 0;
				PlotOutEnemiesForNextHour();
				isSpawningEnemies = true;
			}
			else
			{
				Debug.Log("Not able to spawn enemies on map; no vents were detected or maxEnemyPowerCount is 0.");
			}
		}
	}

	public void SpawnEnemiesOutside()
	{
		if (currentOutsideEnemyPower > currentMaxOutsidePower)
		{
			Debug.Log("Cannot spawn more outside enemies: max power count has been reached");
			return;
		}
		float num = timeScript.lengthOfHours * (float)currentHour;
		float num2 = (float)(int)(currentLevel.outsideEnemySpawnChanceThroughDay.Evaluate(num / timeScript.totalTime) * 100f) / 100f;
		if (StartOfRound.Instance.isChallengeFile)
		{
			num2 += 1f;
		}
		float num3 = num2 + (float)Mathf.Abs(TimeOfDay.Instance.daysUntilDeadline - 3) / 1.6f;
		int num4 = Mathf.Clamp(OutsideEnemySpawnRandom.Next((int)(num3 - 3f), (int)(num2 + 3f)), minOutsideEnemiesToSpawn, 20);
		GameObject[] spawnPoints = GameObject.FindGameObjectsWithTag("OutsideAINode");
		for (int i = 0; i < num4; i++)
		{
			if (!SpawnRandomOutsideEnemy(spawnPoints, num))
			{
				break;
			}
		}
	}

	public void SpawnDaytimeEnemiesOutside()
	{
		if (currentLevel.DaytimeEnemies == null || currentLevel.DaytimeEnemies.Count <= 0 || currentDaytimeEnemyPower > (float)currentLevel.maxDaytimeEnemyPowerCount)
		{
			return;
		}
		float num = timeScript.lengthOfHours * (float)currentHour;
		float num2 = currentLevel.daytimeEnemySpawnChanceThroughDay.Evaluate(num / timeScript.totalTime);
		int num3 = Mathf.Clamp(AnomalyRandom.Next((int)(num2 - currentLevel.daytimeEnemiesProbabilityRange), (int)(num2 + currentLevel.daytimeEnemiesProbabilityRange)), 0, 20);
		GameObject[] spawnPoints = GameObject.FindGameObjectsWithTag("OutsideAINode");
		for (int i = 0; i < num3; i++)
		{
			if (!SpawnRandomDaytimeEnemy(spawnPoints, num))
			{
				break;
			}
		}
	}

	private bool SpawnRandomDaytimeEnemy(GameObject[] spawnPoints, float timeUpToCurrentHour)
	{
		SpawnProbabilities.Clear();
		int num = 0;
		for (int i = 0; i < currentLevel.DaytimeEnemies.Count; i++)
		{
			EnemyType enemyType = currentLevel.DaytimeEnemies[i].enemyType;
			if (firstTimeSpawningDaytimeEnemies)
			{
				enemyType.numberSpawned = 0;
			}
			if (enemyType.PowerLevel > (float)currentLevel.maxDaytimeEnemyPowerCount - currentDaytimeEnemyPower || enemyType.numberSpawned >= currentLevel.DaytimeEnemies[i].enemyType.MaxCount || enemyType.normalizedTimeInDayToLeave < TimeOfDay.Instance.normalizedTimeOfDay || enemyType.spawningDisabled)
			{
				SpawnProbabilities.Add(0);
				continue;
			}
			int num2 = (int)((float)currentLevel.DaytimeEnemies[i].rarity * enemyType.probabilityCurve.Evaluate(timeUpToCurrentHour / timeScript.totalTime));
			SpawnProbabilities.Add(num2);
			num += num2;
		}
		firstTimeSpawningDaytimeEnemies = false;
		if (num <= 0)
		{
			_ = currentDaytimeEnemyPower;
			_ = (float)currentLevel.maxDaytimeEnemyPowerCount;
			return false;
		}
		int randomWeightedIndex = GetRandomWeightedIndex(SpawnProbabilities.ToArray(), EnemySpawnRandom);
		EnemyType enemyType2 = currentLevel.DaytimeEnemies[randomWeightedIndex].enemyType;
		bool result = false;
		float num3 = Mathf.Max(enemyType2.spawnInGroupsOf, 1);
		for (int j = 0; (float)j < num3; j++)
		{
			if (enemyType2.PowerLevel > (float)currentLevel.maxDaytimeEnemyPowerCount - currentDaytimeEnemyPower)
			{
				break;
			}
			currentDaytimeEnemyPower += currentLevel.DaytimeEnemies[randomWeightedIndex].enemyType.PowerLevel;
			Vector3 position = spawnPoints[AnomalyRandom.Next(0, spawnPoints.Length)].transform.position;
			position = GetRandomNavMeshPositionInBoxPredictable(position, 10f, default(NavMeshHit), EnemySpawnRandom, GetLayermaskForEnemySizeLimit(enemyType2));
			position = PositionWithDenialPointsChecked(position, spawnPoints, enemyType2);
			GameObject gameObject = UnityEngine.Object.Instantiate(enemyType2.enemyPrefab, position, Quaternion.Euler(Vector3.zero));
			gameObject.gameObject.GetComponentInChildren<NetworkObject>().Spawn(destroyWithScene: true);
			SpawnedEnemies.Add(gameObject.GetComponent<EnemyAI>());
			gameObject.GetComponent<EnemyAI>().enemyType.numberSpawned++;
			result = true;
		}
		return result;
	}

	private bool SpawnRandomOutsideEnemy(GameObject[] spawnPoints, float timeUpToCurrentHour)
	{
		SpawnProbabilities.Clear();
		int num = 0;
		for (int i = 0; i < currentLevel.OutsideEnemies.Count; i++)
		{
			EnemyType enemyType = currentLevel.OutsideEnemies[i].enemyType;
			if (firstTimeSpawningOutsideEnemies)
			{
				enemyType.numberSpawned = 0;
			}
			if (enemyType.PowerLevel > currentMaxOutsidePower - currentOutsideEnemyPower || enemyType.numberSpawned >= enemyType.MaxCount || enemyType.spawningDisabled)
			{
				SpawnProbabilities.Add(0);
				continue;
			}
			int num2 = ((increasedOutsideEnemySpawnRateIndex == i) ? 100 : ((!enemyType.useNumberSpawnedFalloff) ? ((int)((float)currentLevel.OutsideEnemies[i].rarity * enemyType.probabilityCurve.Evaluate(timeUpToCurrentHour / timeScript.totalTime))) : ((int)((float)currentLevel.OutsideEnemies[i].rarity * (enemyType.probabilityCurve.Evaluate(timeUpToCurrentHour / timeScript.totalTime) * enemyType.numberSpawnedFalloff.Evaluate((float)enemyType.numberSpawned / 10f))))));
			SpawnProbabilities.Add(num2);
			num += num2;
		}
		firstTimeSpawningOutsideEnemies = false;
		if (num <= 0)
		{
			_ = currentOutsideEnemyPower;
			_ = currentMaxOutsidePower;
			return false;
		}
		bool result = false;
		int randomWeightedIndex = GetRandomWeightedIndex(SpawnProbabilities.ToArray(), OutsideEnemySpawnRandom);
		EnemyType enemyType2 = currentLevel.OutsideEnemies[randomWeightedIndex].enemyType;
		if (enemyType2.requireNestObjectsToSpawn)
		{
			bool flag = false;
			EnemyAINestSpawnObject[] array = UnityEngine.Object.FindObjectsByType<EnemyAINestSpawnObject>(FindObjectsSortMode.None);
			for (int j = 0; j < array.Length; j++)
			{
				if (array[j].enemyType == enemyType2)
				{
					flag = true;
					break;
				}
			}
			if (!flag)
			{
				return false;
			}
		}
		float num3 = Mathf.Max(enemyType2.spawnInGroupsOf, 1);
		for (int k = 0; (float)k < num3; k++)
		{
			if (enemyType2.PowerLevel > currentMaxOutsidePower - currentOutsideEnemyPower)
			{
				break;
			}
			currentOutsideEnemyPower += currentLevel.OutsideEnemies[randomWeightedIndex].enemyType.PowerLevel;
			Vector3 position = spawnPoints[AnomalyRandom.Next(0, spawnPoints.Length)].transform.position;
			position = GetRandomNavMeshPositionInBoxPredictable(position, 10f, default(NavMeshHit), AnomalyRandom, GetLayermaskForEnemySizeLimit(enemyType2));
			position = PositionWithDenialPointsChecked(position, spawnPoints, enemyType2);
			GameObject gameObject = UnityEngine.Object.Instantiate(enemyType2.enemyPrefab, position, Quaternion.Euler(Vector3.zero));
			gameObject.gameObject.GetComponentInChildren<NetworkObject>().Spawn(destroyWithScene: true);
			SpawnedEnemies.Add(gameObject.GetComponent<EnemyAI>());
			gameObject.GetComponent<EnemyAI>().enemyType.numberSpawned++;
			result = true;
		}
		Debug.Log("Spawned enemy: " + enemyType2.enemyName);
		return result;
	}

	public int GetLayermaskForEnemySizeLimit(EnemyType enemyType)
	{
		if (enemyType.SizeLimit == NavSizeLimit.MediumSpaces)
		{
			return -97;
		}
		if (enemyType.SizeLimit == NavSizeLimit.SmallSpaces)
		{
			return -33;
		}
		return -1;
	}

	public Vector3 PositionWithDenialPointsChecked(Vector3 spawnPosition, GameObject[] spawnPoints, EnemyType enemyType)
	{
		if (spawnPoints.Length == 0)
		{
			Debug.LogError("Spawn points array was null in denial points check function!");
			return spawnPosition;
		}
		int num = 0;
		bool flag = false;
		for (int i = 0; i < spawnPoints.Length - 1; i++)
		{
			for (int j = 0; j < spawnDenialPoints.Length; j++)
			{
				flag = true;
				if (Vector3.Distance(spawnPosition, spawnDenialPoints[j].transform.position) < 16f)
				{
					num = (num + 1) % spawnPoints.Length;
					spawnPosition = spawnPoints[num].transform.position;
					spawnPosition = GetRandomNavMeshPositionInBoxPredictable(spawnPosition, 10f, default(NavMeshHit), AnomalyRandom, GetLayermaskForEnemySizeLimit(enemyType));
					flag = false;
					break;
				}
			}
			if (flag)
			{
				break;
			}
		}
		return spawnPosition;
	}

	public void PlotOutEnemiesForNextHour()
	{
		if (!base.IsServer)
		{
			return;
		}
		List<EnemyVent> list = new List<EnemyVent>();
		for (int i = 0; i < allEnemyVents.Length; i++)
		{
			if (!allEnemyVents[i].occupied)
			{
				list.Add(allEnemyVents[i]);
			}
		}
		enemySpawnTimes.Clear();
		float num = currentLevel.enemySpawnChanceThroughoutDay.Evaluate(timeScript.currentDayTime / timeScript.totalTime);
		if (StartOfRound.Instance.isChallengeFile)
		{
			num += 1f;
		}
		float num2 = num + (float)Mathf.Abs(TimeOfDay.Instance.daysUntilDeadline - 3) / 1.6f;
		int value = Mathf.Clamp(AnomalyRandom.Next((int)(num2 - currentLevel.spawnProbabilityRange), (int)(num + currentLevel.spawnProbabilityRange)), minEnemiesToSpawn, 20);
		value = Mathf.Clamp(value, 0, list.Count);
		if (currentEnemyPower >= currentMaxInsidePower)
		{
			cannotSpawnMoreInsideEnemies = true;
			return;
		}
		float num3 = timeScript.lengthOfHours * (float)currentHour;
		for (int j = 0; j < value; j++)
		{
			int num4 = AnomalyRandom.Next((int)(10f + num3), (int)(timeScript.lengthOfHours * (float)hourTimeBetweenEnemySpawnBatches + num3));
			if (!AssignRandomEnemyToVent(list[AnomalyRandom.Next(list.Count)], num4))
			{
				break;
			}
			enemySpawnTimes.Add(num4);
		}
		enemySpawnTimes.Sort();
	}

	public void LogEnemySpawnTimes(bool couldNotFinish)
	{
		if (couldNotFinish)
		{
			Debug.Log("Stopped assigning enemies to vents early as there was no enemy with a power count low enough to fit.");
		}
		Debug.Log("Enemy spawn times:");
		for (int i = 0; i < enemySpawnTimes.Count; i++)
		{
			Debug.Log($"time {i}: {enemySpawnTimes[i]}");
		}
	}

	private bool AssignRandomEnemyToVent(EnemyVent vent, float spawnTime)
	{
		SpawnProbabilities.Clear();
		int num = 0;
		for (int i = 0; i < currentLevel.Enemies.Count; i++)
		{
			EnemyType enemyType = currentLevel.Enemies[i].enemyType;
			if (firstTimeSpawningEnemies)
			{
				Debug.Log("Setting enemy numberspawned to 0 to start: " + enemyType.enemyName);
				enemyType.numberSpawned = 0;
			}
			if (EnemyCannotBeSpawned(i))
			{
				SpawnProbabilities.Add(0);
				Debug.Log($"enemy #{i} probability - {0}");
				Debug.Log($"Enemy {i} could not be spawned. current power count is {currentEnemyPower}; max is {currentLevel.maxEnemyPowerCount}.");
			}
			else
			{
				int num2 = ((increasedInsideEnemySpawnRateIndex == i) ? 100 : ((!enemyType.useNumberSpawnedFalloff) ? ((int)((float)currentLevel.Enemies[i].rarity * enemyType.probabilityCurve.Evaluate(timeScript.normalizedTimeOfDay))) : ((int)((float)currentLevel.Enemies[i].rarity * (enemyType.probabilityCurve.Evaluate(timeScript.normalizedTimeOfDay) * enemyType.numberSpawnedFalloff.Evaluate((float)enemyType.numberSpawned / 10f))))));
				SpawnProbabilities.Add(num2);
				Debug.Log($"enemy #{i} probability - {num2}");
				num += num2;
			}
		}
		firstTimeSpawningEnemies = false;
		if (num <= 0)
		{
			if (currentEnemyPower >= currentMaxInsidePower)
			{
				Debug.Log($"Round manager: No more spawnable enemies. Power count: {currentLevel.maxEnemyPowerCount} Max: {currentLevel.maxEnemyPowerCount}");
				cannotSpawnMoreInsideEnemies = true;
			}
			return false;
		}
		int randomWeightedIndex = GetRandomWeightedIndex(SpawnProbabilities.ToArray(), EnemySpawnRandom);
		Debug.Log($"ADDING ENEMY #{randomWeightedIndex}: {currentLevel.Enemies[randomWeightedIndex].enemyType.enemyName}");
		Debug.Log($"Adding {currentLevel.Enemies[randomWeightedIndex].enemyType.PowerLevel} to power level, enemy: {currentLevel.Enemies[randomWeightedIndex].enemyType.enemyName}");
		currentEnemyPower += currentLevel.Enemies[randomWeightedIndex].enemyType.PowerLevel;
		vent.enemyType = currentLevel.Enemies[randomWeightedIndex].enemyType;
		vent.enemyTypeIndex = randomWeightedIndex;
		vent.occupied = true;
		vent.spawnTime = spawnTime;
		if (timeScript.hour - currentHour > 0)
		{
			Debug.Log("RoundManager is catching up to current time! Not syncing vent SFX with clients since enemy will spawn from vent almost immediately.");
		}
		else
		{
			vent.SyncVentSpawnTimeClientRpc((int)spawnTime, randomWeightedIndex);
		}
		currentLevel.Enemies[randomWeightedIndex].enemyType.numberSpawned++;
		return true;
	}

	private bool EnemyCannotBeSpawned(int enemyIndex)
	{
		if (!currentLevel.Enemies[enemyIndex].enemyType.spawningDisabled)
		{
			if (!(currentLevel.Enemies[enemyIndex].enemyType.PowerLevel > currentMaxInsidePower - currentEnemyPower))
			{
				return currentLevel.Enemies[enemyIndex].enemyType.numberSpawned >= currentLevel.Enemies[enemyIndex].enemyType.MaxCount;
			}
			return true;
		}
		return true;
	}

	public void InitializeRandomNumberGenerators()
	{
		SoundManager.Instance.InitializeRandom();
		LevelRandom = new System.Random(playersManager.randomMapSeed);
		AnomalyRandom = new System.Random(playersManager.randomMapSeed + 5);
		EnemySpawnRandom = new System.Random(playersManager.randomMapSeed + 40);
		OutsideEnemySpawnRandom = new System.Random(playersManager.randomMapSeed + 41);
		BreakerBoxRandom = new System.Random(playersManager.randomMapSeed + 20);
	}

	public void SpawnEnemyFromVent(EnemyVent vent)
	{
		Vector3 position = vent.floorNode.position;
		float y = vent.floorNode.eulerAngles.y;
		SpawnEnemyOnServer(position, y, vent.enemyTypeIndex);
		Debug.Log("Spawned enemy from vent");
		vent.OpenVentClientRpc();
		vent.occupied = false;
	}

	public void SpawnEnemyOnServer(Vector3 spawnPosition, float yRot, int enemyNumber = -1)
	{
		if (!base.IsServer)
		{
			SpawnEnemyServerRpc(spawnPosition, yRot, enemyNumber);
		}
		else
		{
			SpawnEnemyGameObject(spawnPosition, yRot, enemyNumber);
		}
	}

	[ServerRpc]
	public void SpawnEnemyServerRpc(Vector3 spawnPosition, float yRot, int enemyNumber)
{		{
			SpawnEnemyGameObject(spawnPosition, yRot, enemyNumber);
		}
}
	public NetworkObjectReference SpawnEnemyGameObject(Vector3 spawnPosition, float yRot, int enemyNumber, EnemyType enemyType = null)
	{
		if (!base.IsServer)
		{
			return currentLevel.Enemies[0].enemyType.enemyPrefab.GetComponent<NetworkObject>();
		}
		if (enemyType != null)
		{
			GameObject gameObject = UnityEngine.Object.Instantiate(enemyType.enemyPrefab, spawnPosition, Quaternion.Euler(new Vector3(0f, yRot, 0f)));
			gameObject.GetComponentInChildren<NetworkObject>().Spawn(destroyWithScene: true);
			SpawnedEnemies.Add(gameObject.GetComponent<EnemyAI>());
			return gameObject.GetComponentInChildren<NetworkObject>();
		}
		int index = enemyNumber;
		switch (enemyNumber)
		{
		case -1:
			index = UnityEngine.Random.Range(0, currentLevel.Enemies.Count);
			break;
		case -2:
		{
			GameObject gameObject3 = UnityEngine.Object.Instantiate(currentLevel.DaytimeEnemies[UnityEngine.Random.Range(0, currentLevel.DaytimeEnemies.Count)].enemyType.enemyPrefab, spawnPosition, Quaternion.Euler(new Vector3(0f, yRot, 0f)));
			gameObject3.GetComponentInChildren<NetworkObject>().Spawn(destroyWithScene: true);
			SpawnedEnemies.Add(gameObject3.GetComponent<EnemyAI>());
			return gameObject3.GetComponentInChildren<NetworkObject>();
		}
		case -3:
		{
			GameObject gameObject2 = UnityEngine.Object.Instantiate(currentLevel.OutsideEnemies[UnityEngine.Random.Range(0, currentLevel.OutsideEnemies.Count)].enemyType.enemyPrefab, spawnPosition, Quaternion.Euler(new Vector3(0f, yRot, 0f)));
			gameObject2.GetComponentInChildren<NetworkObject>().Spawn(destroyWithScene: true);
			SpawnedEnemies.Add(gameObject2.GetComponent<EnemyAI>());
			return gameObject2.GetComponentInChildren<NetworkObject>();
		}
		}
		GameObject gameObject4 = UnityEngine.Object.Instantiate(currentLevel.Enemies[index].enemyType.enemyPrefab, spawnPosition, Quaternion.Euler(new Vector3(0f, yRot, 0f)));
		gameObject4.GetComponentInChildren<NetworkObject>().Spawn(destroyWithScene: true);
		SpawnedEnemies.Add(gameObject4.GetComponent<EnemyAI>());
		return gameObject4.GetComponentInChildren<NetworkObject>();
	}

	public void DespawnEnemyOnServer(NetworkObject enemyNetworkObject)
	{
		if (!base.IsServer)
		{
			DespawnEnemyServerRpc(enemyNetworkObject);
		}
		else
		{
			DespawnEnemyGameObject(enemyNetworkObject);
		}
	}

	[ServerRpc(RequireOwnership = false)]
	public void DespawnEnemyServerRpc(NetworkObjectReference enemyNetworkObject)
			{
				DespawnEnemyGameObject(enemyNetworkObject);
			}

	private void DespawnEnemyGameObject(NetworkObjectReference enemyNetworkObject)
	{
		if (enemyNetworkObject.TryGet(out var networkObject))
		{
			EnemyAI component = networkObject.gameObject.GetComponent<EnemyAI>();
			SpawnedEnemies.Remove(component);
			if (component.enemyType.isOutsideEnemy)
			{
				currentOutsideEnemyPower -= component.enemyType.PowerLevel;
			}
			else if (component.enemyType.isDaytimeEnemy)
			{
				currentDaytimeEnemyPower -= component.enemyType.PowerLevel;
			}
			else
			{
				currentEnemyPower -= component.enemyType.PowerLevel;
			}
			cannotSpawnMoreInsideEnemies = false;
			component.gameObject.GetComponent<NetworkObject>().Despawn();
		}
		else
		{
			Debug.LogError("Round manager despawn enemy gameobject: Could not get network object from reference!");
		}
	}

	public void SwitchPower(bool on)
	{
		if (!base.IsServer)
		{
			return;
		}
		if (on)
		{
			if (!powerOffPermanently)
			{
				PowerSwitchOnClientRpc();
			}
		}
		else
		{
			PowerSwitchOffClientRpc();
		}
	}

	[ClientRpc]
	public void PowerSwitchOnClientRpc()
			{
				onPowerSwitch.Invoke(arg0: true);
				TurnOnAllLights(on: true);
			}

	[ClientRpc]
	public void PowerSwitchOffClientRpc()
			{
				Debug.Log("Calling power switch off event from roundmanager");
				onPowerSwitch.Invoke(arg0: false);
				TurnOnAllLights(on: false);
			}

	public void TurnOnAllLights(bool on)
	{
		if (powerLightsCoroutine != null)
		{
			StopCoroutine(powerLightsCoroutine);
		}
		powerLightsCoroutine = StartCoroutine(turnOnLights(on));
	}

	private IEnumerator turnOnLights(bool turnOn)
	{
		yield return null;
		BreakerBox breakerBox = UnityEngine.Object.FindObjectOfType<BreakerBox>();
		if (breakerBox != null)
		{
			breakerBox.thisAudioSource.PlayOneShot(breakerBox.switchPowerSFX);
			breakerBox.isPowerOn = turnOn;
		}
		int b = 4;
		while (b > 0 && b != 0)
		{
			for (int i = 0; i < allPoweredLightsAnimators.Count / b; i++)
			{
				allPoweredLightsAnimators[i].SetBool("on", turnOn);
			}
			yield return new WaitForSeconds(0.03f);
			b--;
		}
	}

	public void FlickerLights(bool flickerFlashlights = false, bool disableFlashlights = false)
	{
		if (flickerLightsCoroutine != null)
		{
			StopCoroutine(flickerLightsCoroutine);
		}
		flickerLightsCoroutine = StartCoroutine(FlickerPoweredLights(flickerFlashlights, disableFlashlights));
	}

	private IEnumerator FlickerPoweredLights(bool flickerFlashlights = false, bool disableFlashlights = false)
	{
		Debug.Log("Flickering lights");
		if (flickerFlashlights)
		{
			Debug.Log("Flickering flashlights");
			FlashlightItem.globalFlashlightInterferenceLevel = 1;
			FlashlightItem[] array = UnityEngine.Object.FindObjectsOfType<FlashlightItem>();
			if (array != null)
			{
				for (int i = 0; i < array.Length; i++)
				{
					array[i].flashlightAudio.PlayOneShot(array[i].flashlightFlicker);
					WalkieTalkie.TransmitOneShotAudio(array[i].flashlightAudio, array[i].flashlightFlicker, 0.8f);
					if (disableFlashlights && array[i].playerHeldBy != null && array[i].playerHeldBy.isInsideFactory)
					{
						array[i].flashlightInterferenceLevel = 2;
					}
				}
			}
		}
		if (allPoweredLightsAnimators.Count > 0 && allPoweredLightsAnimators[0] != null)
		{
			int loopCount = 0;
			int b = 4;
			while (b > 0 && b != 0)
			{
				for (int j = loopCount; j < allPoweredLightsAnimators.Count / b; j++)
				{
					loopCount++;
					allPoweredLightsAnimators[j].SetTrigger("Flicker");
				}
				yield return new WaitForSeconds(0.05f);
				b--;
			}
		}
		if (!flickerFlashlights)
		{
			yield break;
		}
		yield return new WaitForSeconds(0.3f);
		FlashlightItem[] array2 = UnityEngine.Object.FindObjectsOfType<FlashlightItem>();
		if (array2 != null)
		{
			for (int k = 0; k < array2.Length; k++)
			{
				array2[k].flashlightInterferenceLevel = 0;
			}
		}
		FlashlightItem.globalFlashlightInterferenceLevel = 0;
	}

	private void Start()
	{
		RefreshLightsList();
		RefreshEnemyVents();
		timeScript = UnityEngine.Object.FindObjectOfType<TimeOfDay>();
		FlashlightItem.globalFlashlightInterferenceLevel = 0;
		navHit = default(NavMeshHit);
		if (StartOfRound.Instance.testRoom != null)
		{
			outsideAINodes = GameObject.FindGameObjectsWithTag("OutsideAINode");
		}
	}

	private void ResetEnemyTypesSpawnedCounts()
	{
		EnemyAI[] array = UnityEngine.Object.FindObjectsOfType<EnemyAI>();
		for (int i = 0; i < currentLevel.Enemies.Count; i++)
		{
			currentLevel.Enemies[i].enemyType.numberSpawned = 0;
			for (int j = 0; j < array.Length; j++)
			{
				if (array[j].enemyType == currentLevel.Enemies[i].enemyType)
				{
					currentLevel.Enemies[i].enemyType.numberSpawned++;
				}
			}
		}
		for (int k = 0; k < currentLevel.OutsideEnemies.Count; k++)
		{
			currentLevel.OutsideEnemies[k].enemyType.numberSpawned = 0;
			for (int l = 0; l < array.Length; l++)
			{
				if (array[l].enemyType == currentLevel.OutsideEnemies[k].enemyType)
				{
					currentLevel.OutsideEnemies[k].enemyType.numberSpawned++;
				}
			}
		}
	}

	private void RefreshEnemiesList()
	{
		SpawnedEnemies.Clear();
		EnemyAI[] array = UnityEngine.Object.FindObjectsOfType<EnemyAI>();
		SpawnedEnemies.AddRange(array);
		numberOfEnemiesInScene = array.Length;
		firstTimeSpawningEnemies = true;
		firstTimeSpawningOutsideEnemies = true;
		firstTimeSpawningDaytimeEnemies = true;
		if (StartOfRound.Instance.isChallengeFile)
		{
			System.Random random = new System.Random(StartOfRound.Instance.randomMapSeed + 5781);
			currentMaxInsidePower = currentLevel.maxEnemyPowerCount + random.Next(0, 8);
			currentMaxOutsidePower = currentLevel.maxOutsideEnemyPowerCount + random.Next(0, 8);
		}
		else
		{
			currentMaxInsidePower = currentLevel.maxEnemyPowerCount;
			currentMaxOutsidePower = currentLevel.maxOutsideEnemyPowerCount;
		}
	}

	private void Update()
	{
		if (!base.IsServer || !dungeonFinishedGeneratingForAllPlayers)
		{
			return;
		}
		if (isSpawningEnemies)
		{
			SpawnInsideEnemiesFromVentsIfReady();
			if (timeScript.hour > currentHour && currentEnemySpawnIndex >= enemySpawnTimes.Count)
			{
				AdvanceHourAndSpawnNewBatchOfEnemies();
			}
		}
		else if (timeScript.currentDayTime > 85f && !begunSpawningEnemies)
		{
			begunSpawningEnemies = true;
			BeginEnemySpawning();
		}
	}

	private void SpawnInsideEnemiesFromVentsIfReady()
	{
		if (enemySpawnTimes.Count <= currentEnemySpawnIndex || !(timeScript.currentDayTime > (float)enemySpawnTimes[currentEnemySpawnIndex]))
		{
			return;
		}
		for (int i = 0; i < allEnemyVents.Length; i++)
		{
			if (allEnemyVents[i].occupied && timeScript.currentDayTime > allEnemyVents[i].spawnTime)
			{
				Debug.Log("Found enemy vent which has its time up: " + allEnemyVents[i].gameObject.name + ". Spawning " + allEnemyVents[i].enemyType.enemyName + " from vent.");
				SpawnEnemyFromVent(allEnemyVents[i]);
			}
		}
		currentEnemySpawnIndex++;
	}

	private void AdvanceHourAndSpawnNewBatchOfEnemies()
	{
		currentHour += hourTimeBetweenEnemySpawnBatches;
		SpawnDaytimeEnemiesOutside();
		SpawnEnemiesOutside();
		if (allEnemyVents.Length != 0 && !cannotSpawnMoreInsideEnemies)
		{
			currentEnemySpawnIndex = 0;
			if (StartOfRound.Instance.connectedPlayersAmount + 1 > 0 && TimeOfDay.Instance.daysUntilDeadline <= 2 && (((float)(valueOfFoundScrapItems / TimeOfDay.Instance.profitQuota) > 0.8f && TimeOfDay.Instance.normalizedTimeOfDay > 0.3f) || (float)valueOfFoundScrapItems / totalScrapValueInLevel > 0.65f || StartOfRound.Instance.daysPlayersSurvivedInARow >= 5) && minEnemiesToSpawn == 0)
			{
				Debug.Log("Min enemy spawn chance per hour set to 1!!!");
				minEnemiesToSpawn = 1;
			}
			PlotOutEnemiesForNextHour();
		}
		else
		{
			Debug.Log($"Could not spawn more enemies; vents #: {allEnemyVents.Length}. CannotSpawnMoreInsideEnemies: {cannotSpawnMoreInsideEnemies}");
		}
	}

	public void RefreshLightsList()
	{
		allPoweredLights.Clear();
		allPoweredLightsAnimators.Clear();
		GameObject[] array = GameObject.FindGameObjectsWithTag("PoweredLight");
		if (array == null)
		{
			return;
		}
		for (int i = 0; i < array.Length; i++)
		{
			Animator componentInChildren = array[i].GetComponentInChildren<Animator>();
			if (!(componentInChildren == null))
			{
				allPoweredLightsAnimators.Add(componentInChildren);
				allPoweredLights.Add(array[i].GetComponentInChildren<Light>(includeInactive: true));
			}
		}
		for (int j = 0; j < allPoweredLightsAnimators.Count; j++)
		{
			allPoweredLightsAnimators[j].SetFloat("flickerSpeed", UnityEngine.Random.Range(0.6f, 1.4f));
		}
	}

	public void RefreshEnemyVents()
	{
		allEnemyVents = UnityEngine.Object.FindObjectsOfType<EnemyVent>();
	}

	private void SpawnOutsideHazards()
	{
		System.Random random = new System.Random(StartOfRound.Instance.randomMapSeed + 2);
		outsideAINodes = (from x in GameObject.FindGameObjectsWithTag("OutsideAINode")
			orderby Vector3.Distance(x.transform.position, Vector3.zero)
			select x).ToArray();
		NavMeshHit navMeshHit = default(NavMeshHit);
		int num = 0;
		if (TimeOfDay.Instance.currentLevelWeather == LevelWeatherType.Rainy)
		{
			num = random.Next(5, 15);
			if (random.Next(0, 100) < 7)
			{
				num = random.Next(5, 30);
			}
			for (int i = 0; i < num; i++)
			{
				Vector3 position = outsideAINodes[random.Next(0, outsideAINodes.Length)].transform.position;
				Vector3 position2 = GetRandomNavMeshPositionInBoxPredictable(position, 30f, navMeshHit, random) + Vector3.up;
				GameObject gameObject = UnityEngine.Object.Instantiate(quicksandPrefab, position2, Quaternion.identity, mapPropsContainer.transform);
			}
		}
		int num2 = 0;
		List<Vector3> list = new List<Vector3>();
		spawnDenialPoints = GameObject.FindGameObjectsWithTag("SpawnDenialPoint");
		if (currentLevel.spawnableOutsideObjects != null)
		{
			for (int j = 0; j < currentLevel.spawnableOutsideObjects.Length; j++)
			{
				double num3 = random.NextDouble();
				num = (int)currentLevel.spawnableOutsideObjects[j].randomAmount.Evaluate((float)num3);
				if (increasedMapPropSpawnRateIndex == j)
				{
					num += 12;
				}
				if ((float)random.Next(0, 100) < 20f)
				{
					num *= 2;
				}
				for (int k = 0; k < num; k++)
				{
					int num4 = random.Next(0, outsideAINodes.Length);
					Vector3 position2 = GetRandomNavMeshPositionInBoxPredictable(outsideAINodes[num4].transform.position, 30f, navMeshHit, random);
					if (currentLevel.spawnableOutsideObjects[j].spawnableObject.spawnableFloorTags != null)
					{
						bool flag = false;
						if (Physics.Raycast(position2 + Vector3.up, Vector3.down, out var hitInfo, 5f, StartOfRound.Instance.collidersAndRoomMaskAndDefault))
						{
							for (int l = 0; l < currentLevel.spawnableOutsideObjects[j].spawnableObject.spawnableFloorTags.Length; l++)
							{
								if (hitInfo.collider.transform.CompareTag(currentLevel.spawnableOutsideObjects[j].spawnableObject.spawnableFloorTags[l]))
								{
									flag = true;
									Debug.DrawRay(position2 + Vector3.up, Vector3.down, Color.white, 7f);
									Debug.Log($"prop #{j}, #{k} grounded; Ground tag: {hitInfo.collider.transform.tag}");
									break;
								}
							}
						}
						if (!flag)
						{
							continue;
						}
					}
					position2 = PositionEdgeCheck(position2, currentLevel.spawnableOutsideObjects[j].spawnableObject.objectWidth);
					if (position2 == Vector3.zero)
					{
						continue;
					}
					bool flag2 = false;
					for (int m = 0; m < shipSpawnPathPoints.Length; m++)
					{
						if (Vector3.Distance(shipSpawnPathPoints[m].transform.position, position2) < (float)currentLevel.spawnableOutsideObjects[j].spawnableObject.objectWidth + 6f)
						{
							flag2 = true;
							break;
						}
					}
					if (flag2)
					{
						continue;
					}
					for (int n = 0; n < spawnDenialPoints.Length; n++)
					{
						if (Vector3.Distance(spawnDenialPoints[n].transform.position, position2) < (float)currentLevel.spawnableOutsideObjects[j].spawnableObject.objectWidth + 6f)
						{
							flag2 = true;
							break;
						}
					}
					if (flag2)
					{
						continue;
					}
					if (Vector3.Distance(GameObject.FindGameObjectWithTag("ItemShipLandingNode").transform.position, position2) < (float)currentLevel.spawnableOutsideObjects[j].spawnableObject.objectWidth + 4f)
					{
						flag2 = true;
						break;
					}
					if (flag2)
					{
						continue;
					}
					if (currentLevel.spawnableOutsideObjects[j].spawnableObject.objectWidth > 4)
					{
						flag2 = false;
						for (int num5 = 0; num5 < list.Count; num5++)
						{
							if (Vector3.Distance(position2, list[num5]) < (float)currentLevel.spawnableOutsideObjects[j].spawnableObject.objectWidth)
							{
								flag2 = true;
								break;
							}
						}
						if (flag2)
						{
							continue;
						}
					}
					list.Add(position2);
					GameObject gameObject = UnityEngine.Object.Instantiate(currentLevel.spawnableOutsideObjects[j].spawnableObject.prefabToSpawn, position2 - Vector3.up * 0.7f, Quaternion.identity, mapPropsContainer.transform);
					num2++;
					if (currentLevel.spawnableOutsideObjects[j].spawnableObject.spawnFacingAwayFromWall)
					{
						gameObject.transform.eulerAngles = new Vector3(0f, YRotationThatFacesTheFarthestFromPosition(position2 + Vector3.up * 0.2f), 0f);
					}
					else
					{
						int num6 = random.Next(0, 360);
						gameObject.transform.eulerAngles = new Vector3(gameObject.transform.eulerAngles.x, num6, gameObject.transform.eulerAngles.z);
					}
					gameObject.transform.localEulerAngles = new Vector3(gameObject.transform.localEulerAngles.x + currentLevel.spawnableOutsideObjects[j].spawnableObject.rotationOffset.x, gameObject.transform.localEulerAngles.y + currentLevel.spawnableOutsideObjects[j].spawnableObject.rotationOffset.y, gameObject.transform.localEulerAngles.z + currentLevel.spawnableOutsideObjects[j].spawnableObject.rotationOffset.z);
				}
			}
		}
		if (num2 > 0)
		{
			GameObject gameObject2 = GameObject.FindGameObjectWithTag("OutsideLevelNavMesh");
			if (gameObject2 != null)
			{
				gameObject2.GetComponent<NavMeshSurface>().BuildNavMesh();
			}
		}
		bakedNavMesh = true;
	}

	private Vector3 PositionEdgeCheck(Vector3 position, float width)
	{
		if (NavMesh.FindClosestEdge(position, out navHit, -1) && navHit.distance < width)
		{
			Vector3 position2 = navHit.position;
			if (NavMesh.SamplePosition(new Ray(position2, position - position2).GetPoint(width + 0.5f), out navHit, 10f, -1))
			{
				position = navHit.position;
				return position;
			}
			return Vector3.zero;
		}
		return position;
	}

	private void SpawnRandomStoryLogs()
	{
	}

	public void SetLevelObjectVariables()
	{
		StartCoroutine(waitForMainEntranceTeleportToSpawn());
	}

	private IEnumerator waitForMainEntranceTeleportToSpawn()
	{
		float startTime = Time.timeSinceLevelLoad;
		while (FindMainEntrancePosition() == Vector3.zero && Time.timeSinceLevelLoad - startTime < 15f)
		{
			yield return new WaitForSeconds(1f);
		}
		Vector3 vector = FindMainEntrancePosition();
		SetLockedDoors(vector);
		SetSteamValveTimes(vector);
		SetBigDoorCodes(vector);
		SetExitIDs(vector);
		SetPowerOffAtStart();
	}

	private void SetPowerOffAtStart()
	{
		if (new System.Random(StartOfRound.Instance.randomMapSeed + 3).NextDouble() < 0.07999999821186066)
		{
			TurnBreakerSwitchesOff();
			if (!base.IsServer)
			{
				onPowerSwitch.Invoke(arg0: false);
				TurnOnAllLights(on: false);
			}
			Debug.Log("Turning lights off at start");
		}
		else
		{
			TurnOnAllLights(on: true);
			Debug.Log("Turning lights on at start");
		}
	}

	private void SetBigDoorCodes(Vector3 mainEntrancePosition)
	{
		System.Random random = new System.Random(StartOfRound.Instance.randomMapSeed + 17);
		TerminalAccessibleObject[] array = (from x in UnityEngine.Object.FindObjectsOfType<TerminalAccessibleObject>()
			orderby (x.transform.position - mainEntrancePosition).sqrMagnitude
			select x).ToArray();
		int num = 3;
		int num2 = 0;
		for (int i = 0; i < array.Length; i++)
		{
			array[i].InitializeValues();
			array[i].SetCodeTo(random.Next(possibleCodesForBigDoors.Length));
			if (array[i].isBigDoor && (num2 < num || random.NextDouble() < 0.2199999988079071))
			{
				num2++;
				array[i].SetDoorOpen(open: true);
			}
		}
	}

	private void SetExitIDs(Vector3 mainEntrancePosition)
	{
		int num = 1;
		EntranceTeleport[] array = (from x in UnityEngine.Object.FindObjectsOfType<EntranceTeleport>()
			orderby (x.transform.position - mainEntrancePosition).sqrMagnitude
			select x).ToArray();
		for (int i = 0; i < array.Length; i++)
		{
			if (array[i].entranceId == 1 && !array[i].isEntranceToBuilding)
			{
				array[i].entranceId = num;
				num++;
			}
		}
	}

	private void SetSteamValveTimes(Vector3 mainEntrancePosition)
	{
		System.Random random = new System.Random(StartOfRound.Instance.randomMapSeed + 513);
		SteamValveHazard[] array = (from x in UnityEngine.Object.FindObjectsOfType<SteamValveHazard>()
			orderby (x.transform.position - mainEntrancePosition).sqrMagnitude
			select x).ToArray();
		for (int i = 0; i < array.Length; i++)
		{
			if (random.NextDouble() < 0.75)
			{
				array[i].valveBurstTime = Mathf.Clamp((float)random.NextDouble(), 0.2f, 1f);
				array[i].valveCrackTime = array[i].valveBurstTime * (float)random.NextDouble();
				array[i].fogSizeMultiplier = Mathf.Clamp((float)random.NextDouble(), 0.6f, 0.98f);
			}
			else if (random.NextDouble() < 0.25)
			{
				array[i].valveCrackTime = Mathf.Clamp((float)random.NextDouble(), 0.3f, 0.9f);
			}
		}
	}

	private void SetLockedDoors(Vector3 mainEntrancePosition)
	{
		if (mainEntrancePosition == Vector3.zero)
		{
			Debug.Log("Main entrance teleport was not spawned on local client within 12 seconds. Locking doors based on origin instead.");
		}
		List<DoorLock> list = UnityEngine.Object.FindObjectsOfType<DoorLock>().ToList();
		for (int num = list.Count - 1; num >= 0; num--)
		{
			if (list[num].transform.position.y > -160f)
			{
				list.RemoveAt(num);
			}
		}
		list = list.OrderByDescending((DoorLock x) => (mainEntrancePosition - x.transform.position).sqrMagnitude).ToList();
		float num2 = 1.1f;
		int num3 = 0;
		for (int i = 0; i < list.Count; i++)
		{
			if (LevelRandom.NextDouble() < (double)num2)
			{
				float timeToLockPick = Mathf.Clamp(LevelRandom.Next(2, 90), 2f, 32f);
				list[i].LockDoor(timeToLockPick);
				num3++;
			}
			num2 /= 1.55f;
		}
		if (base.IsServer)
		{
			for (int j = 0; j < num3; j++)
			{
				int num4 = AnomalyRandom.Next(0, insideAINodes.Length);
				Vector3 randomNavMeshPositionInBoxPredictable = GetRandomNavMeshPositionInBoxPredictable(insideAINodes[num4].transform.position, 8f, navHit, AnomalyRandom);
				UnityEngine.Object.Instantiate(keyPrefab, randomNavMeshPositionInBoxPredictable, Quaternion.identity, spawnedScrapContainer).GetComponent<NetworkObject>().Spawn();
			}
		}
	}

	[ServerRpc]
	public void LightningStrikeServerRpc(Vector3 strikePosition)
{		{
			LightningStrikeClientRpc(strikePosition);
		}
}
	[ClientRpc]
	public void LightningStrikeClientRpc(Vector3 strikePosition)
			{
				UnityEngine.Object.FindObjectOfType<StormyWeather>(includeInactive: true).LightningStrike(strikePosition, useTargetedObject: true);
			}

	[ServerRpc]
	public void ShowStaticElectricityWarningServerRpc(NetworkObjectReference warningObject, float timeLeft)
{		{
			ShowStaticElectricityWarningClientRpc(warningObject, timeLeft);
		}
}
	[ClientRpc]
	public void ShowStaticElectricityWarningClientRpc(NetworkObjectReference warningObject, float timeLeft)
{if(warningObject.TryGet(out var networkObject))			{
				UnityEngine.Object.FindObjectOfType<StormyWeather>(includeInactive: true).SetStaticElectricityWarning(networkObject, timeLeft);
			}
}
	public Vector3 RandomlyOffsetPosition(Vector3 pos, float maxRadius, float padding = 1f)
	{
		tempTransform.position = pos;
		tempTransform.eulerAngles = Vector3.forward;
		for (int i = 0; i < 5; i++)
		{
			float num = UnityEngine.Random.Range(-180f, 180f);
			tempTransform.localEulerAngles = new Vector3(0f, tempTransform.localEulerAngles.y + num, 0f);
			Ray ray = new Ray(tempTransform.position, tempTransform.forward);
			if (Physics.Raycast(ray, out var hitInfo, 6f, 2304))
			{
				float num2 = hitInfo.distance - padding;
				if (num2 < 0f)
				{
					return ray.GetPoint(num2);
				}
				float distance = Mathf.Clamp(UnityEngine.Random.Range(0.1f, maxRadius), 0f, num2);
				return ray.GetPoint(distance);
			}
		}
		return pos;
	}

	public static Vector3 RandomPointInBounds(Bounds bounds)
	{
		return new Vector3(UnityEngine.Random.Range(bounds.min.x, bounds.max.x), UnityEngine.Random.Range(bounds.min.y, bounds.max.y), UnityEngine.Random.Range(bounds.min.z, bounds.max.z));
	}

	public Vector3 GetNavMeshPosition(Vector3 pos, NavMeshHit navMeshHit = default(NavMeshHit), float sampleRadius = 5f, int areaMask = -1)
	{
		if (NavMesh.SamplePosition(pos, out navMeshHit, sampleRadius, areaMask))
		{
			GotNavMeshPositionResult = true;
			return navMeshHit.position;
		}
		GotNavMeshPositionResult = false;
		return pos;
	}

	public Transform GetClosestNode(Vector3 pos, bool outside = true)
	{
		GameObject[] array;
		if (outside)
		{
			if (outsideAINodes == null)
			{
				outsideAINodes = GameObject.FindGameObjectsWithTag("OutsideAINode");
			}
			array = outsideAINodes;
		}
		else
		{
			if (insideAINodes == null)
			{
				outsideAINodes = GameObject.FindGameObjectsWithTag("AINode");
			}
			array = insideAINodes;
		}
		float num = 99999f;
		int num2 = 0;
		for (int i = 0; i < array.Length; i++)
		{
			float sqrMagnitude = (array[i].transform.position - pos).sqrMagnitude;
			if (sqrMagnitude < num)
			{
				num = sqrMagnitude;
				num2 = i;
			}
		}
		return array[num2].transform;
	}

	public Vector3 GetRandomNavMeshPositionInRadius(Vector3 pos, float radius = 10f, NavMeshHit navHit = default(NavMeshHit))
	{
		float y = pos.y;
		pos = UnityEngine.Random.insideUnitSphere * radius + pos;
		pos.y = y;
		if (NavMesh.SamplePosition(pos, out navHit, radius, -1))
		{
			return navHit.position;
		}
		Debug.Log("Unable to get random nav mesh position in radius! Returning old pos");
		return pos;
	}

	public Vector3 GetRandomNavMeshPositionInBoxPredictable(Vector3 pos, float radius = 10f, NavMeshHit navHit = default(NavMeshHit), System.Random randomSeed = null, int layerMask = -1)
	{
		float y = pos.y;
		float x = RandomNumberInRadius(radius, randomSeed);
		float y2 = RandomNumberInRadius(radius, randomSeed);
		float z = RandomNumberInRadius(radius, randomSeed);
		Vector3 vector = new Vector3(x, y2, z) + pos;
		vector.y = y;
		float num = Vector3.Distance(pos, vector);
		if (NavMesh.SamplePosition(vector, out navHit, num + 2f, layerMask))
		{
			return navHit.position;
		}
		return pos;
	}

	public Vector3 GetRandomPositionInBoxPredictable(Vector3 pos, float radius = 10f, System.Random randomSeed = null)
	{
		float x = RandomNumberInRadius(radius, randomSeed);
		float y = RandomNumberInRadius(radius, randomSeed);
		float z = RandomNumberInRadius(radius, randomSeed);
		return new Vector3(x, y, z) + pos;
	}

	private float RandomNumberInRadius(float radius, System.Random randomSeed)
	{
		return ((float)randomSeed.NextDouble() - 0.5f) * radius;
	}

	public Vector3 GetRandomNavMeshPositionInRadiusSpherical(Vector3 pos, float radius = 10f, NavMeshHit navHit = default(NavMeshHit))
	{
		pos = UnityEngine.Random.insideUnitSphere * radius + pos;
		if (NavMesh.SamplePosition(pos, out navHit, radius + 2f, 1))
		{
			Debug.DrawRay(pos + Vector3.forward * 0.01f, Vector3.up * 2f, Color.blue);
			return navHit.position;
		}
		Debug.DrawRay(pos + Vector3.forward * 0.01f, Vector3.up * 2f, Color.yellow);
		return pos;
	}

	public Vector3 GetRandomPositionInRadius(Vector3 pos, float minRadius, float radius, System.Random randomGen = null)
	{
		radius *= 2f;
		float num = RandomFloatWithinRadius(minRadius, radius, randomGen);
		float num2 = RandomFloatWithinRadius(minRadius, radius, randomGen);
		float num3 = RandomFloatWithinRadius(minRadius, radius, randomGen);
		return new Vector3(pos.x + num, pos.y + num2, pos.z + num3);
	}

	private float RandomFloatWithinRadius(float minValue, float radius, System.Random randomGenerator)
	{
		if (randomGenerator == null)
		{
			return UnityEngine.Random.Range(minValue, radius) * ((UnityEngine.Random.value > 0.5f) ? 1f : (-1f));
		}
		return (float)randomGenerator.Next((int)minValue, (int)radius) * ((randomGenerator.NextDouble() > 0.5) ? 1f : (-1f));
	}

	public static Vector3 AverageOfLivingGroupedPlayerPositions()
	{
		Vector3 zero = Vector3.zero;
		for (int i = 0; i < StartOfRound.Instance.connectedPlayersAmount + 1; i++)
		{
			if (StartOfRound.Instance.allPlayerScripts[i].isPlayerControlled && !StartOfRound.Instance.allPlayerScripts[i].isPlayerAlone)
			{
				zero += StartOfRound.Instance.allPlayerScripts[i].transform.position;
			}
		}
		return zero / (StartOfRound.Instance.connectedPlayersAmount + 1);
	}

	public void PlayAudibleNoise(Vector3 noisePosition, float noiseRange = 10f, float noiseLoudness = 0.5f, int timesPlayedInSameSpot = 0, bool noiseIsInsideClosedShip = false, int noiseID = 0)
	{
		if (noiseIsInsideClosedShip)
		{
			noiseRange /= 2f;
		}
		int num = Physics.OverlapSphereNonAlloc(noisePosition, noiseRange, tempColliderResults, 8912896);
		for (int i = 0; i < num; i++)
		{
			if (!tempColliderResults[i].transform.TryGetComponent<INoiseListener>(out var component))
			{
				continue;
			}
			if (noiseIsInsideClosedShip)
			{
				EnemyAI component2 = tempColliderResults[i].gameObject.GetComponent<EnemyAI>();
				if ((component2 == null || !component2.isInsidePlayerShip) && noiseLoudness < 0.9f)
				{
					continue;
				}
			}
			component.DetectNoise(noisePosition, noiseLoudness, timesPlayedInSameSpot, noiseID);
		}
	}

	public static int PlayRandomClip(AudioSource audioSource, AudioClip[] clipsArray, bool randomize = true, float oneShotVolume = 1f, int audibleNoiseID = 0, int maxIndex = 1000)
	{
		if (randomize)
		{
			audioSource.pitch = UnityEngine.Random.Range(0.94f, 1.06f);
		}
		int num = UnityEngine.Random.Range(0, Mathf.Min(maxIndex, clipsArray.Length));
		audioSource.PlayOneShot(clipsArray[num], UnityEngine.Random.Range(oneShotVolume - 0.18f, oneShotVolume));
		WalkieTalkie.TransmitOneShotAudio(audioSource, clipsArray[num], 0.85f);
		if (audioSource.spatialBlend > 0f && audibleNoiseID >= 0)
		{
			Instance.PlayAudibleNoise(audioSource.transform.position, 4f * oneShotVolume, oneShotVolume / 2f, 0, noiseIsInsideClosedShip: true, audibleNoiseID);
		}
		return num;
	}

	public static EntranceTeleport FindMainEntranceScript(bool getOutsideEntrance = false)
	{
		EntranceTeleport[] array = UnityEngine.Object.FindObjectsOfType<EntranceTeleport>(includeInactive: false);
		for (int i = 0; i < array.Length; i++)
		{
			if (array[i].entranceId != 0)
			{
				continue;
			}
			if (!getOutsideEntrance)
			{
				if (!array[i].isEntranceToBuilding)
				{
					return array[i];
				}
			}
			else if (array[i].isEntranceToBuilding)
			{
				return array[i];
			}
		}
		if (array.Length == 0)
		{
			Debug.LogError("Main entrance was not spawned and could not be found; returning null");
			return null;
		}
		Debug.LogError("Main entrance script could not be found. Returning first entrance teleport script found.");
		return array[0];
	}

	public static Vector3 FindMainEntrancePosition(bool getTeleportPosition = false, bool getOutsideEntrance = false)
	{
		EntranceTeleport[] array = UnityEngine.Object.FindObjectsOfType<EntranceTeleport>(includeInactive: false);
		for (int i = 0; i < array.Length; i++)
		{
			if (array[i].entranceId != 0)
			{
				continue;
			}
			if (!getOutsideEntrance)
			{
				if (!array[i].isEntranceToBuilding)
				{
					if (getTeleportPosition)
					{
						return array[i].entrancePoint.position;
					}
					return array[i].transform.position;
				}
			}
			else if (array[i].isEntranceToBuilding)
			{
				if (getTeleportPosition)
				{
					return array[i].entrancePoint.position;
				}
				return array[i].transform.position;
			}
		}
		Debug.LogError("Main entrance position could not be found. Returning origin.");
		return Vector3.zero;
	}

	public int GetRandomWeightedIndex(int[] weights, System.Random randomSeed = null)
	{
		if (randomSeed == null)
		{
			randomSeed = AnomalyRandom;
		}
		if (weights == null || weights.Length == 0)
		{
			Debug.Log("Could not get random weighted index; array is empty or null.");
			return -1;
		}
		int num = 0;
		for (int i = 0; i < weights.Length; i++)
		{
			if (weights[i] >= 0)
			{
				num += weights[i];
			}
		}
		if (num <= 0)
		{
			return randomSeed.Next(0, weights.Length);
		}
		float num2 = (float)randomSeed.NextDouble();
		float num3 = 0f;
		for (int i = 0; i < weights.Length; i++)
		{
			if (!((float)weights[i] <= 0f))
			{
				num3 += (float)weights[i] / (float)num;
				if (num3 >= num2)
				{
					return i;
				}
			}
		}
		Debug.LogError("Error while calculating random weighted index. Choosing randomly. Weights given:");
		for (int i = 0; i < weights.Length; i++)
		{
			Debug.LogError($"{weights[i]},");
		}
		if (!hasInitializedLevelRandomSeed)
		{
			InitializeRandomNumberGenerators();
		}
		return randomSeed.Next(0, weights.Length);
	}

	public int GetRandomWeightedIndexList(List<int> weights, System.Random randomSeed = null)
	{
		if (weights == null || weights.Count == 0)
		{
			Debug.Log("Could not get random weighted index; array is empty or null.");
			return -1;
		}
		int num = 0;
		for (int i = 0; i < weights.Count; i++)
		{
			if (weights[i] >= 0)
			{
				num += weights[i];
			}
		}
		float num2 = ((randomSeed != null) ? ((float)randomSeed.NextDouble()) : UnityEngine.Random.value);
		float num3 = 0f;
		for (int i = 0; i < weights.Count; i++)
		{
			if (!((float)weights[i] <= 0f))
			{
				num3 += (float)weights[i] / (float)num;
				if (num3 >= num2)
				{
					return i;
				}
			}
		}
		Debug.LogError("Error while calculating random weighted index.");
		for (int i = 0; i < weights.Count; i++)
		{
			Debug.LogError($"{weights[i]},");
		}
		if (!hasInitializedLevelRandomSeed)
		{
			InitializeRandomNumberGenerators();
		}
		return randomSeed.Next(0, weights.Count);
	}

	public int GetWeightedValue(int indexLength)
	{
		return Mathf.Clamp(UnityEngine.Random.Range(0, indexLength * 2) - (indexLength - 1), 0, indexLength);
	}

	private static int SortBySize(int p1, int p2)
	{
		return p1.CompareTo(p2);
	}
}
