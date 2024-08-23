/*using System;
using BepInEx.Configuration;
using Unity.Netcode;
using Random = UnityEngine.Random;
using DunGen;
using Moonswept.Utils.Extensions.Enumeration;
using System.Collections;

namespace Moonswept {
    public class WCCA : GenericBase<WCCA> {
        public GameObject WCCAPrefab;
        public float WCCASpawnChance => config.Bind<float>("WCCA", "Spawn Chance", 100f, "The percentage chance for the WCCA interactable to be present in a facility.").Value;
        public override void Initialize()
        {
            return;

            WCCAPrefab = Main.assets.LoadAsset<GameObject>("WCCA.prefab");

            On.RoundManager.LoadNewLevelWait += HandleWCCA;
        }

        private IEnumerator HandleWCCA(On.RoundManager.orig_LoadNewLevelWait orig, RoundManager self, int randomSeed)
        {
            yield return orig(self, randomSeed);

            if (Random.Range(0f, 100f) <= WCCASpawnChance) {
                EnemyVent[] vents = self.allEnemyVents.OrderByDescending(x => Random.Range(0, 100f)).ToArray();
                EnemyVent targetVent = null;

                for (int i = 0; i < vents.Length; i++) {
                    EnemyVent current = vents[i];

                    bool valid1 = CheckValid(current.floorNode.position, current.floorNode.right, 4f);
                    bool valid2 = CheckValid(current.floorNode.position, -current.floorNode.right, 4f);
                    bool valid3 = CheckValid(current.floorNode.position, current.floorNode.forward, 4.5f);

                    if (valid1 && valid2 && valid3) {
                        targetVent = current;
                        break;
                    }
                }

                if (targetVent) {
                    Vector3 pos = targetVent.floorNode.position + targetVent.floorNode.forward;
                    GameObject wcca = GameObject.Instantiate(WCCAPrefab, pos, targetVent.floorNode.rotation);
                    wcca.GetComponent<NetworkObject>().Spawn();
                    // wcca.transform.parent = targetVent.floorNode;
                    targetVent.occupied = true;
                    targetVent.gameObject.SetActive(false);
                    targetVent.enabled = false;
                    Debug.Log("Spawning WCCA at " + pos);
                }
                else {
                    Debug.Log("Failed to find a valid vent to spawn WCCA from.");
                }
            }
        }

        public bool CheckValid(Vector3 pos, Vector3 dir, float dist) {
            return !Physics.Raycast(pos, dir, dist, LayerMask.GetMask("Room", "Colliders"));
        }
    }
}*/