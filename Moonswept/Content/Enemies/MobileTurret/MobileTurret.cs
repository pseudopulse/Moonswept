using System;
using System.Runtime.CompilerServices;
using LethalLib;
using LethalLib.Modules;
using Unity.Netcode;

namespace Moonswept {
    public class MobileTurret : GenericBase<MobileTurret> {
        public EnemyType enemy;
        public TerminalNode tNode;
        public TerminalKeyword tKeyword;
        public override void Initialize()
        {
            base.Initialize();
            // Debug.Log("Spawning.");
            enemy = Main.assets.LoadAsset<EnemyType>("WalkerTurret.asset");
            tNode = Main.assets.LoadAsset<TerminalNode>("WalkerTurretTN.asset");
            tKeyword = Main.assets.LoadAsset<TerminalKeyword>("WalkerTurretTK.asset");
            LethalLib.Modules.NetworkPrefabs.RegisterNetworkPrefab(Main.assets.LoadAsset<GameObject>("WalkerTurret.prefab"));
            Enemies.RegisterEnemy(enemy, Main.config.Bind<int>("Mobile Turret", "Weight", 60, "Spawn weight. Higher is more common.").Value, 
                Main.config.Bind<Levels.LevelTypes>("Mobile Turret", "Spawn Locations", Levels.LevelTypes.All, "The moons this enemy can spawn on.").Value
            , Enemies.SpawnType.Default, tNode, tKeyword);
            // Debug.Log("registered!");
        }
    }

    public class MobileTurretAI : EnemyAI {
        public Transform aimTarget;
        public Transform muzzle;
        public ParticleSystem gunshots;
        private Vector3 targetLastSeenAt;
        private float lockOnTimer = 0f;
        private float firingTimer = 0f;
        private float firingDelay = 0f;
        private bool isDoingGunshots;
        public AudioSource source;
        public AudioSource seePlayerSource;
        public enum BehaviourState {
            Patrolling,
            Chasing,
            LockingOn,
            Firing
        }

        public override void Start()
        {
            base.Start();
            currentBehaviourStateIndex = (int)BehaviourState.Patrolling;
            StartSearch(transform.position);
        }

        public override void Update()
        {
            base.Update();

            if (stunNormalizedTimer >= 0f) {
                agent.speed = 0;
            }

            if (currentBehaviourStateIndex != (int)BehaviourState.Firing && targetPlayer) {
                aimTarget.LookAt(targetPlayer.gameplayCamera.transform.position);
                transform.rotation = Quaternion.Lerp(transform.rotation, Quaternion.Euler(0, aimTarget.eulerAngles.y, 0), 4f * Time.fixedDeltaTime);
            }

            if (isDoingGunshots) {
                firingDelay += Time.fixedDeltaTime;

                if (firingDelay >= 0.21f) {
                    firingDelay = 0f;

                    if (CheckLineOfSightForPlayer(35f, 60, -1) == GameNetworkManager.Instance.localPlayerController) {
                        GameNetworkManager.Instance.localPlayerController.DamagePlayer(15, true, true, CauseOfDeath.Gunshots);
                    }
                }
            }
        }

        public override void DoAIInterval()
        {
            base.DoAIInterval();

            if (isEnemyDead || StartOfRound.Instance.allPlayersDead) {
                return;
            }


            // Debug.Log("do we have a target?: " + targetPlayer);
            // Debug.Log("last seen position: " + targetLastSeenAt);


            switch (currentBehaviourStateIndex) {
                case (int)BehaviourState.Patrolling:
                    agent.speed = 2f;
                    
                    if (TargetClosestPlayer(10f, false, 70)) {
                        if (Vector3.Distance(targetPlayer.transform.position, base.transform.position) > 10f) {
                            targetPlayer = null;
                            return;
                        }

                        StopSearch(currentSearch);
                        SwitchToBehaviourState((int)BehaviourState.Chasing);
                        // Debug.Log("patrol -> chase");
                    }

                    break;
                case (int)BehaviourState.LockingOn:
                    agent.speed = 0f;

                    /*if (!targetPlayer || !CheckLineOfSightForPosition(targetPlayer.transform.position, 360, 60)) {
                        lockOnTimer = 0f;
                        // Debug.Log("no line of sight");
                        SwitchToBehaviourState((int)BehaviourState.Chasing);
                        // Debug.Log("lock -> chase");
                        targetPlayer = null;
                    }*/

                    lockOnTimer += AIIntervalTime;

                    if (lockOnTimer >= 1f) {
                        lockOnTimer = 0f;
                        StartGunshotsClientRpc();
                        SwitchToBehaviourState((int)BehaviourState.Firing);
                        // Debug.Log("fire");
                    }

                    break;
                case (int)BehaviourState.Chasing:
                    agent.speed = 2f;

                    if (targetLastSeenAt != Vector3.zero) {
                        SetDestinationToPosition(targetLastSeenAt);
                    }

                    if (targetPlayer) {
                        targetLastSeenAt = targetPlayer.transform.position;

                        if (Vector3.Distance(targetLastSeenAt, base.transform.position) < 10f) {
                            lockOnTimer = 0f;
                            SwitchToBehaviourState((int)BehaviourState.LockingOn);
                            // Debug.Log("chase -> lock");

                            if (!seePlayerSource.isPlaying) {
                                seePlayerSource.Play();
                            }
                        }

                        if (!CheckLineOfSightForPosition(targetPlayer.transform.position, 90, 60)) {
                            targetPlayer = null;
                        }

                        return;

                    }
                    

                    if (targetLastSeenAt == Vector3.zero || Vector3.Distance(targetLastSeenAt, base.transform.position) < 3f) {
                        targetLastSeenAt = Vector3.zero;
                        StartSearch(transform.position);
                        SwitchToBehaviourState((int)BehaviourState.Patrolling);
                        // Debug.Log("chase -> patrol");
                    }

                    break;
                case (int)BehaviourState.Firing:
                    agent.speed = 0f;

                    firingTimer += AIIntervalTime;

                    if (firingTimer >= 2f) {
                        firingTimer = 0f;
                        StartSearch(transform.position);
                        SwitchToBehaviourState((int)BehaviourState.Chasing);
                        // Debug.Log("fire -> patrol");
                        StopGunshotsClientRpc();
                    }
                    
                    break;
            }
        }

        [ClientRpc]
        public void StartGunshotsClientRpc() {
            gunshots.Play();
            source.Play();
            isDoingGunshots = true;
            firingDelay = 0f;
        }

        [ClientRpc]
        public void StopGunshotsClientRpc() {
            gunshots.Stop();
            source.Stop();
            isDoingGunshots = false;
            firingDelay = 0f;
        }
    }
}
