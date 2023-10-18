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
using Unity.Burst.CompilerServices;

namespace AmandsController
{
    [BepInPlugin("com.Amanda.Controller", "Controller", "0.2.9")]
    public class AmandsControllerPlugin : BaseUnityPlugin
    {
        public static GameObject Hook;
        public static AmandsControllerClass AmandsControllerClassComponent;
        public static InputGetKeyDownLeftControlPatch LeftControl;
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
        public static ConfigEntry<Color> SelectColor { get; set; }
        public static ConfigEntry<Vector2> BlockPosition { get; set; }
        public static ConfigEntry<Vector2> BlockSize { get; set; }
        public static ConfigEntry<int> BlockSpacing { get; set; }
        public static ConfigEntry<int> BlockIconSpacing { get; set; }
        public static ConfigEntry<int> PressFontSize { get; set; }
        public static ConfigEntry<int> HoldDoubleClickFontSize { get; set; }
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
            BlockPosition = Config.Bind("ControllerUI", "BlockPosition", new Vector2(950f, 80f), new ConfigDescription("", null, new ConfigurationManagerAttributes { Order = 1500 }));
            BlockSize = Config.Bind("ControllerUI", "BlockSize", new Vector2(40f, 40f), new ConfigDescription("", null, new ConfigurationManagerAttributes { Order = 1400 }));
            BlockSpacing = Config.Bind("ControllerUI", "BlockSpacing", 16, new ConfigDescription("", null, new ConfigurationManagerAttributes { Order = 1300 }));
            BlockIconSpacing = Config.Bind("ControllerUI", "BlockIconSpacing", 8, new ConfigDescription("", null, new ConfigurationManagerAttributes { Order = 1200 }));
            PressFontSize = Config.Bind("ControllerUI", "PressFontSize", 20, new ConfigDescription("", null, new ConfigurationManagerAttributes { Order = 1100 }));
            HoldDoubleClickFontSize = Config.Bind("ControllerUI", "HoldDoubleClickFontSize", 12, new ConfigDescription("", null, new ConfigurationManagerAttributes { Order = 1000 }));

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
            AutoAimEnemyVelocity = Config.Bind("Controller", "AutoAimEnemyVelocity", 0.01f, new ConfigDescription("", null, new ConfigurationManagerAttributes { Order = 188 }));
            Radius = Config.Bind("Controller", "Radius", 5f, new ConfigDescription("", null, new ConfigurationManagerAttributes { Order = 180, IsAdvanced = true }));
            Sensitivity = Config.Bind("Controller", "Sensitivity", new Vector2(20f,-12f), new ConfigDescription("", null, new ConfigurationManagerAttributes { Order = 170 }));
            ScrollSensitivity = Config.Bind("Controller", "ScrollSensitivity", 1f, new ConfigDescription("", null, new ConfigurationManagerAttributes { Order = 168 }));
            LeanSensitivity = Config.Bind("Controller", "LeanSensitivity", 50f, new ConfigDescription("", null, new ConfigurationManagerAttributes { Order = 160, IsAdvanced = true }));
            LDeadzone = Config.Bind("Controller", "LDeadzone", 0.25f, new ConfigDescription("", null, new ConfigurationManagerAttributes { Order = 150 }));
            RDeadzone = Config.Bind("Controller", "RDeadzone", 0.08f, new ConfigDescription("", null, new ConfigurationManagerAttributes { Order = 140 }));
            DeadzoneBuffer = Config.Bind("Controller", "DeadzoneBuffer", 0.5f, new ConfigDescription("", null, new ConfigurationManagerAttributes { Order = 130, IsAdvanced = true }));
            FloorDecimalAdd = Config.Bind("Controller", "FloorDecimalAdd", 0.005f, new ConfigDescription("", null, new ConfigurationManagerAttributes { Order = 120, IsAdvanced = true }));

            DoubleClickDelay = Config.Bind("Controller", "DoubleClickDelay", 0.25f, new ConfigDescription("", null, new ConfigurationManagerAttributes { Order = 110, IsAdvanced = true }));
            HoldDelay = Config.Bind("Controller", "HoldDelay", 0.25f, new ConfigDescription("", null, new ConfigurationManagerAttributes { Order = 100, IsAdvanced = true }));
            SelectColor = Config.Bind("Controller", "SelectColor", new Color(1f, 0.7659f, 0.3518f, 1), new ConfigDescription("", null, new ConfigurationManagerAttributes { Order = 90 }));

            new AmandsLocalPlayerPatch().Enable();
            new AmandsTarkovApplicationPatch().Enable();
            new AmandsSSAAPatch().Enable();
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
            new SimpleContextMenuButtonShowPatch().Enable();
            new SimpleContextMenuButtonClosePatch().Enable();
            new ScrollRectNoDragOnEnable().Enable();
            new ScrollRectNoDragOnDisable().Enable();

            new ItemViewOnBeginDrag().Enable();
            new ItemViewOnEndDrag().Enable();
            new ItemViewUpdate().Enable();
            new DraggedItemViewMethod_3().Enable();
            new TooltipMethod_0().Enable();
            new SimpleStashPanelShowPatch().Enable();
            new SplitDialogShowPatch().Enable();
            new SplitDialogHidePatch().Enable();
            new SearchableSlotViewShowPatch().Enable();
            new SearchableSlotViewHidePatch().Enable();

            LeftControl = new InputGetKeyDownLeftControlPatch();
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
    public class AmandsSSAAPatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            return typeof(SSAA).GetMethod("Awake", BindingFlags.Instance | BindingFlags.NonPublic);
        }
        [PatchPostfix]
        private static void PatchPostFix(ref SSAA __instance)
        {
            AmandsControllerPlugin.AmandsControllerClassComponent.currentSSAA = __instance;
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
            AmandsControllerPlugin.AmandsControllerClassComponent.Tabs = Traverse.Create(__instance).Field("dictionary_0").GetValue<Dictionary<InventoryScreen.EInventoryTab, Tab>>();
            AmandsControllerPlugin.AmandsControllerClassComponent.UpdateInterfaceBinds(true);
            AmandsControllerPlugin.AmandsControllerClassComponent.UpdateInterface(__instance);
            AsynControllerUIMoveToClosest();
        }
        private static async void AsynControllerUIMoveToClosest()
        {
            await Task.Delay(200);
            AmandsControllerPlugin.AmandsControllerClassComponent.ControllerUIMoveToClosest(false);
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
                if (AmandsControllerPlugin.AmandsControllerClassComponent.containedGridsViews.Contains(__instance)) return;
                AmandsControllerPlugin.AmandsControllerClassComponent.containedGridsViews.Add(__instance);
            }
            else
            {
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
                if (AmandsControllerPlugin.AmandsControllerClassComponent.containedGridsViews.Contains(__instance)) return;
                AmandsControllerPlugin.AmandsControllerClassComponent.containedGridsViews.Add(__instance);
            }
            else
            {
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
                AmandsControllerPlugin.AmandsControllerClassComponent.tradingTableGridView = null;
                //AmandsControllerPlugin.AmandsControllerClassComponent.currentTradingTableGridView = null;
                return;
            }
            if (!AmandsControllerPlugin.AmandsControllerClassComponent.gridViews.Contains(__instance)) return;
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
            if (__instance.gameObject.name == "Gear Panel")
            {
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
            if (__instance.transform.parent.gameObject.name == "Scrollview Parent")
            {
                foreach (KeyValuePair<EquipmentSlot, SlotView> slotView in Traverse.Create(__instance).Field("dictionary_0").GetValue<Dictionary<EquipmentSlot, SlotView>>())
                {
                    if (slotView.Key != EquipmentSlot.Pockets)
                    {
                        AmandsControllerPlugin.AmandsControllerClassComponent.containersSlotViews.Add(slotView.Value);
                    }
                }
            }
            else
            {
                foreach (KeyValuePair<EquipmentSlot, SlotView> slotView in Traverse.Create(__instance).Field("dictionary_0").GetValue<Dictionary<EquipmentSlot, SlotView>>())
                {
                    if (slotView.Key != EquipmentSlot.Pockets) AmandsControllerPlugin.AmandsControllerClassComponent.lootContainersSlotViews.Add(slotView.Value);
                }
                SlotView dogtagSlotView = Traverse.Create(__instance).Field("slotView_0").GetValue<SlotView>();
                if (dogtagSlotView != null && dogtagSlotView.gameObject.activeSelf)
                {
                    AmandsControllerPlugin.AmandsControllerClassComponent.dogtagSlotView = dogtagSlotView;
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
            if (__instance.transform.parent.gameObject.name == "Scrollview Parent")
            {
                //AmandsControllerPlugin.AmandsControllerClassComponent.currentContainersSlotView = null;
                AmandsControllerPlugin.AmandsControllerClassComponent.containersSlotViews.Clear();
                AmandsControllerPlugin.AmandsControllerClassComponent.specialSlotSlotViews.Clear();
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
                AmandsControllerPlugin.AmandsControllerClassComponent.searchButtons.Add(__instance);
            }
            else
            {
                if (!AmandsControllerPlugin.AmandsControllerClassComponent.searchButtons.Contains(__instance)) return;
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
            if (AmandsControllerPlugin.AmandsControllerClassComponent.Dragging && (AmandsControllerPlugin.AmandsControllerClassComponent.InRaid || AmandsControllerPlugin.AmandsControllerClassComponent.ForceOutsideRaid))
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
            if (AmandsControllerPlugin.AmandsControllerClassComponent.Dragging && (AmandsControllerPlugin.AmandsControllerClassComponent.InRaid || AmandsControllerPlugin.AmandsControllerClassComponent.ForceOutsideRaid))
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
            if (!(AmandsControllerPlugin.AmandsControllerClassComponent.InRaid || AmandsControllerPlugin.AmandsControllerClassComponent.ForceOutsideRaid)) return true;
            return (!AmandsControllerPlugin.AmandsControllerClassComponent.Dragging);
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
            if (AmandsControllerPlugin.AmandsControllerClassComponent.Dragging && (AmandsControllerPlugin.AmandsControllerClassComponent.InRaid || AmandsControllerPlugin.AmandsControllerClassComponent.ForceOutsideRaid))
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
            if (AmandsControllerPlugin.AmandsControllerClassComponent.connected && (AmandsControllerPlugin.AmandsControllerClassComponent.InRaid || AmandsControllerPlugin.AmandsControllerClassComponent.ForceOutsideRaid)) position = AmandsControllerPlugin.AmandsControllerClassComponent.globalPosition + new Vector2(32f,-19f);
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
                if (AmandsControllerPlugin.AmandsControllerClassComponent.scrollRectNoDrags.Count == 0)
                {
                    RectTransform rectTransform;
                    rectTransform = __instance.GetComponent<RectTransform>();
                    AmandsControllerPlugin.AmandsControllerClassComponent.currentScrollRectNoDrag = __instance;
                    AmandsControllerPlugin.AmandsControllerClassComponent.currentScrollRectNoDragRectTransform = rectTransform;
                }
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
        public static bool Searching = false;
        protected override MethodBase GetTargetMethod()
        {
            return typeof(SimpleStashPanel).GetMethod("Show", BindingFlags.Instance | BindingFlags.Public);
        }
        [PatchPostfix]
        private static void PatchPostFix(ref SimpleStashPanel __instance)
        {
            if (!Searching && (AmandsControllerPlugin.AmandsControllerClassComponent.InRaid || AmandsControllerPlugin.AmandsControllerClassComponent.ForceOutsideRaid)) ShowAsync(__instance);
        }
        private async static void ShowAsync(SimpleStashPanel instance)
        {
            Searching = true;
            await Task.Delay(100);
            SearchableItemView searchableItemView = Traverse.Create(instance).Field("_simplePanel").GetValue<SearchableItemView>();
            if (searchableItemView != null)
            {
                GeneratedGridsView generatedGridsView = Traverse.Create(searchableItemView).Field("containedGridsView_0").GetValue<GeneratedGridsView>();
                if (generatedGridsView != null)
                {
                    if (generatedGridsView.GridViews.Count() == 0)
                    {
                        ShowAsync(instance);
                        return;
                    }
                    GridView gridView = generatedGridsView.GridViews[0];
                    if (gridView != null)
                    {
                        Searching = false;
                        AmandsControllerPlugin.AmandsControllerClassComponent.SimpleStashGridView = gridView;
                        AmandsControllerPlugin.AmandsControllerClassComponent.ControllerUISelect(gridView);
                    }
                }
            }
            Searching = false;
        }
    }
    public class SimpleContextMenuButtonShowPatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            return typeof(SimpleContextMenuButton).GetMethod("Show", BindingFlags.Instance | BindingFlags.Public);
        }
        [PatchPostfix]
        private static void PatchPostFix(ref SimpleContextMenuButton __instance)
        {
            if (AmandsControllerPlugin.AmandsControllerClassComponent.simpleContextMenuButtons.Contains(__instance)) return;
            AmandsControllerPlugin.AmandsControllerClassComponent.simpleContextMenuButtons.Add(__instance);
            if (!AmandsControllerPlugin.AmandsControllerClassComponent.ContextMenu)
            {
                AmandsControllerPlugin.AmandsControllerClassComponent.UpdateContextMenuBinds(true);
                if ((AmandsControllerPlugin.AmandsControllerClassComponent.InRaid || AmandsControllerPlugin.AmandsControllerClassComponent.ForceOutsideRaid)) AmandsControllerPlugin.AmandsControllerClassComponent.ControllerUISelect(__instance);
                //ControllerUIMoveAsync(__instance);
            }
        }
        /*private async static void ControllerUIMoveAsync(SimpleContextMenuButton instance)
        {
            await Task.Delay(100);
            AmandsControllerPlugin.AmandsControllerClassComponent.ControllerUISelect(instance);
        }*/
    }
    public class SimpleContextMenuButtonClosePatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            return typeof(SimpleContextMenuButton).GetMethod("Close", BindingFlags.Instance | BindingFlags.Public);
        }
        [PatchPostfix]
        private static void PatchPostFix(ref SimpleContextMenuButton __instance)
        {
            if (__instance == null)
            {
                goto Skip;
            }
            AmandsControllerPlugin.AmandsControllerClassComponent.simpleContextMenuButtons.Remove(__instance);
            Skip:
            if (AmandsControllerPlugin.AmandsControllerClassComponent.simpleContextMenuButtons.Count == 0)
            {
                AmandsControllerPlugin.AmandsControllerClassComponent.UpdateContextMenuBinds(false);
            }
        }
    }
    public class SplitDialogShowPatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            return typeof(SplitDialog).GetMethods().First(x => x.Name == "Show" && x.GetParameters().Count() > 7);
        }
        [PatchPostfix]
        private static void PatchPostFix(ref SplitDialog __instance)
        {
            AmandsControllerPlugin.AmandsControllerClassComponent.splitDialog = __instance;
            AmandsControllerPlugin.AmandsControllerClassComponent.UpdateSplitDialogBinds(true);
        }
    }
    public class SplitDialogHidePatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            return typeof(SplitDialog).GetMethod("Hide", BindingFlags.Instance | BindingFlags.Public);
        }
        [PatchPostfix]
        private static void PatchPostFix(ref SplitDialog __instance)
        {
            AmandsControllerPlugin.AmandsControllerClassComponent.splitDialog = null;
            AmandsControllerPlugin.AmandsControllerClassComponent.UpdateSplitDialogBinds(false);
        }
    }
    public class SearchableSlotViewShowPatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            return typeof(SearchableSlotView).GetMethod("Show", BindingFlags.Instance | BindingFlags.Public);
        }
        [PatchPostfix]
        private static void PatchPostFix(ref SearchableSlotView __instance)
        {
            if (__instance.Slot != null && __instance.Slot.IsSpecial)
            {
                AmandsControllerPlugin.AmandsControllerClassComponent.specialSlotSlotViews.Add(__instance);
            }
        }
    }
    public class SearchableSlotViewHidePatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            return typeof(SearchableSlotView).GetMethod("Close", BindingFlags.Instance | BindingFlags.Public);
        }
        [PatchPostfix]
        private static void PatchPostFix(ref SearchableSlotView __instance)
        {
            if (__instance == null) return;
            if (__instance.Slot != null && __instance.Slot.IsSpecial)
            {
                AmandsControllerPlugin.AmandsControllerClassComponent.specialSlotSlotViews.Remove(__instance);
            }

        }
    }
    public class InputGetKeyDownLeftControlPatch : ModulePatch
    {
        MethodInfo methodInfo;
        public InputGetKeyDownLeftControlPatch()
        {
            methodInfo = typeof(Input).GetMethods().First(x => x.Name == "GetKey" && x.GetParamsNames().Contains("key"));
        }
        protected override MethodBase GetTargetMethod()
        {
            return methodInfo;
        }
        [PatchPrefix]
        private static bool PatchPreFix(ref bool __result, KeyCode key)
        {
            switch (key)
            {
                case KeyCode.LeftControl:
                    __result = true;
                    return false;
            }
            return true;
        }
    }
}
