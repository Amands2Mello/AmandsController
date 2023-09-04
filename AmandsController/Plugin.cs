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

namespace AmandsController
{
    [BepInPlugin("com.Amanda.Controller", "Controller", "0.0.1")]
    public class AmandsControllerPlugin : BaseUnityPlugin
    {
        public static GameObject Hook;
        public static AmandsControllerClass AmandsControllerClassComponent;
        public static ConfigEntry<int> DebugX { get; set; }
        public static ConfigEntry<int> DebugY { get; set; }
        public static ConfigEntry<float> AngleMin { get; set; }
        public static ConfigEntry<float> AngleMax { get; set; }
        public static ConfigEntry<float> Stickness { get; set; }
        public static ConfigEntry<float> SticknessSmooth { get; set; }
        public static ConfigEntry<float> Radius { get; set; }
        public static ConfigEntry<Vector2> Sensitivity { get; set; }
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
            AmandsControllerClassComponent = Hook.AddComponent<AmandsControllerClass>();
            DontDestroyOnLoad(Hook);
        }

        private void Start()
        {
            DebugX = Config.Bind("Controller", "Debug X", 1920 / 2, new ConfigDescription("", null, new ConfigurationManagerAttributes { Order = 250 }));
            DebugY = Config.Bind("Controller", "Debug Y", 1080 / 2, new ConfigDescription("", null, new ConfigurationManagerAttributes { Order = 240 }));
            AngleMin = Config.Bind("Controller", "AngleMin", 1.0f, new ConfigDescription("", null, new ConfigurationManagerAttributes { Order = 230, IsAdvanced = true }));
            AngleMax = Config.Bind("Controller", "AngleMax", 0.1f, new ConfigDescription("", null, new ConfigurationManagerAttributes { Order = 220, IsAdvanced = true }));
            Stickness = Config.Bind("Controller", "Stickness", 0.25f, new ConfigDescription("", null, new ConfigurationManagerAttributes { Order = 210 }));
            SticknessSmooth = Config.Bind("Controller", "SticknessSmooth", 10f, new ConfigDescription("", null, new ConfigurationManagerAttributes { Order = 200 }));
            Radius = Config.Bind("Controller", "Radius", 1f, new ConfigDescription("", null, new ConfigurationManagerAttributes { Order = 180, IsAdvanced = true }));
            Sensitivity = Config.Bind("Controller", "Sensitivity", new Vector2(20f,-12f), new ConfigDescription("", null, new ConfigurationManagerAttributes { Order = 170 }));
            LeanSensitivity = Config.Bind("Controller", "LeanSensitivity", 50f, new ConfigDescription("", null, new ConfigurationManagerAttributes { Order = 160, IsAdvanced = true }));
            LDeadzone = Config.Bind("Controller", "LDeadzone", 0.25f, new ConfigDescription("", null, new ConfigurationManagerAttributes { Order = 150 }));
            RDeadzone = Config.Bind("Controller", "RDeadzone", 0.08f, new ConfigDescription("", null, new ConfigurationManagerAttributes { Order = 140 }));
            DeadzoneBuffer = Config.Bind("Controller", "DeadzoneBuffer", 0.5f, new ConfigDescription("", null, new ConfigurationManagerAttributes { Order = 130, IsAdvanced = true }));
            FloorDecimalAdd = Config.Bind("Controller", "FloorDecimalAdd", 0.005f, new ConfigDescription("", null, new ConfigurationManagerAttributes { Order = 120, IsAdvanced = true }));

            DoubleClickDelay = Config.Bind("Controller", "DoubleClickDelay", 0.3f, new ConfigDescription("", null, new ConfigurationManagerAttributes { Order = 110, IsAdvanced = true }));
            HoldDelay = Config.Bind("Controller", "HoldDelay", 0.3f, new ConfigDescription("", null, new ConfigurationManagerAttributes { Order = 100, IsAdvanced = true }));

            new AmandsLocalPlayerPatch().Enable();
            new AmandsTarkovApplicationPatch().Enable();
            new AmandsActionPanelPatch().Enable();
            new AmandsHealingLimbSelectorShowPatch().Enable();
            new AmandsHealingLimbSelectorClosePatch().Enable();
            new BattleStancePanelPatch().Enable();
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
}
