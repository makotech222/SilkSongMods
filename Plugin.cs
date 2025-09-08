using System;
using System.Collections.Generic;
using System.Linq;
using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using HarmonyLib;
using HutongGames.PlayMaker.Actions;
using UnityEngine;

namespace MakoMod;

[BepInPlugin(MyPluginInfo.PLUGIN_GUID, MyPluginInfo.PLUGIN_NAME, MyPluginInfo.PLUGIN_VERSION)]
public class SilksongQOLMod : BaseUnityPlugin
{
    private KeyboardShortcut _addItem;
    private KeyboardShortcut _altAddItem;
    private KeyboardShortcut _removeItem;
    private KeyboardShortcut _altRemoveItem;
    private KeyboardShortcut _upItem;
    private KeyboardShortcut _downItem;
    private int _itemIndex = -1;
    public static CollectableItem SelectedItem { get; set; }
    private void Awake()
    {
        Patch.Logger = base.Logger;
        Patch.Logger.LogInfo($"Plugin {MyPluginInfo.PLUGIN_GUID} is loaded!");
        LoadConfig();
        Harmony.CreateAndPatchAll(typeof(Patch));
        _addItem = new KeyboardShortcut(KeyCode.KeypadPlus, KeyCode.LeftControl);
        _altAddItem = new KeyboardShortcut(KeyCode.Plus, KeyCode.LeftControl);
        _removeItem = new KeyboardShortcut(KeyCode.KeypadMinus, KeyCode.LeftControl);
        _altRemoveItem = new KeyboardShortcut(KeyCode.Minus, KeyCode.LeftControl);

        _upItem = new KeyboardShortcut(KeyCode.UpArrow, KeyCode.LeftControl);
        _downItem = new KeyboardShortcut(KeyCode.DownArrow, KeyCode.LeftControl);
    }

    private void Update()
    {
        try
        {
            if ((_addItem.IsDown() || _altAddItem.IsDown()) && _itemIndex > 0)
            {
                var items = CollectableItemManager.Instance.GetAllCollectables().OrderBy(x => x.GetDisplayName(CollectableItem.ReadSource.Inventory)).ToList();
                var item = items[_itemIndex];
                Logger.LogInfo($"Adding Item: {item.GetDisplayName(CollectableItem.ReadSource.Inventory)}");
                //CollectableItemManager.AddItem(item, 1);
                item.Collect();
            }
            if ((_removeItem.IsDown() || _altRemoveItem.IsDown()) && _itemIndex > 0)
            {
                var items = CollectableItemManager.Instance.GetAllCollectables().OrderBy(x => x.GetDisplayName(CollectableItem.ReadSource.Inventory)).ToList();
                var item = items[_itemIndex];
                Logger.LogInfo($"Removing Item: {item.GetDisplayName(CollectableItem.ReadSource.Inventory)}");
                //CollectableItemManager.AddItem(item, 1);
                item.Take();
            }
            if (_upItem.IsDown())
            {
                var items = CollectableItemManager.Instance.GetAllCollectables().OrderBy(x => x.GetDisplayName(CollectableItem.ReadSource.Inventory)).ToList();
                _itemIndex += 1;
                _itemIndex  = _itemIndex > items.Count - 1 ? 0 : _itemIndex;
                var item = items[_itemIndex];
                Logger.LogInfo($"Selected Item: {item.GetDisplayName(CollectableItem.ReadSource.Inventory)}");
                CollectableUIMsg collectableUIMsg = CollectableUIMsg.Spawn(item, null, false);
            }
            if (_downItem.IsDown())
            {
                var items = CollectableItemManager.Instance.GetAllCollectables().OrderBy(x => x.GetDisplayName(CollectableItem.ReadSource.Inventory)).ToList();
                _itemIndex -= 1;
                _itemIndex = _itemIndex < 0 ? items.Count - 1 : _itemIndex;
                var item = items[_itemIndex];
                Logger.LogInfo($"Selected Item: {item.GetDisplayName(CollectableItem.ReadSource.Inventory)}");
                CollectableUIMsg collectableUIMsg = CollectableUIMsg.Spawn(item, null, false);
            }

        }
        catch (Exception ex)
        {
           
        }
    }

    public void LoadConfig()
    {
        Patch.RosaryMultiplier = Config.Bind("1. Cheats", "Rosary recieved multiplier", 1, "").Value;
        Patch.ShardsMultiplier = Config.Bind("1. Cheats", "Shards recieved multiplier", 1, "").Value;
        Patch.SilkNeverDecreases = Config.Bind("1. Cheats", "Silk Never Decreases", false, "").Value;
        Patch.AddToolSlots = Config.Bind("1. Cheats", "Add 25 yellow and blue slots to unlocked crests", false, "Experimental. Does affect save data, so be sure to backup").Value;
        Patch.DisableVignette = Config.Bind("1. Cheats", "Disable Vignette", false, "").Value;
        Patch.InfToolUsage = Config.Bind("1. Cheats", "Infinite Tool Usage", false, "").Value;
        Patch.AlwaysFleaBrew = Config.Bind("1. Cheats", "Flea Brew effect always on", false, "").Value;
    }

}

public class Patch
{
    internal static new ManualLogSource Logger;
    public static int RosaryMultiplier { get; set; }
    public static int ShardsMultiplier { get; set; }
    public static bool SilkNeverDecreases { get; set; }
    public static bool AddToolSlots { get; set; }
    public static bool DisableVignette { get; set; }
    public static bool InfToolUsage { get; set; }
    public static bool AlwaysFleaBrew { get; set; }

    [HarmonyPatch(typeof(PlayerData), "AddGeo")]
    [HarmonyPrefix]
    private static void AddGeoPrefix(PlayerData __instance, ref int amount)
    {
        amount *= RosaryMultiplier;
    }

    [HarmonyPatch(typeof(PlayerData), "AddShards")]
    [HarmonyPrefix]
    private static void AddShardsPrefix(PlayerData __instance, ref int amount)
    {
        amount *= ShardsMultiplier;
    }

    [HarmonyPatch(typeof(PlayerData), "TakeSilk")]
    [HarmonyPrefix]
    private static void TakeSilkPrefix(PlayerData __instance, ref int amount)
    {
        amount = (SilkNeverDecreases ? 0 : amount);
    }

    //[HarmonyPatch(typeof(ToolItemManager), "IsToolEquipped",typeof(string))]
    //[HarmonyPostfix]
    //private static void IsEquippedPostFix(ToolItemManager __instance, string name, ref bool __result)
    //{
    //    if (!EquipAllTools)
    //        return;
    //    var tool = ToolItemManager.GetToolByName(name);
    //    if (tool.IsUnlockedNotHidden && tool.Type != ToolItemType.Red && tool.Type != ToolItemType.Skill)
    //    {
    //        __result = true;
    //    }
    //}

    [HarmonyPatch(typeof(HeroController), "Respawn")]
    [HarmonyPostfix]
    private static void HeroRespawnPostfix(HeroController __instance)
    {
        if (!DisableVignette)
            return;
        __instance.vignette.enabled = false;
        Transform transform = __instance.transform.Find("Vignette");
        if (transform != null)
        {
            transform.gameObject.SetActive(false);
        }
    }

    [HarmonyPatch(typeof(HeroController), "Start")]
    [HarmonyPostfix]
    private static void HeroStartPostfix(HeroController __instance)
    {
        if (!DisableVignette)
            return;
        __instance.vignette.enabled = false;
        Transform transform = __instance.transform.Find("Vignette");
        if (transform != null)
        {
            transform.gameObject.SetActive(false);
        }
    }

    [HarmonyPatch(typeof(InventoryToolCrest), "Setup")]
    [HarmonyPrefix]
    private static void ToolItemManagerSetEquippedCrestPostfix(InventoryToolCrest __instance, ref ToolCrest newCrestData)
    {
        if (!__instance.IsUnlocked || !AddToolSlots)
            return;

        var slots = Traverse.Create(newCrestData).Field("slots").GetValue() as ToolCrest.SlotInfo[];
        var t = slots.ToList();
        var existingBlue = slots.Where(x => x.Type == ToolItemType.Blue).Count();
        var existingYellow = slots.Where(x => x.Type == ToolItemType.Yellow).Count();
        Logger.LogInfo($"Adding crest slots for {newCrestData?.name}. Current blue: {existingBlue} current yellow: {existingYellow}");
        float vecX = -4f;
        float vecY = -2f;
        while (existingBlue <= 15)
        {
            t.Add(new ToolCrest.SlotInfo()
            {
                Type = ToolItemType.Blue,
                IsLocked = false,
                Position = new Vector2 (vecX, vecY),
            });
            vecX += 1f;
            if (vecX >= 4f)
            {
                vecX = -4f;
                vecY -= 1f;
            }
            existingBlue++;
        }
        while (existingYellow <= 15)
        {
            t.Add(new ToolCrest.SlotInfo()
            {
                Type = ToolItemType.Yellow,
                IsLocked = false,
                Position = new Vector2(vecX, vecY),
            });
            vecX += 1f;
            if (vecX >= 4f)
            {
                vecX = -4f;
                vecY -= 1f;
            }
            existingYellow++;
        }
        Traverse.Create(newCrestData).Field("slots").SetValue(t.ToArray());
    }

    [HarmonyPatch(typeof(HeroController), "DidUseAttackTool")]
    [HarmonyPrefix]
    private static void InfToolUsage_Patch(HeroController __instance, ref ToolItemsData.Data toolData)
    {
        if (!InfToolUsage)
            return;
        var tool = Traverse.Create(__instance).Field("willThrowTool").GetValue() as ToolItem;
        if (!tool.IsCustomUsage && toolData.AmountLeft > 0)
        {
            toolData.AmountLeft++;
            tool.SavedData = toolData;
        }
    }

    [HarmonyPatch(typeof(HeroController), "IsUsingQuickening", MethodType.Getter)]
    [HarmonyPostfix]
    private static void AlwaysFleaBrew_Patch(HeroController __instance, ref bool __result)
    {
        if (!AlwaysFleaBrew)
            return;
        __result = true;
    }
}
