using System;

namespace Moonswept.Content.Enemies {
    public class MobileTurret : EnemyBase<MobileTurret>
    {
        public override string EnemyName => "Mobile Turret";

        public override int PowerLevel => 1;

        public override int MaximumSpawns => 1;

        public override EnemyClass EnemyClass => EnemyClass.Indoors;

        public override bool Stunnable => true;

        public override bool Killable => false;

        public override GameObject GetEnemyObject()
        {
            return null;
        }

        public override SpawnableEnemyWithRarity GetSpawnCard(SelectableLevel level)
        {
            return new SpawnableEnemyWithRarity() {
                enemyType = EnemyType,
                rarity = 40
            };
        }

        public override bool IsSpawnAllowed(SelectableLevel level)
        {
            return true;
        }
    }
}