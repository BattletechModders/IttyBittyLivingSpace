using System.Collections.Generic;

namespace IttyBittyLivingSpace
{
    public class ModText
    {
        public const string LT_Label_Mech_Upkeep = "MECH_UPKEEP";
        public const string LT_Label_Cargo_Gear = "CARGO_GEAR";
        public const string LT_Label_Cargo_Mech_Parts = "CARGO_MECH_PARTS";

        public Dictionary<string, string> Labels = new Dictionary<string, string>
        {
            { LT_Label_Mech_Upkeep, "UPKEEP: {0}" },
            { LT_Label_Cargo_Gear, "CARGO: Gear ({0} units)" },
            { LT_Label_Cargo_Mech_Parts, "CARGO: Mech Parts ({0} tons)" },
        };

        public const string LT_Tooltip_Cargo_Chassis = "CARGO_CHASSIS";
        public const string LT_Tooltip_Cargo_Equipment = "CARGO_EQUIPMENT";
        public const string LT_Tooltip_Cargo_Weapon = "CARGO_WEAPON";

        public Dictionary<string, string> Tooltips = new Dictionary<string, string>
        {
            { LT_Tooltip_Cargo_Chassis, "\n\n<color=#FF0000>Cargo Cost: {0} from {1} tons</color>" },
            { LT_Tooltip_Cargo_Equipment, "\n\n<color=#FF0000>Cargo Cost: {0} from {1}u size</color>" },
            { LT_Tooltip_Cargo_Weapon, "\n\n<color=#FF0000>Cargo Cost: {0} from {1}u size</color>" },
        };

        // Newtonsoft seems to merge values into existing dictionaries instead of replacing them entirely. So instead
        //   populate default values in dictionaries through this call instead
        public void InitUnsetValues()
        {
        }
    }
}
