# IttyBittyLivingSpace
This mod for the [HBS BattleTech](http://battletechgame.com/) game makes storage aboard the dropships significantly more expensive. Inventory and mech parts kept in storage have a monthly recurring cost that increases exponentially. 

Each item in storage (weapons, engines, heat sinks, etc.) requires 1 *unit* of storage for each slot they occupy in a mech. A medium laser adds 1 unit of storage, while a large laser adds 2 units of storage. The sum of all these values is then divided by *GearFactor* (default 1) and then raised to *GearPower* (default 2).  For example, 22 items would result in (22/ 1) ^ 2 = 10,648 units. These are then multiplied by GearCostPerUnit (default 100), which gives the c-bill cost. This would result in a cost of 1,064,800 c-bills.

Each mech part in storage contributes it's tonnage to a similar calculation. The sum of the tonnage is then divided by *MechPartsFactor*(default 1) and then raised to *MechPartsFactor* (default 2). For example, 105 tons would result in (105 / 1) ^ 2 = 11,025 tons. This is then multiplied by *MechPartsCostPerUnit* (default 20) which gives the c-bill cost. This would result in 220,500 c-bills.

Each mech part only contributes a portion of it's tonnage to the calculation equal to the mech parts to assemble default. For instance if the mech parts to assemble is set to 3, then each part of a 60 ton mech contributes 20 tons to the stored tonnage calculation.

## Configuration

The mod can be customized through the following values in the **IttyBittyLivingSpace/mod.json**:

* **GearCostPerUnit** defines the base c-bill cost for each *unit* of gear storage (as defined above). Defaults to 100.
* **GearFactor** defines a divisor that will be applied to the summed value, before going through the exponential growth. Defaults to 1.0.
* **GearExponent** defines the exponent that will be applied to the summed unit value, after it has been factored. Defaults to 2.
* **MechPartsCostPerTon** defines the base c-bill cost for each ton of mech parts stored. Defaults to 20.
* **MechPartsFactor** defines a divisor that will be applied to the summed tonnage of mech parts, after they have been factored. Defaults to 1.0.
* **MechPartsExponent** defines the exponent that will be applied to the summed tonnage of mech parts, after they have been factored. Defaults to 2.