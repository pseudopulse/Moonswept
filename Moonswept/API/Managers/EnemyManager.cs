using System;
using HarmonyLib;

namespace Moonswept.Utils.Managers {
    public class EnemyManager {
        [AutoRun]
        public static void Initialize() {
            On.StartOfRound.Awake += RegisterEnemies;
            On.QuickMenuManager.Debug_SetEnemyDropdownOptions += SetupDebugEnemies;
            On.Terminal.Start += AddTerminalEntries;
        }

        private static void AddTerminalEntries(On.Terminal.orig_Start orig, Terminal self)
        {
            foreach (EnemyBase enemy in EnemyBase.Instances) {
                if (self.enemyFiles.Contains(enemy.TerminalNode)) {
                    continue;
                }

                self.enemyFiles.Add(enemy.TerminalNode);
                self.terminalNodes.allKeywords.AddItem(enemy.Keyword);
                enemy.SetFileID(self.enemyFiles.Count);
            }

            orig(self);
        }

        private static void SetupDebugEnemies(On.QuickMenuManager.orig_Debug_SetEnemyDropdownOptions orig, QuickMenuManager self)
        {
            SetupSpawnsForLevel(self.testAllEnemiesLevel);
            orig(self);
        }

        private static void RegisterEnemies(On.StartOfRound.orig_Awake orig, StartOfRound self) {
            orig(self);

            foreach (SelectableLevel level in self.levels) {
                SetupSpawnsForLevel(level);
            }
        }
        private static void SetupSpawnsForLevel(SelectableLevel level) {
            foreach (EnemyBase enemy in EnemyBase.Instances) {
                if (enemy.IsSpawnAllowed(level)) {
                    List<SpawnableEnemyWithRarity> enemiesList = enemy.EnemyClass.HasFlag(EnemyClass.Outside) ? level.OutsideEnemies : level.Enemies;
                    enemiesList.Add(enemy.GetSpawnCard(level));
                }
            }
        }
    }
}