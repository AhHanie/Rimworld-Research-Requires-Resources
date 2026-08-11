# Example XML for `ResearchMaterialCostsExtension`

These are reference examples only. They are not loaded by the mod: the def and patch files they used to live in have been removed from `1.6/Defs/ResearchCosts` and `1.6/Patches` so they never appear in a real game. Copy the snippets you need into your own def or patch files.

`ResearchMaterialCostsExtension` describes a one-time material payment. Colonists must deliver every required material to a research bench before research on that project can begin; once fully paid, the project researches exactly like vanilla with no further material requests.

## Standalone example research project

This formerly lived in `1.6/Defs/ResearchCosts/Examples.xml`.

### Basic material cost

```xml
<ResearchProjectDef>
  <defName>RRR_ExampleCost</defName>
  <label>funded metallurgy</label>
  <description>Example project demonstrating a material cost. Not part of the normal tech tree; disable or remove this file once you have configured costs on your own research projects.</description>
  <baseCost>800</baseCost>
  <techLevel>Industrial</techLevel>
  <tab>Main</tab>
  <researchViewX>22.0</researchViewX>
  <researchViewY>0.0</researchViewY>
  <modExtensions>
    <li Class="Research_Requires_Resources.ResearchMaterialCostsExtension">
      <requirements>
        <li><id>steel</id><thingDef>Steel</thingDef><count>100</count></li>
        <li><id>components</id><thingDef>ComponentIndustrial</thingDef><count>4</count></li>
        <li><id>cloth</id><thingDef>Cloth</thingDef><count>20</count></li>
      </requirements>
      <consumedRefundPercent>0.5</consumedRefundPercent>
    </li>
  </modExtensions>
</ResearchProjectDef>
```

### Custom refund percentage and progress scaling

```xml
<ResearchProjectDef>
  <defName>RRR_ExampleCustomRefund</defName>
  <label>recoverable prototype program</label>
  <description>Example project demonstrating a custom refund percentage that scales down as research progress accumulates. Not part of the normal tech tree; disable or remove this file once you have configured costs on your own research projects.</description>
  <baseCost>1200</baseCost>
  <techLevel>Industrial</techLevel>
  <tab>Main</tab>
  <researchViewX>22.0</researchViewX>
  <researchViewY>1.0</researchViewY>
  <modExtensions>
    <li Class="Research_Requires_Resources.ResearchMaterialCostsExtension">
      <consumedRefundPercent>0.75</consumedRefundPercent>
      <scaleConsumedRefundByRemainingProgress>true</scaleConsumedRefundByRemainingProgress>
      <requirements>
        <li><id>fabric</id><thingDef>Cloth</thingDef><count>60</count></li>
      </requirements>
    </li>
  </modExtensions>
</ResearchProjectDef>
```

## Patch example for a vanilla research project

This `PatchOperationAddModExtension` patch formerly lived in `1.6/Patches`. It attaches `ResearchMaterialCostsExtension` to an existing vanilla `ResearchProjectDef` by `defName` instead of defining a new project.

### Basic material cost (was `Example_PatchOperationAddModExtension_UpfrontOnly.xml`)

Adds a one-time funding requirement to `Smithing`.

```xml
<Patch>
  <Operation Class="PatchOperationAddModExtension">
    <xpath>Defs/ResearchProjectDef[defName="Smithing"]</xpath>
    <value>
      <li Class="Research_Requires_Resources.ResearchMaterialCostsExtension">
        <requirements>
          <li><id>steel</id><thingDef>Steel</thingDef><count>75</count></li>
          <li><id>wood</id><thingDef>WoodLog</thingDef><count>50</count></li>
        </requirements>
        <consumedRefundPercent>0.5</consumedRefundPercent>
      </li>
    </value>
  </Operation>
</Patch>
```
