

namespace IttyBittyLivingSpace {

    public class ModConfig {

        // If true, many logs will be printed
        public bool Debug = false;

        public float GearFactor = 1.0f;
        public int GearCostPerUnit = 100;
        public int GearExponent = 4;

        public float MechPartsFactor = 1.0f;
        public int MechPartsCostPerTon = 100;
        public int MechPartsExponent = 4;

        public void LogConfig() {
            Mod.Log.Info("=== MOD CONFIG BEGIN ===");
            Mod.Log.Info($"  DEBUG: {this.Debug}");
            Mod.Log.Info($"  Gear - Factor:x{GearFactor} CostPerUnit:{GearCostPerUnit}");
            Mod.Log.Info($"  MechParts - Factor:x{MechPartsFactor} MechPartsCostPerTon:{MechPartsCostPerTon}");
            Mod.Log.Info("=== MOD CONFIG END ===");
        }
    }
}
