﻿using System;
using HarmonyLib;
using System.Collections.Generic;
using System.Linq;

namespace TripleBronze
{
    [HarmonyPatch]
    public static class Patches
    {
        public static BepInEx.Logging.ManualLogSource logger = BepInEx.Logging.Logger.CreateLogSource(TripleBronze.PLUGIN_NAME);

        [HarmonyPostfix]
        [HarmonyPatch(typeof(ObjectDB), "Awake")]
        public static void ObjectDB_Awake_DebugPatch(ref ObjectDB __instance)
        {
            if (!TripleBronze.DebugMessagesEnabled) return;
            foreach (var i in __instance.m_items)
            {
                var item = i.GetComponent<ItemDrop>();
                logger.LogDebug($"Item \"{i.name}\": \"{item.m_itemData.m_shared.m_name}\"");
            }
            foreach (var r in __instance.m_recipes.Where(r => r.m_item?.m_itemData?.m_shared?.m_name != null && r.m_resources.Length != 0))
            {
                logger.LogDebug($"Recipe: \"{r.m_item.m_itemData.m_shared.m_name} (x{r.m_amount})\" requires:" + string.Join(
                    ", ",
                    r.m_resources
                      .Where(res => res.m_resItem?.m_itemData?.m_shared?.m_name != null)
                      .Select(res => $"\"{res.m_resItem.m_itemData.m_shared.m_name}\" (x{res.m_amount})")
                  )
                );
            }

            List<CraftingStation> craftingStations = __instance.m_recipes
                .Where(r => r != null)
                .Select(i => i.m_craftingStation)
                .Where(r => r != null)
                .Distinct()
                .ToList();

            foreach (var cs in craftingStations)
            {
                logger.LogDebug($"Crafting station: \"{cs.m_name}\"");
            }
        }

        private static bool flagBronze = false; // dumb fix
                [HarmonyPostfix]
        [HarmonyPatch(typeof(ObjectDB), "Awake")]
        public static void ObjectDB_Awake_BronzePatch(ref ObjectDB __instance)
        {
            if (flagBronze) return;

            foreach (var recipe in __instance.m_recipes)
            {
                if (recipe.m_item?.m_itemData.m_shared.m_name != @"$item_bronze") continue;
                recipe.m_amount *= (int)TripleBronze.BronzeMultiplier;
                flagBronze = true;
            }
        }

        private static Recipe makeRecipe(Tuple<CraftingStation, int> station,
                                         CraftingStation repairStation,
                                         Tuple<ItemDrop, int> resultItem,
                                         params Tuple<ItemDrop, int>[] ingredients) => new Recipe()
        {
            m_enabled = true,
            m_item = resultItem.Item1,
            m_amount = resultItem.Item2,
            m_resources = ingredients.Select(t => new Piece.Requirement() { m_resItem = t.Item1, m_amount = t.Item2 }).ToArray(),
            m_craftingStation = station.Item1,
            m_repairStation = repairStation,
            m_minStationLevel = station.Item2
        };

        private static bool flagQuickSmelt = false; // dumb fix
        [HarmonyPostfix]
        [HarmonyPatch(typeof(ObjectDB), "Awake")]
        public static void ObjectDB_Awake_QuickSmeltPatch(ref ObjectDB __instance)
        {
            if (flagQuickSmelt || !TripleBronze.CraftBarsInForgeEnabled) return;

            List<CraftingStation> craftingStations = __instance.m_recipes
                .Where(r => r != null)
                .Select(i => i.m_craftingStation)
                .Where(r => r != null)
                .Distinct()
                .ToList();

            ItemDrop woodDrop      = __instance.GetItemPrefab(@"Wood")?.GetComponent<ItemDrop>();
            ItemDrop coalDrop      = __instance.GetItemPrefab(@"Coal")?.GetComponent<ItemDrop>();
            ItemDrop copperOreDrop = __instance.GetItemPrefab(@"CopperOre")?.GetComponent<ItemDrop>();
            ItemDrop copperDrop    = __instance.GetItemPrefab(@"Copper")?.GetComponent<ItemDrop>();
            ItemDrop tinOreDrop    = __instance.GetItemPrefab(@"TinOre")?.GetComponent<ItemDrop>();
            ItemDrop tinDrop       = __instance.GetItemPrefab(@"Tin")?.GetComponent<ItemDrop>();
            ItemDrop ironOreDrop   = __instance.GetItemPrefab(@"IronOre")?.GetComponent<ItemDrop>();
            ItemDrop ironScrapDrop = __instance.GetItemPrefab(@"IronScrap")?.GetComponent<ItemDrop>();
            ItemDrop ironDrop      = __instance.GetItemPrefab(@"Iron")?.GetComponent<ItemDrop>();

            if (woodDrop == null)      { logger.LogError($"woodDrop is null!"); return; }
            if (coalDrop == null)      { logger.LogError($"coalDrop is null!"); return; }
            if (copperOreDrop == null) { logger.LogError($"copperOreDrop is null!"); return; }
            if (copperDrop == null)    { logger.LogError($"copperDrop is null!"); return; }
            if (tinOreDrop == null)    { logger.LogError($"tinOreDrop is null!"); return; }
            if (tinDrop == null)       { logger.LogError($"tinDrop is null!"); return; }
            if (ironScrapDrop == null) { logger.LogError($"ironScrapDrop is null!"); return; }
            if (ironOreDrop == null)   { logger.LogError($"ironOreDrop is null!"); return; }
            if (ironDrop == null)      { logger.LogError($"ironDrop is null!"); return; }

            CraftingStation forge = craftingStations.Find(c => c.m_name.Contains(@"forge"));
            CraftingStation workbench = craftingStations.Find(c => c.m_name.Contains(@"workbench"));

            Recipe coalRecipe = makeRecipe(Tuple.Create(workbench, 4),
                                           null,
                                           Tuple.Create(coalDrop, 1),
                                           Tuple.Create(woodDrop, 1));

            Recipe copperBarRecipe = makeRecipe(Tuple.Create(forge, 2),
                                                null,
                                                Tuple.Create(copperDrop, 1),
                                                Tuple.Create(coalDrop, (int)TripleBronze.CoalPerBar),
                                                Tuple.Create(copperOreDrop, 1));
            Recipe tinBarRecipe = makeRecipe(Tuple.Create(forge, 3),
                                             null,
                                             Tuple.Create(tinDrop, 1),
                                             Tuple.Create(coalDrop, (int)TripleBronze.CoalPerBar),
                                             Tuple.Create(tinOreDrop, 1));
            Recipe ironBarRecipe1 = makeRecipe(Tuple.Create(forge, 4),
                                               null,
                                               Tuple.Create(ironDrop, 1),
                                               Tuple.Create(coalDrop, (int)TripleBronze.CoalPerBar),
                                               Tuple.Create(ironScrapDrop, 1));
            Recipe ironBarRecipe2 = makeRecipe(Tuple.Create(forge, 4),
                                               null,
                                               Tuple.Create(ironDrop, 1),
                                               Tuple.Create(coalDrop, (int)TripleBronze.CoalPerBar),
                                               Tuple.Create(ironOreDrop, 1));

            Recipe[] recipes = new[] { coalRecipe, copperBarRecipe, tinBarRecipe, ironBarRecipe1, ironBarRecipe2 };

            __instance.m_recipes.AddRange(recipes);

            var player = Game.instance.m_playerPrefab.GetComponent<Player>();
            if (player == null)
            {
                logger.LogInfo("Player not yet instantiated, not going to add quicksmelt recipes to player.");
                return;
            }
            foreach (var recipe in recipes)
            {
                player.AddKnownRecipe(recipe);
            }

            flagQuickSmelt = true;
        }
    }
}