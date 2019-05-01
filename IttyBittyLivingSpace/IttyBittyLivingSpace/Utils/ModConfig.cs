

namespace IttyBittyLivingSpace {

    public class ModConfig {

        // If true, many logs will be printed
        public bool Debug = false;

        public float EquipmentFactor = 1.0f;
        public int EquipmentCostPerUnit = 100;

        public float MechPartsFactor = 1.0f;
        public int MechPartsCostPerTon = 100;

        public void LogConfig() {
            Mod.Log.Info("=== MOD CONFIG BEGIN ===");
            Mod.Log.Info($"  DEBUG: {this.Debug}");
            Mod.Log.Info($"  Equipment - Factor:x{EquipmentFactor} CostPerUnit:{EquipmentCostPerUnit}");
            Mod.Log.Info($"  MechParts - Factor:x{MechPartsFactor} MechPartsCostPerTon:{MechPartsCostPerTon}");
            Mod.Log.Info("=== MOD CONFIG END ===");
        }
    }
}
