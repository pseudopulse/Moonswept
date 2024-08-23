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
            // Debug.Log("registered!");

            Enemies.RegisterEnemy(enemy, Main.config.Bind<int>("TZP Cleaner", "Weight", 80, "Spawn weight. Higher is more common.").Value, 
                Main.config.Bind<Levels.LevelTypes>("TZP Cleaner", "Spawn Locations", Levels.LevelTypes.All, "The moons this enemy can spawn on.").Value
            , Enemies.SpawnType.Default, tNode, tKeyword);


            /*Enemies.RegisterEnemy(enemy, 1, Levels.LevelTypes.None, Enemies.SpawnType.Default, tNode, tKeyword);
            Enemies.RemoveEnemyFromLevels(enemy);*/

            On.StartOfRound.Awake += KillYourself;

            /*string levelsString = Main.config.Bind<string>("TZP Cleaner", "Spawn Locations", "all:75", "The moons this enemy can spawn on, alongside their weight. 'all' correlates to every moon. Format: [moon name]:[weight]").Value;
            string[] array = levelsString.Split(" ");

            Dictionary<string, int> dict = new();

            foreach (string str in array) {
                if (str.Contains(":")) {
                    string[] str2 = str.Split(":");
                    
                    if (str2.Length != 2) continue;

                    string moon = str2[0];
                    int weight = Int32.Parse(str2[1]);

                    Debug.Log(moon);
                    Debug.Log(weight);

                    if (moon == "all") {
                        Enemies.RegisterEnemy(enemy, weight, Levels.LevelTypes.All, Enemies.SpawnType.Default, tNode, tKeyword);
                        return;
                    }

                    dict.Add(moon, weight);
                }
            }

            Enemies.RegisterEnemy(enemy, Enemies.SpawnType.Default, new() {
                [Levels.LevelTypes.None] = 0
            }, dict, tNode, tKeyword);*/
        }

        private void KillYourself(On.StartOfRound.orig_Awake orig, StartOfRound self)
        {
            orig(self);
            // self.drunknessSideEffect.AddKey(6f, 2f);
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
                controller.drunknessInertia = Mathf.Clamp(controller.drunknessInertia + Time.fixedDeltaTime / 1f * controller.drunknessSpeed, 0.1f, 4.5f);
                // Debug.Log(StartOfRound.Instance.drunknessSideEffect.Evaluate(controller.drunkness));
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
