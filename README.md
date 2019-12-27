# IttyBittyLivingSpace
This mod for the [HBS BattleTech](http://battletechgame.com/) game makes storage aboard the dropships significantly more expensive. Inventory and mech parts kept in storage have a monthly recurring cost that increases exponentially.

This mod requires [https://github.com/iceraptor/IRBTModUtils/]. Grab the latest release of __IRBTModUtils__ and extract it in your Mods/ directory alongside of this mod.

## Active Mech Upkeep

Mechs currently in a Mechbay during a financial report triggers add to the monthly upkeep as well. Each mech's `ChassisDef.DescriptionDef.Cost` count as its base cost, which is multiplied an upkeep factor. This factor is defined as **UpkeepChassisMulti**, which defaults to 0.01. For a chassis cost of 600,000 c-bills and the default multi, the monthly upkeep would be 6,000 c-bills.

This value can be modified by tags defined on the `ChassisDef`. **UpkeepChassisTagMultis** is a dictionary of chassis tags and multiplier values. The values of any chassis tags that match will be added to a 1.0 modifier. The final modifier will multiply the chassis upkeep defined above. Here's an example of the dictionary:

```
"UpkeepChassisTagMultis" : {
    "clan" : 0.015,
    "elite" : 0.005
}
```

If the chassis above had both the `clan` and `elite` tags, it's final cost would be `6,000 c-bills * (1.0 + 0.015 + 0.005) = 6,000 * 1.02 = 6,120`.

In addition, every component currently installed on the mech adds its `MechComponentDef.DescriptionDef.Cost` to the upkeep total. This value is multiplied by a different upkeep factor, defined as **UpkeepGearMulti**. For a component with a cost of 150,000 and the default multiplier value of `0.02`, the component would add an additional cost additional cost would be `150,000 c-bills * 0.02 = 3000`.

Another [Custom Component](https://github.com/BattletechModders/CustomComponents/) attribute named **IBLS** overrides the default multiplier. An example attribute that would set the component cost multiplier to 0.05 would be:

```
"Custom" : {
    "IBLS" : {
        "CostMulti" : 0.05,
        "StorageSize" : 0.5
    }   
}
```

### Upkeep Company Modifier

All upkeep costs are multiplied by a statistic named `IBLS_MechbayUpkeepModifier`. This statistic is read from `SimGameState.CompanyStats`, and directly multiplies the total upkeep. For a final upkeep of 100,000 c-bills, if `IBLS_MechbayUpkeepModifier` was 1.3 the actual upkeep would be calculated as `100,000 * 1.3 = 130,000`. 

## Cargo Storage

### Gear Storage
Each item in storage (weapons, engines, heat sinks, etc.) requires 1 *unit* of storage for each slot they occupy in a mech. A medium laser adds 1 unit of storage, while a large laser adds 2 units of storage. The sum of all these values is then divided by *GearFactor* (default 1) and then raised to *GearPower* (default 2).  For example, 22 items would result in (22/ 1) ^ 2 = 10,648 units. These are then multiplied by GearCostPerUnit (default 100), which gives the c-bill cost. This would result in a cost of 1,064,800 c-bills.

Gear can override this default size by specifying a [Custom Component](https://github.com/BattletechModders/CustomComponents/) attribute named **Storage.Size**. This value will override the default calculation above. Fractional values will be rounded up to the nearest whole number. An example attribute that would set the storage size for a component to 3.75 would be:
```
"Custom" : {
    "Storage" : {
        "Size" : 3.75
    }   
}
```

### Mech Part Storage
Each stored mech part contributes it's tonnage to a similar calculation. The sum of the tonnage is divided by *MechPartsFactor*(default 1) and then raised to *MechPartsFactor* (default 2). For example, 105 tons would result in (105 / 1) ^ 2 = 11,025 tons. This is then multiplied by *MechPartsCostPerUnit* (default 20) which gives the c-bill cost. This would result in 220,500 c-bills.

Each mech part contributes a **fractional** portion of the chassis tonnage. The fraction is equal to the number of stored mech parts divided by the   the number of mech parts required to assemble a full mech. If parts to assemble is 3, then each part of a 60 ton mech contributes 20 tons. If 5 parts are in storage, then the total amount contributed is 100 tons (20 * 5).
**Note:** Complete mechs in storage *do not display a parts count*.  

For each mech part, the contributed tonnage is further modified by tags on the chassisDef and mechDef. Any tag defined in **mechPartsMulti** will multiply the fractional tonnage by the multiplier, and all values will be added to the final tonnage. If a tag 'clan' adds a 1.5 multiplier, and the tag 'elite' adds a 2.0 multiplier, each mech part of 20 tons would instead contribute (20 * 1.5 = 30 tons) + (20 * 2.0 = 40 tons) = 70 tons.

### Cargo Storage Multiplier

CargoStorageModifier - impacts gear & mech parts storage

## Configuration

The mod can be customized through the following values in the **IttyBittyLivingSpace/mod.json**:

* **GearCostPerUnit** defines the base c-bill cost for each *unit* of gear storage (as defined above). Defaults to 100.
* **GearFactor** defines a divisor that will be applied to the summed value, before going through the exponential growth. Defaults to 1.0.
* **GearExponent** defines the exponent that will be applied to the summed unit value, after it has been factored. Defaults to 2.
* **MechPartsCostPerTon** defines the base c-bill cost for each ton of mech parts stored. Defaults to 20.
* **MechPartsFactor** defines a divisor that will be applied to the summed tonnage of mech parts, after they have been factored. Defaults to 1.0.
* **MechPartsExponent** defines the exponent that will be applied to the summed tonnage of mech parts, after they have been factored. Defaults to 2.
