using GameNetcodeStuff;
using Unity.Netcode;
using UnityEngine;

namespace Moonswept.Enemies.MovingTurret;

public class MovingTurret : EnemyAI {
#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider adding the 'required' modifier or declaring as nullable.
    public Transform aimTarget;
    public ParticleSystem gunshots;
    public AudioSource source;
    public AudioSource seePlayerSource;
    public Light searchLight;

    private Vector3 _targetLastSeenAt;
    private float _lockOnTimer;
    private float _firingTimer;
    private float _firingDelay;
    private bool _isDoingGunshots;
    private PlayerControllerB _lastTarget;

    private const float _VIEW_DISTANCE = 8F;
    private const float _BULLET_FIRE_WIDTH = 25F;
    private const int _DAMAGE_AMOUNT = 15;
    private const float _DEFAULT_SPEED = 2F;
    private const float _CHASE_SPEED = 14F;
    private const float _WIDTH_FOV = 80F;
    private const float _FIRE_DELAY = 0.21F;
    private const float _LOCK_ON_TIME = 1F;
    private const float _TURN_SPEED = 4F;
#pragma warning restore CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider adding the 'required' modifier or declaring as nullable.

    private enum BehaviourState {
        PATROLLING,
        CHASING,
        SPOTTED_PLAYER,
        FIRING,
    }

    public override void Start() {
        base.Start();
        SwitchToBehaviourState((int) BehaviourState.PATROLLING);
        StartSearch(transform.position);
        source.Play();
    }

    public override void Update() {
        base.Update();

        if (isEnemyDead) return;

        searchLight.gameObject.SetActive(currentBehaviourStateIndex == (int) BehaviourState.SPOTTED_PLAYER);

        if (stunNormalizedTimer >= 0F) {
            agent.speed = 0;
            return;
        }

        if (currentBehaviourStateIndex != (int) BehaviourState.FIRING && targetPlayer) AimAtTarget();

        if (currentBehaviourStateIndex is (int) BehaviourState.PATROLLING or (int) BehaviourState.CHASING) ResetAim();

        HandleGunshots();
    }

    public override void DoAIInterval() {
        base.DoAIInterval();

        if (isEnemyDead || StartOfRound.Instance.allPlayersDead) return;

        switch ((BehaviourState) currentBehaviourStateIndex) {
            case BehaviourState.PATROLLING:
                DoPatrollingInterval();
                break;
            case BehaviourState.SPOTTED_PLAYER:
                DoSpottedPlayerInterval();
                break;
            case BehaviourState.CHASING:
                DoChasingInterval();
                break;
            case BehaviourState.FIRING:
                DoFiringInterval();
                break;
            default:
                Moonswept.Logger.LogWarning($"Unexpected behavior state '{currentBehaviourStateIndex}'!");
                break;
        }
    }

    private void AimAtTarget() {
        aimTarget.LookAt(targetPlayer.gameplayCamera.transform.position);
        transform.rotation = Quaternion.Lerp(transform.rotation, Quaternion.Euler(0, aimTarget.eulerAngles.y, 0), _TURN_SPEED * Time.fixedDeltaTime);
    }

    private void ResetAim() {
        var lookAt = _targetLastSeenAt;

        if (lookAt == Vector3.zero) lookAt = agent.steeringTarget;

        aimTarget.LookAt(lookAt);
        transform.rotation = Quaternion.Lerp(transform.rotation, Quaternion.Euler(0, aimTarget.eulerAngles.y, 0), _TURN_SPEED * Time.fixedDeltaTime);
    }

    private void HandleGunshots() {
        if (!_isDoingGunshots) return;

        _firingDelay += Time.fixedDeltaTime;

        if (_firingDelay < _FIRE_DELAY) return;

        _firingDelay = 0F;

        var localPlayer = GameNetworkManager.Instance.localPlayerController;

        if (CheckLineOfSightForPlayer(_BULLET_FIRE_WIDTH) != localPlayer) return;

        if (Physics.Linecast(eye.position, localPlayer.transform.position, 1 << 9, QueryTriggerInteraction.Collide)) return;

        localPlayer.DamagePlayer(_DAMAGE_AMOUNT, true, true, CauseOfDeath.Gunshots);
    }

    private void DoPatrollingInterval() {
        agent.speed = _DEFAULT_SPEED;

        if (!TargetClosestPlayer(_VIEW_DISTANCE, true)) return;

        if (Vector3.Distance(targetPlayer.transform.position, transform.position) > _VIEW_DISTANCE) {
            targetPlayer = null;
            return;
        }

        StopSearch(currentSearch);
        _lastTarget = targetPlayer;
        SwitchToBehaviourState((int) BehaviourState.CHASING);
    }

    private void DoSpottedPlayerInterval() {
        agent.speed = 0F;

        _lockOnTimer += AIIntervalTime;

        if (_lockOnTimer < _LOCK_ON_TIME) return;

        _lockOnTimer = 0F;
        StartGunshotsClientRpc();
        SwitchToBehaviourState((int) BehaviourState.FIRING);
    }

    private void DoChasingInterval() {
        agent.speed = _CHASE_SPEED;

        if (_targetLastSeenAt != Vector3.zero) SetDestinationToPosition(_targetLastSeenAt);

        if (targetPlayer) _targetLastSeenAt = targetPlayer.transform.position;

        var foundNewTarget = TargetClosestPlayer(_VIEW_DISTANCE, true);
        var isTargetObstructed = !foundNewTarget || Physics.Linecast(eye.position, targetPlayer.transform.position, 1 << 9, QueryTriggerInteraction.Collide);
        foundNewTarget = !isTargetObstructed;

        var hasLineOfSightToLastTarget = _lastTarget && CheckLineOfSightForPosition(_lastTarget.transform.position, _WIDTH_FOV);
        var isLastTargetObstructed = !hasLineOfSightToLastTarget
                                  || Physics.Linecast(eye.position, _lastTarget.transform.position, 1 << 9, QueryTriggerInteraction.Collide);
        hasLineOfSightToLastTarget = !isLastTargetObstructed;

        if (hasLineOfSightToLastTarget) targetPlayer = _lastTarget;

        if (hasLineOfSightToLastTarget || foundNewTarget) {
            _lockOnTimer = 0F;
            SwitchToBehaviourState((int) BehaviourState.SPOTTED_PLAYER);
            return;
        }

        if (_targetLastSeenAt != Vector3.zero && Vector3.Distance(_targetLastSeenAt, transform.position) >= 3F) return;

        _targetLastSeenAt = Vector3.zero;
        StartSearch(transform.position);
        SwitchToBehaviourState((int) BehaviourState.PATROLLING);
    }

    private void DoFiringInterval() {
        agent.speed = 0F;

        _firingTimer += AIIntervalTime;

        if (_firingTimer < _DEFAULT_SPEED) return;

        _firingTimer = 0F;
        StartSearch(transform.position);
        SwitchToBehaviourState((int) BehaviourState.CHASING);
        StopGunshotsClientRpc();
    }


    [ClientRpc]
    private void StartGunshotsClientRpc() {
        gunshots.Play();
        _isDoingGunshots = true;
        _firingDelay = 0F;
    }

    [ClientRpc]
    private void StopGunshotsClientRpc() {
        gunshots.Stop();
        _isDoingGunshots = false;
        _firingDelay = 0F;
    }
}