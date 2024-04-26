using System;
using GameNetcodeStuff;
using LethalLib;
using LethalLib.Modules;
using Unity.Netcode;

namespace Moonswept {
    public class Cleaner : GenericBase<Cleaner> {
        public EnemyType enemy;
        public TerminalNode tNode;
        public TerminalKeyword tKeyword;
        public GameObject fogPrefab;
        public override void Initialize()
        {
            base.Initialize();
            // Debug.Log("Spawning.");
            enemy = Main.assets.LoadAsset<EnemyType>("Cleaner.asset");
            tNode = Main.assets.LoadAsset<TerminalNode>("CleanerTN.asset");
            tKeyword = Main.assets.LoadAsset<TerminalKeyword>("CleanerTK.asset");
            // fogPrefab = Main.assets.LoadAsset<GameObject>("TheFogHasCome.prefab");
            LethalLib.Modules.NetworkPrefabs.RegisterNetworkPrefab(Main.assets.LoadAsset<GameObject>("Cleaner.prefab"));
            // NetworkPrefabs.RegisterNetworkPrefab(fogPrefab);
            Enemies.RegisterEnemy(enemy, Main.config.Bind<int>("TZP Cleaner", "Weight", 75, "Spawn weight. Higher is more common.").Value, Levels.LevelTypes.All, Enemies.SpawnType.Default, tNode, tKeyword);
            // Debug.Log("registered!");
        }
    }

    public class CleanerAI : EnemyAI {
        public Transform modelRoot;
        public AnimationCurve movement;
        public float rotationSpeed;
        private float movementStopwatch = 0f;
        private Transform currentTargetNode;
        private Vector3 initialPos;
        private float stopwatch = 0f;
        public GameObject fogPrefab;
        private float stopwatch2 = 0f;
        public enum BehaviourState {
            Wander,
            Retreat,
            DispenseGas,
        }

        public override void Start()
        {
            base.Start();
            currentBehaviourStateIndex = (int)BehaviourState.Wander;
            StartSearch(transform.position);
        }

        public override void Update()
        {
            base.Update();

            modelRoot.transform.Rotate(new Vector3(0, rotationSpeed, 0) * Time.fixedDeltaTime);
            movementStopwatch += Time.fixedDeltaTime;
            if (movementStopwatch >= 4f) movementStopwatch = 0;
            modelRoot.transform.localPosition = new(0, 2.24f + movement.Evaluate(movementStopwatch), 0);
        }

        public override void DoAIInterval()
        {
            base.DoAIInterval();

            if (isEnemyDead) {
                // Debug.Log("we fucking died lmao!");
            }

            // Debug.Log(StartOfRound.Instance.drunknessSideEffect.Evaluate(40f));

            stopwatch2 += AIIntervalTime;

            if (stopwatch2 >= 0.5f) {
                stopwatch2 = 0f;
                // Debug.Log("spawning fog");
                SpawnFogClientRpc();
            }

            switch ((BehaviourState)currentBehaviourStateIndex) {
                case BehaviourState.Wander:
                    agent.speed = 2f;

                    break;
                case BehaviourState.Retreat:
                    agent.speed = 14;

                    float init = Vector3.Distance(initialPos, currentTargetNode.position);
                    float current = Vector3.Distance(transform.position, currentTargetNode.position);

                    // Debug.Log(current + " : " + init);
                    // Debug.Log(current / init);

                    if (current / init <= 0.4f) {
                        StartSearch(transform.position);
                        SwitchToBehaviourState((int)BehaviourState.Wander);
                        return;
                    }

                    base.SetDestinationToPosition(currentTargetNode.position);

                    break;
                case BehaviourState.DispenseGas:
                    agent.speed = 0f;

                    if (stopwatch <= 0f) {
                        SpawnFogClientRpc();
                    }

                    stopwatch += AIIntervalTime;

                    if (stopwatch >= 2f) {
                        currentBehaviourStateIndex = (int)BehaviourState.Wander;
                        StartSearch(transform.position);
                    }

                    break;
            }
        }

        public override void HitEnemy(int force = 1, PlayerControllerB playerWhoHit = null, bool playHitSFX = false, int hitID = -1)
        {
            base.HitEnemy(force, playerWhoHit, playHitSFX, hitID);
            // Debug.Log("we took damage?");
            StopSearch(currentSearch);
            SwitchToBehaviourState((int)BehaviourState.Retreat);
            initialPos = transform.position;
            currentTargetNode = ChooseFarthestNodeFromPosition(transform.position);
            enemyHP -= force;

            if (enemyHP <= 0) {
                KillEnemyClientRpc(true);
            }
        }

        [ClientRpc]
        public void SpawnFogClientRpc() {
            // Debug.Log("Spawning fog.");
            GameObject.Instantiate(fogPrefab, modelRoot.transform.position, Quaternion.identity);
        }

    }

    public class TZPFogZone : MonoBehaviour {
        public SphereCollider collider;
        public float destroyAfter;
        private float stopwatch;

        public void FixedUpdate() {
            PlayerControllerB controller = GameNetworkManager.Instance.localPlayerController;
            
            if (collider.bounds.Contains(controller.playerEye.position)) {
                controller.increasingDrunknessThisFrame = true;
                controller.drunknessInertia = Mathf.Clamp(controller.drunknessInertia + Time.fixedDeltaTime / 0.25f * controller.drunknessSpeed, 0.1f, 0.8f);
            }

            stopwatch += Time.fixedDeltaTime;
            if (stopwatch >= 1.5f) {
                GetComponentInChildren<ParticleSystem>().Stop();
            }
            if (stopwatch >= destroyAfter) {
                GameObject.Destroy(this.gameObject);
            }
        }
    }
}
