using BepInEx;
using BepInEx.Configuration;
using Digitalroot.Valheim.Common;
using JetBrains.Annotations;
using Jotunn.Configs;
using Jotunn.Entities;
using Jotunn.Managers;
using Jotunn.Utils;
using System;
using System.Linq;

namespace TripleBronzeJVL
{
  [BepInDependency(Jotunn.Main.ModGuid, "2.3.0")]
  [NetworkCompatibility(CompatibilityLevel.EveryoneMustHaveMod, VersionStrictness.Minor)]
  [BepInPlugin(Guid, Name, Version)]
  public class Main : BaseUnityPlugin, ITraceableLogging
  {
    public const string Version = "1.1.0";
    public const string Name = "Digitalroot TripleBronzeJVL";
    public const string Guid = "digitalroot.mods.triplebronze.jvl";
    public const string Namespace = "TripleBronzeJVL";
    public static Main Instance;

    public static ConfigEntry<int> NexusId;
    public ConfigEntry<int> BronzeMultiplier;
    public ConfigEntry<bool> CraftBarsInForge;
    public ConfigEntry<int> CoalPerBar;

    public Main()
    {
      Instance = this;
#if DEBUG
      EnableTrace = true;
      Log.RegisterSource(Instance);
#else
      EnableTrace = false;
#endif
    }

    [UsedImplicitly]
    private void Awake()
    {
      Config.SaveOnConfigSet = true;
      NexusId = Config.Bind("General", "NexusID", 1463, new ConfigDescription("Nexus mod ID for updates", null, new ConfigurationManagerAttributes { IsAdminOnly = false, Browsable = false, ReadOnly = true }));
      BronzeMultiplier = Config.Bind("General", "Bronze Multiplier", 3, new ConfigDescription("The normal recipe result for bronze is multiplied by this value.", null, new ConfigurationManagerAttributes { IsAdminOnly = true }));
      CraftBarsInForge = Config.Bind("General", "Craft Bars In Forge", true, new ConfigDescription("Allows bypassing the smelter by creating crafting recipes for ores/scraps -> bars.", null, new ConfigurationManagerAttributes { IsAdminOnly = true }));
      CoalPerBar = Config.Bind("General", "Coal Per Bar", 5, new ConfigDescription("Can create bars using this many coal. Ores/Scrap are crafted to bars in a 1:1 ratio.", null, new ConfigurationManagerAttributes { IsAdminOnly = true }));

      PrefabManager.OnVanillaPrefabsAvailable += ItemManagerOnVanillaItemsAvailableAddNewRecipes;
      ItemManager.OnItemsRegisteredFejd += ItemManagerOnVanillaItemsAvailableUpdateBronze;

      BronzeMultiplier.SettingChanged += BronzeMultiplierSettingChanged;
      CraftBarsInForge.SettingChanged += CraftBarsInForgeSettingChanged;
      CoalPerBar.SettingChanged += CoalPerBarSettingChanged;
    }

    private void CoalPerBarSettingChanged(object sender, EventArgs e)
    {
      UpdateColeForNewBars(Digitalroot.Valheim.Common.Names.ItemDropNames.Copper);
      UpdateColeForNewBars(Digitalroot.Valheim.Common.Names.ItemDropNames.Tin);
      UpdateColeForNewBars(Digitalroot.Valheim.Common.Names.ItemDropNames.Iron);
      UpdateColeForNewBars(Digitalroot.Valheim.Common.Names.ItemDropNames.Silver);
      UpdateColeForNewBars(Digitalroot.Valheim.Common.Names.ItemDropNames.BlackMetal);
      UpdateColeForNewBars(Digitalroot.Valheim.Common.Names.ItemDropNames.Flametal);
    }

    private void CraftBarsInForgeSettingChanged(object sender, EventArgs e)
    {
      UpdateEnabledNewBarsRecipes(Digitalroot.Valheim.Common.Names.ItemDropNames.Coal);
      UpdateEnabledNewBarsRecipes(Digitalroot.Valheim.Common.Names.ItemDropNames.Copper);
      UpdateEnabledNewBarsRecipes(Digitalroot.Valheim.Common.Names.ItemDropNames.Tin);
      UpdateEnabledNewBarsRecipes(Digitalroot.Valheim.Common.Names.ItemDropNames.Iron);
      UpdateEnabledNewBarsRecipes(Digitalroot.Valheim.Common.Names.ItemDropNames.Silver);
      UpdateEnabledNewBarsRecipes(Digitalroot.Valheim.Common.Names.ItemDropNames.BlackMetal);
      UpdateEnabledNewBarsRecipes(Digitalroot.Valheim.Common.Names.ItemDropNames.Flametal);
    }

    private void BronzeMultiplierSettingChanged(object sender, EventArgs e) => ItemManagerOnVanillaItemsAvailableUpdateBronze();

    private void UpdateBronze()
    {
      foreach (Recipe instanceMRecipe in ObjectDB.instance.m_recipes.Where(r => r.m_item?.name == Digitalroot.Valheim.Common.Names.ItemDropNames.Bronze))
      {
        instanceMRecipe.m_amount = 1 * BronzeMultiplier.Value;
        Log.Debug(Instance, $"Updated {instanceMRecipe.m_item.name} of {instanceMRecipe.name}, set m_amount to {instanceMRecipe.m_amount}");
      }

      ItemManager.OnItemsRegisteredFejd -= ItemManagerOnVanillaItemsAvailableUpdateBronze;
    }

    private void UpdateColeForNewBars(string itemDropName)
    {
      foreach (Recipe instanceMRecipe in ObjectDB.instance.m_recipes.Where(r => r.m_item?.name == itemDropName))
      {
        var r = instanceMRecipe.m_resources.FirstOrDefault(r => r.m_resItem.name == Digitalroot.Valheim.Common.Names.ItemDropNames.Coal);
        if (r == null) continue;

        r.m_amount = CoalPerBar.Value;
        Log.Debug(Instance, $"Updated {instanceMRecipe.m_item.name} of {instanceMRecipe.name}, set m_amount of {r.m_resItem.name} to {r.m_amount}");
      }
    }

    private void UpdateEnabledNewBarsRecipes(string itemDropName)
    {
      foreach (Recipe instanceMRecipe in ObjectDB.instance.m_recipes.Where(r => r.m_item?.name == itemDropName))
      {
        instanceMRecipe.m_enabled = CraftBarsInForge.Value;
        Log.Debug(Instance, $"Updated {instanceMRecipe.m_item.name} of {instanceMRecipe.name}, set m_enabled to {instanceMRecipe.m_enabled}");
      }
    }

    private void ItemManagerOnVanillaItemsAvailableUpdateBronze()
    {
      foreach (Recipe instanceMRecipe in ObjectDB.instance.m_recipes.Where(r => r.m_item?.name == Digitalroot.Valheim.Common.Names.ItemDropNames.Bronze))
      {
        instanceMRecipe.m_amount = instanceMRecipe.name switch
        {
          "Recipe_Bronze" => 1 * BronzeMultiplier.Value
          , "Recipe_Bronze5" => 5 * BronzeMultiplier.Value
          , _ => instanceMRecipe.m_amount
        };
        Log.Debug(Instance, $"Updated {instanceMRecipe.m_item.name} of {instanceMRecipe.name}, set m_amount to {instanceMRecipe.m_amount}");
      }

      ItemManager.OnItemsRegisteredFejd -= ItemManagerOnVanillaItemsAvailableUpdateBronze;
    }

    private void ItemManagerOnVanillaItemsAvailableAddNewRecipes()
    {
      PrefabManager.OnVanillaPrefabsAvailable -= ItemManagerOnVanillaItemsAvailableAddNewRecipes;
      if (!CraftBarsInForge.Value) return;
      AddNewCoalRecipe();
      AddNewBarRecipe(Digitalroot.Valheim.Common.Names.ItemDropNames.Copper, Digitalroot.Valheim.Common.Names.ItemDropNames.CopperOre, 1);
      AddNewBarRecipe(Digitalroot.Valheim.Common.Names.ItemDropNames.Tin, Digitalroot.Valheim.Common.Names.ItemDropNames.TinOre, 1);
      AddNewBarRecipe(Digitalroot.Valheim.Common.Names.ItemDropNames.Iron, Digitalroot.Valheim.Common.Names.ItemDropNames.IronOre, 5);
      AddNewBarRecipe(Digitalroot.Valheim.Common.Names.ItemDropNames.Iron, Digitalroot.Valheim.Common.Names.ItemDropNames.IronScrap, 5);
      AddNewBarRecipe(Digitalroot.Valheim.Common.Names.ItemDropNames.Silver, Digitalroot.Valheim.Common.Names.ItemDropNames.SilverOre, 5);
      AddNewBarRecipe(Digitalroot.Valheim.Common.Names.ItemDropNames.BlackMetal, Digitalroot.Valheim.Common.Names.ItemDropNames.BlackMetalScrap, 6);
      AddNewBarRecipe(Digitalroot.Valheim.Common.Names.ItemDropNames.Flametal, Digitalroot.Valheim.Common.Names.ItemDropNames.FlametalOre, 7);
    }

    private void AddNewCoalRecipe()
    {
      CustomRecipe customRecipe = new CustomRecipe(new RecipeConfig
      {
        Amount = 1
        , Item = Digitalroot.Valheim.Common.Names.ItemDropNames.Coal
        , CraftingStation = Digitalroot.Valheim.Common.Names.CraftingStationNames.Forge
        , Name = $"TripleBronzeQOL_{Digitalroot.Valheim.Common.Names.ItemDropNames.Wood}_to_{Digitalroot.Valheim.Common.Names.ItemDropNames.Coal}"
        , MinStationLevel = 1
        , Requirements = new[] // Resources and amount needed for it to be crafted
        {
          new RequirementConfig { Item = Digitalroot.Valheim.Common.Names.ItemDropNames.Wood, Amount = 1 }
        }
      });
      ItemManager.Instance.AddRecipe(customRecipe);
      Log.Debug(Instance, $"Added {Digitalroot.Valheim.Common.Names.ItemDropNames.Coal} to {Digitalroot.Valheim.Common.Names.CraftingStationNames.Forge}");
    }

    private void AddNewBarRecipe(string barName, string oreName, uint minStationLevel)
    {
      CustomRecipe customRecipe = new CustomRecipe(new RecipeConfig
      {
        Amount = 1, Item = barName, CraftingStation = Digitalroot.Valheim.Common.Names.CraftingStationNames.Forge, Name = $"TripleBronzeQOL_{oreName}_to_{barName}", MinStationLevel = Convert.ToInt32(minStationLevel), Requirements = new[] // Resources and amount needed for it to be crafted
        {
          new RequirementConfig { Item = oreName, Amount = 1 }
          , new RequirementConfig { Item = Digitalroot.Valheim.Common.Names.ItemDropNames.Coal, Amount = CoalPerBar.Value },
        }
      });
      ItemManager.Instance.AddRecipe(customRecipe);
      Log.Debug(Instance, $"Added {barName} to {Digitalroot.Valheim.Common.Names.CraftingStationNames.Forge}");
    }

    #region Implementation of ITraceableLogging

    /// <inheritdoc />
    public string Source => Namespace;

    /// <inheritdoc />
    public bool EnableTrace { get; }

    #endregion
  }
}
