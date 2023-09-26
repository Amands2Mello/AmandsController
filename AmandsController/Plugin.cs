using Aki.Reflection.Patching;
using BepInEx;
using System.Reflection;
using UnityEngine;
using EFT.UI;
using EFT.UI.DragAndDrop;
using System.Linq;
using EFT.InventoryLogic;
using System.Collections.Generic;
using EFT;
using HarmonyLib;
using BepInEx.Configuration;
using UnityEngine.UI;
using System.Threading.Tasks;
using Sirenix.Utilities;
using EFT.InputSystem;
using UnityEngine.EventSystems;

namespace AmandsController
{
    [BepInPlugin("com.Amanda.Controller", "Controller", "0.2.2")]
    public class AmandsControllerPlugin : BaseUnityPlugin
    {
        public static GameObject Hook;
        public static AmandsControllerClass AmandsControllerClassComponent;
        public static ConfigEntry<int> UserIndex { get; set; }
        public static ConfigEntry<int> DebugX { get; set; }
        public static ConfigEntry<int> DebugY { get; set; }
        public static ConfigEntry<bool> Magnetism { get; set; }
        public static ConfigEntry<float> Stickiness { get; set; }
        public static ConfigEntry<float> AutoAim { get; set; }
        public static ConfigEntry<float> StickinessSmooth { get; set; }
        public static ConfigEntry<float> AutoAimSmooth { get; set; }
        public static ConfigEntry<float> MagnetismRadius { get; set; }
        public static ConfigEntry<float> StickinessRadius { get; set; }
        public static ConfigEntry<float> AutoAimRadius { get; set; }
        public static ConfigEntry<float> AutoAimEnemyVelocity { get; set; }
        public static ConfigEntry<float> Radius { get; set; }
        public static ConfigEntry<Vector2> Sensitivity { get; set; }
        public static ConfigEntry<float> ScrollSensitivity { get; set; }
        public static ConfigEntry<float> LeanSensitivity { get; set; }
        public static ConfigEntry<float> LDeadzone { get; set; }
        public static ConfigEntry<float> RDeadzone { get; set; }
        public static ConfigEntry<float> DeadzoneBuffer { get; set; }
        public static ConfigEntry<float> FloorDecimalAdd { get; set; }
        public static ConfigEntry<float> DoubleClickDelay { get; set; }
        public static ConfigEntry<float> HoldDelay { get; set; }
        private void Awake()
        {
            Debug.LogError("Controller Awake()");
            Hook = new GameObject();
            Hook.name = "AmandsController";
            AmandsControllerClassComponent = Hook.AddComponent<AmandsControllerClass>();
            DontDestroyOnLoad(Hook);
        }

        private void Start()
        {
            UserIndex = Config.Bind("Controller", "User Index", 1, new ConfigDescription("", null, new ConfigurationManagerAttributes { Order = 270 }));
            DebugX = Config.Bind("Controller", "Debug X", 1920 / 2, new ConfigDescription("", null, new ConfigurationManagerAttributes { Order = 260 }));
            DebugY = Config.Bind("Controller", "Debug Y", 1080 / 2, new ConfigDescription("", null, new ConfigurationManagerAttributes { Order = 250 }));
            Magnetism = Config.Bind("Controller", "Magnetism", true, new ConfigDescription("", null, new ConfigurationManagerAttributes { Order = 240 }));
            Stickiness = Config.Bind("Controller", "Stickiness", 0.3f, new ConfigDescription("", null, new ConfigurationManagerAttributes { Order = 230 }));
            AutoAim = Config.Bind("Controller", "AutoAim", 0.25f, new ConfigDescription("", null, new ConfigurationManagerAttributes { Order = 220 }));
            StickinessSmooth = Config.Bind("Controller", "StickinessSmooth", 10f, new ConfigDescription("", null, new ConfigurationManagerAttributes { Order = 230 }));
            AutoAimSmooth = Config.Bind("Controller", "AutoAimSmooth", 10f, new ConfigDescription("", null, new ConfigurationManagerAttributes { Order = 220 }));
            MagnetismRadius = Config.Bind("Controller", "MagnetismRadius", 0.1f, new ConfigDescription("", null, new ConfigurationManagerAttributes { Order = 210 }));
            StickinessRadius = Config.Bind("Controller", "StickinessRadius", 0.2f, new ConfigDescription("", null, new ConfigurationManagerAttributes { Order = 200 }));
            AutoAimRadius = Config.Bind("Controller", "AutoAimRadius", 0.5f, new ConfigDescription("", null, new ConfigurationManagerAttributes { Order = 190 }));
            AutoAimEnemyVelocity = Config.Bind("Controller", "AutoAimEnemyVelocity", 0.1f, new ConfigDescription("", null, new ConfigurationManagerAttributes { Order = 188 }));
            Radius = Config.Bind("Controller", "Radius", 5f, new ConfigDescription("", null, new ConfigurationManagerAttributes { Order = 180, IsAdvanced = true }));
            Sensitivity = Config.Bind("Controller", "Sensitivity", new Vector2(20f,-12f), new ConfigDescription("", null, new ConfigurationManagerAttributes { Order = 170 }));
            ScrollSensitivity = Config.Bind("Controller", "ScrollSensitivity", 1f, new ConfigDescription("", null, new ConfigurationManagerAttributes { Order = 168 }));
            LeanSensitivity = Config.Bind("Controller", "LeanSensitivity", 50f, new ConfigDescription("", null, new ConfigurationManagerAttributes { Order = 160, IsAdvanced = true }));
            LDeadzone = Config.Bind("Controller", "LDeadzone", 0.25f, new ConfigDescription("", null, new ConfigurationManagerAttributes { Order = 150 }));
            RDeadzone = Config.Bind("Controller", "RDeadzone", 0.08f, new ConfigDescription("", null, new ConfigurationManagerAttributes { Order = 140 }));
            DeadzoneBuffer = Config.Bind("Controller", "DeadzoneBuffer", 0.5f, new ConfigDescription("", null, new ConfigurationManagerAttributes { Order = 130, IsAdvanced = true }));
            FloorDecimalAdd = Config.Bind("Controller", "FloorDecimalAdd", 0.005f, new ConfigDescription("", null, new ConfigurationManagerAttributes { Order = 120, IsAdvanced = true }));

            DoubleClickDelay = Config.Bind("Controller", "DoubleClickDelay", 0.3f, new ConfigDescription("", null, new ConfigurationManagerAttributes { Order = 110, IsAdvanced = true }));
            HoldDelay = Config.Bind("Controller", "HoldDelay", 0.3f, new ConfigDescription("", null, new ConfigurationManagerAttributes { Order = 100, IsAdvanced = true }));

            new AmandsLocalPlayerPatch().Enable();
            new AmandsTarkovApplicationPatch().Enable();
            new AmandsInventoryScreenShowPatch().Enable();
            new AmandsInventoryScreenClosePatch().Enable();
            new AmandsActionPanelPatch().Enable();
            new AmandsHealingLimbSelectorShowPatch().Enable();
            new AmandsHealingLimbSelectorClosePatch().Enable();
            new BattleStancePanelPatch().Enable();
            // Controller UI
            new TemplatedGridsViewShowPatch().Enable();
            new GeneratedGridsViewShowPatch().Enable();
            //new TradingGridViewShowPatch().Enable();
            //new TradingGridViewTraderShowPatch().Enable();
            //new TradingTableGridViewShowPatch().Enable();
            new GridViewHidePatch().Enable();
            new ContainedGridsViewClosePatch().Enable();
            new ItemSpecificationPanelShowPatch().Enable();
            new ItemSpecificationPanelClosePatch().Enable();
            new EquipmentTabShowPatch().Enable();
            new EquipmentTabHidePatch().Enable();
            new ContainersPanelShowPatch().Enable();
            new ContainersPanelClosePatch().Enable();
            new SearchButtonShowPatch().Enable();
            new SearchButtonClosePatch().Enable();
            new ScrollRectNoDragOnEnable().Enable();
            new ScrollRectNoDragOnDisable().Enable();

            new ItemViewOnBeginDrag().Enable();
            new ItemViewOnEndDrag().Enable();
            new ItemViewUpdate().Enable();
            new DraggedItemViewMethod_3().Enable();
            new TooltipMethod_0().Enable();
            new SimpleStashPanelShowPatch().Enable();
        }
    }
    public class AmandsLocalPlayerPatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            return typeof(LocalPlayer).GetMethod("Create", BindingFlags.Static | BindingFlags.Public);
        }
        [PatchPostfix]
        private static void PatchPostFix(ref Task<LocalPlayer> __result)
        {
            LocalPlayer localPlayer = __result.Result;
            if (localPlayer != null && localPlayer.IsYourPlayer)
            {
                AmandsControllerPlugin.AmandsControllerClassComponent.UpdateController(localPlayer);
            }
        }
    }
    public class AmandsTarkovApplicationPatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            return typeof(TarkovApplication).GetMethod("Init", BindingFlags.Instance | BindingFlags.NonPublic);
        }
        [PatchPostfix]
        private static void PatchPostFix(ref TarkovApplication __instance, InputTree inputTree)
        {
            AmandsControllerPlugin.AmandsControllerClassComponent.inputTree = inputTree;
        }
    }
    public class AmandsInventoryScreenShowPatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            return typeof(InventoryScreen).GetMethod("Show", BindingFlags.Instance | BindingFlags.Public);
        }
        [PatchPostfix]
        private static void PatchPostFix(ref InventoryScreen __instance)
        {
            AmandsControllerPlugin.AmandsControllerClassComponent.UpdateInterfaceBinds(true);
        }
    }
    public class AmandsInventoryScreenClosePatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            return typeof(InventoryScreen).GetMethod("Close", BindingFlags.Instance | BindingFlags.Public);
        }
        [PatchPostfix]
        private static void PatchPostFix(ref InventoryScreen __instance)
        {
            AmandsControllerPlugin.AmandsControllerClassComponent.UpdateInterfaceBinds(false);
        }
    }
    public class AmandsActionPanelPatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            return typeof(ActionPanel).GetMethod("method_0", BindingFlags.Instance | BindingFlags.NonPublic);
        }
        [PatchPostfix]
        private static void PatchPostFix(ref ActionPanel __instance)
        {
            // Possible Explosion of the whole mod
            bool Enabled = Traverse.Create(__instance).Field("bool_0").GetValue<bool>();
            AmandsControllerPlugin.AmandsControllerClassComponent.UpdateActionPanelBinds(Enabled);
        }
    }
    public class AmandsHealingLimbSelectorShowPatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            return typeof(HealingLimbSelector).GetMethod("Show", BindingFlags.Instance | BindingFlags.Public);
        }
        [PatchPostfix]
        private static void PatchPostFix(ref HealingLimbSelector __instance)
        {
            AmandsControllerPlugin.AmandsControllerClassComponent.UpdateHealingLimbSelectorBinds(true);
        }
    }
    public class AmandsHealingLimbSelectorClosePatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            return typeof(HealingLimbSelector).GetMethod("Close", BindingFlags.Instance | BindingFlags.Public);
        }
        [PatchPostfix]
        private static void PatchPostFix(ref HealingLimbSelector __instance)
        {
            AmandsControllerPlugin.AmandsControllerClassComponent.UpdateHealingLimbSelectorBinds(false);
        }
    }
    /*public class AmandsGamePlayerOwnerPatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            return typeof(GamePlayerOwner).GetMethod("Create", BindingFlags.Static | BindingFlags.Public);
        }
        [PatchPostfix]
        private static void PatchPostFix(ref GamePlayerOwner __instance, Player player)
        {
            ConsoleScreen.Log("GamePlayerOwner Create()");
            LocalPlayer localPlayer = player as LocalPlayer;
            if (localPlayer != null && localPlayer.IsYourPlayer)
            {
                AmandsControllerPlugin.AmandsControllerClassComponent.UpdateController(localPlayer);
            }
        }
    }*/
    public class BattleStancePanelPatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            return typeof(BattleStancePanel).GetMethod("Show", BindingFlags.Instance | BindingFlags.Public);
        }
        [PatchPostfix]
        private static void PatchPostFix(ref BattleStancePanel __instance)
        {
            AmandsControllerPlugin.AmandsControllerClassComponent.speedSlider = Traverse.Create(__instance).Field("_speedSlider").GetValue<Slider>();
        }
    }
    // Controller UI
    public class TemplatedGridsViewShowPatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            return typeof(TemplatedGridsView).GetMethod("Show", BindingFlags.Instance | BindingFlags.Public);
        }
        [PatchPostfix]
        private static void PatchPostFix(ref TemplatedGridsView __instance)
        {
            if (__instance.GetComponentInParent<GridWindow>() != null)
            {
                AmandsControllerPlugin.AmandsControllerClassComponent.DebugStuff("Show GridWindow");
                if (AmandsControllerPlugin.AmandsControllerClassComponent.containedGridsViews.Contains(__instance)) return;
                AmandsControllerPlugin.AmandsControllerClassComponent.containedGridsViews.Add(__instance);
            }
            else
            {
                AmandsControllerPlugin.AmandsControllerClassComponent.DebugStuff("Show Grids");
                foreach (GridView gridView in __instance.GridViews)
                {
                    if (AmandsControllerPlugin.AmandsControllerClassComponent.gridViews.Contains(gridView)) continue;
                    AmandsControllerPlugin.AmandsControllerClassComponent.gridViews.Add(gridView);
                }
            }
        }
    }
    public class GeneratedGridsViewShowPatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            return typeof(GeneratedGridsView).GetMethod("Show", BindingFlags.Instance | BindingFlags.Public);
        }
        [PatchPostfix]
        private static void PatchPostFix(ref GeneratedGridsView __instance)
        {
            if (__instance.GetComponentInParent<GridWindow>() != null)
            {
                AmandsControllerPlugin.AmandsControllerClassComponent.DebugStuff("Show GridWindow 2");
                if (AmandsControllerPlugin.AmandsControllerClassComponent.containedGridsViews.Contains(__instance)) return;
                AmandsControllerPlugin.AmandsControllerClassComponent.containedGridsViews.Add(__instance);
            }
            else
            {
                AmandsControllerPlugin.AmandsControllerClassComponent.DebugStuff("Show Grids 2");
                foreach (GridView gridView in __instance.GridViews)
                {
                    if (AmandsControllerPlugin.AmandsControllerClassComponent.gridViews.Contains(gridView)) continue;
                    AmandsControllerPlugin.AmandsControllerClassComponent.gridViews.Add(gridView);
                }
            }
        }
    }
    public class TradingGridViewShowPatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            return typeof(TradingGridView).GetMethods().First((MethodInfo x) => x.Name == "Show" && x.GetParameters().Count() == 5);
        }
        [PatchPostfix]
        private static void PatchPostFix(ref TradingGridView __instance)
        {
            if (AmandsControllerPlugin.AmandsControllerClassComponent.gridViews.Contains(__instance)) return;
            AmandsControllerPlugin.AmandsControllerClassComponent.gridViews.Add(__instance);
        }
    }
    public class TradingGridViewTraderShowPatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            return typeof(TradingGridView).GetMethods().First((MethodInfo x) => x.Name == "Show" && x.GetParameters().Count() == 6 && x.GetParameters()[5].Name == "raiseEvents");
        }
        [PatchPostfix]
        private static void PatchPostFix(ref TradingGridView __instance)
        {
            if (AmandsControllerPlugin.AmandsControllerClassComponent.gridViews.Contains(__instance)) return;
            AmandsControllerPlugin.AmandsControllerClassComponent.gridViews.Add(__instance);
        }
    }
    public class TradingTableGridViewShowPatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            return typeof(TradingTableGridView).GetMethods().First((MethodInfo x) => x.Name == "Show" && x.GetParameters().Count() == 4);
        }
        [PatchPostfix]
        private static void PatchPostFix(ref TradingTableGridView __instance)
        {
            AmandsControllerPlugin.AmandsControllerClassComponent.tradingTableGridView = __instance;
        }
    }
    public class ContainedGridsViewClosePatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            return typeof(ContainedGridsView).GetMethod("Close", BindingFlags.Instance | BindingFlags.Public);
        }
        [PatchPostfix]
        private static void PatchPostFix(ref ContainedGridsView __instance)
        {
            if (__instance == null) return;

            if (__instance.GetComponentInParent<GridWindow>() != null)
            {
                AmandsControllerPlugin.AmandsControllerClassComponent.DebugStuff("Close GridWindow");
                foreach (GridView gridView in __instance.GridViews)
                {
                    //if (AmandsControllerPlugin.AmandsControllerClassComponent.currentGridView == gridView) AmandsControllerPlugin.AmandsControllerClassComponent.currentGridView = null;
                }
                AmandsControllerPlugin.AmandsControllerClassComponent.containedGridsViews.Remove(__instance);
            }
        }
    }
    public class GridViewHidePatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            return typeof(GridView).GetMethod("Hide", BindingFlags.Instance | BindingFlags.Public);
        }
        [PatchPostfix]
        private static void PatchPostFix(ref GridView __instance)
        {
            if (__instance == null) return;
            if (AmandsControllerPlugin.AmandsControllerClassComponent.tradingTableGridView == __instance)
            {
                AmandsControllerPlugin.AmandsControllerClassComponent.DebugStuff("Hide TradingTableGridView");
                AmandsControllerPlugin.AmandsControllerClassComponent.tradingTableGridView = null;
                //AmandsControllerPlugin.AmandsControllerClassComponent.currentTradingTableGridView = null;
                return;
            }
            if (!AmandsControllerPlugin.AmandsControllerClassComponent.gridViews.Contains(__instance)) return;
            AmandsControllerPlugin.AmandsControllerClassComponent.DebugStuff("Hide GridView");
            //if (AmandsControllerPlugin.AmandsControllerClassComponent.currentGridView == __instance) AmandsControllerPlugin.AmandsControllerClassComponent.currentGridView = null;
            AmandsControllerPlugin.AmandsControllerClassComponent.gridViews.Remove(__instance);
        }
    }
    public class EquipmentTabShowPatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            return typeof(EquipmentTab).GetMethod("Show", BindingFlags.Instance | BindingFlags.Public);
        }
        [PatchPostfix]
        private static void PatchPostFix(ref EquipmentTab __instance, InventoryControllerClass inventoryController)
        {
            AmandsControllerPlugin.AmandsControllerClassComponent.DebugStuff("Show EquipmentTab");
            if (__instance.gameObject.name == "Gear Panel")
            {
                AmandsControllerPlugin.AmandsControllerClassComponent.DebugStuff("__instance.gameObject.name == Gear Panel");
                foreach (KeyValuePair<EquipmentSlot, SlotView> slotView in Traverse.Create(__instance).Field("_slotViews").GetValue<Dictionary<EquipmentSlot, SlotView>>())
                {
                    if (slotView.Key == EquipmentSlot.FirstPrimaryWeapon || slotView.Key == EquipmentSlot.SecondPrimaryWeapon)
                    {
                        AmandsControllerPlugin.AmandsControllerClassComponent.weaponsSlotViews.Add(slotView.Value);
                    }
                    else if (slotView.Key == EquipmentSlot.ArmBand)
                    {
                        AmandsControllerPlugin.AmandsControllerClassComponent.armbandSlotView = slotView.Value;
                    }
                    else
                    {
                        AmandsControllerPlugin.AmandsControllerClassComponent.equipmentSlotViews.Add(slotView.Value);
                    }
                }
            }
            else
            {
                foreach (KeyValuePair<EquipmentSlot, SlotView> slotView in Traverse.Create(__instance).Field("_slotViews").GetValue<Dictionary<EquipmentSlot, SlotView>>())
                {
                    if (slotView.Key == EquipmentSlot.FirstPrimaryWeapon || slotView.Key == EquipmentSlot.SecondPrimaryWeapon)
                    {
                        AmandsControllerPlugin.AmandsControllerClassComponent.lootWeaponsSlotViews.Add(slotView.Value);
                    }
                    else if (slotView.Key == EquipmentSlot.ArmBand)
                    {
                        if (slotView.Value.Slot != null) AmandsControllerPlugin.AmandsControllerClassComponent.lootArmbandSlotView = slotView.Value;
                    }
                    else
                    {
                        AmandsControllerPlugin.AmandsControllerClassComponent.lootEquipmentSlotViews.Add(slotView.Value);
                    }
                }
            }
        }
    }
    public class EquipmentTabHidePatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            return typeof(EquipmentTab).GetMethod("Hide", BindingFlags.Instance | BindingFlags.Public);
        }
        [PatchPostfix]
        private static void PatchPostFix(ref EquipmentTab __instance)
        {
            AmandsControllerPlugin.AmandsControllerClassComponent.DebugStuff("Hide EquipmentTab");
            if (__instance.gameObject.name == "Gear Panel")
            {
                //AmandsControllerPlugin.AmandsControllerClassComponent.currentEquipmentSlotView = null;
                //AmandsControllerPlugin.AmandsControllerClassComponent.currentWeaponsSlotView = null;
                //AmandsControllerPlugin.AmandsControllerClassComponent.currentArmbandSlotView = null;
                AmandsControllerPlugin.AmandsControllerClassComponent.equipmentSlotViews.Clear();
                AmandsControllerPlugin.AmandsControllerClassComponent.weaponsSlotViews.Clear();
                AmandsControllerPlugin.AmandsControllerClassComponent.armbandSlotView = null;
            }
            else
            {
                //AmandsControllerPlugin.AmandsControllerClassComponent.currentEquipmentSlotView = null;
                //AmandsControllerPlugin.AmandsControllerClassComponent.currentWeaponsSlotView = null;
                //AmandsControllerPlugin.AmandsControllerClassComponent.currentArmbandSlotView = null;
                AmandsControllerPlugin.AmandsControllerClassComponent.lootEquipmentSlotViews.Clear();
                AmandsControllerPlugin.AmandsControllerClassComponent.lootWeaponsSlotViews.Clear();
                AmandsControllerPlugin.AmandsControllerClassComponent.lootArmbandSlotView = null;
            }
        }
    }
    public class ContainersPanelShowPatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            return typeof(ContainersPanel).GetMethod("Show", BindingFlags.Instance | BindingFlags.Public);
        }
        [PatchPostfix]
        private static void PatchPostFix(ref ContainersPanel __instance)
        {
            AmandsControllerPlugin.AmandsControllerClassComponent.DebugStuff("Show ContainersPanel");
            if (__instance.gameObject.name == "Containers Scrollview")
            {
                foreach (KeyValuePair<EquipmentSlot, SlotView> slotView in Traverse.Create(__instance).Field("dictionary_0").GetValue<Dictionary<EquipmentSlot, SlotView>>())
                {
                    if (slotView.Key != EquipmentSlot.Pockets) AmandsControllerPlugin.AmandsControllerClassComponent.containersSlotViews.Add(slotView.Value);
                }
            }
            else
            {
                foreach (KeyValuePair<EquipmentSlot, SlotView> slotView in Traverse.Create(__instance).Field("dictionary_0").GetValue<Dictionary<EquipmentSlot, SlotView>>())
                {
                    if (slotView.Key != EquipmentSlot.Pockets) AmandsControllerPlugin.AmandsControllerClassComponent.lootContainersSlotViews.Add(slotView.Value);
                }
            }
        }
    }
    public class ContainersPanelClosePatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            return typeof(ContainersPanel).GetMethod("Close", BindingFlags.Instance | BindingFlags.Public);
        }
        [PatchPostfix]
        private static void PatchPostFix(ref ContainersPanel __instance)
        {
            AmandsControllerPlugin.AmandsControllerClassComponent.DebugStuff("Close ContainersPanel");
            if (__instance.gameObject.name == "Containers Scrollview")
            {
                //AmandsControllerPlugin.AmandsControllerClassComponent.currentContainersSlotView = null;
                AmandsControllerPlugin.AmandsControllerClassComponent.containersSlotViews.Clear();
            }
            else
            {
                //AmandsControllerPlugin.AmandsControllerClassComponent.currentContainersSlotView = null;
                AmandsControllerPlugin.AmandsControllerClassComponent.lootContainersSlotViews.Clear();
            }
        }
    }


    public class ItemSpecificationPanelShowPatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            return typeof(ItemSpecificationPanel).GetMethod("Show", BindingFlags.Instance | BindingFlags.Public);
        }
        [PatchPostfix]
        private static void PatchPostFix(ref ItemSpecificationPanel __instance)
        {
            AmandsControllerPlugin.AmandsControllerClassComponent.DebugStuff("Show ItemSpecificationPanel");
            if (AmandsControllerPlugin.AmandsControllerClassComponent.itemSpecificationPanels.Contains(__instance)) return;
            AmandsControllerPlugin.AmandsControllerClassComponent.itemSpecificationPanels.Add(__instance);
        }
    }
    public class ItemSpecificationPanelClosePatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            return typeof(ItemSpecificationPanel).GetMethod("Close", BindingFlags.Instance | BindingFlags.Public);
        }
        [PatchPostfix]
        private static void PatchPostFix(ref ItemSpecificationPanel __instance)
        {
            if (__instance == null) return;
            AmandsControllerPlugin.AmandsControllerClassComponent.DebugStuff("Hide ItemSpecificationPanel");
            AmandsControllerPlugin.AmandsControllerClassComponent.itemSpecificationPanels.Remove(__instance);
        }
    }
    public class SearchButtonShowPatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            return typeof(SearchButton).GetMethod("SetEnabled", BindingFlags.Instance | BindingFlags.Public);
        }
        [PatchPostfix]
        private static void PatchPostFix(ref SearchButton __instance, bool value)
        {
            if (__instance.gameObject.activeSelf)
            {
                if (AmandsControllerPlugin.AmandsControllerClassComponent.searchButtons.Contains(__instance)) return;
                AmandsControllerPlugin.AmandsControllerClassComponent.DebugStuff("Enabled SearchButton");
                AmandsControllerPlugin.AmandsControllerClassComponent.searchButtons.Add(__instance);
            }
            else
            {
                if (!AmandsControllerPlugin.AmandsControllerClassComponent.searchButtons.Contains(__instance)) return;
                AmandsControllerPlugin.AmandsControllerClassComponent.DebugStuff("Disable SearchButton");
                AmandsControllerPlugin.AmandsControllerClassComponent.searchButtons.Remove(__instance);
            }
        }
    }
    public class SearchButtonClosePatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            return typeof(SearchButton).GetMethod("Close", BindingFlags.Instance | BindingFlags.Public);
        }
        [PatchPostfix]
        private static void PatchPostFix(ref SearchButton __instance)
        {
            if (!AmandsControllerPlugin.AmandsControllerClassComponent.searchButtons.Contains(__instance)) return;
            AmandsControllerPlugin.AmandsControllerClassComponent.DebugStuff("Close SearchButton");
            AmandsControllerPlugin.AmandsControllerClassComponent.searchButtons.Remove(__instance);
        }
    }
    public class ItemViewOnBeginDrag : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            return typeof(ItemView).GetMethod("OnBeginDrag", BindingFlags.Instance | BindingFlags.Public);
        }
        [PatchPrefix]
        private static void PatchPreFix(ref ItemView __instance, PointerEventData eventData)
        {
            if (AmandsControllerPlugin.AmandsControllerClassComponent.Dragging)
            {
                AmandsControllerPlugin.AmandsControllerClassComponent.AmandsControllerCancelDrag();
            }
        }
    }
    public class ItemViewOnEndDrag : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            return typeof(ItemView).GetMethod("OnEndDrag", BindingFlags.Instance | BindingFlags.Public);
        }
        [PatchPostfix]
        private static void PatchPostFix(ref ItemView __instance, PointerEventData eventData)
        {
            if (AmandsControllerPlugin.AmandsControllerClassComponent.Dragging)
            {
                AmandsControllerPlugin.AmandsControllerClassComponent.AmandsControllerCancelDrag();
            }
        }
    }
    public class ItemViewUpdate : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            return typeof(ItemView).GetMethod("Update", BindingFlags.Instance | BindingFlags.NonPublic);
        }
        [PatchPrefix]
        private static bool PatchPreFix(ref ItemView __instance)
        {
            return !AmandsControllerPlugin.AmandsControllerClassComponent.Dragging;
        }
    }
    public class DraggedItemViewMethod_3 : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            return typeof(DraggedItemView).GetMethod("method_3", BindingFlags.Instance | BindingFlags.NonPublic);
        }
        [PatchPrefix]
        private static bool PatchPreFix(ref DraggedItemView __instance)
        {
            if (AmandsControllerPlugin.AmandsControllerClassComponent.Dragging)
            {
                RectTransform RectTransform_0 = Traverse.Create(__instance).Property("RectTransform_0").GetValue<RectTransform>();
                RectTransform_0.position = AmandsControllerPlugin.AmandsControllerClassComponent.globalPosition;
                return false;
            }
            else
            {
                return true;
            }
        }
    }
    public class TooltipMethod_0 : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            return typeof(Tooltip).GetMethod("method_0", BindingFlags.Instance | BindingFlags.NonPublic);
        }
        [PatchPrefix]
        private static void PatchPreFix(ref ItemView __instance, ref Vector2 position)
        {
            if (AmandsControllerPlugin.AmandsControllerClassComponent.Dragging)
            {
                position = AmandsControllerPlugin.AmandsControllerClassComponent.globalPosition;
            }
        }
    }
    public class ScrollRectNoDragOnEnable : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            return typeof(ScrollRectNoDrag).GetMethod("OnEnable", BindingFlags.Instance | BindingFlags.NonPublic);
        }
        [PatchPostfix]
        private static void PatchPostFix(ref ScrollRectNoDrag __instance)
        {
            if (!AmandsControllerPlugin.AmandsControllerClassComponent.scrollRectNoDrags.Contains(__instance))
            {
                AmandsControllerPlugin.AmandsControllerClassComponent.scrollRectNoDrags.Add(__instance);
            }
        }
    }
    public class ScrollRectNoDragOnDisable : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            return typeof(ScrollRectNoDrag).GetMethod("OnDisable", BindingFlags.Instance | BindingFlags.NonPublic);
        }
        [PatchPostfix]
        private static void PatchPostFix(ref ScrollRectNoDrag __instance)
        {
            AmandsControllerPlugin.AmandsControllerClassComponent.scrollRectNoDrags.Remove(__instance);
        }
    }
    public class SimpleStashPanelShowPatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            return typeof(SimpleStashPanel).GetMethod("Show", BindingFlags.Instance | BindingFlags.Public);
        }
        [PatchPostfix]
        private static void PatchPostFix(ref SimpleStashPanel __instance)
        {
            ScrollRect scrollRect = Traverse.Create(__instance).Field("_stashScroll").GetValue<ScrollRect>();
            if (scrollRect != null)
            {
                GridViewMagnifier gridViewMagnifier = scrollRect.gameObject.GetComponent<GridViewMagnifier>();
                if (gridViewMagnifier != null)
                {
                    GridView gridView = Traverse.Create(gridViewMagnifier).Field("_gridView").GetValue<GridView>();
                    AmandsControllerPlugin.AmandsControllerClassComponent.SimpleStashGridView = gridView;
                }
            }
        }
    }
}
