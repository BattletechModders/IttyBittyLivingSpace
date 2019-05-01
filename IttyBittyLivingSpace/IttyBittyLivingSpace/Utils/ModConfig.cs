

namespace IttyBittyLivingSpace {

    public class ModConfig {

        // If true, many logs will be printed
        public bool Debug = false;

        public float GearUnitFactor = 1.0f;
        public int GearCostPerUnit = 100;

        public float PartUnitFactor = 1.0f;
        public int PartCostPerUnit = 100;

        public void LogConfig() {
            Mod.Log.Info("=== MOD CONFIG BEGIN ===");
            Mod.Log.Info($"  DEBUG: {this.Debug}");
            Mod.Log.Info("=== MOD CONFIG END ===");
        }
    }
}
