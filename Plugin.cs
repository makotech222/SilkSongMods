using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;
using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using HarmonyLib;
using HutongGames.PlayMaker.Actions;
using TeamCherry.NestedFadeGroup;
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
        _altAddItem = new KeyboardShortcut(KeyCode.Equals, KeyCode.LeftControl);
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
        Patch.AddYellowToolSlots = Config.Bind("1. Cheats", "Add yellow tool slots to unlocked crests", 0, "").Value;
        Patch.AddBlueToolSlots = Config.Bind("1. Cheats", "Add blue tool slots to unlocked crests", 0, "").Value;
        Patch.DisableVignette = Config.Bind("1. Cheats", "Disable Vignette", false, "").Value;
        Patch.InfToolUsage = Config.Bind("1. Cheats", "Infinite Tool Usage", false, "").Value;
        Patch.AlwaysFleaBrew = Config.Bind("1. Cheats", "Flea Brew effect always on", false, "").Value;
        Patch.AlwaysNailElementImbue = Config.Bind("1. Cheats", "Flintslate effect always on with given element", "None", "Rest at bench once to initiate buff. Allowed Values: None, Fire, Poison").Value;
        Patch.DisableNailElementImbueSpriteEffect = Config.Bind("1. Cheats", "Remove Flintslate sprite effects", false, "Graphical change only. Turns off flashing effect and glow effect").Value;
    }

}

public class Patch
{
    internal static ManualLogSource Logger;
    public static int RosaryMultiplier { get; set; }
    public static int ShardsMultiplier { get; set; }
    public static bool SilkNeverDecreases { get; set; }
    public static int AddYellowToolSlots { get; set; }
    public static int AddBlueToolSlots { get; set; }
    public static bool DisableVignette { get; set; }
    public static bool InfToolUsage { get; set; }
    public static bool AlwaysFleaBrew { get; set; }
    public static string AlwaysNailElementImbue { get; set; }
    public static bool DisableNailElementImbueSpriteEffect { get; set; }

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
        if (!__instance.IsUnlocked || (AddYellowToolSlots == 0 && AddBlueToolSlots == 0))
            return;

        var slots = Traverse.Create(newCrestData).Field("slots").GetValue() as ToolCrest.SlotInfo[];
        var t = slots.ToList();
        float vecX = -4f;
        float vecY = -2f;
        for (int i = 0; i < AddBlueToolSlots; i++)
        {
            t.Add(new ToolCrest.SlotInfo()
            {
                Type = ToolItemType.Blue,
                IsLocked = false,
                Position = new Vector2 (vecX, vecY),
            });
            vecX += 1f;
            if (vecX >= 3f)
            {
                vecX = -4f;
                vecY -= 1f;
            }
        }
        for (int i = 0; i < AddYellowToolSlots; i++)
        {
            t.Add(new ToolCrest.SlotInfo()
            {
                Type = ToolItemType.Yellow,
                IsLocked = false,
                Position = new Vector2(vecX, vecY),
            });
            vecX += 1f;
            if (vecX >= 3f)
            {
                vecX = -4f;
                vecY -= 1f;
            }
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

    [HarmonyPatch(typeof(HeroNailImbuement), "SetElement")]
    [HarmonyPrefix]
    private static void HeroNailImbuement_Patch(HeroNailImbuement __instance, ref NailElements element)
    {
        if (AlwaysNailElementImbue == "None" || (AlwaysNailElementImbue != "Fire" && AlwaysNailElementImbue != "Poison"))
            return;
        if (Enum.TryParse<NailElements>(AlwaysNailElementImbue, true, out NailElements newElement))
        {
            element = newElement;
        }
    }

    [HarmonyPatch(typeof(HeroNailImbuement), "SetElement")]
    [HarmonyPostfix]
    private static void HeroNailImbuementPost_Patch(HeroNailImbuement __instance, ref NailElements element)
    {
        if (!DisableNailElementImbueSpriteEffect)
            return;
        
        var sprite = Traverse.Create(__instance).Field("imbuedHeroLightRenderer").GetValue() as SpriteRenderer;
        if (sprite != null)
        {
            sprite.enabled = false;
        }
        var spriteFlash = Traverse.Create(__instance).Field("spriteFlash").GetValue() as SpriteFlash;
        if (spriteFlash != null)
        {
            var spriteFlashHandle = (SpriteFlash.FlashHandle)Traverse.Create(__instance).Field("flashingHandle").GetValue();
            spriteFlash.CancelRepeatingFlash(spriteFlashHandle);
        }
        var imbuedHeroLightGroup = Traverse.Create(__instance).Field("imbuedHeroLightGroup").GetValue() as NestedFadeGroupBase;
        if (imbuedHeroLightGroup != null)
        {
            imbuedHeroLightGroup.FadeTo(0f, 1, null, false, null);
        }
    }
}
