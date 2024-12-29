using GameNetcodeStuff;
using Unity.Netcode;
using UnityEngine;

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

    public enum BehaviourState {
        WANDER,
        RETREAT,
    }

    public override void Start() {
        base.Start();
        currentBehaviourStateIndex = (int) BehaviourState.WANDER;
        StartSearch(transform.position);
    }

    public override void Update() {
        base.Update();

        modelRoot.transform.Rotate(new Vector3(0, rotationSpeed, 0) * Time.fixedDeltaTime);
        _movementStopwatch += Time.fixedDeltaTime;
        if (_movementStopwatch >= 4F) _movementStopwatch = 0;
        modelRoot.transform.localPosition = new(0, 8.24F + movement.Evaluate(_movementStopwatch) * 4, 0);
    }

    public override void DoAIInterval() {
        base.DoAIInterval();

        if (isEnemyDead) return;

        _gasStopwatch += AIIntervalTime;

        if (_gasStopwatch >= _GAS_DISPENSE_INTERVAL) {
            _gasStopwatch = 0F;
            SpawnFogClientRpc();
        }

        switch ((BehaviourState) currentBehaviourStateIndex) {
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
            SwitchToBehaviourState((int) BehaviourState.WANDER);
            return;
        }

        SetDestinationToPosition(_currentTargetNode.position);
    }

    public override void HitEnemy(int force = 1, PlayerControllerB playerWhoHit = null!, bool playHitSfx = false, int hitID = -1) {
        base.HitEnemy(force, playerWhoHit, playHitSfx, hitID);

        StopSearch(currentSearch);
        SwitchToBehaviourState((int) BehaviourState.RETREAT);
        _initialPos = transform.position;
        _currentTargetNode = ChooseFarthestNodeFromPosition(transform.position);
        enemyHP -= force;

        if (enemyHP > 0) return;
        KillEnemyClientRpc(true);
    }

    [ClientRpc]
    public void SpawnFogClientRpc() => Instantiate(fogPrefab, modelRoot.transform.position, Quaternion.identity);
}