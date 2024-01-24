using System;
using UnityEngine;
using BepInEx.Configuration;
using Moonswept.Utils.Extensions.Text;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace Moonswept.Utils.ContentBases {
    public abstract class EnemyBase<T> : EnemyBase where T : EnemyBase<T>{
        public static T Instance { get; private set; }

        public EnemyBase() {
            if (Instance == null) {
                Instance = this as T;
            }
        }
    }

    public enum EnemyClass {
        Daytime,
        Outside,
        Indoors,
    }

    public abstract class EnemyBase {
        public static List<EnemyBase> Instances = new();
        public abstract string EnemyName { get; }
        public abstract string EnemyTerminalEntry { get; }
        public abstract int PowerLevel { get; }
        public abstract int MaximumSpawns { get; }
        public abstract EnemyClass EnemyClass { get; }
        public abstract bool Stunnable { get; }
        public abstract bool Killable { get; }
        // ----
        public virtual GameObject EnemyPrefab { get; private set; }
        public virtual float StunMultiplier { get; } = 2f;
        // ---
        public EnemyType EnemyType;
        public TerminalNode TerminalNode;
        public TerminalKeyword Keyword;
        public virtual void Initialize() {
            Instances.Add(this);
            EnemyPrefab = GetEnemyObject();
            CreateEnemyType();
            PostCreation();
        }

        public abstract GameObject GetEnemyObject();

        public virtual void CreateEnemyType() {
            EnemyType = ScriptableObject.CreateInstance<EnemyType>();
            EnemyType.canDie = Killable;
            EnemyType.canBeStunned = Stunnable;
            EnemyType.enemyName = EnemyName;
            EnemyType.enemyPrefab = EnemyPrefab;
            EnemyType.isDaytimeEnemy = (EnemyClass & EnemyClass.Daytime) != 0;
            EnemyType.isOutsideEnemy = (EnemyClass & EnemyClass.Outside) != 0;
            EnemyType.MaxCount = MaximumSpawns;
            EnemyType.PowerLevel = PowerLevel;
            EnemyType.stunTimeMultiplier = StunMultiplier;
        }

        public virtual void SetupTerminalNode() {
            TerminalNode = ScriptableObject.CreateInstance<TerminalNode>();
            TerminalNode.displayText = EnemyTerminalEntry;
            TerminalNode.clearPreviousText = true;
            TerminalNode.maxCharactersToType = 35;
            TerminalNode.creatureName = EnemyName;

            SetupTerminalKeyword();

            ScanNodeProperties prop = EnemyPrefab.GetComponentInChildren<ScanNodeProperties>();

            if (prop) {
                prop.creatureScanID = TerminalNode.creatureFileID;
            }
        }

        public virtual void SetupTerminalKeyword() {
            Keyword = ScriptableObject.CreateInstance<TerminalKeyword>();
            Keyword.word = EnemyName.ToLower().Replace(" ", "-");
            Keyword.compatibleNouns = new CompatibleNoun[] { new CompatibleNoun() {
                noun = Keyword,
                result = TerminalNode
            }};
        }

        public void SetFileID(int id) {
            TerminalNode.creatureFileID = id;

            ScanNodeProperties prop = EnemyPrefab.GetComponentInChildren<ScanNodeProperties>();

            if (prop) {
                prop.creatureScanID = TerminalNode.creatureFileID;
            }
        }

        public virtual void PostCreation() {

        }

        public abstract bool IsSpawnAllowed(SelectableLevel level);
        public abstract SpawnableEnemyWithRarity GetSpawnCard(SelectableLevel level);
    }
}