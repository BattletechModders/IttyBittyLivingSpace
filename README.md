# IttyBittyLivingSpace
This mod for the [HBS BattleTech](http://battletechgame.com/) game makes storage aboard the dropships significantly more expensive. Inventory and mech parts kept in storage have a monthly recurring cost that increases exponentially. 

Each item in storage (weapons, engines, heat sinks, etc.) counts as 1 unit of storage. The sum of all these values is then divided by *GearFactor* (default 1) and then raised to *GearPower* (default 3).  For example, 40 items would result in (40 / 1) ^ 3 = 64,000 c-bills.

Each mech part in storage contributes it's tonnage to a similar calculation. The sum of the tonnage is then divided by *MechPartsFactor*(default 1) and then raised to *MechPartsFactor* (default 3). For example, 70 tons would result in a (70 / 1) ^ 3 = 343,000. 

Each mech part only contributes a portion of it's tonnage to the calculation equal to the mech parts to assemble default. For instance if the mech parts to assemble is set to 3, then each part of a 60 ton mech contributes 20 tons to the stored tonnage calculation.

## Configuration

The mod can be customized through the following values in the **IttyBittyLivingSpace/mod.json**:

* **GearCostPerUnit** defines the base c-bill cost for each *unit* of gear storage (as defined above). Defaults to 100.
* **GearFactor** defines a divisor that will be applied to the summed value, before going through the exponential growth. Defaults to 1.0.
* **GearExponent** defines the exponent that will be applied to the summed unit value, after it has been factored. Defaults to 3.
* **MechPartsCostPerTon** defines the base c-bill cost for each ton of mech parts stored. Defaults to 100.
* **MechPartsFactor** defines a divisor that will be applied to the summed tonnage of mech parts, after they have been factored. Defaults to 1.0.
* **MechPartsExponent** defines the exponent that will be applied to the summed tonnage of mech parts, after they have been factored. Defaults to 3.