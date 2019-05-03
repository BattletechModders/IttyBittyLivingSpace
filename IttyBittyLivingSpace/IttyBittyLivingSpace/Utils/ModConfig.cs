

using System.Collections.Generic;

namespace IttyBittyLivingSpace {

    public class ModConfig {

        // If true, many logs will be printed
        public bool Debug = false;

        public float GearFactor = 1.0f;
        public int GearCostPerUnit = 100;
        public float GearExponent = 2.0f;

        public float PartsFactor = 1.0f;
        public int PartsCostPerTon = 10;
        public float PartsExponent = 2.0f;

        public Dictionary<string, float> PartsStorageMulti = new Dictionary<string, float>();
        // { "clan" : 1.5, "elite" : 2.0 },

        public float UpkeepGearMulti = 0.2f;

        public float UpkeepChassisMulti = 0.02f;

        public Dictionary<string, float> UpkeepChassisMultis = new Dictionary<string, float>();
        // { "clan" : 1.25, "elite" : 1.5 }

        public void LogConfig() {
            Mod.Log.Info("=== MOD CONFIG BEGIN ===");
            Mod.Log.Info($"  DEBUG: {this.Debug}");
            Mod.Log.Info($"  Gear - Factor:x{GearFactor} CostPerUnit:{GearCostPerUnit}");
            Mod.Log.Info($"  MechParts - Factor:x{PartsFactor} MechPartsCostPerTon:{PartsCostPerTon}");
            Mod.Log.Info("=== MOD CONFIG END ===");
        }
        
    }
}
