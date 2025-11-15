using GameNetcodeStuff;
using UnityEngine;
using UnityEngine.Pool;

namespace Moonswept.Enemies.CleaningDrone;

public class CleaningDroneAI : EnemyAI {
#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider adding the 'required' modifier or declaring as nullable.
    public Transform modelRoot;
    public AnimationCurve movement;
    public float rotationSpeed;
    private float _movementStopwatch;
    private Transform _currentTargetNode;
    private Vector3 _initialPos;
    public GameObject fogPrefab;
    private float _gasStopwatch;
#pragma warning restore CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider adding the 'required' modifier or declaring as nullable.

    private const float _GAS_DISPENSE_INTERVAL = .5F;
    private const float _DEFAULT_SPEED = 2F;
    private const float _RETREAT_SPEED = 14F;

    private const int _FOG_POOL_INITIAL_CAPACITY = 8;
    private const int _FOG_POOL_MAX_SIZE = 64;

    public enum BehaviourState {
        WANDER,
        RETREAT,
    }

    /*
    Shared pool for all CleaningDroneAI instances (single pool for the fog prefab type).
    This is initialized once in Start() of the first instance that has a non-null fogPrefab.
    */
    public static ObjectPool<GameObject>? FogPool;

    public override void Start() {
        base.Start();

        currentBehaviourStateIndex = (int)BehaviourState.WANDER;
        StartSearch(transform.position);

        InitializeFogPool();
    }

    private void InitializeFogPool() {
        if (fogPrefab == null) return;

        FogPool ??= new(
            createFunc: () => {
                var fog = Instantiate(fogPrefab);
                fog.SetActive(false);
                return fog;
            },
            actionOnGet: fog => {
                fog.SetActive(true);
                var fogBehavior = fog.GetComponent<FogBehavior>();
                fogBehavior.ResetForReuse();
            },
            actionOnRelease: fog => {
                var fogBehavior = fog.GetComponent<FogBehavior>();
                fogBehavior.OnRelease();
                fog.SetActive(false);
            },
            actionOnDestroy: Destroy,
            collectionCheck: false,
            defaultCapacity: _FOG_POOL_INITIAL_CAPACITY,
            maxSize: _FOG_POOL_MAX_SIZE
        );
    }

    public override void Update() {
        base.Update();

        if (isEnemyDead) return;

        _gasStopwatch += Time.deltaTime;

        if (_gasStopwatch >= _GAS_DISPENSE_INTERVAL) {
            _gasStopwatch -= _GAS_DISPENSE_INTERVAL;
            SpawnFog();
        }

        modelRoot.transform.Rotate(new Vector3(0, rotationSpeed, 0) * Time.deltaTime);

        _movementStopwatch += Time.deltaTime;
        if (_movementStopwatch >= 4F) _movementStopwatch = 0;
        modelRoot.transform.localPosition = new(0, 6.24F + movement.Evaluate(_movementStopwatch) * 4, 0);
    }

    private void SpawnFog() {
        var spawnPosition = modelRoot.transform.position;
        var spawnRotation = Quaternion.identity;

        var instance = FogPool?.Get();
        if (instance is null || !instance) {
            Moonswept.Logger.LogWarning("Failed to get fog instance from pool! Is the pool empty?");
            return;
        }

        instance.transform.position = spawnPosition;
        instance.transform.rotation = spawnRotation;
    }

    public override void DoAIInterval() {
        base.DoAIInterval();

        if (isEnemyDead) return;

        switch ((BehaviourState)currentBehaviourStateIndex) {
            case BehaviourState.WANDER:
                agent.speed = _DEFAULT_SPEED;
                return;
            case BehaviourState.RETREAT:
                DoRetreatInterval();
                return;
            default:
                Moonswept.Logger.LogWarning($"Unexpected behavior state: {currentBehaviourStateIndex}");
                break;
        }
    }

    public void DoRetreatInterval() {
        agent.speed = _RETREAT_SPEED;

        var init = Vector3.Distance(_initialPos, _currentTargetNode.position);
        var current = Vector3.Distance(transform.position, _currentTargetNode.position);

        if (current / init <= 0.4F) {
            StartSearch(transform.position);
            SwitchToBehaviourState((int)BehaviourState.WANDER);
            return;
        }

        SetDestinationToPosition(_currentTargetNode.position);
    }

    public override void HitEnemy(int force = 1, PlayerControllerB playerWhoHit = null!, bool playHitSfx = false, int hitID = -1) {
        base.HitEnemy(force, playerWhoHit, playHitSfx, hitID);

        StopSearch(currentSearch);
        SwitchToBehaviourState((int)BehaviourState.RETREAT);
        _initialPos = transform.position;
        _currentTargetNode = ChooseFarthestNodeFromPosition(transform.position);
    }
}