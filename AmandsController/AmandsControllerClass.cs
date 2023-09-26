using UnityEngine;
using System.Collections.Generic;
using EFT.UI;
using EFT;
using SharpDX.XInput;
using HarmonyLib;
using UnityEngine.UI;
using System;
using System.Reflection;
using EFT.InputSystem;
using System.Threading.Tasks;
using EFT.Interactive;
using System.Security.Cryptography;
using Mono.Security;
using static Audio.SpatialSystem.RoomPair;
using static EFT.Player;
using EFT.UI.DragAndDrop;
using UnityEngine.EventSystems;
using Comfort.Common;
using SharpDX;
using EFT.Communications;
using EFT.InventoryLogic;
using Cutscene;
using Diz.Binding;

namespace AmandsController
{
    public class AmandsControllerClass : MonoBehaviour
    {
        public InputTree inputTree;

        public LocalPlayer localPlayer;
        //public MovementContext movementContext;

        public bool isAiming = false;

        public object MovementContextObject;
        public Type MovementContextType;
        public MethodInfo SetCharacterMovementSpeed;
        private object[] MovementInvokeParameters = new object[2] { 0.0, false };

        public Slider speedSlider;

        private MethodInfo TranslateInput;
        private List<ECommand> commands = new List<ECommand>();
        private object[] TranslateInputInvokeParameters = new object[3] { new List<ECommand>(), null, ECursorResult.Ignore };

        private MethodInfo Press;

        Controller controller;
        Gamepad gamepad;
        public bool connected = false;
        public float maxValue = short.MaxValue;
        public Vector2 leftThumb, rightThumb, Aim = new Vector2(0, 0);
        public float leftTrigger, rightTrigger, leftThumbXYSqrt, rightThumbXYSqrt;
        public bool resetCharacterMovementSpeed = false;
        public AnimationCurve AimAnimationCurve = new AnimationCurve();
        public Keyframe[] AimKeys = new Keyframe[3] { new Keyframe(0f,0f), new Keyframe(0.75f,0.5f, 0.75f, 0.5f), new Keyframe(1f, 1f), };
        public bool SlowLeanLeft;
        public bool SlowLeanRight;

        bool leftThumbUP = false;
        bool leftThumbDOWN = false;
        bool leftThumbLEFT = false;
        bool leftThumbRIGHT = false;

        bool rightThumbUP = false;
        bool rightThumbDOWN = false;
        bool rightThumbLEFT = false;
        bool rightThumbRIGHT = false;

        bool A = false;
        bool B = false;
        bool X = false;
        bool Y = false;

        bool LB = false;
        bool RB = false;
        bool LB_RB = false;

        bool RT = false;
        bool LT = false;

        bool R = false;
        bool L = false;

        bool UP = false;
        bool DOWN = false;
        bool LEFT = false;
        bool RIGHT = false;

        bool BACK = false;
        bool MENU = false;

        float CharacterMovementSpeed = 0f;
        float StateSpeedLimit = 0f;
        float MaxSpeed = 0f;

        AmandsControllerButtonBind EmptyBind = new AmandsControllerButtonBind(new List<ECommand>(), EAmandsControllerCommand.Empty, EAmandsControllerPressType.Press, -100, "");
        Dictionary<string,Dictionary<EAmandsControllerButton, List<AmandsControllerButtonBind>>> AmandsControllerSets = new Dictionary<string, Dictionary<EAmandsControllerButton, List<AmandsControllerButtonBind>>>();
        List<string> ActiveAmandsControllerSets = new List<string>();
        //Dictionary<string, Dictionary<EAmandsControllerButton, List<AmandsControllerButtonBind>>> ActiveAmandsControllerSets = new Dictionary<string, Dictionary<EAmandsControllerButton, List<AmandsControllerButtonBind>>>();

        Dictionary<EAmandsControllerButton, AmandsControllerButtonSnapshot> AmandsControllerButtonSnapshots = new Dictionary<EAmandsControllerButton, AmandsControllerButtonSnapshot>();
        Dictionary<EAmandsControllerButton, List<AmandsControllerButtonBind>> AmandsControllerButtonBinds = new Dictionary<EAmandsControllerButton, List<AmandsControllerButtonBind>>();
        List<string> AsyncPress = new List<string>();
        List<string> AsyncHold = new List<string>();


        public LayerMask AimAssistLayerMask = LayerMask.GetMask("Player");
        public Dictionary<LocalPlayer, float> AimAssistPlayers = new Dictionary<LocalPlayer, float>();

        private RaycastHit hit;
        private RaycastHit foliageHit;
        private LayerMask LowLayerMask = LayerMask.GetMask("Terrain", "LowPolyCollider", "HitCollider");
        private LayerMask HighLayerMask = LayerMask.GetMask("Terrain", "HighPolyCollider");
        private LayerMask FoliageLayerMask = LayerMask.GetMask("Terrain", "HighPolyCollider", "HitCollider", "Foliage");
        private Vector3 TargetLocal;

        private Collider[] colliders;
        public int colliderCount;

        public FirearmController firearmController;

        /*public LocalPlayer AimAssistLocalPlayer = null;
        public LocalPlayer HitAimAssistLocalPlayer = null;
        public float AimAssistLocalPlayerDistance = 1000000.0f;
        public float Angle;
        public float AngleMin;
        public float AngleMax;
        public float AimAssistStrength;
        public float AimAssistStrengthSmooth;
        public float AimAssistStrengthSmoothChange;*/

        private Vector2 ScreenSize = new Vector2(Screen.width, Screen.height);
        private Vector2 ScreenSizeRatioMultiplier = new Vector2(1f, Screen.height / Screen.width);

        private bool Magnetism;
        private float Stickiness;
        private Vector2 AutoAim;

        private float StickinessSmooth;
        private Vector2 AutoAimSmooth;

        private float AimAssistAngle = 100000f;
        private float AimAssistBoneAngle;
        private LocalPlayer AimAssistLocalPlayer = null;
        private Vector2 AimAssistTarget2DPoint;

        private Vector2 AimAssistScreenLocalPosition;

        private LocalPlayer HitAimAssistLocalPlayer;

        // Controller UI

        public List<GridView> gridViews = new List<GridView>(); // GridViews
        public GridView SimpleStashGridView;
        public TradingTableGridView tradingTableGridView; // Trading
        public List<ContainedGridsView> containedGridsViews = new List<ContainedGridsView>(); // GridWindow
        public List<ItemSpecificationPanel> itemSpecificationPanels = new List<ItemSpecificationPanel>(); // ItemSpecificationPanelWindow

        public List<SlotView> equipmentSlotViews = new List<SlotView>();
        public List<SlotView> weaponsSlotViews = new List<SlotView>();
        public SlotView armbandSlotView;
        public List<SlotView> containersSlotViews = new List<SlotView>();
        public List<SlotView> lootEquipmentSlotViews = new List<SlotView>();
        public List<SlotView> lootWeaponsSlotViews = new List<SlotView>();
        public SlotView lootArmbandSlotView;
        public List<SlotView> lootContainersSlotViews = new List<SlotView>();

        public List<SearchButton> searchButtons = new List<SearchButton>();

        public List<ScrollRectNoDrag> scrollRectNoDrags = new List<ScrollRectNoDrag>();

        public GridView currentGridView;
        public ModSlotView currentModSlotView;
        public TradingTableGridView currentTradingTableGridView;
        public ContainedGridsView currentContainedGridsView;
        public ItemSpecificationPanel currentItemSpecificationPanel;
        public SlotView currentEquipmentSlotView;
        public SlotView currentWeaponsSlotView;
        public SlotView currentArmbandSlotView;
        public SlotView currentContainersSlotView;
        public SearchButton currentSearchButton;
        public ScrollRectNoDrag currentScrollRectNoDrag;
        public RectTransform currentScrollRectNoDragRectTransform;

        public Vector2 globalPosition = Vector2.zero;
        public Vector2 tglobalPosition = Vector2.zero;
        public Vector2Int gridViewLocation = Vector2Int.one;
        public Vector2Int lastDirection = Vector2Int.zero;

        public List<Vector2> gridViewsDebug = new List<Vector2>();
        public List<Vector2> slotViewsDebug = new List<Vector2>();

        public Vector2 hitPointDebug = Vector2.zero;
        public Vector2 hitDebug = Vector2.zero;
        public Vector2 hitSizeDebug = Vector2.zero;

        public float ScreenRatio = 1f;
        public float GridSize = 63f;
        public float ModSize = 63f;
        public float SlotSize = 124f;

        Vector2 directiontest;

        public Vector2 AimAssistDebugPoint1 = Vector2.zero;
        public Vector2 AimAssistDebugPoint2 = Vector2.zero;
        public Vector2 AimAssistDebugPoint3 = Vector2.zero;

        public ItemView itemView = null;
        public PointerEventData pointerEventData = null;
        public EventSystem eventSystem = null;
        public bool Dragging = false;

        public bool Interface = false;
        public bool AutoMove = false;
        public float AutoMoveTime = 0f;
        public float SkipMoveTime = 0f;

        public MethodInfo ExecuteInteraction;
        private object[] ExecuteInteractionInvokeParameters = new object[1] { EItemInfoButton.Inspect };
        public MethodInfo IsInteractionAvailable;
        private object[] IsInteractionAvailableInvokeParameters = new object[1] { EItemInfoButton.Inspect };
        public MethodInfo ExecuteMiddleClick;

        public MethodInfo QuickFindAppropriatePlace;
        public MethodInfo CanExecute;
        public MethodInfo RunNetworkTransaction;

        public void OnGUI()
        {
            GUILayout.BeginArea(new Rect(AmandsControllerPlugin.DebugX.Value, AmandsControllerPlugin.DebugY.Value, 1280, 720));
            /*GUILayout.Label("leftThumb X " + leftThumb.x.ToString());
            GUILayout.Label("leftThumb Y " + leftThumb.y.ToString()); 
            GUILayout.Label("rightThumb X " + rightThumb.x.ToString());
            GUILayout.Label("rightThumb Y " + rightThumb.y.ToString());
            GUILayout.Label("leftThumbXYSqrt " + leftThumbXYSqrt.ToString());
            GUILayout.Label("rightThumbXYSqrt " + rightThumbXYSqrt.ToString());
            GUILayout.Label("Aim X " + Aim.x.ToString());
            GUILayout.Label("Aim Y " + Aim.y.ToString());*/
            /*foreach (KeyValuePair<LocalPlayer,float> AimAssistPlayer in AimAssistPlayers)
            {
                GUILayout.Label("AimAssistPlayer " + AimAssistPlayer.Key.Profile.Nickname + " Angle " + AimAssistPlayer.Value);
            }
            GUILayout.Label("AimAssistStrength " + AimAssistStrength.ToString());
            if (AimAssistStrengthSmooth > 0.01)
            {
                GUILayout.Label("AimAssist " + AimAssistStrengthSmooth.ToString());
            }*/

            //GUILayout.Label("AimAssistAngle " + AimAssistAngle.ToString());
            //GUILayout.Label("AimAssistBoneAngle " + AimAssistBoneAngle.ToString());
            /*GUILayout.Label("Magnetism " + Magnetism.ToString());
            GUILayout.Label("Stickness " + Stickiness.ToString());
            GUILayout.Label("AutoAim.x " + AutoAim.x.ToString());
            GUILayout.Label("AutoAim.y " + AutoAim.y.ToString());*/

            GUILayout.EndArea();

            GUIContent gUIContent = new GUIContent();
            if (Interface)
            {
                GUI.Box(new Rect(new Vector2(globalPosition.x - (GridSize / 2), (Screen.height) - globalPosition.y - (GridSize / 2)), new Vector2(GridSize, GridSize)), gUIContent);
            }
            /*RectTransform rectTransform;
            foreach (ScrollRectNoDrag scrollRectNoDrag in scrollRectNoDrags)
            {
                rectTransform = scrollRectNoDrag.GetComponent<RectTransform>();
                //GUI.Box(new Rect(new Vector2(rectTransform.position.x - ((1 - rectTransform.pivot.x) * rectTransform.rect.width), (Screen.height) - (rectTransform.position.y + ((1 - rectTransform.pivot.y) * rectTransform.rect.height))), new Vector2(rectTransform.rect.width, rectTransform.rect.height)), gUIContent);
                GUI.Box(new Rect(new Vector2(rectTransform.position.x + rectTransform.rect.x, (Screen.height) - (rectTransform.position.y - (rectTransform.rect.height * (rectTransform.pivot.y - 1f)))), new Vector2(rectTransform.rect.width, rectTransform.rect.height)), gUIContent);
            }*/
            /*GUI.Box(new Rect(new Vector2(AimAssistDebugPoint1.x, (Screen.height) - AimAssistDebugPoint1.y), new Vector2(0,0)), gUIContent);
            GUI.Box(new Rect(new Vector2(AimAssistDebugPoint2.x, (Screen.height) - AimAssistDebugPoint2.y), new Vector2(0, 0)), gUIContent);
            GUI.Box(new Rect(new Vector2(AimAssistDebugPoint3.x, (Screen.height) - AimAssistDebugPoint3.y), new Vector2(0, 0)), gUIContent);*/

            // Controller UI

            // Debug Removal Start 1 !!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!

            /*GUILayout.BeginArea(new Rect(20, 20, 1280, 720));
            if (gridViews != null)
            {
                GUILayout.Label("GridViews.Count " + gridViews.Count);
            }
            if (tradingTableGridView != null)
            {
                GUILayout.Label("tradingTableGridView.Count" + (tradingTableGridView != null ? "1" : "0"));
            }
            if (containedGridsViews != null)
            {
                GUILayout.Label("containedGridsViews.Count " + containedGridsViews.Count);
            }
            if (itemSpecificationPanels != null)
            {
                GUILayout.Label("itemSpecificationPanels.Count " + itemSpecificationPanels.Count);
            }
            if (equipmentSlotViews != null)
            {
                GUILayout.Label("equipmentSlotViews.Count " + equipmentSlotViews.Count);
            }
            if (weaponsSlotViews != null)
            {
                GUILayout.Label("weaponsSlotViews.Count " + weaponsSlotViews.Count);
            }
            if (armbandSlotView != null)
            {
                GUILayout.Label("armbandSlotView.Count " + (armbandSlotView != null ? "1" : "0"));
            }
            if (containersSlotViews != null)
            {
                GUILayout.Label("containersSlotViews.Count " + containersSlotViews.Count);
            }
            if (lootEquipmentSlotViews != null)
            {
                GUILayout.Label("lootEquipmentSlotViews.Count " + lootEquipmentSlotViews.Count);
            }
            if (lootWeaponsSlotViews != null)
            {
                GUILayout.Label("lootWeaponsSlotViews.Count " + lootWeaponsSlotViews.Count);
            }
            if (lootArmbandSlotView != null)
            {
                GUILayout.Label("lootArmbandSlotView.Count " + (lootArmbandSlotView != null ? "1" : "0"));
            }
            if (lootContainersSlotViews != null)
            {
                GUILayout.Label("lootContainersSlotViews.Count " + lootContainersSlotViews.Count);
            }
            if (searchButtons != null)
            {
                GUILayout.Label("searchButtons.Count " + searchButtons.Count);
            }
            GUILayout.Label("currentGridView " + (currentGridView != null));
            GUILayout.Label("currentTradingTableGridView " + (currentTradingTableGridView != null));
            GUILayout.Label("currentContainedGridsView " + (currentContainedGridsView != null));
            GUILayout.Label("currentEquipmentSlotView " + (currentEquipmentSlotView != null));
            GUILayout.Label("currentWeaponsSlotView " + (currentWeaponsSlotView != null));
            GUILayout.Label("currentArmbandSlotView " + (currentArmbandSlotView != null));
            GUILayout.Label("currentContainersSlotView " + (currentContainersSlotView != null));
            GUILayout.Label("currentItemSpecificationPanel " + (currentItemSpecificationPanel != null));
            GUILayout.Label("currentSearchButton " + (currentSearchButton != null));
            if (currentGridView != null)
            {
                GUILayout.Label("Current " + "GridView");
            }
            if (currentModSlotView != null)
            {
                GUILayout.Label("Current " + "ModSlotView");
            }
            if (currentTradingTableGridView != null)
            {
                GUILayout.Label("Current " + "TradingTable");
            }
            if (currentItemSpecificationPanel != null)
            {
                GUILayout.Label("Current " + "ItemSpecificationPanel");
            }
            if (currentContainedGridsView != null)
            {
                GUILayout.Label("Current " + "Contained");
            }
            if (currentEquipmentSlotView != null)
            {
                GUILayout.Label("Current " + "Equipment");
            }
            if (currentWeaponsSlotView != null)
            {
                GUILayout.Label("Current " + "Weapons");
            }
            if (currentArmbandSlotView != null)
            {
                GUILayout.Label("Current " + "Armband");
            }
            if (currentContainersSlotView != null)
            {
                GUILayout.Label("Current " + "Containers");
            }
            if (localPlayer != null)
            {
                GUILayout.Label("localPlayer " + "true");
            }
            if (currentSearchButton != null)
            {
                GUILayout.Label("currentSearchButton " + "true");
            }
            GUILayout.EndArea();

            GUIContent gUIContent = new GUIContent();
            GUIContent gUIContentText = new GUIContent();
            gUIContentText.text = "Sel";
            GUIContent gUIContentText3 = new GUIContent();
            gUIContentText3.text = "Hit";
            GUIContent gUIContentText4 = new GUIContent();
            gUIContentText4.text = "HitPoint";

            GUIContent gUIContentGridView = new GUIContent();
            gUIContentGridView.text = "GridView";

            GUIContent gUIContentPanel = new GUIContent();
            gUIContentPanel.text = "Panel";*/

            // Debug Removal End 1 !!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!


            /*foreach (GridView gridView in gridViews)
            {
                Vector2 position;
                position.x = gridView.transform.position.x;
                position.y = gridView.transform.position.y;
                GUI.Box(new Rect(new Vector2(position.x, (Screen.height) - position.y), gridView.GetComponent<RectTransform>().sizeDelta), gUIContentGridView);
            }
            foreach (ContainedGridsView containedGridsView in containedGridsViews)
            {
                GUI.Box(new Rect(new Vector2(containedGridsView.transform.position.x, (Screen.height) - (containedGridsView.transform.position.y - (containedGridsView.GetComponent<RectTransform>().sizeDelta.y * (containedGridsView.GetComponent<RectTransform>().pivot.y - 1f)))), containedGridsView.GetComponent<RectTransform>().sizeDelta), gUIContentcontainedGridsView);
                foreach (GridView gridView in containedGridsView.GridViews)
                {
                    GUI.Box(new Rect(new Vector2(gridView.transform.position.x, (Screen.height) - gridView.transform.position.y), gridView.GetComponent<RectTransform>().sizeDelta), gUIContentGridView);
                }
            }
            if (tradingTableGridView != null)
            {
                Vector2 position;
                position.x = tradingTableGridView.transform.position.x - (tradingTableGridView.GetComponent<RectTransform>().sizeDelta.x / 2f);
                position.y = tradingTableGridView.transform.position.y + (tradingTableGridView.GetComponent<RectTransform>().sizeDelta.y / 2f);
                GUI.Box(new Rect(new Vector2(position.x, (Screen.height) - position.y), tradingTableGridView.GetComponent<RectTransform>().sizeDelta), gUIContenttradingTableGridView);
            }
            foreach (Vector2 pos in gridViewsDebug)
            {
                GUI.Box(new Rect(new Vector2(pos.x - (GridSize / 2), (Screen.height) - pos.y - (GridSize / 2)), new Vector2(GridSize, GridSize)), gUIContentText);
            }
            RectTransform rectTransform;

            if (itemSpecificationPanels.Count != 0)
            {
                Vector2 position;
                int bestDepth = -1;
                CanvasRenderer canvasRenderer;
                ItemSpecificationPanel bestItemSpecificationPanel = null;
                foreach (ItemSpecificationPanel itemSpecificationPanel in itemSpecificationPanels)
                {
                    rectTransform = itemSpecificationPanel.GetComponent<RectTransform>();
                    position = new Vector2(itemSpecificationPanel.transform.position.x - ((rectTransform.sizeDelta.x / 2) * ScreenRatio), itemSpecificationPanel.transform.position.y + ((rectTransform.sizeDelta.y / 2) * ScreenRatio));
                    if (Input.mousePosition.x > position.x && Input.mousePosition.x < (position.x + (rectTransform.sizeDelta.x * ScreenRatio)) && Input.mousePosition.y < position.y && Input.mousePosition.y > (position.y - (rectTransform.sizeDelta.y * ScreenRatio)))
                    {
                        canvasRenderer = itemSpecificationPanel.GetComponent<CanvasRenderer>();
                        if (canvasRenderer != null && canvasRenderer.absoluteDepth > bestDepth)
                        {
                            bestDepth = canvasRenderer.absoluteDepth;
                            bestItemSpecificationPanel = itemSpecificationPanel;
                        }
                    }
                }
                if (bestItemSpecificationPanel != null)
                {
                    GUI.Box(new Rect(new Vector2(bestItemSpecificationPanel.transform.position.x - ((bestItemSpecificationPanel.GetComponent<RectTransform>().sizeDelta.x / 2) * ScreenRatio), (Screen.height) - (bestItemSpecificationPanel.transform.position.y + ((bestItemSpecificationPanel.GetComponent<RectTransform>().sizeDelta.y / 2) * ScreenRatio))), bestItemSpecificationPanel.GetComponent<RectTransform>().sizeDelta), gUIContentPanel);
                    ModSlotView[] modSlotViews = Traverse.Create(bestItemSpecificationPanel).Field("_modsContainer").GetValue<RectTransform>().GetComponentsInChildren<ModSlotView>();
                    ConsoleScreen.Log("count " + modSlotViews.Count());
                    foreach (ModSlotView modSlotView in modSlotViews)
                    {
                        GUI.Box(new Rect(new Vector2(modSlotView.transform.position.x - (32f * ScreenRatio), (Screen.height) - (modSlotView.transform.position.y + (32f * ScreenRatio))), new Vector2(64,64)), gUIContent);
                    }
                }
            }*/

            // Debug Removal Start 2 !!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!

            /*if (currentGridView != null)
            {
                Vector2 position;
                position.x = currentGridView.transform.position.x;
                position.y = currentGridView.transform.position.y;
                GUI.Box(new Rect(new Vector2(position.x, (Screen.height) - position.y), currentGridView.GetComponent<RectTransform>().sizeDelta), gUIContentGridView);
            }
            foreach (SlotView slotView in equipmentSlotViews)
            {
                Vector2 position;
                position.x = slotView.transform.position.x;// - 62.5f;
                position.y = slotView.transform.position.y;
                GUI.Box(new Rect(new Vector2(position.x, (Screen.height) - position.y), new Vector2(125f, 125f)), gUIContent);
            }
            foreach (SlotView slotView in weaponsSlotViews)
            {
                Vector2 position;
                position.x = slotView.transform.position.x;// - 157.0811f;
                position.y = slotView.transform.position.y;
                GUI.Box(new Rect(new Vector2(position.x, (Screen.height) - position.y), new Vector2(314.1622f,125f)), gUIContent);
            }
            if (armbandSlotView != null)
            {
                GUI.Box(new Rect(new Vector2(armbandSlotView.transform.position.x, (Screen.height) - armbandSlotView.transform.position.y), new Vector2(125f, 64f)), gUIContent);// - 62.5f, (Screen.height) - armbandSlotView.transform.position.y), new Vector2(125f, 64f)), gUIContent);
            }
            foreach (SlotView slotView in containersSlotViews)
            {
                Vector2 position;
                position.x = slotView.transform.position.x;
                position.y = slotView.transform.position.y;
                GUI.Box(new Rect(new Vector2(position.x, (Screen.height) - position.y), new Vector2(125f, 125f)), gUIContent);
            }
            foreach (SlotView slotView in lootEquipmentSlotViews)
            {
                Vector2 position;
                position.x = slotView.transform.position.x;// - 62.5f;
                position.y = slotView.transform.position.y;
                GUI.Box(new Rect(new Vector2(position.x, (Screen.height) - position.y), new Vector2(125f, 125f)), gUIContent);
            }
            foreach (SlotView slotView in lootWeaponsSlotViews)
            {
                Vector2 position;
                position.x = slotView.transform.position.x;// - 133.0724f;
                position.y = slotView.transform.position.y;
                GUI.Box(new Rect(new Vector2(position.x, (Screen.height) - position.y), new Vector2(314.1622f, 125f)), gUIContent);
            }
            if (lootArmbandSlotView != null)
            {
                GUI.Box(new Rect(new Vector2(lootArmbandSlotView.transform.position.x, (Screen.height) - lootArmbandSlotView.transform.position.y), new Vector2(125f, 64f)), gUIContent);
            }
            foreach (SlotView slotView in lootContainersSlotViews)
            {
                Vector2 position;
                position.x = slotView.transform.position.x;
                position.y = slotView.transform.position.y;
                GUI.Box(new Rect(new Vector2(position.x, (Screen.height) - position.y), new Vector2(125f, 125f)), gUIContent);
            }
            GUI.Box(new Rect(new Vector2(globalPosition.x - (GridSize / 2), (Screen.height) - globalPosition.y - (GridSize / 2)), new Vector2(GridSize, GridSize)), gUIContentText);
            //GUI.Box(new Rect(new Vector2(hitDebug.x, (Screen.height) - hitDebug.y), hitSizeDebug), gUIContentText3);
            //GUI.Box(new Rect(new Vector2(hitPointDebug.x, (Screen.height) - hitPointDebug.y), new Vector2(64, 32)), gUIContentText4);

            GUI.Box(new Rect(new Vector2(Input.mousePosition.x, Screen.height - Input.mousePosition.y), new Vector2(GridSize, GridSize)), gUIContent);
            foreach (ItemSpecificationPanel itemSpecificationPanel in itemSpecificationPanels)
            {
                //GUI.Box(new Rect(new Vector2(itemSpecificationPanel.transform.position.x - (itemSpecificationPanel.GetComponent<RectTransform>().sizeDelta.x / 2), (Screen.height) - (itemSpecificationPanel.transform.position.y - (itemSpecificationPanel.GetComponent<RectTransform>().sizeDelta.y * (itemSpecificationPanel.GetComponent<RectTransform>().pivot.y - 1f)))), itemSpecificationPanel.GetComponent<RectTransform>().sizeDelta), gUIContentPanel);
                Vector2 point = (Vector2)Input.mousePosition + (directiontest * 1000f);
                RectTransform rectTransform2 = itemSpecificationPanel.GetComponent<RectTransform>();
                Vector2 position = Vector2.zero;
                position.x = itemSpecificationPanel.transform.position.x - (rectTransform2.sizeDelta.x / 2);
                position.y = itemSpecificationPanel.transform.position.y + (rectTransform2.sizeDelta.y / 2);
                GUI.Box(new Rect(new Vector2(position.x + Mathf.Clamp(point.x - position.x, 0, rectTransform2.sizeDelta.x), Screen.height - (position.y - Mathf.Clamp(position.y - point.y, 0, rectTransform2.sizeDelta.y))), new Vector2(GridSize, GridSize)), gUIContent);
            }
            foreach (ContainedGridsView containedGridsView in containedGridsViews)
            {
                GUI.Box(new Rect(new Vector2(containedGridsView.transform.position.x, (Screen.height) - (containedGridsView.transform.position.y - (containedGridsView.GetComponent<RectTransform>().sizeDelta.y * (containedGridsView.GetComponent<RectTransform>().pivot.y - 1f)))), containedGridsView.GetComponent<RectTransform>().sizeDelta), gUIContent);
            }
            RectTransform rectTransform;
            if (itemSpecificationPanels.Count != 0)
            {
                Vector2 position;
                int bestDepth = -1;
                CanvasRenderer canvasRenderer;
                ItemSpecificationPanel bestItemSpecificationPanel = null;
                foreach (ItemSpecificationPanel itemSpecificationPanel in itemSpecificationPanels)
                {
                    rectTransform = itemSpecificationPanel.GetComponent<RectTransform>();
                    position = new Vector2(itemSpecificationPanel.transform.position.x - ((rectTransform.sizeDelta.x / 2) * ScreenRatio), itemSpecificationPanel.transform.position.y + ((rectTransform.sizeDelta.y / 2) * ScreenRatio));
                    if (Input.mousePosition.x > position.x && Input.mousePosition.x < (position.x + (rectTransform.sizeDelta.x * ScreenRatio)) && Input.mousePosition.y < position.y && Input.mousePosition.y > (position.y - (rectTransform.sizeDelta.y * ScreenRatio)))
                    {
                        canvasRenderer = itemSpecificationPanel.GetComponent<CanvasRenderer>();
                        if (canvasRenderer != null && canvasRenderer.absoluteDepth > bestDepth)
                        {
                            bestDepth = canvasRenderer.absoluteDepth;
                            bestItemSpecificationPanel = itemSpecificationPanel;
                        }
                    }
                }
                if (bestItemSpecificationPanel != null)
                {
                    GUI.Box(new Rect(new Vector2(bestItemSpecificationPanel.transform.position.x - ((bestItemSpecificationPanel.GetComponent<RectTransform>().sizeDelta.x / 2) * ScreenRatio), (Screen.height) - (bestItemSpecificationPanel.transform.position.y + ((bestItemSpecificationPanel.GetComponent<RectTransform>().sizeDelta.y / 2) * ScreenRatio))), bestItemSpecificationPanel.GetComponent<RectTransform>().sizeDelta), gUIContentPanel);
                    ModSlotView[] modSlotViews = Traverse.Create(bestItemSpecificationPanel).Field("_modsContainer").GetValue<RectTransform>().GetComponentsInChildren<ModSlotView>();
                    foreach (ModSlotView modSlotView in modSlotViews)
                    {
                        GUI.Box(new Rect(new Vector2(modSlotView.transform.position.x - (32f * ScreenRatio), (Screen.height) - (modSlotView.transform.position.y + (32f * ScreenRatio))), new Vector2(64, 64)), gUIContent);
                    }
                }
            }
            foreach (SearchButton searchButton in searchButtons)
            {
                Vector2 position;
                rectTransform = searchButton.GetComponent<RectTransform>();
                position.x = searchButton.transform.position.x;// - (rectTransform.sizeDelta.x / 2);
                position.y = searchButton.transform.position.y;// - (rectTransform.sizeDelta.y / 2);
                GUI.Box(new Rect(new Vector2(position.x, (Screen.height) - position.y), new Vector2(10, 10)), gUIContent);
            }*/

            // Debug Removal End 2 !!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!

            //GUI.Box(new Rect(new Vector2(tglobalPosition.x - 1, (Screen.height) - tglobalPosition.y + 1), new Vector2(2, 2)), gUIContentText);
        }
        public void Start()
        {
            TranslateInput = typeof(InputTree).GetMethod("TranslateInput", BindingFlags.Instance | BindingFlags.NonPublic);
            Press = typeof(Button).GetMethod("Press", BindingFlags.Instance | BindingFlags.NonPublic);
            ExecuteMiddleClick = typeof(ItemView).GetMethod("ExecuteMiddleClick", BindingFlags.Instance | BindingFlags.NonPublic);
            QuickFindAppropriatePlace = typeof(ItemUiContext).GetMethod("QuickFindAppropriatePlace", BindingFlags.Instance | BindingFlags.Public);
            CanExecute = typeof(TraderControllerClass).GetMethod("CanExecute", BindingFlags.Instance | BindingFlags.Public);
            RunNetworkTransaction = typeof(TraderControllerClass).GetMethod("RunNetworkTransaction", BindingFlags.Instance | BindingFlags.Public);

            AimAnimationCurve.keys = AimKeys;
        }
        public void Update()
        {
            if (Input.GetKeyDown(KeyCode.Keypad1))
            {
                UpdateController(null);
                UpdateInterfaceBinds(true);
            }
            if (AutoMove)
            {
                AutoMoveTime += Time.deltaTime;
                if (AutoMoveTime > 0.2f)
                {
                    AutoMoveTime = 0f;
                    ControllerUIMove(lastDirection, false);
                }
            }
            if (!connected) return;

            gamepad = controller.GetState().Gamepad;

            if (leftTrigger > 0.25)
            {
                if (!LT)
                {
                    LT = true;
                    AmandsControllerGeneratePressType(EAmandsControllerButton.LeftTrigger, true);
                }
            }
            else
            {
                if (LT)
                {
                    LT = false;
                    AmandsControllerGeneratePressType(EAmandsControllerButton.LeftTrigger, false);
                }
            }
            if (rightTrigger > 0.25)
            {
                if (!RT)
                {
                    RT = true;
                    AmandsControllerGeneratePressType(EAmandsControllerButton.RightTrigger, true);
                }
            }
            else
            {
                if (RT)
                {
                    RT = false;
                    AmandsControllerGeneratePressType(EAmandsControllerButton.RightTrigger, false);
                }
            }
            if (gamepad.Buttons.HasFlag(GamepadButtonFlags.A))
            {
                if (!A)
                {
                    A = true;
                    AmandsControllerGeneratePressType(EAmandsControllerButton.A, true);

                }
            }
            else
            {
                if (A)
                {
                    A = false;
                    AmandsControllerGeneratePressType(EAmandsControllerButton.A, false);
                }
            }
            if (gamepad.Buttons.HasFlag(GamepadButtonFlags.B))
            {
                if (!B)
                {
                    B = true;
                    AmandsControllerGeneratePressType(EAmandsControllerButton.B, true);
                }
            }
            else
            {
                if (B)
                {
                    B = false;
                    AmandsControllerGeneratePressType(EAmandsControllerButton.B, false);
                }
            }
            if (gamepad.Buttons.HasFlag(GamepadButtonFlags.X))
            {
                if (!X)
                {
                    X = true;
                    AmandsControllerGeneratePressType(EAmandsControllerButton.X, true);
                }
            }
            else
            {
                if (X)
                {
                    X = false;
                    AmandsControllerGeneratePressType(EAmandsControllerButton.X, false);
                }
            }
            if (gamepad.Buttons.HasFlag(GamepadButtonFlags.Y))
            {
                if (!Y)
                {
                    Y = true;
                    AmandsControllerGeneratePressType(EAmandsControllerButton.Y, true);
                }
            }
            else
            {
                if (Y)
                {
                    Y = false;
                    AmandsControllerGeneratePressType(EAmandsControllerButton.Y, false);
                }
            }
            if (gamepad.Buttons.HasFlag(GamepadButtonFlags.LeftShoulder))
            {
                if (!LB)
                {
                    LB = true;
                    AmandsControllerGeneratePressType(EAmandsControllerButton.LeftShoulder, true);
                    if (AmandsControllerSets.ContainsKey("LB") && !ActiveAmandsControllerSets.Contains("LB"))
                    {
                        ActiveAmandsControllerSets.Add("LB");
                    }
                }
            }
            else
            {
                if (LB)
                {
                    LB = false;
                    AmandsControllerGeneratePressType(EAmandsControllerButton.LeftShoulder, false);
                    ActiveAmandsControllerSets.Remove("LB");
                }
            }
            if (gamepad.Buttons.HasFlag(GamepadButtonFlags.RightShoulder))
            {
                if (!RB)
                {
                    RB = true;
                    AmandsControllerGeneratePressType(EAmandsControllerButton.RightShoulder, true);
                    if (AmandsControllerSets.ContainsKey("RB") && !ActiveAmandsControllerSets.Contains("RB"))
                    {
                        ActiveAmandsControllerSets.Add("RB");
                    }
                }
            }
            else
            {
                if (RB)
                {
                    RB = false;
                    AmandsControllerGeneratePressType(EAmandsControllerButton.RightShoulder, false);
                    ActiveAmandsControllerSets.Remove("RB");
                }
            }
            if (gamepad.Buttons.HasFlag(GamepadButtonFlags.LeftShoulder | GamepadButtonFlags.RightShoulder))
            {
                if (!LB_RB)
                {
                    LB_RB = true;
                    if (AmandsControllerSets.ContainsKey("LB_RB") && !ActiveAmandsControllerSets.Contains("LB_RB"))
                    {
                        ActiveAmandsControllerSets.Add("LB_RB");
                    }
                }
            }
            else
            {
                if (LB_RB)
                {
                    LB_RB = false;
                    ActiveAmandsControllerSets.Remove("LB_RB");
                }
            }
            if (gamepad.Buttons.HasFlag(GamepadButtonFlags.LeftThumb))
            {
                if (!L)
                {
                    L = true;
                    AmandsControllerGeneratePressType(EAmandsControllerButton.LeftThumb, true);
                }
            }
            else
            {
                if (L)
                {
                    L = false;
                    AmandsControllerGeneratePressType(EAmandsControllerButton.LeftThumb, false);
                }
            }
            if (gamepad.Buttons.HasFlag(GamepadButtonFlags.RightThumb))
            {
                if (!R)
                {
                    R = true;
                    AmandsControllerGeneratePressType(EAmandsControllerButton.RightThumb, true);
                }
            }
            else
            {
                if (R)
                {
                    R = false;
                    AmandsControllerGeneratePressType(EAmandsControllerButton.RightThumb, false);
                }
            }
            if (gamepad.Buttons.HasFlag(GamepadButtonFlags.DPadUp))
            {
                if (!UP)
                {
                    UP = true;
                    AmandsControllerGeneratePressType(EAmandsControllerButton.UP, true);
                }
            }
            else
            {
                if (UP)
                {
                    UP = false;
                    AmandsControllerGeneratePressType(EAmandsControllerButton.UP, false);
                }
            }
            if (gamepad.Buttons.HasFlag(GamepadButtonFlags.DPadDown))
            {
                if (!DOWN)
                {
                    DOWN = true;
                    AmandsControllerGeneratePressType(EAmandsControllerButton.DOWN, true);
                }
            }
            else
            {
                if (DOWN)
                {
                    DOWN = false;
                    AmandsControllerGeneratePressType(EAmandsControllerButton.DOWN, false);
                }
            }
            if (gamepad.Buttons.HasFlag(GamepadButtonFlags.DPadLeft))
            {
                if (!LEFT)
                {
                    LEFT = true;
                    AmandsControllerGeneratePressType(EAmandsControllerButton.LEFT, true);
                }
            }
            else
            {
                if (LEFT)
                {
                    LEFT = false;
                    AmandsControllerGeneratePressType(EAmandsControllerButton.LEFT, false);
                }
            }
            if (gamepad.Buttons.HasFlag(GamepadButtonFlags.DPadRight))
            {
                if (!RIGHT)
                {
                    RIGHT = true;
                    AmandsControllerGeneratePressType(EAmandsControllerButton.RIGHT, true);
                }
            }
            else
            {
                if (RIGHT)
                {
                    RIGHT = false;
                    AmandsControllerGeneratePressType(EAmandsControllerButton.RIGHT, false);
                }
            }
            if (gamepad.Buttons.HasFlag(GamepadButtonFlags.Back))
            {
                if (!BACK)
                {
                    BACK = true;
                    AmandsControllerGeneratePressType(EAmandsControllerButton.BACK, true);
                }
            }
            else
            {
                if (BACK)
                {
                    BACK = false;
                    AmandsControllerGeneratePressType(EAmandsControllerButton.BACK, false);
                }
            }
            if (gamepad.Buttons.HasFlag(GamepadButtonFlags.Start))
            {
                if (!MENU)
                {
                    MENU = true;
                    AmandsControllerGeneratePressType(EAmandsControllerButton.MENU, true);
                }
            }
            else
            {
                if (MENU)
                {
                    MENU = false;
                    AmandsControllerGeneratePressType(EAmandsControllerButton.MENU, false);
                }
            }

            leftTrigger = (float)gamepad.LeftTrigger / 255f;
            rightTrigger = (float)gamepad.RightTrigger / 255f;

            leftThumb.x = (float)gamepad.LeftThumbX / maxValue;
            leftThumb.y = (float)gamepad.LeftThumbY / maxValue;
            leftThumbXYSqrt = Mathf.Sqrt(Mathf.Pow(leftThumb.x, 2) + Mathf.Pow(leftThumb.y, 2));

            rightThumb.x = (float)gamepad.RightThumbX / maxValue;
            rightThumb.y = (float)gamepad.RightThumbY / maxValue;
            rightThumbXYSqrt = Mathf.Sqrt(Mathf.Pow(rightThumb.x, 2) + Mathf.Pow(rightThumb.y, 2));

            if (currentScrollRectNoDrag != null && currentScrollRectNoDragRectTransform != null)
            {
                float height = currentScrollRectNoDrag.content.rect.height;
                float position = currentScrollRectNoDragRectTransform.position.y - (currentScrollRectNoDragRectTransform.rect.height * (currentScrollRectNoDragRectTransform.pivot.y - 1f));
                if (Mathf.Abs(leftThumb.y) > 0.2f)
                {
                    currentScrollRectNoDrag.verticalNormalizedPosition = currentScrollRectNoDrag.verticalNormalizedPosition + ((((leftThumb.y * 1000f) / height) / height) * 10000f * Time.deltaTime * AmandsControllerPlugin.ScrollSensitivity.Value);
                    UpdateGlobalPosition();
                    if ((globalPosition.y + (GridSize / 2f)) > position)
                    {
                        ControllerUIMove(new Vector2Int(0, -1), false);
                    }
                    else if ((globalPosition.y - (GridSize / 2f)) < (position - (currentScrollRectNoDragRectTransform.rect.height * ScreenRatio)))
                    {
                        ControllerUIMove(new Vector2Int(0, 1), false);
                    }
                }
                else
                {
                    if ((globalPosition.y + (GridSize / 2f)) > position)
                    {
                        currentScrollRectNoDrag.verticalNormalizedPosition = currentScrollRectNoDrag.verticalNormalizedPosition + (((1000f / height) / height) * 10000f * Time.deltaTime * AmandsControllerPlugin.ScrollSensitivity.Value);
                        UpdateGlobalPosition();
                    }
                    else if ((globalPosition.y - (GridSize / 2f)) < (position - (currentScrollRectNoDragRectTransform.rect.height * ScreenRatio)))
                    {
                        currentScrollRectNoDrag.verticalNormalizedPosition = currentScrollRectNoDrag.verticalNormalizedPosition + (((-1000f / height) / height) * 10000f * Time.deltaTime * AmandsControllerPlugin.ScrollSensitivity.Value);
                        UpdateGlobalPosition();
                    }
                }
            }

            if (Interface)
            {
                SkipMoveTime += Time.deltaTime;
                if (SkipMoveTime > 0.3f && (Mathf.Abs(rightThumb.x) > 0.3f || Mathf.Abs(rightThumb.y) > 0.3f))
                {
                    SkipMoveTime = 0f;
                    ControllerUIMove(new Vector2Int(rightThumb.x > 0.3f ? 1 : rightThumb.x < -0.3f ? -1 : 0, rightThumb.y > 0.3f ? 1 : rightThumb.y < -0.3f ? -1 : 0), true);
                }
                else if (!(Mathf.Abs(rightThumb.x) > 0.3f || Mathf.Abs(rightThumb.y) > 0.3f))
                {
                    SkipMoveTime = 1f;
                }
                return;
            }

            if (localPlayer == null) return;

            //if (MovementContextObject == null) return;
            if (localPlayer.HandsController != null)
            {
                if (localPlayer.HandsController.IsAiming && !isAiming)
                {
                    isAiming = true;
                    if (AmandsControllerSets.ContainsKey("Aiming") && !ActiveAmandsControllerSets.Contains("Aiming"))
                    {
                        ActiveAmandsControllerSets.Add("Aiming");
                    }
                }
                else if (isAiming && !localPlayer.HandsController.IsAiming)
                {
                    isAiming = false;
                    ActiveAmandsControllerSets.Remove("Aiming");
                }
            }
            if (leftThumbXYSqrt > AmandsControllerPlugin.LDeadzone.Value)
            {
                localPlayer.Move(leftThumb.normalized);
                CharacterMovementSpeed = 0f;
                /*if (movementContext != null)
                {
                    CharacterMovementSpeed = Mathf.Lerp(-AmandsControllerPlugin.Deadzone.Value - AmandsControllerPlugin.DeadzoneBuffer.Value, 1f, xySqrt) * Mathf.Min(movementContext.StateSpeedLimit, movementContext.MaxSpeed);
                    movementContext.SetCharacterMovementSpeed(CharacterMovementSpeed, false);
                    movementContext.RaiseChangeSpeedEvent();
                }*/
                if (MovementContextObject != null)
                {
                    StateSpeedLimit = Traverse.Create(MovementContextObject).Property("StateSpeedLimit").GetValue<float>();
                    MaxSpeed = Traverse.Create(MovementContextObject).Property("MaxSpeed").GetValue<float>();
                    CharacterMovementSpeed = Mathf.Lerp(-AmandsControllerPlugin.LDeadzone.Value - AmandsControllerPlugin.DeadzoneBuffer.Value, 1f, leftThumbXYSqrt) * Mathf.Min(StateSpeedLimit, MaxSpeed);
                    MovementInvokeParameters[0] = CharacterMovementSpeed;
                    SetCharacterMovementSpeed.Invoke(MovementContextObject, MovementInvokeParameters);
                }
                if (speedSlider != null)
                {
                    speedSlider.value = Mathf.Floor(((CharacterMovementSpeed + AmandsControllerPlugin.FloorDecimalAdd.Value) / speedSlider.maxValue) * 20f) * (speedSlider.maxValue / 20f);
                }
                resetCharacterMovementSpeed = true;
            }
            else if (resetCharacterMovementSpeed)
            {
                /*resetCharacterMovementSpeed = false;
                if (movementContext != null)
                {
                    movementContext.SetCharacterMovementSpeed(0, false);
                    movementContext.RaiseChangeSpeedEvent();
                }*/
                if (MovementContextObject != null)
                {
                    MovementInvokeParameters[0] = 0f;
                    SetCharacterMovementSpeed.Invoke(MovementContextObject, MovementInvokeParameters);
                }
                if (speedSlider != null)
                {
                    speedSlider.value = 0;
                }
            }
            // Aim Assist

            Magnetism = false;
            Stickiness = 0;
            AutoAim = Vector2.zero;

            if (firearmController == null)
            {
                firearmController = localPlayer.gameObject.GetComponent<FirearmController>();
            }
            if (localPlayer != null && Camera.main != null)
            {
                Vector3 position = Vector3.one;
                Vector3 direction = Vector3.forward;

                if (firearmController != null)
                {
                    position = firearmController.CurrentFireport.position;
                    direction = firearmController.WeaponDirection;
                    firearmController.AdjustShotVectors(ref position, ref direction);
                }
                colliders = new Collider[100];
                colliderCount = Physics.OverlapCapsuleNonAlloc(position, position + (direction * 1000f), AmandsControllerPlugin.Radius.Value, colliders, AimAssistLayerMask, QueryTriggerInteraction.Ignore);

                ScreenSize = new Vector2(Screen.width, Screen.height);
                ScreenSizeRatioMultiplier = new Vector2(1f, ScreenSize.y / ScreenSize.x);


                AimAssistAngle = 100000f;
                AimAssistLocalPlayer = null;

                for (int i = 0; i < colliderCount; i++)
                {
                    HitAimAssistLocalPlayer = colliders[i].transform.gameObject.GetComponent<LocalPlayer>();
                    if (HitAimAssistLocalPlayer != null && HitAimAssistLocalPlayer != localPlayer)
                    {
                        AimAssistScreenLocalPosition = (((((Vector2)Camera.main.WorldToScreenPoint(HitAimAssistLocalPlayer.PlayerBones.Head.position + (HitAimAssistLocalPlayer.Velocity * AmandsControllerPlugin.AutoAimEnemyVelocity.Value)) - ((((Vector2)Camera.main.WorldToScreenPoint(position + (direction * Vector3.Distance(position, HitAimAssistLocalPlayer.PlayerBones.Head.position + (HitAimAssistLocalPlayer.Velocity * AmandsControllerPlugin.AutoAimEnemyVelocity.Value)))) - ((Vector2)Camera.main.WorldToScreenPoint(HitAimAssistLocalPlayer.PlayerBones.Head.position + (HitAimAssistLocalPlayer.Velocity * AmandsControllerPlugin.AutoAimEnemyVelocity.Value)) - (ScreenSize / 2f))) - (ScreenSize / 2f)) * 2f)) - (ScreenSize / 2f)) / ScreenSize) * ScreenSizeRatioMultiplier);
                        AimAssistBoneAngle = Mathf.Sqrt(Vector2.SqrMagnitude(AimAssistScreenLocalPosition)) / (ScreenSize.y / ScreenSize.x);
                        if (AimAssistBoneAngle < Mathf.Max(AmandsControllerPlugin.MagnetismRadius.Value, AmandsControllerPlugin.StickinessRadius.Value, AmandsControllerPlugin.AutoAimRadius.Value) && AimAssistBoneAngle < AimAssistAngle && !Physics.Raycast(position, (HitAimAssistLocalPlayer.PlayerBones.Head.position - position).normalized, out hit, Vector3.Distance(HitAimAssistLocalPlayer.PlayerBones.Head.position, position), HighLayerMask, QueryTriggerInteraction.Ignore))
                        {
                            AimAssistAngle = AimAssistBoneAngle;
                            AimAssistLocalPlayer = HitAimAssistLocalPlayer;
                            AimAssistTarget2DPoint = AimAssistScreenLocalPosition;
                        }
                        AimAssistScreenLocalPosition = (((((Vector2)Camera.main.WorldToScreenPoint(HitAimAssistLocalPlayer.PlayerBones.Ribcage.position + (HitAimAssistLocalPlayer.Velocity * AmandsControllerPlugin.AutoAimEnemyVelocity.Value)) - ((((Vector2)Camera.main.WorldToScreenPoint(position + (direction * Vector3.Distance(position, HitAimAssistLocalPlayer.PlayerBones.Ribcage.position + (HitAimAssistLocalPlayer.Velocity * AmandsControllerPlugin.AutoAimEnemyVelocity.Value)))) - ((Vector2)Camera.main.WorldToScreenPoint(HitAimAssistLocalPlayer.PlayerBones.Ribcage.position + (HitAimAssistLocalPlayer.Velocity * AmandsControllerPlugin.AutoAimEnemyVelocity.Value)) - (ScreenSize / 2f))) - (ScreenSize / 2f)) * 2f)) - (ScreenSize / 2f)) / ScreenSize) * ScreenSizeRatioMultiplier);
                        AimAssistBoneAngle = Mathf.Sqrt(Vector2.SqrMagnitude(AimAssistScreenLocalPosition)) / (ScreenSize.y / ScreenSize.x);
                        if (AimAssistBoneAngle < Mathf.Max(AmandsControllerPlugin.MagnetismRadius.Value, AmandsControllerPlugin.StickinessRadius.Value, AmandsControllerPlugin.AutoAimRadius.Value) && AimAssistBoneAngle < AimAssistAngle && !Physics.Raycast(position, (HitAimAssistLocalPlayer.PlayerBones.Ribcage.position - position).normalized, out hit, Vector3.Distance(HitAimAssistLocalPlayer.PlayerBones.Ribcage.position, position), HighLayerMask, QueryTriggerInteraction.Ignore))
                        {
                            AimAssistAngle = AimAssistBoneAngle;
                            AimAssistLocalPlayer = HitAimAssistLocalPlayer;
                            AimAssistTarget2DPoint = AimAssistScreenLocalPosition;
                        }
                        AimAssistScreenLocalPosition = (((((Vector2)Camera.main.WorldToScreenPoint(HitAimAssistLocalPlayer.PlayerBones.Pelvis.position + (HitAimAssistLocalPlayer.Velocity * AmandsControllerPlugin.AutoAimEnemyVelocity.Value)) - ((((Vector2)Camera.main.WorldToScreenPoint(position + (direction * Vector3.Distance(position, HitAimAssistLocalPlayer.PlayerBones.Pelvis.position + (HitAimAssistLocalPlayer.Velocity * AmandsControllerPlugin.AutoAimEnemyVelocity.Value)))) - ((Vector2)Camera.main.WorldToScreenPoint(HitAimAssistLocalPlayer.PlayerBones.Pelvis.position + (HitAimAssistLocalPlayer.Velocity * AmandsControllerPlugin.AutoAimEnemyVelocity.Value)) - (ScreenSize / 2f))) - (ScreenSize / 2f)) * 2f)) - (ScreenSize / 2f)) / ScreenSize) * ScreenSizeRatioMultiplier);
                        AimAssistBoneAngle = Mathf.Sqrt(Vector2.SqrMagnitude(AimAssistScreenLocalPosition)) / (ScreenSize.y / ScreenSize.x);
                        if (AimAssistBoneAngle < Mathf.Max(AmandsControllerPlugin.MagnetismRadius.Value, AmandsControllerPlugin.StickinessRadius.Value, AmandsControllerPlugin.AutoAimRadius.Value) && AimAssistBoneAngle < AimAssistAngle && !Physics.Raycast(position, (HitAimAssistLocalPlayer.PlayerBones.Pelvis.position - position).normalized, out hit, Vector3.Distance(HitAimAssistLocalPlayer.PlayerBones.Pelvis.position, position), HighLayerMask, QueryTriggerInteraction.Ignore))
                        {
                            AimAssistAngle = AimAssistBoneAngle;
                            AimAssistLocalPlayer = HitAimAssistLocalPlayer;
                            AimAssistTarget2DPoint = AimAssistScreenLocalPosition;
                        }
                    }
                }
                if (AimAssistLocalPlayer != null && firearmController != null)
                {
                    if (AimAssistAngle < AmandsControllerPlugin.MagnetismRadius.Value)
                    {
                        Magnetism = true;
                    }
                    if (AimAssistAngle < AmandsControllerPlugin.StickinessRadius.Value)
                    {
                        Stickiness = Mathf.Lerp(1f, 0f, (Mathf.Clamp(AimAssistAngle / AmandsControllerPlugin.StickinessRadius.Value, 0.5f, 1f) - 0.5f) / (1f - 0.5f));
                    }
                    if (AimAssistAngle < AmandsControllerPlugin.AutoAimRadius.Value)
                    {
                        AutoAim = Vector2.Lerp(Vector2.zero, Vector2.Lerp(new Vector2(Mathf.Clamp(AimAssistTarget2DPoint.x * 10f, -0.5f, 0.5f), Mathf.Clamp(AimAssistTarget2DPoint.y * -5f, -0.5f, 0.5f)) * 100f * Time.deltaTime, Vector2.zero, (Mathf.Clamp(AimAssistAngle / AmandsControllerPlugin.AutoAimRadius.Value, 0.5f, 1f) - 0.5f) / (1f - 0.5f)) * AmandsControllerPlugin.AutoAim.Value, 1f) / firearmController.AimingSensitivity * (firearmController.IsAiming ? 2f : 1f);
                    }
                }
            }
            StickinessSmooth += ((Stickiness - StickinessSmooth) * AmandsControllerPlugin.StickinessSmooth.Value) * Time.deltaTime;
            AutoAimSmooth += ((AutoAim - AutoAimSmooth) * AmandsControllerPlugin.AutoAimSmooth.Value) * Time.deltaTime;
            /*AimAssistStrengthSmoothChange = ((AimAssistStrength - AimAssistStrengthSmooth) * AmandsControllerPlugin.SticknessSmooth.Value) * Time.deltaTime;
            if (AimAssistStrengthSmoothChange > 0f)
            {
                AimAssistStrengthSmooth += AimAssistStrengthSmoothChange * 2f;
            }
            else
            {
                AimAssistStrengthSmooth += AimAssistStrengthSmoothChange * 0.5f;
            }*/

            if (rightThumbXYSqrt > AmandsControllerPlugin.RDeadzone.Value || Mathf.Sqrt(Mathf.Pow(AutoAim.x, 2) + Mathf.Pow(AutoAim.y, 2)) > AmandsControllerPlugin.RDeadzone.Value)
            {
                Aim.x = rightThumb.x * AimAnimationCurve.Evaluate(rightThumbXYSqrt);
                Aim.y = rightThumb.y * AimAnimationCurve.Evaluate(rightThumbXYSqrt);
                localPlayer.Rotate(((Aim * AmandsControllerPlugin.Sensitivity.Value * 100f * Time.deltaTime) * Mathf.Lerp(1f, AmandsControllerPlugin.Stickiness.Value, StickinessSmooth)) + AutoAimSmooth, false);
            }
            if (SlowLeanLeft || SlowLeanRight)
            {
                localPlayer.SlowLean(((SlowLeanLeft ? -AmandsControllerPlugin.LeanSensitivity.Value: 0) + (SlowLeanRight ? AmandsControllerPlugin.LeanSensitivity.Value : 0)) * Time.deltaTime);
            }
        }
        public void UpdateController(LocalPlayer Player)
        {
            eventSystem = FindObjectOfType<EventSystem>();
            pointerEventData = new PointerEventData(eventSystem);
            pointerEventData.button = PointerEventData.InputButton.Left;
            switch (AmandsControllerPlugin.UserIndex.Value)
            {
                case 1:
                    controller = new Controller(UserIndex.One);
                    connected = controller.IsConnected;
                    break;
                case 2:
                    controller = new Controller(UserIndex.Two);
                    connected = controller.IsConnected;
                    break;
                case 3:
                    controller = new Controller(UserIndex.Three);
                    connected = controller.IsConnected;
                    break;
                case 4:
                    controller = new Controller(UserIndex.Four);
                    connected = controller.IsConnected;
                    break;
                default:
                    controller = new Controller(UserIndex.One);
                    connected = controller.IsConnected;
                    break;
            }

            AmandsControllerSets.Clear();
            AmandsControllerSets.Add("LB", new Dictionary<EAmandsControllerButton, List<AmandsControllerButtonBind>>());
            AmandsControllerSets["LB"].Add(EAmandsControllerButton.UP, new List<AmandsControllerButtonBind> { new AmandsControllerButtonBind(new List<ECommand> { ECommand.ThrowGrenade }, EAmandsControllerCommand.InputTree, EAmandsControllerPressType.Press, 2, "") });
            AmandsControllerSets["LB"].Add(EAmandsControllerButton.DOWN, new List<AmandsControllerButtonBind> { new AmandsControllerButtonBind(new List<ECommand> { ECommand.SelectSecondaryWeapon }, EAmandsControllerCommand.InputTree, EAmandsControllerPressType.Press, 2, "") });
            AmandsControllerSets["LB"][EAmandsControllerButton.DOWN].Add(new AmandsControllerButtonBind(new List<ECommand> { ECommand.QuickSelectSecondaryWeapon }, EAmandsControllerCommand.InputTree, EAmandsControllerPressType.DoubleClick, 2, ""));
            AmandsControllerSets["LB"].Add(EAmandsControllerButton.LEFT, new List<AmandsControllerButtonBind> { new AmandsControllerButtonBind(new List<ECommand> { ECommand.SelectFirstPrimaryWeapon }, EAmandsControllerCommand.InputTree, EAmandsControllerPressType.Press, 2, "") });
            AmandsControllerSets["LB"].Add(EAmandsControllerButton.RIGHT, new List<AmandsControllerButtonBind> { new AmandsControllerButtonBind(new List<ECommand> { ECommand.SelectSecondPrimaryWeapon }, EAmandsControllerCommand.InputTree, EAmandsControllerPressType.Press, 2, "") });

            AmandsControllerSets["LB"].Add(EAmandsControllerButton.A, new List<AmandsControllerButtonBind> { new AmandsControllerButtonBind(new List<ECommand> { ECommand.SelectFastSlot4 }, EAmandsControllerCommand.InputTree, EAmandsControllerPressType.Press, 2, "") });
            AmandsControllerSets["LB"].Add(EAmandsControllerButton.B, new List<AmandsControllerButtonBind> { new AmandsControllerButtonBind(new List<ECommand> { ECommand.SelectFastSlot5 }, EAmandsControllerCommand.InputTree, EAmandsControllerPressType.Press, 2, "") });
            AmandsControllerSets["LB"].Add(EAmandsControllerButton.X, new List<AmandsControllerButtonBind> { new AmandsControllerButtonBind(new List<ECommand> { ECommand.SelectFastSlot6 }, EAmandsControllerCommand.InputTree, EAmandsControllerPressType.Press, 2, "") });
            AmandsControllerSets["LB"].Add(EAmandsControllerButton.Y, new List<AmandsControllerButtonBind> { new AmandsControllerButtonBind(new List<ECommand> { ECommand.SelectFastSlot7 }, EAmandsControllerCommand.InputTree, EAmandsControllerPressType.Press, 2, "") });

            AmandsControllerSets["LB"].Add(EAmandsControllerButton.LeftThumb, new List<AmandsControllerButtonBind> { new AmandsControllerButtonBind(new List<ECommand> { ECommand.DropBackpack }, EAmandsControllerCommand.InputTree, EAmandsControllerPressType.Press, 2, "") });

            AmandsControllerSets["LB"].Add(EAmandsControllerButton.BACK, new List<AmandsControllerButtonBind> { new AmandsControllerButtonBind(new List<ECommand> { ECommand.ToggleGoggles }, EAmandsControllerCommand.InputTree, EAmandsControllerPressType.Press, 2, "") });

            AmandsControllerSets.Add("RB", new Dictionary<EAmandsControllerButton, List<AmandsControllerButtonBind>>());
            //AmandsControllerSets["RB"].Add(EAmandsControllerButton.A, new List<AmandsControllerButtonBind> { new AmandsControllerButtonBind(ECommand.DropBackpack, EAmandsControllerCommand.GamePlayerOwner, EAmandsControllerPressType.Press, 1, "") });
            AmandsControllerSets["RB"].Add(EAmandsControllerButton.X, new List<AmandsControllerButtonBind> { new AmandsControllerButtonBind(new List<ECommand> { ECommand.ChamberUnload }, EAmandsControllerCommand.InputTree, EAmandsControllerPressType.Press, 1, "") });
            AmandsControllerSets["RB"][EAmandsControllerButton.X].Add(new AmandsControllerButtonBind(new List<ECommand> { ECommand.CheckChamber }, EAmandsControllerCommand.InputTree, EAmandsControllerPressType.Hold, 1, "") );
            AmandsControllerSets["RB"][EAmandsControllerButton.X].Add(new AmandsControllerButtonBind(new List<ECommand> { ECommand.UnloadMagazine }, EAmandsControllerCommand.InputTree, EAmandsControllerPressType.DoubleClick, 1, ""));

            AmandsControllerSets["RB"].Add(EAmandsControllerButton.B, new List<AmandsControllerButtonBind> { new AmandsControllerButtonBind(new List<ECommand>(), EAmandsControllerCommand.EnableSet, EAmandsControllerPressType.Press, 1, "Movement") });
            AmandsControllerSets["RB"][EAmandsControllerButton.B].Add(new AmandsControllerButtonBind(new List<ECommand> { ECommand.DisplayTimer }, EAmandsControllerCommand.DisableSet, EAmandsControllerPressType.Release, 1, "Movement"));

            AmandsControllerSets["RB"].Add(EAmandsControllerButton.Y, new List<AmandsControllerButtonBind> { new AmandsControllerButtonBind(new List<ECommand> { ECommand.FoldStock }, EAmandsControllerCommand.InputTree, EAmandsControllerPressType.Press, 1, "") });
            AmandsControllerSets["RB"][EAmandsControllerButton.Y].Add(new AmandsControllerButtonBind(new List<ECommand> { ECommand.CheckChamber }, EAmandsControllerCommand.InputTree, EAmandsControllerPressType.Hold, 1, ""));

            AmandsControllerSets["RB"].Add(EAmandsControllerButton.LEFT, new List<AmandsControllerButtonBind> { new AmandsControllerButtonBind(new List<ECommand> { ECommand.ToggleLeanLeft }, EAmandsControllerCommand.InputTree, EAmandsControllerPressType.Press, 1, "") });
            AmandsControllerSets["RB"].Add(EAmandsControllerButton.RIGHT, new List<AmandsControllerButtonBind> { new AmandsControllerButtonBind(new List<ECommand> { ECommand.ToggleLeanRight }, EAmandsControllerCommand.InputTree, EAmandsControllerPressType.Press, 1, "") });

            AmandsControllerSets["RB"].Add(EAmandsControllerButton.BACK, new List<AmandsControllerButtonBind> { new AmandsControllerButtonBind(new List<ECommand> { ECommand.SwitchHeadLight }, EAmandsControllerCommand.InputTree, EAmandsControllerPressType.Press, 1, "") });
            AmandsControllerSets["RB"][EAmandsControllerButton.BACK].Add(new AmandsControllerButtonBind(new List<ECommand> { ECommand.ToggleHeadLight }, EAmandsControllerCommand.InputTree, EAmandsControllerPressType.Hold, 1, ""));

            //AmandsControllerSets["RB"].Add(EAmandsControllerButton.LeftThumb, new List<AmandsControllerButtonBind> { new AmandsControllerButtonBind(ECommand., EAmandsControllerCommand.GamePlayerOwner, EAmandsControllerPressType.Press, 1, "") });

            AmandsControllerSets.Add("LB_RB", new Dictionary<EAmandsControllerButton, List<AmandsControllerButtonBind>>());
            AmandsControllerSets["LB_RB"].Add(EAmandsControllerButton.UP, new List<AmandsControllerButtonBind> { new AmandsControllerButtonBind(new List<ECommand> { ECommand.ToggleBlindAbove }, EAmandsControllerCommand.InputTree, EAmandsControllerPressType.Press, 3, "") });
            AmandsControllerSets["LB_RB"][EAmandsControllerButton.UP].Add(new AmandsControllerButtonBind(new List<ECommand> { ECommand.BlindShootEnd }, EAmandsControllerCommand.InputTree, EAmandsControllerPressType.Release, 3, ""));
            AmandsControllerSets["LB_RB"].Add(EAmandsControllerButton.DOWN, new List<AmandsControllerButtonBind> { new AmandsControllerButtonBind(new List<ECommand> { ECommand.ToggleBlindRight }, EAmandsControllerCommand.InputTree, EAmandsControllerPressType.Press, 3, "") });
            AmandsControllerSets["LB_RB"][EAmandsControllerButton.DOWN].Add(new AmandsControllerButtonBind(new List<ECommand> { ECommand.BlindShootEnd }, EAmandsControllerCommand.InputTree, EAmandsControllerPressType.Release, 3, ""));
            AmandsControllerSets["LB_RB"].Add(EAmandsControllerButton.LEFT, new List<AmandsControllerButtonBind> { new AmandsControllerButtonBind(new List<ECommand> { ECommand.ToggleStepLeft }, EAmandsControllerCommand.InputTree, EAmandsControllerPressType.Press, 3, "") });
            AmandsControllerSets["LB_RB"][EAmandsControllerButton.LEFT].Add(new AmandsControllerButtonBind(new List<ECommand> { ECommand.ReturnFromLeftStep }, EAmandsControllerCommand.InputTree, EAmandsControllerPressType.Release, 3, ""));
            AmandsControllerSets["LB_RB"].Add(EAmandsControllerButton.RIGHT, new List<AmandsControllerButtonBind> { new AmandsControllerButtonBind(new List<ECommand> { ECommand.ToggleStepRight }, EAmandsControllerCommand.InputTree, EAmandsControllerPressType.Press, 3, "") });
            AmandsControllerSets["LB_RB"][EAmandsControllerButton.RIGHT].Add(new AmandsControllerButtonBind(new List<ECommand> { ECommand.ReturnFromRightStep }, EAmandsControllerCommand.InputTree, EAmandsControllerPressType.Release, 3, ""));

            AmandsControllerSets["LB_RB"].Add(EAmandsControllerButton.A, new List<AmandsControllerButtonBind> { new AmandsControllerButtonBind(new List<ECommand> { ECommand.SelectFastSlot8 }, EAmandsControllerCommand.InputTree, EAmandsControllerPressType.Press, 3, "") });
            AmandsControllerSets["LB_RB"].Add(EAmandsControllerButton.B, new List<AmandsControllerButtonBind> { new AmandsControllerButtonBind(new List<ECommand> { ECommand.SelectFastSlot9 }, EAmandsControllerCommand.InputTree, EAmandsControllerPressType.Press, 3, "") });
            AmandsControllerSets["LB_RB"].Add(EAmandsControllerButton.X, new List<AmandsControllerButtonBind> { new AmandsControllerButtonBind(new List<ECommand> { ECommand.SelectFastSlot0 }, EAmandsControllerCommand.InputTree, EAmandsControllerPressType.Press, 3, "") });
            AmandsControllerSets["LB_RB"].Add(EAmandsControllerButton.Y, new List<AmandsControllerButtonBind> { new AmandsControllerButtonBind(new List<ECommand> { ECommand.DisplayTimer }, EAmandsControllerCommand.InputTree, EAmandsControllerPressType.Press, 3, "") });
            AmandsControllerSets["LB_RB"][EAmandsControllerButton.Y].Add(new AmandsControllerButtonBind(new List<ECommand> { ECommand.DisplayTimerAndExits }, EAmandsControllerCommand.InputTree, EAmandsControllerPressType.DoubleClick, 3, ""));

            AmandsControllerSets.Add("ActionPanel", new Dictionary<EAmandsControllerButton, List<AmandsControllerButtonBind>>());
            AmandsControllerSets["ActionPanel"].Add(EAmandsControllerButton.X, new List<AmandsControllerButtonBind> { new AmandsControllerButtonBind(new List<ECommand> { ECommand.BeginInteracting }, EAmandsControllerCommand.InputTree, EAmandsControllerPressType.Press, 10, "") });
            AmandsControllerSets["ActionPanel"][EAmandsControllerButton.X].Add(new AmandsControllerButtonBind(new List<ECommand> { ECommand.EndInteracting }, EAmandsControllerCommand.InputTree, EAmandsControllerPressType.Release, 10, ""));
            AmandsControllerSets["ActionPanel"].Add(EAmandsControllerButton.UP, new List<AmandsControllerButtonBind> { new AmandsControllerButtonBind(new List<ECommand> { ECommand.ScrollPrevious }, EAmandsControllerCommand.InputTree, EAmandsControllerPressType.Press, 10, "") });
            AmandsControllerSets["ActionPanel"].Add(EAmandsControllerButton.DOWN, new List<AmandsControllerButtonBind> { new AmandsControllerButtonBind(new List<ECommand> { ECommand.ScrollNext }, EAmandsControllerCommand.InputTree, EAmandsControllerPressType.Press, 10, "") });

            AmandsControllerSets.Add("HealingLimbSelector", new Dictionary<EAmandsControllerButton, List<AmandsControllerButtonBind>>());
            AmandsControllerSets["HealingLimbSelector"].Add(EAmandsControllerButton.UP, new List<AmandsControllerButtonBind> { new AmandsControllerButtonBind(new List<ECommand> { ECommand.ScrollNext }, EAmandsControllerCommand.InputTree, EAmandsControllerPressType.Press, 9, "") });
            AmandsControllerSets["HealingLimbSelector"].Add(EAmandsControllerButton.DOWN, new List<AmandsControllerButtonBind> { new AmandsControllerButtonBind(new List<ECommand> { ECommand.ScrollPrevious }, EAmandsControllerCommand.InputTree, EAmandsControllerPressType.Press, 9, "") });

            AmandsControllerSets.Add("Movement", new Dictionary<EAmandsControllerButton, List<AmandsControllerButtonBind>>());
            AmandsControllerSets["Movement"].Add(EAmandsControllerButton.UP, new List<AmandsControllerButtonBind> { new AmandsControllerButtonBind(new List<ECommand> { ECommand.NextWalkPose }, EAmandsControllerCommand.InputTree, EAmandsControllerPressType.Press, 3, "") });
            AmandsControllerSets["Movement"].Add(EAmandsControllerButton.DOWN, new List<AmandsControllerButtonBind> { new AmandsControllerButtonBind(new List<ECommand> { ECommand.PreviousWalkPose }, EAmandsControllerCommand.InputTree, EAmandsControllerPressType.Press, 3, "") });
            AmandsControllerSets["Movement"].Add(EAmandsControllerButton.LEFT, new List<AmandsControllerButtonBind> { new AmandsControllerButtonBind(new List<ECommand>(), EAmandsControllerCommand.SlowLeanLeft, EAmandsControllerPressType.Press, 3, "") });
            AmandsControllerSets["Movement"][EAmandsControllerButton.LEFT].Add(new AmandsControllerButtonBind(new List<ECommand> { ECommand.DisplayTimer }, EAmandsControllerCommand.EndSlowLean, EAmandsControllerPressType.Release, 3, ""));
            AmandsControllerSets["Movement"].Add(EAmandsControllerButton.RIGHT, new List<AmandsControllerButtonBind> { new AmandsControllerButtonBind(new List<ECommand>(), EAmandsControllerCommand.SlowLeanRight, EAmandsControllerPressType.Press, 3, "") });
            AmandsControllerSets["Movement"][EAmandsControllerButton.RIGHT].Add(new AmandsControllerButtonBind(new List<ECommand> { ECommand.DisplayTimer }, EAmandsControllerCommand.EndSlowLean, EAmandsControllerPressType.Release, 3, ""));
            AmandsControllerSets["Movement"].Add(EAmandsControllerButton.LeftThumb, new List<AmandsControllerButtonBind> { new AmandsControllerButtonBind(new List<ECommand>(), EAmandsControllerCommand.RestoreLean, EAmandsControllerPressType.Press, 3, "") });

            AmandsControllerSets.Add("Aiming", new Dictionary<EAmandsControllerButton, List<AmandsControllerButtonBind>>());
            AmandsControllerSets["Aiming"].Add(EAmandsControllerButton.RightThumb, new List<AmandsControllerButtonBind> { new AmandsControllerButtonBind(new List<ECommand> { ECommand.ToggleBreathing }, EAmandsControllerCommand.InputTree, EAmandsControllerPressType.Press, 4, "") });
            AmandsControllerSets["Aiming"].Add(EAmandsControllerButton.RightShoulder, new List<AmandsControllerButtonBind> { new AmandsControllerButtonBind(new List<ECommand>(), EAmandsControllerCommand.EnableSet, EAmandsControllerPressType.Press, 4, "Aiming_RB") });
            AmandsControllerSets["Aiming"][EAmandsControllerButton.RightShoulder].Add(new AmandsControllerButtonBind(new List<ECommand> { ECommand.DisplayTimer }, EAmandsControllerCommand.DisableSet, EAmandsControllerPressType.Release, 4, "Aiming_RB"));

            AmandsControllerSets.Add("Aiming_RB", new Dictionary<EAmandsControllerButton, List<AmandsControllerButtonBind>>());
            AmandsControllerSets["Aiming_RB"].Add(EAmandsControllerButton.UP, new List<AmandsControllerButtonBind> { new AmandsControllerButtonBind(new List<ECommand> { ECommand.OpticCalibrationSwitchUp }, EAmandsControllerCommand.InputTree, EAmandsControllerPressType.Press, 4, "") });
            AmandsControllerSets["Aiming_RB"].Add(EAmandsControllerButton.DOWN, new List<AmandsControllerButtonBind> { new AmandsControllerButtonBind(new List<ECommand> { ECommand.OpticCalibrationSwitchDown }, EAmandsControllerCommand.InputTree, EAmandsControllerPressType.Press, 4, "") });

            AmandsControllerSets.Add("Interface", new Dictionary<EAmandsControllerButton, List<AmandsControllerButtonBind>>());
            AmandsControllerSets["Interface"].Add(EAmandsControllerButton.UP, new List<AmandsControllerButtonBind> { new AmandsControllerButtonBind(new List<ECommand>(), EAmandsControllerCommand.InterfaceUp, EAmandsControllerPressType.Press, 20, "") });
            AmandsControllerSets["Interface"][EAmandsControllerButton.UP].Add(new AmandsControllerButtonBind(new List<ECommand> { ECommand.DisplayTimer }, EAmandsControllerCommand.InterfaceDisableAutoMove, EAmandsControllerPressType.Release, 20, ""));
            AmandsControllerSets["Interface"].Add(EAmandsControllerButton.DOWN, new List<AmandsControllerButtonBind> { new AmandsControllerButtonBind(new List<ECommand>(), EAmandsControllerCommand.InterfaceDown, EAmandsControllerPressType.Press, 20, "") });
            AmandsControllerSets["Interface"][EAmandsControllerButton.DOWN].Add(new AmandsControllerButtonBind(new List<ECommand> { ECommand.DisplayTimer }, EAmandsControllerCommand.InterfaceDisableAutoMove, EAmandsControllerPressType.Release, 20, ""));
            AmandsControllerSets["Interface"].Add(EAmandsControllerButton.LEFT, new List<AmandsControllerButtonBind> { new AmandsControllerButtonBind(new List<ECommand>(), EAmandsControllerCommand.InterfaceLeft, EAmandsControllerPressType.Press, 20, "") });
            AmandsControllerSets["Interface"][EAmandsControllerButton.LEFT].Add(new AmandsControllerButtonBind(new List<ECommand> { ECommand.DisplayTimer }, EAmandsControllerCommand.InterfaceDisableAutoMove, EAmandsControllerPressType.Release, 20, ""));
            AmandsControllerSets["Interface"].Add(EAmandsControllerButton.RIGHT, new List<AmandsControllerButtonBind> { new AmandsControllerButtonBind(new List<ECommand>(), EAmandsControllerCommand.InterfaceRight, EAmandsControllerPressType.Press, 20, "") });
            AmandsControllerSets["Interface"][EAmandsControllerButton.RIGHT].Add(new AmandsControllerButtonBind(new List<ECommand> { ECommand.DisplayTimer }, EAmandsControllerCommand.InterfaceDisableAutoMove, EAmandsControllerPressType.Release, 20, ""));
            AmandsControllerSets["Interface"].Add(EAmandsControllerButton.A, new List<AmandsControllerButtonBind> { new AmandsControllerButtonBind(new List<ECommand>(), EAmandsControllerCommand.BeginDrag, EAmandsControllerPressType.Press, 20, "") });
            AmandsControllerSets["Interface"].Add(EAmandsControllerButton.B, new List<AmandsControllerButtonBind> { new AmandsControllerButtonBind(new List<ECommand> { ECommand.Escape }, EAmandsControllerCommand.InputTree, EAmandsControllerPressType.Press, 20, "") });
            AmandsControllerSets["Interface"].Add(EAmandsControllerButton.X, new List<AmandsControllerButtonBind> { new AmandsControllerButtonBind(new List<ECommand>(), EAmandsControllerCommand.Use, EAmandsControllerPressType.Press, 20, "") });
            AmandsControllerSets["Interface"][EAmandsControllerButton.X].Add(new AmandsControllerButtonBind(new List<ECommand> { ECommand.DisplayTimer }, EAmandsControllerCommand.UseHold, EAmandsControllerPressType.Hold, 20, ""));
            AmandsControllerSets["Interface"].Add(EAmandsControllerButton.Y, new List<AmandsControllerButtonBind> { new AmandsControllerButtonBind(new List<ECommand>(), EAmandsControllerCommand.QuickMove, EAmandsControllerPressType.Press, 20, "") });
            AmandsControllerSets["Interface"].Add(EAmandsControllerButton.RightThumb, new List<AmandsControllerButtonBind> { new AmandsControllerButtonBind(new List<ECommand>(), EAmandsControllerCommand.Discard, EAmandsControllerPressType.Press, 20, "") });

            AmandsControllerSets.Add("OnDrag", new Dictionary<EAmandsControllerButton, List<AmandsControllerButtonBind>>());
            AmandsControllerSets["OnDrag"].Add(EAmandsControllerButton.A, new List<AmandsControllerButtonBind> { new AmandsControllerButtonBind(new List<ECommand>(), EAmandsControllerCommand.EndDrag, EAmandsControllerPressType.Press, 22, "") });
            AmandsControllerSets["OnDrag"].Add(EAmandsControllerButton.X, new List<AmandsControllerButtonBind> { new AmandsControllerButtonBind(new List<ECommand>(), EAmandsControllerCommand.EndDrag, EAmandsControllerPressType.Press, 22, "") });

            AmandsControllerSets.Add("SearchButton", new Dictionary<EAmandsControllerButton, List<AmandsControllerButtonBind>>());
            AmandsControllerSets["SearchButton"].Add(EAmandsControllerButton.A, new List<AmandsControllerButtonBind> { new AmandsControllerButtonBind(new List<ECommand>(), EAmandsControllerCommand.Search, EAmandsControllerPressType.Press, 21, "") });

            AmandsControllerButtonBinds.Clear();
            AmandsControllerButtonBinds.Add(EAmandsControllerButton.LeftTrigger, new List<AmandsControllerButtonBind> { new AmandsControllerButtonBind(new List<ECommand> { ECommand.ToggleAlternativeShooting, ECommand.EndSprinting, ECommand.TryLowThrow }, EAmandsControllerCommand.InputTree, EAmandsControllerPressType.Press, -1, "") });
            AmandsControllerButtonBinds[EAmandsControllerButton.LeftTrigger].Add(new AmandsControllerButtonBind(new List<ECommand> { ECommand.FinishLowThrow }, EAmandsControllerCommand.InputTree, EAmandsControllerPressType.Release, -1, ""));
            AmandsControllerButtonBinds.Add(EAmandsControllerButton.RightTrigger, new List<AmandsControllerButtonBind> { new AmandsControllerButtonBind(new List<ECommand> { ECommand.ToggleShooting, ECommand.EndSprinting, ECommand.TryHighThrow }, EAmandsControllerCommand.InputTree, EAmandsControllerPressType.Press, -1, "") });
            AmandsControllerButtonBinds[EAmandsControllerButton.RightTrigger].Add(new AmandsControllerButtonBind(new List<ECommand> { ECommand.EndShooting, ECommand.FinishHighThrow }, EAmandsControllerCommand.InputTree, EAmandsControllerPressType.Release, -1, ""));
            AmandsControllerButtonBinds.Add(EAmandsControllerButton.A, new List<AmandsControllerButtonBind> { new AmandsControllerButtonBind(new List<ECommand> { ECommand.Jump }, EAmandsControllerCommand.InputTree, EAmandsControllerPressType.Press, -1, "") });
            AmandsControllerButtonBinds.Add(EAmandsControllerButton.B, new List<AmandsControllerButtonBind> { new AmandsControllerButtonBind(new List<ECommand> { ECommand.ToggleDuck }, EAmandsControllerCommand.InputTree, EAmandsControllerPressType.Press, -1, "") });
            AmandsControllerButtonBinds[EAmandsControllerButton.B].Add(new AmandsControllerButtonBind(new List<ECommand> { ECommand.ToggleProne }, EAmandsControllerCommand.InputTree, EAmandsControllerPressType.Hold, -1, ""));
            AmandsControllerButtonBinds.Add(EAmandsControllerButton.X, new List<AmandsControllerButtonBind> { new AmandsControllerButtonBind(new List<ECommand> { ECommand.ReloadWeapon }, EAmandsControllerCommand.InputTree, EAmandsControllerPressType.Press, -1, "") });
            AmandsControllerButtonBinds[EAmandsControllerButton.X].Add(new AmandsControllerButtonBind(new List<ECommand> { ECommand.CheckAmmo }, EAmandsControllerCommand.InputTree, EAmandsControllerPressType.Hold, -1, ""));
            AmandsControllerButtonBinds[EAmandsControllerButton.X].Add(new AmandsControllerButtonBind(new List<ECommand> { ECommand.QuickReloadWeapon }, EAmandsControllerCommand.InputTree, EAmandsControllerPressType.DoubleClick, -1, ""));
            AmandsControllerButtonBinds.Add(EAmandsControllerButton.Y, new List<AmandsControllerButtonBind> { new AmandsControllerButtonBind(new List<ECommand>(), EAmandsControllerCommand.QuickSelectWeapon, EAmandsControllerPressType.Press, -1, "") });
            AmandsControllerButtonBinds[EAmandsControllerButton.Y].Add(new AmandsControllerButtonBind(new List<ECommand> { ECommand.ExamineWeapon }, EAmandsControllerCommand.InputTree, EAmandsControllerPressType.Hold, -1, ""));
            AmandsControllerButtonBinds.Add(EAmandsControllerButton.LeftThumb, new List<AmandsControllerButtonBind> { new AmandsControllerButtonBind(new List<ECommand> { ECommand.ToggleSprinting }, EAmandsControllerCommand.InputTree, EAmandsControllerPressType.Press, -1, "") });
            AmandsControllerButtonBinds.Add(EAmandsControllerButton.RightThumb, new List<AmandsControllerButtonBind> { new AmandsControllerButtonBind(new List<ECommand> { ECommand.QuickKnifeKick }, EAmandsControllerCommand.InputTree, EAmandsControllerPressType.Press, -1, "") });
            AmandsControllerButtonBinds[EAmandsControllerButton.RightThumb].Add(new AmandsControllerButtonBind(new List<ECommand> { ECommand.SelectKnife }, EAmandsControllerCommand.InputTree, EAmandsControllerPressType.Hold, -1, ""));
            AmandsControllerButtonBinds.Add(EAmandsControllerButton.UP, new List<AmandsControllerButtonBind> { new AmandsControllerButtonBind(new List<ECommand> { ECommand.NextTacticalDevice }, EAmandsControllerCommand.InputTree, EAmandsControllerPressType.Press, -1, "") });
            AmandsControllerButtonBinds[EAmandsControllerButton.UP].Add(new AmandsControllerButtonBind(new List<ECommand> { ECommand.ToggleTacticalDevice }, EAmandsControllerCommand.InputTree, EAmandsControllerPressType.Hold, -1, ""));
            AmandsControllerButtonBinds.Add(EAmandsControllerButton.DOWN, new List<AmandsControllerButtonBind> { new AmandsControllerButtonBind(new List<ECommand> { ECommand.ChangeWeaponMode }, EAmandsControllerCommand.InputTree, EAmandsControllerPressType.Press, -1, "") });
            AmandsControllerButtonBinds[EAmandsControllerButton.DOWN].Add(new AmandsControllerButtonBind(new List<ECommand> { ECommand.CheckFireMode }, EAmandsControllerCommand.InputTree, EAmandsControllerPressType.Hold, -1, ""));
            AmandsControllerButtonBinds[EAmandsControllerButton.DOWN].Add(new AmandsControllerButtonBind(new List<ECommand> { ECommand.ForceAutoWeaponMode }, EAmandsControllerCommand.InputTree, EAmandsControllerPressType.DoubleClick, -1, ""));
            AmandsControllerButtonBinds.Add(EAmandsControllerButton.LEFT, new List<AmandsControllerButtonBind> { new AmandsControllerButtonBind(new List<ECommand> { ECommand.ChangeScopeMagnification }, EAmandsControllerCommand.InputTree, EAmandsControllerPressType.Press, -1, "") });
            AmandsControllerButtonBinds[EAmandsControllerButton.LEFT].Add(new AmandsControllerButtonBind(new List<ECommand> { ECommand.ChangeScope }, EAmandsControllerCommand.InputTree, EAmandsControllerPressType.Hold, -1, ""));
            AmandsControllerButtonBinds.Add(EAmandsControllerButton.RIGHT, new List<AmandsControllerButtonBind> { new AmandsControllerButtonBind(new List<ECommand> { ECommand.ChangeScope }, EAmandsControllerCommand.InputTree, EAmandsControllerPressType.Press, -1, "") });
            AmandsControllerButtonBinds.Add(EAmandsControllerButton.BACK, new List<AmandsControllerButtonBind> { new AmandsControllerButtonBind(new List<ECommand> { ECommand.ToggleInventory }, EAmandsControllerCommand.InputTree, EAmandsControllerPressType.Press, -1, "") });
            
            if (Player != null)
            {
                localPlayer = Player;
                //movementContext = localPlayer.MovementContext;
                MovementContextObject = Traverse.Create(localPlayer).Property("MovementContext").GetValue<object>();
                MovementContextType = MovementContextObject.GetType();
                SetCharacterMovementSpeed = MovementContextType.GetMethod("SetCharacterMovementSpeed", BindingFlags.Instance | BindingFlags.Public);
            }
        }
        public void UpdateInterfaceBinds(bool Enabled)
        {
            Interface = Enabled;
            if (Enabled)
            {
                if (AmandsControllerSets.ContainsKey("Interface") && !ActiveAmandsControllerSets.Contains("Interface"))
                {
                    ActiveAmandsControllerSets.Add("Interface");
                }
            }
            else
            {
                ActiveAmandsControllerSets.Remove("Interface");
                ActiveAmandsControllerSets.Remove("OnDrag");
                ActiveAmandsControllerSets.Remove("SearchButton");
            }
        }
        public void UpdateActionPanelBinds(bool Enabled)
        {
            if (Enabled)
            {
                if (AmandsControllerSets.ContainsKey("ActionPanel") && !ActiveAmandsControllerSets.Contains("ActionPanel"))
                {
                    ActiveAmandsControllerSets.Add("ActionPanel");
                }
            }
            else
            {
                ActiveAmandsControllerSets.Remove("ActionPanel");
            }
        }
        public void UpdateHealingLimbSelectorBinds(bool Enabled)
        {
            if (Enabled)
            {
                if (AmandsControllerSets.ContainsKey("HealingLimbSelector") && !ActiveAmandsControllerSets.Contains("HealingLimbSelector"))
                {
                    ActiveAmandsControllerSets.Add("HealingLimbSelector");
                }
            }
            else
            {
                ActiveAmandsControllerSets.Remove("HealingLimbSelector");
            }
        }
        public void AmandsControllerGeneratePressType(EAmandsControllerButton Button, bool Pressed)
        {
            foreach (AmandsControllerButtonBind test in GetPriorityButtonBinds(Button))
            {
                //ConsoleScreen.Log("AmandsControllerButtonSnapshot " + (test.Commands.Count).ToString());
            }
            if (AmandsControllerButtonSnapshots.ContainsKey(Button))
            { 
                AmandsControllerButtonSnapshot AmandsControllerButtonSnapshot = AmandsControllerButtonSnapshots[Button];
                if (Pressed)
                {
                    if (AmandsControllerButtonSnapshot.DoubleClickBind.Commands.Count != 0 && Time.time - AmandsControllerButtonSnapshot.Time <= AmandsControllerPlugin.DoubleClickDelay.Value)
                    {
                        AmandsControllerButton(AmandsControllerButtonSnapshot.DoubleClickBind);
                    }
                    AsyncHold.Remove(Button.ToString() + AmandsControllerButtonSnapshot.Time.ToString());
                    AsyncPress.Remove(Button.ToString() + AmandsControllerButtonSnapshot.Time.ToString());
                    AmandsControllerButtonSnapshots.Remove(Button);
                }
                else
                {
                    // Temp
                    if (AmandsControllerButtonSnapshot.ReleaseBind.Commands.Count != 0)
                    {
                        AmandsControllerButton(AmandsControllerButtonSnapshot.ReleaseBind);
                        AmandsControllerButtonSnapshots.Remove(Button);
                    }
                    else
                    {
                        // Temp
                        if (AmandsControllerButtonSnapshot.HoldBind.Commands.Count == 0 && AmandsControllerButtonSnapshot.DoubleClickBind.Commands.Count == 0)
                        {
                            if (AmandsControllerButtonSnapshot.ReleaseBind.Commands.Count != 0)
                            {
                                AmandsControllerButton(AmandsControllerButtonSnapshot.ReleaseBind);
                            }
                            AmandsControllerButtonSnapshots.Remove(Button);
                        }
                        else if (AmandsControllerButtonSnapshot.HoldBind.Commands.Count != 0 || AmandsControllerButtonSnapshot.DoubleClickBind.Commands.Count != 0)
                        {
                            AsyncHold.Remove(Button.ToString() + AmandsControllerButtonSnapshot.Time.ToString());
                        }
                        if (AmandsControllerButtonSnapshot.DoubleClickBind.Commands.Count == 0 && AmandsControllerButtonSnapshot.ReleaseBind.Commands.Count == 0)
                        {
                            AsyncPress.Remove(Button.ToString() + AmandsControllerButtonSnapshot.Time.ToString());
                            AmandsControllerButton(AmandsControllerButtonSnapshot.PressBind);
                            AmandsControllerButtonSnapshots.Remove(Button);
                        }
                    }
                }
            }
            else if (Pressed)
            {
                float time = Time.time;
                AmandsControllerButtonBind[] Binds = GetPriorityButtonBinds(Button);
                // Temp
                if (Binds[1].Commands.Count != 0)
                {
                    AmandsControllerButtonSnapshots.Add(Button, new AmandsControllerButtonSnapshot(true, time, Binds[0], Binds[1], Binds[2], Binds[3]));
                    AmandsControllerButton(Binds[0]);
                }
                else
                {
                    // Temp
                    if (Binds[2].Commands.Count == 0 && Binds[3].Commands.Count == 0)
                    {
                        AmandsControllerButton(Binds[0]);
                    }
                    else if (Binds[2].Commands.Count != 0 || Binds[3].Commands.Count != 0)
                    {
                        AmandsControllerButtonTimer(Button.ToString() + time.ToString(), Button);
                    }
                    if (Binds[2].Commands.Count != 0 || Binds[3].Commands.Count != 0)
                    {
                        AmandsControllerButtonSnapshots.Add(Button, new AmandsControllerButtonSnapshot(true, time, Binds[0], Binds[1], Binds[2], Binds[3]));
                    }
                }
            }
        }
        public AmandsControllerButtonBind[] GetPriorityButtonBinds(EAmandsControllerButton Button)
        {
            AmandsControllerButtonBind PressBind = EmptyBind;
            AmandsControllerButtonBind ReleaseBind = EmptyBind;
            AmandsControllerButtonBind HoldBind = EmptyBind;
            AmandsControllerButtonBind DoubleClickBind = EmptyBind;

            int SetPriority = -69;
            string PrioritySet = "";

            foreach (string Set in ActiveAmandsControllerSets)
            {
                if (AmandsControllerSets[Set].ContainsKey(Button))
                {
                    foreach (AmandsControllerButtonBind Bind in AmandsControllerSets[Set][Button])
                    {
                        if (Bind.Priority > SetPriority)
                        {
                            SetPriority = Bind.Priority;
                            PrioritySet = Set;
                        }
                    }
                }
            }
            if (PrioritySet != "")
            {
                foreach (AmandsControllerButtonBind Bind in AmandsControllerSets[PrioritySet][Button])
                {
                    switch (Bind.PressType)
                    {
                        case EAmandsControllerPressType.Press:
                            PressBind = Bind;
                            break;
                        case EAmandsControllerPressType.Release:
                            ReleaseBind = Bind;
                            break;
                        case EAmandsControllerPressType.Hold:
                            HoldBind = Bind;
                            break;
                        case EAmandsControllerPressType.DoubleClick:
                            DoubleClickBind = Bind;
                            break;
                    }
                }
            }
            else
            {
                if (AmandsControllerButtonBinds.ContainsKey(Button))
                {
                    foreach (AmandsControllerButtonBind Bind in AmandsControllerButtonBinds[Button])
                    {
                        switch (Bind.PressType)
                        {
                            case EAmandsControllerPressType.Press:
                                PressBind = Bind;
                                break;
                            case EAmandsControllerPressType.Release:
                                ReleaseBind = Bind;
                                break;
                            case EAmandsControllerPressType.Hold:
                                HoldBind = Bind;
                                break;
                            case EAmandsControllerPressType.DoubleClick:
                                DoubleClickBind = Bind;
                                break;
                        }
                    }
                }
            }
            return new AmandsControllerButtonBind[4] { PressBind, ReleaseBind, HoldBind, DoubleClickBind };
        }
        public AmandsControllerButtonBind GetPriorityButtonBind(EAmandsControllerButton Button, EAmandsControllerPressType PressType)
        {
            int BindPriority = -69;
            List<ECommand> PriorityCommand = new List<ECommand>();
            EAmandsControllerCommand PriorityAmandsControllerCommand = EAmandsControllerCommand.Empty;
            string PriorityAmandsControllerSet = "";
            if (AmandsControllerButtonBinds.ContainsKey(Button))
            {
                foreach (AmandsControllerButtonBind Bind in AmandsControllerButtonBinds[Button])
                {
                    if (Bind.PressType == PressType && Bind.Priority > BindPriority)
                    {
                        BindPriority = Bind.Priority;
                        PriorityCommand = Bind.Commands;
                        PriorityAmandsControllerCommand = Bind.AmandsControllerCommand;
                        PriorityAmandsControllerSet = Bind.AmandsControllerSet;
                    }
                }
            }
            foreach (string Set in ActiveAmandsControllerSets)
            {
                if (AmandsControllerSets[Set].ContainsKey(Button))
                {
                    foreach (AmandsControllerButtonBind Bind in AmandsControllerSets[Set][Button])
                    {
                        if (Bind.PressType == PressType && Bind.Priority > BindPriority)
                        {
                            BindPriority = Bind.Priority;
                            PriorityCommand = Bind.Commands;
                            PriorityAmandsControllerCommand = Bind.AmandsControllerCommand;
                            PriorityAmandsControllerSet = Bind.AmandsControllerSet;
                        }
                    }
                }
            }
            return new AmandsControllerButtonBind(PriorityCommand, PriorityAmandsControllerCommand, PressType, BindPriority, PriorityAmandsControllerSet);
        }
        public void AmandsControllerButton(AmandsControllerButtonBind Bind)
        {
            ConsoleScreen.Log(Bind.PressType.ToString());
            switch (Bind.AmandsControllerCommand)
            {
                case EAmandsControllerCommand.ToggleSet:
                    if (ActiveAmandsControllerSets.Contains(Bind.AmandsControllerSet))
                    {
                        ConsoleScreen.Log("ToggleSet Remove " + Bind.AmandsControllerSet);
                        ActiveAmandsControllerSets.Remove(Bind.AmandsControllerSet);
                    }
                    else if (AmandsControllerSets.ContainsKey(Bind.AmandsControllerSet))
                    {
                        ConsoleScreen.Log("ToggleSet Add " + Bind.AmandsControllerSet);
                        ActiveAmandsControllerSets.Add(Bind.AmandsControllerSet);
                    }
                    break;
                case EAmandsControllerCommand.EnableSet:
                    if (AmandsControllerSets.ContainsKey(Bind.AmandsControllerSet) && !ActiveAmandsControllerSets.Contains(Bind.AmandsControllerSet))
                    {
                        ConsoleScreen.Log("EnableSet " + Bind.AmandsControllerSet);
                        ActiveAmandsControllerSets.Add(Bind.AmandsControllerSet);
                    }
                    break;
                case EAmandsControllerCommand.DisableSet:
                    ConsoleScreen.Log("DisableSet " + Bind.AmandsControllerSet);
                    ActiveAmandsControllerSets.Remove(Bind.AmandsControllerSet);
                    break;
                case EAmandsControllerCommand.InputTree:
                    if (Bind.Commands.Count != 0 && inputTree)
                    {
                        foreach (ECommand command in Bind.Commands)
                        {
                            ConsoleScreen.Log(command.ToString());
                        }

                        /*commands.Clear();
                        commands.Add(Bind.Command);
                        switch (Bind.Command)
                        {
                            case ECommand.ToggleShooting:
                                commands.Add(ECommand.TryHighThrow);
                                break;
                            case ECommand.EndShooting:
                                commands.Add(ECommand.FinishHighThrow);
                                break;
                            case ECommand.ToggleAlternativeShooting:
                                commands.Add(ECommand.TryLowThrow);
                                break;
                            case ECommand.EndAlternativeShooting:
                                commands.Add(ECommand.FinishLowThrow);
                                break;
                        }*/
                        TranslateInputInvokeParameters[0] = new List<ECommand>(Bind.Commands);
                        TranslateInput.Invoke(inputTree, TranslateInputInvokeParameters);
                    }
                    break;
                case EAmandsControllerCommand.QuickSelectWeapon:
                    ConsoleScreen.Log("QuickSelectWeapon");
                    break;
                case EAmandsControllerCommand.SlowLeanLeft:
                    SlowLeanLeft = true;
                    break;
                case EAmandsControllerCommand.SlowLeanRight:
                    SlowLeanRight = true;
                    break;
                case EAmandsControllerCommand.EndSlowLean:
                    SlowLeanLeft = false;
                    SlowLeanRight = false;
                    break;
                case EAmandsControllerCommand.RestoreLean:
                    if (inputTree)
                    {
                        commands.Clear();
                        commands.Add(ECommand.EndLeanLeft);
                        commands.Add(ECommand.EndLeanRight);
                        TranslateInputInvokeParameters[0] = commands;
                        TranslateInput.Invoke(inputTree, TranslateInputInvokeParameters);
                    }
                    break;
                case EAmandsControllerCommand.InterfaceUp:
                    ControllerUIMove(new Vector2Int(0, 1), false);
                    AutoMove = true;
                    AutoMoveTime = 0f;
                    gridslotDebug();
                    break;
                case EAmandsControllerCommand.InterfaceDown:
                    ControllerUIMove(new Vector2Int(0, -1), false);
                    AutoMove = true;
                    AutoMoveTime = 0f;
                    gridslotDebug();
                    break;
                case EAmandsControllerCommand.InterfaceLeft:
                    ControllerUIMove(new Vector2Int(-1, 0), false);
                    AutoMove = true;
                    AutoMoveTime = 0f;
                    gridslotDebug();
                    break;
                case EAmandsControllerCommand.InterfaceRight:
                    ControllerUIMove(new Vector2Int(1, 0), false);
                    AutoMove = true;
                    AutoMoveTime = 0f;
                    gridslotDebug();
                    break;
                case EAmandsControllerCommand.InterfaceDisableAutoMove:
                    AutoMove = false;
                    break;
                case EAmandsControllerCommand.BeginDrag:
                    AmandsControllerBeginDrag();
                    break;
                case EAmandsControllerCommand.EndDrag:
                    AmandsControllerEndDrag();
                    break;
                case EAmandsControllerCommand.Search:
                    AmandsControllerSearch();
                    break;
                case EAmandsControllerCommand.Use:
                    AmandsControllerUse(false);
                    break;
                case EAmandsControllerCommand.UseHold:
                    AmandsControllerUse(true);
                    break;
                case EAmandsControllerCommand.QuickMove:
                    AmandsControllerQuickMove();
                    break;
                case EAmandsControllerCommand.Discard:
                    AmandsControllerDiscard();
                    break;
            }
        }
        public async void AmandsControllerButtonTimer(string Token, EAmandsControllerButton Button)
        {
            AsyncPress.Add(Token);
            AsyncHold.Add(Token);
            await Task.Delay((int)(AmandsControllerPlugin.HoldDelay.Value * 1000));
            if (AsyncHold.Contains(Token))
            {
                AmandsControllerButton(AmandsControllerButtonSnapshots[Button].HoldBind);
                AsyncHold.Remove(Token);
                AmandsControllerButtonSnapshots.Remove(Button);
            }
            else if (AsyncPress.Contains(Token))
            {
                AmandsControllerButton(AmandsControllerButtonSnapshots[Button].PressBind);
                AsyncPress.Remove(Token);
                AmandsControllerButtonSnapshots.Remove(Button);
            }
        }
        public void AmandsControllerSearch()
        {
            if (Interface && currentSearchButton != null)
            {
                Press.Invoke(currentSearchButton, null);
            }
            else
            {
                ActiveAmandsControllerSets.Remove("SearchButton");
            }
        }
        // Controller UI
        public void ResetAllCurrent()
        {
            currentGridView = null;
            currentModSlotView = null;
            currentTradingTableGridView = null;
            currentContainedGridsView = null;
            currentItemSpecificationPanel = null;
            currentEquipmentSlotView = null;
            currentWeaponsSlotView = null;
            currentArmbandSlotView = null;
            currentContainersSlotView = null;
            currentSearchButton = null;
        }
        public bool FindGridView(Vector2 Position)
        {
            tglobalPosition = Position;
            hitPointDebug = Position;
            RectTransform rectTransform;
            // GridViews Window stuff inside needs to be out
            if (containedGridsViews.Count != 0)
            {
                Vector2 position;
                int bestDepth = -1;
                CanvasRenderer canvasRenderer;
                ContainedGridsView bestContainedGridsView = null;
                foreach (ContainedGridsView containedGridsView in containedGridsViews)
                {
                    if (containedGridsView == currentContainedGridsView) continue;
                    rectTransform = containedGridsView.GetComponent<RectTransform>();
                    position = new Vector2(containedGridsView.transform.position.x, containedGridsView.transform.position.y - (rectTransform.sizeDelta.y * (rectTransform.pivot.y - 1f) * ScreenRatio));
                    if (Position.x > position.x && Position.x < (position.x + (rectTransform.sizeDelta.x * ScreenRatio)) && Position.y < position.y && Position.y > (position.y - (rectTransform.sizeDelta.y * ScreenRatio)))
                    {
                        canvasRenderer = containedGridsView.GetComponent<CanvasRenderer>();
                        if (canvasRenderer != null && canvasRenderer.absoluteDepth > bestDepth)
                        {
                            bestDepth = canvasRenderer.absoluteDepth;
                            bestContainedGridsView = containedGridsView;
                        }
                    }
                }
                if (bestContainedGridsView != null)
                {
                    rectTransform = bestContainedGridsView.GetComponent<RectTransform>();
                    hitDebug = new Vector2(bestContainedGridsView.transform.position.x, bestContainedGridsView.transform.position.y - (rectTransform.sizeDelta.y * (rectTransform.pivot.y - 1f) * ScreenRatio));
                    hitSizeDebug = rectTransform.sizeDelta * ScreenRatio;
                    int GridWidth;
                    int GridHeight;

                    float distance;
                    float bestDistance = 999999f;

                    GridView bestGridView = null;
                    Vector2Int bestGridViewLocation = Vector2Int.zero;

                    foreach (GridView gridView in bestContainedGridsView.GridViews)
                    {
                        GridWidth = Traverse.Create(Traverse.Create(gridView).Field("Grid").GetValue<object>()).Property("GridWidth").GetValue<IBindable<int>>().Value;
                        GridHeight = Traverse.Create(Traverse.Create(gridView).Field("Grid").GetValue<object>()).Property("GridHeight").GetValue<IBindable<int>>().Value;
                        //GridWidth = gridView.Grid.GridWidth.Value;
                        //GridHeight = gridView.Grid.GridHeight.Value;

                        if (GridWidth == 1 && GridHeight == 1)
                        {
                            position.x = GridSize / 2f;
                            position.y = -GridSize / 2f;
                        }
                        else if (GridWidth == 1)
                        {
                            position.x = GridSize / 2f;
                            position.y = -Mathf.Clamp(gridView.transform.position.y - Position.y, GridSize / 2f, (GridSize * GridHeight) - (GridSize / 2f));
                        }
                        else if (GridHeight == 1)
                        {
                            position.x = Mathf.Clamp(Position.x - gridView.transform.position.x, GridSize / 2f, (GridSize * GridWidth) - (GridSize / 2f));
                            position.y = -GridSize / 2f;
                        }
                        else
                        {
                            position.x = Mathf.Clamp(Position.x - gridView.transform.position.x, GridSize / 2f, (GridSize * GridWidth) - (GridSize / 2f));
                            position.y = -Mathf.Clamp(gridView.transform.position.y - Position.y, GridSize / 2f, (GridSize * GridHeight) - (GridSize / 2f));
                        }

                        distance = Vector2.Distance(Position, (Vector2)gridView.transform.position + position);

                        if (distance < bestDistance)
                        {
                            bestDistance = distance;
                            bestGridView = gridView;
                            bestGridViewLocation = new Vector2Int(Mathf.RoundToInt((position.x + (GridSize / 2f)) / GridSize), -Mathf.RoundToInt((position.y - (GridSize / 2f)) / GridSize));
                        }
                    }
                    if (bestGridView != null)
                    {
                        ResetAllCurrent();
                        currentGridView = bestGridView;
                        currentContainedGridsView = bestContainedGridsView;
                        gridViewLocation = bestGridViewLocation;
                        globalPosition.x = bestGridView.transform.position.x + (GridSize * gridViewLocation.x) - (GridSize / 2f);
                        globalPosition.y = bestGridView.transform.position.y - (GridSize * gridViewLocation.y) + (GridSize / 2f);
                        return true;
                    }
                }
            }
            // Support SlotViews
            if (itemSpecificationPanels.Count != 0)
            {
                Vector2 position;
                int bestDepth = -1;
                CanvasRenderer canvasRenderer;
                ItemSpecificationPanel bestItemSpecificationPanel = null;
                if (currentItemSpecificationPanel != null)
                {
                    canvasRenderer = currentItemSpecificationPanel.GetComponent<CanvasRenderer>();
                    bestDepth = canvasRenderer.absoluteDepth;
                }
                foreach (ItemSpecificationPanel itemSpecificationPanel in itemSpecificationPanels)
                {
                    if (itemSpecificationPanel == currentItemSpecificationPanel) continue;
                    rectTransform = itemSpecificationPanel.GetComponent<RectTransform>();
                    position = new Vector2(itemSpecificationPanel.transform.position.x - ((rectTransform.sizeDelta.x / 2) * ScreenRatio), itemSpecificationPanel.transform.position.y + ((rectTransform.sizeDelta.y / 2) * ScreenRatio));
                    if (Position.x > position.x && Position.x < (position.x + (rectTransform.sizeDelta.x * ScreenRatio)) && Position.y < position.y && Position.y > (position.y - (rectTransform.sizeDelta.y * ScreenRatio)))
                    {
                        canvasRenderer = itemSpecificationPanel.GetComponent<CanvasRenderer>();
                        if (canvasRenderer != null && canvasRenderer.absoluteDepth > bestDepth)
                        {
                            bestDepth = canvasRenderer.absoluteDepth;
                            bestItemSpecificationPanel = itemSpecificationPanel;
                        }
                    }
                }
                if (bestItemSpecificationPanel != null)
                {
                    rectTransform = bestItemSpecificationPanel.GetComponent<RectTransform>();
                    hitDebug = new Vector2(bestItemSpecificationPanel.transform.position.x - ((rectTransform.sizeDelta.x / 2) * ScreenRatio), bestItemSpecificationPanel.transform.position.y + ((rectTransform.sizeDelta.y / 2) * ScreenRatio));
                    hitSizeDebug = rectTransform.sizeDelta * ScreenRatio;

                    float distance;
                    float bestDistance = 999999f;

                    ModSlotView bestModSlotView = null;

                    foreach (ModSlotView modSlotView in Traverse.Create(bestItemSpecificationPanel).Field("_modsContainer").GetValue<RectTransform>().GetComponentsInChildren<ModSlotView>())
                    {
                        distance = Vector2.Distance(Position, (Vector2)modSlotView.transform.position);

                        if (distance < bestDistance)
                        {
                            bestDistance = distance;
                            bestModSlotView = modSlotView;
                        }
                    }
                    if (bestModSlotView != null)
                    {
                        ResetAllCurrent();
                        currentModSlotView = bestModSlotView;
                        currentItemSpecificationPanel = bestItemSpecificationPanel;
                        gridViewLocation = new Vector2Int(1, 1);
                        globalPosition.x = currentModSlotView.transform.position.x;
                        globalPosition.y = currentModSlotView.transform.position.y;
                        return true;
                    }
                }
            }

            // find gridviews 0 depth
            if (currentContainedGridsView != null || currentItemSpecificationPanel != null)
            {
                foreach (GridView gridView in gridViews)
                {
                    rectTransform = gridView.GetComponent<RectTransform>();
                    if (Position.x > gridView.transform.position.x && Position.x < (gridView.transform.position.x + (rectTransform.sizeDelta.x * ScreenRatio)) && Position.y < gridView.transform.position.y && Position.y > (gridView.transform.position.y - (rectTransform.sizeDelta.y * ScreenRatio)))
                    {
                        Vector2 position;
                        int GridWidth = Traverse.Create(Traverse.Create(gridView).Field("Grid").GetValue<object>()).Property("GridWidth").GetValue<IBindable<int>>().Value;
                        int GridHeight = Traverse.Create(Traverse.Create(gridView).Field("Grid").GetValue<object>()).Property("GridHeight").GetValue<IBindable<int>>().Value;
                        //int GridWidth = gridView.Grid.GridWidth.Value;
                        //int GridHeight = gridView.Grid.GridHeight.Value;

                        hitDebug = gridView.transform.position;
                        hitSizeDebug = rectTransform.sizeDelta * ScreenRatio;

                        if (GridWidth == 1 && GridHeight == 1)
                        {
                            position.x = GridSize / 2f;
                            position.y = -GridSize / 2f;
                        }
                        else if (GridWidth == 1)
                        {
                            position.x = GridSize / 2f;
                            position.y = -Mathf.Clamp(gridView.transform.position.y - Position.y, GridSize / 2f, (GridSize * GridHeight) - (GridSize / 2f));
                        }
                        else if (GridHeight == 1)
                        {
                            position.x = Mathf.Clamp(Position.x - gridView.transform.position.x, GridSize / 2f, (GridSize * GridWidth) - (GridSize / 2f));
                            position.y = -GridSize / 2f;
                        }
                        else
                        {
                            position.x = Mathf.Clamp(Position.x - gridView.transform.position.x, GridSize / 2f, (GridSize * GridWidth) - (GridSize / 2f));
                            position.y = -Mathf.Clamp(gridView.transform.position.y - Position.y, GridSize / 2f, (GridSize * GridHeight) - (GridSize / 2f));
                        }
                        ResetAllCurrent();
                        currentGridView = gridView;
                        gridViewLocation = new Vector2Int(Mathf.RoundToInt((position.x + (GridSize / 2f)) / GridSize), -Mathf.RoundToInt((position.y - (GridSize / 2f)) / GridSize));
                        ConsoleScreen.Log(gridViewLocation.ToString());
                        globalPosition.x = gridView.transform.position.x + (GridSize * gridViewLocation.x) - (GridSize / 2f);
                        globalPosition.y = gridView.transform.position.y - (GridSize * gridViewLocation.y) + (GridSize / 2f);
                        return true;
                    }
                }
            }
            // find tradingtablegridview 0 depth
            if (currentContainedGridsView != null && tradingTableGridView != null)
            {
                rectTransform = tradingTableGridView.GetComponent<RectTransform>();
                Vector2 size = rectTransform.sizeDelta * ScreenRatio;
                Vector2 position = new Vector2(tradingTableGridView.transform.position.x - (size.x / 2f), tradingTableGridView.transform.position.y + (size.y / 2f));
                if (Position.x > position.x && Position.x < (position.x + size.x) && Position.y < position.y && Position.y > (position.y - size.y))
                {
                    int GridWidth = Traverse.Create(Traverse.Create(tradingTableGridView).Field("Grid").GetValue<object>()).Property("GridWidth").GetValue<IBindable<int>>().Value;
                    int GridHeight = Traverse.Create(Traverse.Create(tradingTableGridView).Field("Grid").GetValue<object>()).Property("GridHeight").GetValue<IBindable<int>>().Value;
                    //int GridWidth = tradingTableGridView.Grid.GridWidth.Value;
                    //int GridHeight = tradingTableGridView.Grid.GridHeight.Value;

                    hitDebug = position;
                    hitSizeDebug = rectTransform.sizeDelta * ScreenRatio;

                    if (GridWidth == 1 && GridHeight == 1)
                    {
                        position.x = GridSize / 2f;
                        position.y = -GridSize / 2f;
                    }
                    else if (GridWidth == 1)
                    {
                        position.x = GridSize / 2f;
                        position.y = -Mathf.Clamp(position.y - Position.y, GridSize / 2f, (GridSize * GridHeight) - (GridSize / 2f));
                    }
                    else if (GridHeight == 1)
                    {
                        position.x = Mathf.Clamp(Position.x - position.x, GridSize / 2f, (GridSize * GridWidth) - (GridSize / 2f));
                        position.y = -GridSize / 2f;
                    }
                    else
                    {
                        position.x = Mathf.Clamp(Position.x - position.x, GridSize / 2f, (GridSize * GridWidth) - (GridSize / 2f));
                        position.y = -Mathf.Clamp(position.y - Position.y, GridSize / 2f, (GridSize * GridHeight) - (GridSize / 2f));
                    }
                    ResetAllCurrent();
                    currentTradingTableGridView = tradingTableGridView;
                    gridViewLocation = new Vector2Int(Mathf.RoundToInt((position.x + (GridSize / 2f)) / GridSize), -Mathf.RoundToInt((position.y - (GridSize / 2f)) / GridSize));
                    ConsoleScreen.Log(gridViewLocation.ToString());
                    size = currentTradingTableGridView.GetComponent<RectTransform>().sizeDelta * ScreenRatio;
                    globalPosition.x = (currentTradingTableGridView.transform.position.x - (size.x / 2f)) + (GridSize * gridViewLocation.x) - (GridSize / 2f);
                    globalPosition.y = (currentTradingTableGridView.transform.position.y + (size.y / 2f)) - (GridSize * gridViewLocation.y) + (GridSize / 2f);
                    return true;
                }
            }

            // find equipmentSlotViews 0 depth
            if (currentContainedGridsView != null)
            {
                foreach (SlotView slotView in equipmentSlotViews)
                {
                    //if (Position.x > slotView.transform.position.x - (SlotSize / 2f) && Position.x < slotView.transform.position.x + (SlotSize / 2f) && Position.y < slotView.transform.position.y && Position.y > (slotView.transform.position.y - SlotSize)
                    if (Position.x > slotView.transform.position.x && Position.x < slotView.transform.position.x + SlotSize && Position.y < slotView.transform.position.y && Position.y > (slotView.transform.position.y - SlotSize))
                    {
                        // Debug start
                        Vector2 position;
                        position.x = slotView.transform.position.x;// - SlotSize / 2f;
                        position.y = slotView.transform.position.y;
                        hitDebug = position;
                        hitSizeDebug = new Vector2(SlotSize, SlotSize);
                        ConsoleScreen.Log("Hitdection hit Equipment Slot");
                        // Debug end
                        ResetAllCurrent();
                        currentEquipmentSlotView = slotView;
                        gridViewLocation = new Vector2Int(1, 1);
                        ConsoleScreen.Log(gridViewLocation.ToString());
                        globalPosition.x = currentEquipmentSlotView.transform.position.x + (SlotSize / 2f);
                        globalPosition.y = currentEquipmentSlotView.transform.position.y - (SlotSize / 2f);
                        return true;
                    }
                }
            }
            // find weaponsSlotViews 0 depth
            if (currentContainedGridsView != null)
            {
                foreach (SlotView slotView in weaponsSlotViews)
                {
                    //if (Position.x > slotView.transform.position.x - (157.0811f * ScreenRatio) && Position.x < slotView.transform.position.x + (157.0811f * ScreenRatio) && Position.y < slotView.transform.position.y && Position.y > (slotView.transform.position.y - SlotSize))
                    if (Position.x > slotView.transform.position.x && Position.x < slotView.transform.position.x + (314.1622f * ScreenRatio) && Position.y < slotView.transform.position.y && Position.y > (slotView.transform.position.y - SlotSize))
                    {
                        // Debug start
                        Vector2 position;
                        position.x = slotView.transform.position.x;// - (157.0811f * ScreenRatio);
                        position.y = slotView.transform.position.y;
                        hitDebug = position;
                        hitSizeDebug = new Vector2((314.1622f * ScreenRatio), SlotSize);
                        ConsoleScreen.Log("Hitdection hit Weapon Slot");
                        // Debug end
                        ResetAllCurrent();
                        currentWeaponsSlotView = slotView;
                        gridViewLocation = new Vector2Int(1, 1);
                        ConsoleScreen.Log(gridViewLocation.ToString());
                        globalPosition.x = currentWeaponsSlotView.transform.position.x + (157.0811f * ScreenRatio);
                        globalPosition.y = currentWeaponsSlotView.transform.position.y - (SlotSize / 2f);
                        return true;
                    }
                }
            }
            // find armbandSlotView 0 depth
            if (currentContainedGridsView != null)
            {
                if (armbandSlotView != null)
                {
                    //if (Position.x > armbandSlotView.transform.position.x - (62.5f * ScreenRatio) && Position.x < armbandSlotView.transform.position.x + (62.5f * ScreenRatio) && Position.y < armbandSlotView.transform.position.y && Position.y > (armbandSlotView.transform.position.y - (64f * ScreenRatio)))
                    if (Position.x > armbandSlotView.transform.position.x && Position.x < armbandSlotView.transform.position.x + SlotSize && Position.y < armbandSlotView.transform.position.y && Position.y > (armbandSlotView.transform.position.y - (64f * ScreenRatio)))
                    {
                        // Debug start
                        Vector2 position;
                        position.x = armbandSlotView.transform.position.x;// - (62.5f * ScreenRatio);
                        position.y = armbandSlotView.transform.position.y;
                        hitDebug = position;
                        hitSizeDebug = new Vector2(SlotSize, (64f * ScreenRatio));
                        ConsoleScreen.Log("Hitdection hit Armband Slot");
                        // Debug end
                        ResetAllCurrent();
                        currentArmbandSlotView = armbandSlotView;
                        gridViewLocation = new Vector2Int(1, 1);
                        ConsoleScreen.Log(gridViewLocation.ToString());
                        globalPosition.x = currentArmbandSlotView.transform.position.x + (SlotSize / 2f);
                        globalPosition.y = currentArmbandSlotView.transform.position.y - (32f * ScreenRatio);
                        return true;
                    }
                }
            }
            // find containersSlotViews 0 depth
            if (currentContainedGridsView != null)
            {
                foreach (SlotView slotView in containersSlotViews)
                {
                    if (Position.x > slotView.transform.position.x && Position.x < slotView.transform.position.x + SlotSize && Position.y < slotView.transform.position.y && Position.y > (slotView.transform.position.y - SlotSize))
                    {
                        // Debug start
                        Vector2 position;
                        position.x = slotView.transform.position.x;
                        position.y = slotView.transform.position.y;
                        hitDebug = position;
                        hitSizeDebug = new Vector2(SlotSize, SlotSize);
                        ConsoleScreen.Log("Hitdection hit Container Slot");
                        // Debug end
                        ResetAllCurrent();
                        currentContainersSlotView = slotView;
                        gridViewLocation = new Vector2Int(1, 1);
                        ConsoleScreen.Log(gridViewLocation.ToString());
                        globalPosition.x = currentContainersSlotView.transform.position.x + (SlotSize / 2f);
                        globalPosition.y = currentContainersSlotView.transform.position.y - (SlotSize / 2f);
                        return true;
                    }
                }
            }

            // find lootEquipmentSlotViews 0 depth
            if (currentContainedGridsView != null)
            {
                foreach (SlotView slotView in lootEquipmentSlotViews)
                {
                    if (Position.x > slotView.transform.position.x && Position.x < slotView.transform.position.x + SlotSize && Position.y < slotView.transform.position.y && Position.y > (slotView.transform.position.y - SlotSize))
                    {
                        // Debug start
                        Vector2 position;
                        position.x = slotView.transform.position.x;// - SlotSize / 2f;
                        position.y = slotView.transform.position.y;
                        hitDebug = position;
                        hitSizeDebug = new Vector2(SlotSize, SlotSize);
                        ConsoleScreen.Log("Hitdection hit Equipment Slot");
                        // Debug end
                        ResetAllCurrent();
                        currentEquipmentSlotView = slotView;
                        gridViewLocation = new Vector2Int(1, 1);
                        ConsoleScreen.Log(gridViewLocation.ToString());
                        globalPosition.x = currentEquipmentSlotView.transform.position.x + (SlotSize / 2f);
                        globalPosition.y = currentEquipmentSlotView.transform.position.y - (SlotSize / 2f);
                        return true;
                    }
                }
            }
            // find lootWeaponsSlotViews 0 depth
            if (currentContainedGridsView != null)
            {
                foreach (SlotView slotView in lootWeaponsSlotViews)
                {
                    if (Position.x > slotView.transform.position.x && Position.x < slotView.transform.position.x + (314.1622f * ScreenRatio) && Position.y < slotView.transform.position.y && Position.y > (slotView.transform.position.y - SlotSize))
                    {
                        // Debug start
                        Vector2 position;
                        position.x = slotView.transform.position.x;// - (133.0724f * ScreenRatio);
                        position.y = slotView.transform.position.y;
                        hitDebug = position;
                        hitSizeDebug = new Vector2((314.1622f * ScreenRatio), SlotSize);
                        ConsoleScreen.Log("Hitdection hit Weapon Slot");
                        // Debug end
                        ResetAllCurrent();
                        currentWeaponsSlotView = slotView;
                        gridViewLocation = new Vector2Int(1, 1);
                        ConsoleScreen.Log(gridViewLocation.ToString());
                        globalPosition.x = currentWeaponsSlotView.transform.position.x + (157.0811f * ScreenRatio);
                        globalPosition.y = currentWeaponsSlotView.transform.position.y - (SlotSize / 2f);
                        return true;
                    }
                }
            }
            // find lootArmbandSlotView 0 depth
            if (currentContainedGridsView != null)
            {
                if (lootArmbandSlotView != null)
                {
                    if (Position.x > lootArmbandSlotView.transform.position.x && Position.x < lootArmbandSlotView.transform.position.x + SlotSize && Position.y < lootArmbandSlotView.transform.position.y && Position.y > (lootArmbandSlotView.transform.position.y - (64f * ScreenRatio)))
                    {
                        // Debug start
                        Vector2 position;
                        position.x = lootArmbandSlotView.transform.position.x;// - (62.5f * ScreenRatio);
                        position.y = lootArmbandSlotView.transform.position.y;
                        hitDebug = position;
                        hitSizeDebug = new Vector2(SlotSize, (64f * ScreenRatio));
                        ConsoleScreen.Log("Hitdection hit Armband Slot");
                        // Debug end
                        ResetAllCurrent();
                        currentArmbandSlotView = lootArmbandSlotView;
                        gridViewLocation = new Vector2Int(1, 1);
                        ConsoleScreen.Log(gridViewLocation.ToString());
                        globalPosition.x = currentArmbandSlotView.transform.position.x + (SlotSize / 2f);
                        globalPosition.y = currentArmbandSlotView.transform.position.y - (32f * ScreenRatio);
                        return true;
                    }
                }
            }
            // find lootContainersSlotViews 0 depth
            if (currentContainedGridsView != null)
            {
                foreach (SlotView slotView in lootContainersSlotViews)
                {
                    if (Position.x > slotView.transform.position.x && Position.x < slotView.transform.position.x + SlotSize && Position.y < slotView.transform.position.y && Position.y > (slotView.transform.position.y - SlotSize))
                    {
                        // Debug start
                        Vector2 position;
                        position.x = slotView.transform.position.x;
                        position.y = slotView.transform.position.y;
                        hitDebug = position;
                        hitSizeDebug = new Vector2(SlotSize, SlotSize);
                        ConsoleScreen.Log("Hitdection hit Container Slot");
                        // Debug end
                        ResetAllCurrent();
                        currentContainersSlotView = slotView;
                        gridViewLocation = new Vector2Int(1, 1);
                        ConsoleScreen.Log(gridViewLocation.ToString());
                        globalPosition.x = currentContainersSlotView.transform.position.x + (SlotSize / 2f);
                        globalPosition.y = currentContainersSlotView.transform.position.y - (SlotSize / 2f);
                        return true;
                    }
                }
            }
            return false;
        }
        public bool FindGridWindow(Vector2 Position)
        {
            RectTransform rectTransform;

            if (containedGridsViews.Count != 0)
            {
                Vector2 position;
                int bestDepth = -1;
                CanvasRenderer canvasRenderer;
                ContainedGridsView bestContainedGridsView = null;
                if (currentContainedGridsView != null)
                {
                    canvasRenderer = currentContainedGridsView.GetComponent<CanvasRenderer>();
                    bestDepth = canvasRenderer.absoluteDepth;
                }
                foreach (ContainedGridsView containedGridsView in containedGridsViews)
                {
                    if (containedGridsView == currentContainedGridsView) continue;
                    rectTransform = containedGridsView.GetComponent<RectTransform>();
                    position = new Vector2(containedGridsView.transform.position.x, containedGridsView.transform.position.y - (rectTransform.sizeDelta.y * (rectTransform.pivot.y - 1f) * ScreenRatio));
                    if (Position.x > position.x && Position.x < (position.x + (rectTransform.sizeDelta.x * ScreenRatio)) && Position.y < position.y && Position.y > (position.y - (rectTransform.sizeDelta.y * ScreenRatio)))
                    {
                        canvasRenderer = containedGridsView.GetComponent<CanvasRenderer>();
                        if (canvasRenderer != null && canvasRenderer.absoluteDepth > bestDepth)
                        {
                            bestDepth = canvasRenderer.absoluteDepth;
                            bestContainedGridsView = containedGridsView;
                        }
                    }
                }
                if (bestContainedGridsView != null)
                {
                    rectTransform = bestContainedGridsView.GetComponent<RectTransform>();
                    hitDebug = new Vector2(bestContainedGridsView.transform.position.x, bestContainedGridsView.transform.position.y - (rectTransform.sizeDelta.y * (rectTransform.pivot.y - 1f) * ScreenRatio));
                    hitSizeDebug = rectTransform.sizeDelta * ScreenRatio;
                    int GridWidth;
                    int GridHeight;

                    float distance;
                    float bestDistance = 999999f;

                    GridView bestGridView = null;
                    Vector2Int bestGridViewLocation = Vector2Int.zero;

                    foreach (GridView gridView in bestContainedGridsView.GridViews)
                    {
                        GridWidth = Traverse.Create(Traverse.Create(tradingTableGridView).Field("Grid").GetValue<object>()).Property("GridWidth").GetValue<IBindable<int>>().Value;
                        GridHeight = Traverse.Create(Traverse.Create(tradingTableGridView).Field("Grid").GetValue<object>()).Property("GridHeight").GetValue<IBindable<int>>().Value;
                        //GridWidth = gridView.Grid.GridWidth.Value;
                        //GridHeight = gridView.Grid.GridHeight.Value;

                        if (GridWidth == 1 && GridHeight == 1)
                        {
                            position.x = GridSize / 2f;
                            position.y = -GridSize / 2f;
                        }
                        else if (GridWidth == 1)
                        {
                            position.x = GridSize / 2f;
                            position.y = -Mathf.Clamp(gridView.transform.position.y - Position.y, GridSize / 2f, (GridSize * GridHeight) - (GridSize / 2f));
                        }
                        else if (GridHeight == 1)
                        {
                            position.x = Mathf.Clamp(Position.x - gridView.transform.position.x, GridSize / 2f, (GridSize * GridWidth) - (GridSize / 2f));
                            position.y = -GridSize / 2f;
                        }
                        else
                        {
                            position.x = Mathf.Clamp(Position.x - gridView.transform.position.x, GridSize / 2f, (GridSize * GridWidth) - (GridSize / 2f));
                            position.y = -Mathf.Clamp(gridView.transform.position.y - Position.y, GridSize / 2f, (GridSize * GridHeight) - (GridSize / 2f));
                        }

                        distance = Vector2.Distance(Position, (Vector2)gridView.transform.position + position);

                        if (distance < bestDistance)
                        {
                            bestDistance = distance;
                            bestGridView = gridView;
                            bestGridViewLocation = new Vector2Int(Mathf.RoundToInt((position.x + (GridSize / 2f)) / GridSize), -Mathf.RoundToInt((position.y - (GridSize / 2f)) / GridSize));
                        }
                    }
                    if (bestGridView != null)
                    {
                        ResetAllCurrent();
                        currentGridView = bestGridView;
                        currentContainedGridsView = bestContainedGridsView;
                        gridViewLocation = bestGridViewLocation;
                        globalPosition.x = currentGridView.transform.position.x + (GridSize * gridViewLocation.x) - (GridSize / 2f);
                        globalPosition.y = currentGridView.transform.position.y - (GridSize * gridViewLocation.y) + (GridSize / 2f);
                        return true;
                    }
                }
            }
            // Support SlotViews Window

            if (itemSpecificationPanels.Count != 0)
            {
                Vector2 position;
                int bestDepth = -1;
                CanvasRenderer canvasRenderer;
                ItemSpecificationPanel bestItemSpecificationPanel = null;
                if (currentItemSpecificationPanel != null)
                {
                    canvasRenderer = currentItemSpecificationPanel.GetComponent<CanvasRenderer>();
                    bestDepth = canvasRenderer.absoluteDepth;
                }
                foreach (ItemSpecificationPanel itemSpecificationPanel in itemSpecificationPanels)
                {
                    if (itemSpecificationPanel == currentItemSpecificationPanel) continue;
                    rectTransform = itemSpecificationPanel.GetComponent<RectTransform>();
                    position = new Vector2(itemSpecificationPanel.transform.position.x - ((rectTransform.sizeDelta.x / 2) * ScreenRatio), itemSpecificationPanel.transform.position.y + ((rectTransform.sizeDelta.y / 2) * ScreenRatio));
                    if (Position.x > position.x && Position.x < (position.x + (rectTransform.sizeDelta.x * ScreenRatio)) && Position.y < position.y && Position.y > (position.y - (rectTransform.sizeDelta.y * ScreenRatio)))
                    {
                        canvasRenderer = itemSpecificationPanel.GetComponent<CanvasRenderer>();
                        if (canvasRenderer != null && canvasRenderer.absoluteDepth > bestDepth)
                        {
                            bestDepth = canvasRenderer.absoluteDepth;
                            bestItemSpecificationPanel = itemSpecificationPanel;
                        }
                    }
                }
                if (bestItemSpecificationPanel != null)
                {
                    rectTransform = bestItemSpecificationPanel.GetComponent<RectTransform>();
                    hitDebug = new Vector2(bestItemSpecificationPanel.transform.position.x - ((rectTransform.sizeDelta.x / 2) * ScreenRatio), bestItemSpecificationPanel.transform.position.y + ((rectTransform.sizeDelta.y / 2) * ScreenRatio));
                    hitSizeDebug = rectTransform.sizeDelta * ScreenRatio;

                    float distance;
                    float bestDistance = 999999f;

                    ModSlotView bestModSlotView = null;

                    foreach (ModSlotView modSlotView in Traverse.Create(bestItemSpecificationPanel).Field("_modsContainer").GetValue<RectTransform>().GetComponentsInChildren<ModSlotView>())
                    {
                        distance = Vector2.Distance(Position, (Vector2)modSlotView.transform.position);

                        if (distance < bestDistance)
                        {
                            bestDistance = distance;
                            bestModSlotView = modSlotView;
                        }
                    }
                    if (bestModSlotView != null)
                    {
                        ResetAllCurrent();
                        currentModSlotView = bestModSlotView;
                        currentItemSpecificationPanel = bestItemSpecificationPanel;
                        gridViewLocation = new Vector2Int(1, 1);
                        globalPosition.x = currentModSlotView.transform.position.x;
                        globalPosition.y = currentModSlotView.transform.position.y;
                        return true;
                    }
                }
            }
            return false;
        }
        public bool FindScrollRectNoDrang(Vector2 Position)
        {
            RectTransform rectTransform;
            Vector2 position;
            foreach (ScrollRectNoDrag scrollRectNoDrag in scrollRectNoDrags)
            {
                rectTransform = scrollRectNoDrag.GetComponent<RectTransform>();
                position = new Vector2(rectTransform.position.x + rectTransform.rect.x, rectTransform.position.y - (rectTransform.rect.height * (rectTransform.pivot.y - 1f)));
                if (Position.x > position.x && Position.x < (position.x + (rectTransform.rect.width * ScreenRatio)) && Position.y < position.y && Position.y > (position.y - (rectTransform.rect.height * ScreenRatio)))
                {
                    currentScrollRectNoDrag = scrollRectNoDrag;
                    currentScrollRectNoDragRectTransform = rectTransform;
                    return true;
                }
            }
            return false;
        }
        public void ControllerUIOnMove(Vector2Int direction, Vector2 position)
        {
            AmandsOnDrag();
            FindScrollRectNoDrang(position);
        }
        public void ControllerUIMove(Vector2Int direction, bool Skip)
        {
            ActiveAmandsControllerSets.Remove("SearchButton");

            lastDirection = direction;
            if (currentGridView == null && currentTradingTableGridView == null && currentEquipmentSlotView == null && currentWeaponsSlotView == null && currentArmbandSlotView == null && currentContainersSlotView == null && currentModSlotView == null && currentSearchButton == null) currentGridView = gridViews[0];

            ScreenRatio = (Screen.height / 1080f);
            GridSize = 63f * ScreenRatio;
            ModSize = 63f * ScreenRatio;
            SlotSize = 125f * ScreenRatio;

            Vector2 position;

            int GridWidth = 1;
            int GridHeight = 1;

            float dot;
            float distance;
            float score;
            float bestScore = 99999f;

            GridView bestGridView = null;
            ModSlotView bestModSlotView = null;
            SlotView bestEquipmentSlotView = null;
            SlotView bestWeaponsSlotView = null;
            SlotView bestArmbandSlotView = null;
            SlotView bestContainersSlotView = null;
            ItemSpecificationPanel bestItemSpecificationPanel = null;
            TradingTableGridView bestTradingTableGridView = null;
            ContainedGridsView bestContainedGridsView = null;
            SearchButton bestSearchButton = null;
            Vector2Int bestGridViewLocation = Vector2Int.one;

            if (Skip) goto Skip1;

            // Local GridView Search
            if (currentGridView != null)
            {
                GridWidth = Traverse.Create(Traverse.Create(currentGridView).Field("Grid").GetValue<object>()).Property("GridWidth").GetValue<IBindable<int>>().Value;
                GridHeight = Traverse.Create(Traverse.Create(currentGridView).Field("Grid").GetValue<object>()).Property("GridHeight").GetValue<IBindable<int>>().Value;
            }
            if (currentGridView != null && gridViewLocation.x + direction.x >= 1 && gridViewLocation.x + direction.x <= GridWidth && gridViewLocation.y - direction.y >= 1 && gridViewLocation.y - direction.y <= GridHeight)
            //if (currentGridView != null && gridViewLocation.x + direction.x >= 1 && gridViewLocation.x + direction.x <= currentGridView.Grid.GridWidth.Value && gridViewLocation.y - direction.y >= 1 && gridViewLocation.y - direction.y <= currentGridView.Grid.GridHeight.Value)
            {
                gridViewLocation.x += direction.x;
                gridViewLocation.y -= direction.y;

                globalPosition.x = currentGridView.transform.position.x + (GridSize * gridViewLocation.x) - (GridSize / 2f);
                globalPosition.y = currentGridView.transform.position.y - (GridSize * gridViewLocation.y) + (GridSize / 2f);
                FindGridWindow(globalPosition);
                ControllerUIOnMove(direction, globalPosition);
                return;
            }

            // Local TradingTableGridView Search
            if (currentTradingTableGridView != null)
            {
                GridWidth = Traverse.Create(Traverse.Create(currentTradingTableGridView).Field("Grid").GetValue<object>()).Property("GridWidth").GetValue<IBindable<int>>().Value;
                GridHeight = Traverse.Create(Traverse.Create(currentTradingTableGridView).Field("Grid").GetValue<object>()).Property("GridHeight").GetValue<IBindable<int>>().Value;
            }
            if (currentTradingTableGridView != null && gridViewLocation.x + direction.x >= 1 && gridViewLocation.x + direction.x <= GridWidth && gridViewLocation.y - direction.y >= 1 && gridViewLocation.y - direction.y <= GridHeight)
            //if (currentTradingTableGridView != null && gridViewLocation.x + direction.x >= 1 && gridViewLocation.x + direction.x <= currentTradingTableGridView.Grid.GridWidth.Value && gridViewLocation.y - direction.y >= 1 && gridViewLocation.y - direction.y <= currentTradingTableGridView.Grid.GridHeight.Value)
            {
                gridViewLocation.x += direction.x;
                gridViewLocation.y -= direction.y;

                Vector2 size = currentTradingTableGridView.GetComponent<RectTransform>().sizeDelta * ScreenRatio;
                globalPosition.x = (currentTradingTableGridView.transform.position.x - (size.x / 2f)) + (GridSize * gridViewLocation.x) - (GridSize / 2f);
                globalPosition.y = (currentTradingTableGridView.transform.position.y + (size.y / 2f)) - (GridSize * gridViewLocation.y) + (GridSize / 2f);
                FindGridWindow(globalPosition);
                ControllerUIOnMove(direction, globalPosition);
                return;
            }

            // Local ContainedGridsView GridView Blind Search
            if (currentGridView != null && currentContainedGridsView != null)
            {
                globalPosition.x = currentGridView.transform.position.x + (GridSize * gridViewLocation.x) - (GridSize / 2f);
                globalPosition.y = currentGridView.transform.position.y - (GridSize * gridViewLocation.y) + (GridSize / 2f);

                foreach (GridView gridView in currentContainedGridsView.GridViews)
                {
                    if (gridView == currentGridView) continue;

                    GridWidth = Traverse.Create(Traverse.Create(gridView).Field("Grid").GetValue<object>()).Property("GridWidth").GetValue<IBindable<int>>().Value;
                    GridHeight = Traverse.Create(Traverse.Create(gridView).Field("Grid").GetValue<object>()).Property("GridHeight").GetValue<IBindable<int>>().Value;
                    //GridWidth = gridView.Grid.GridWidth.Value;
                    //GridHeight = gridView.Grid.GridHeight.Value;

                    if (GridWidth == 1 && GridHeight == 1)
                    {
                        position.x = GridSize / 2f;
                        position.y = -GridSize / 2f;
                    }
                    else if (GridWidth == 1)
                    {
                        position.x = GridSize / 2f;
                        position.y = -Mathf.Clamp(gridView.transform.position.y - globalPosition.y, GridSize / 2f, (GridSize * GridHeight) - (GridSize / 2f));
                    }
                    else if (GridHeight == 1)
                    {
                        position.x = Mathf.Clamp(globalPosition.x - gridView.transform.position.x, GridSize / 2f, (GridSize * GridWidth) - (GridSize / 2f));
                        position.y = -GridSize / 2f;
                    }
                    else
                    {
                        position.x = Mathf.Clamp(globalPosition.x - gridView.transform.position.x, GridSize / 2f, (GridSize * GridWidth) - (GridSize / 2f));
                        position.y = -Mathf.Clamp(gridView.transform.position.y - globalPosition.y, GridSize / 2f, (GridSize * GridHeight) - (GridSize / 2f));
                    }

                    dot = Vector2.Dot((globalPosition - ((Vector2)gridView.transform.position + position)).normalized, -direction);
                    distance = Vector2.Distance(globalPosition, (Vector2)gridView.transform.position + position);
                    score = Mathf.Lerp(distance, distance * 0.25f, dot);

                    if (score < bestScore && dot > 0.4f)
                    {
                        bestScore = score;
                        bestGridView = gridView;
                        bestGridViewLocation = new Vector2Int(Mathf.RoundToInt((position.x + (GridSize / 2f)) / GridSize), -Mathf.RoundToInt((position.y - (GridSize / 2f)) / GridSize));
                    }
                }

                if (bestGridView != null)
                {
                    bestContainedGridsView = currentContainedGridsView;
                    ResetAllCurrent();
                    currentGridView = bestGridView;
                    currentContainedGridsView = bestContainedGridsView;
                    gridViewLocation = bestGridViewLocation;

                    globalPosition.x = currentGridView.transform.position.x + (GridSize * gridViewLocation.x) - (GridSize / 2f);
                    globalPosition.y = currentGridView.transform.position.y - (GridSize * gridViewLocation.y) + (GridSize / 2f);
                    FindGridWindow(globalPosition);
                    ControllerUIOnMove(direction, globalPosition);
                    return;
                }
                Vector2 point = globalPosition + ((Vector2)direction * 1000f);
                RectTransform rectTransform = currentContainedGridsView.GetComponent<RectTransform>();
                position.x = currentContainedGridsView.transform.position.x;
                position.y = currentContainedGridsView.transform.position.y - ((rectTransform.sizeDelta.y * ScreenRatio) * (rectTransform.pivot.y - 1f));
                if (FindGridView(new Vector2(position.x + Mathf.Clamp(point.x - position.x, 0, rectTransform.sizeDelta.x * ScreenRatio) + (direction.x * (ModSize / 2)), position.y - Mathf.Clamp(position.y - point.y, 0, rectTransform.sizeDelta.y * ScreenRatio) + (direction.y * (ModSize / 2)))))
                {
                    ControllerUIOnMove(direction, globalPosition);
                    return;
                }
            }

            // Local ItemSpecificationPanel ModSlotView Blind Search
            if (currentModSlotView != null && currentItemSpecificationPanel != null)
            {
                globalPosition.x = currentModSlotView.transform.position.x;
                globalPosition.y = currentModSlotView.transform.position.y;

                foreach (ModSlotView modSlotView in Traverse.Create(currentItemSpecificationPanel).Field("_modsContainer").GetValue<RectTransform>().GetComponentsInChildren<ModSlotView>())
                {
                    if (modSlotView == currentModSlotView) continue;

                    dot = Vector2.Dot((globalPosition - ((Vector2)modSlotView.transform.position)).normalized, -direction);
                    distance = Vector2.Distance(globalPosition, (Vector2)modSlotView.transform.position);
                    score = Mathf.Lerp(distance, distance * 0.25f, dot);

                    if (score < bestScore && dot > 0.4f)
                    {
                        bestScore = score;
                        bestModSlotView = modSlotView;
                    }
                }

                if (bestModSlotView != null)
                {
                    bestItemSpecificationPanel = currentItemSpecificationPanel;
                    ResetAllCurrent();
                    currentModSlotView = bestModSlotView;
                    currentItemSpecificationPanel = bestItemSpecificationPanel;
                    gridViewLocation = new Vector2Int(1, 1);

                    globalPosition.x = currentModSlotView.transform.position.x;
                    globalPosition.y = currentModSlotView.transform.position.y;
                    FindGridWindow(globalPosition);
                    ControllerUIOnMove(direction, globalPosition);
                    return;
                }
                Vector2 point = globalPosition + ((Vector2)direction * 1000f);
                RectTransform rectTransform = currentItemSpecificationPanel.GetComponent<RectTransform>();
                position.x = currentItemSpecificationPanel.transform.position.x - ((rectTransform.sizeDelta.x * ScreenRatio) / 2);
                position.y = currentItemSpecificationPanel.transform.position.y + ((rectTransform.sizeDelta.y * ScreenRatio) / 2);
                if (FindGridView(new Vector2(position.x + Mathf.Clamp(point.x - position.x, 0, rectTransform.sizeDelta.x * ScreenRatio) + (direction.x * (ModSize / 2)), position.y - Mathf.Clamp(position.y - point.y, 0, rectTransform.sizeDelta.y * ScreenRatio) + (direction.y * (ModSize / 2)))))
                {
                    ControllerUIOnMove(direction, globalPosition);
                    return;
                }


            }

            Skip1:

            // GlobalPosition
            UpdateGlobalPosition();
            /*if (currentGridView != null)
            {
                globalPosition.x = currentGridView.transform.position.x + (GridSize * gridViewLocation.x) - (GridSize / 2f);
                globalPosition.y = currentGridView.transform.position.y - (GridSize * gridViewLocation.y) + (GridSize / 2f);
            }
            else if (currentModSlotView != null)
            {
                globalPosition = currentModSlotView.transform.position;
            }
            else if (currentTradingTableGridView != null)
            {
                Vector2 size = currentTradingTableGridView.GetComponent<RectTransform>().sizeDelta * ScreenRatio;
                globalPosition.x = (currentTradingTableGridView.transform.position.x - (size.x / 2f)) + (GridSize * gridViewLocation.x) - (GridSize / 2f);
                globalPosition.y = (currentTradingTableGridView.transform.position.y + (size.y / 2f)) - (GridSize * gridViewLocation.y) + (GridSize / 2f);
            }
            else if (currentEquipmentSlotView != null)
            {
                globalPosition = new Vector2(currentEquipmentSlotView.transform.position.x + (SlotSize / 2f), currentEquipmentSlotView.transform.position.y - (SlotSize / 2f));
            }
            else if (currentWeaponsSlotView != null)
            {
                globalPosition = new Vector2(currentWeaponsSlotView.transform.position.x + (157.0811f * ScreenRatio), currentWeaponsSlotView.transform.position.y - (SlotSize / 2f));
            }
            else if (currentArmbandSlotView != null)
            {
                globalPosition = new Vector2(currentArmbandSlotView.transform.position.x + (SlotSize / 2f), currentArmbandSlotView.transform.position.y - (32f * ScreenRatio));
            }
            else if (currentContainersSlotView != null)
            {
                globalPosition = new Vector2(currentContainersSlotView.transform.position.x + (SlotSize / 2f), currentContainersSlotView.transform.position.y - (SlotSize / 2f));
            }
            else if (currentSearchButton != null)
            {
                Vector2 size = currentSearchButton.GetComponent<RectTransform>().sizeDelta * ScreenRatio;
                globalPosition.x = currentSearchButton.transform.position.x;// - (size.x / 2f);
                globalPosition.y = currentSearchButton.transform.position.y;// - (size.y / 2f);
                if (AmandsControllerSets.ContainsKey("SearchButton") && !ActiveAmandsControllerSets.Contains("SearchButton"))
                {
                    ActiveAmandsControllerSets.Add("SearchButton");
                }
            }*/
            // Global Blind Search

            if (Skip) goto Skip2;

            // GridView Blind Search
            foreach (GridView gridView in gridViews)
            {
                if (gridView == currentGridView) continue;

                GridWidth = Traverse.Create(Traverse.Create(gridView).Field("Grid").GetValue<object>()).Property("GridWidth").GetValue<IBindable<int>>().Value;
                GridHeight = Traverse.Create(Traverse.Create(gridView).Field("Grid").GetValue<object>()).Property("GridHeight").GetValue<IBindable<int>>().Value;
                //GridWidth = gridView.Grid.GridWidth.Value;
                //GridHeight = gridView.Grid.GridHeight.Value;

                if (GridWidth == 1 && GridHeight == 1)
                {
                    position.x = GridSize / 2f;
                    position.y = -GridSize / 2f;
                }
                else if (GridWidth == 1)
                {
                    position.x = GridSize / 2f;
                    position.y = -Mathf.Clamp(gridView.transform.position.y - globalPosition.y, GridSize / 2f, (GridSize * GridHeight) - (GridSize / 2f));
                }
                else if (GridHeight == 1)
                {
                    position.x = Mathf.Clamp(globalPosition.x - gridView.transform.position.x, GridSize / 2f, (GridSize * GridWidth) - (GridSize / 2f));
                    position.y = -GridSize / 2f;
                }
                else
                {
                    position.x = Mathf.Clamp(globalPosition.x - gridView.transform.position.x, GridSize / 2f, (GridSize * GridWidth) - (GridSize / 2f));
                    position.y = -Mathf.Clamp(gridView.transform.position.y - globalPosition.y, GridSize / 2f, (GridSize * GridHeight) - (GridSize / 2f));
                }

                dot = Vector2.Dot((globalPosition - ((Vector2)gridView.transform.position + position)).normalized, -direction);
                distance = Vector2.Distance(globalPosition, (Vector2)gridView.transform.position + position);
                score = Mathf.Lerp(distance, distance * 0.25f, dot);

                if (score < bestScore && dot > 0.4f)
                {
                    bestScore = score;
                    bestGridView = gridView;
                    bestModSlotView = null;
                    bestItemSpecificationPanel = null;
                    bestTradingTableGridView = null;
                    bestContainedGridsView = null;
                    bestEquipmentSlotView = null;
                    bestWeaponsSlotView = null;
                    bestArmbandSlotView = null;
                    bestContainersSlotView = null;
                    bestSearchButton = null;
                    bestGridViewLocation = new Vector2Int(Mathf.RoundToInt((position.x + (GridSize / 2f)) / GridSize), -Mathf.RoundToInt((position.y - (GridSize / 2f)) / GridSize));
                }
            }
            // ContainedGridsView GridView Blind Search
            foreach (ContainedGridsView containedGridsView in containedGridsViews)
            {
                if (containedGridsView == currentContainedGridsView) continue;
                foreach (GridView gridView in containedGridsView.GridViews)
                {
                    if (gridView == currentGridView) continue;

                    GridWidth = Traverse.Create(Traverse.Create(gridView).Field("Grid").GetValue<object>()).Property("GridWidth").GetValue<IBindable<int>>().Value;
                    GridHeight = Traverse.Create(Traverse.Create(gridView).Field("Grid").GetValue<object>()).Property("GridHeight").GetValue<IBindable<int>>().Value;
                    //GridWidth = gridView.Grid.GridWidth.Value;
                    //GridHeight = gridView.Grid.GridHeight.Value;

                    if (GridWidth == 1 && GridHeight == 1)
                    {
                        position.x = GridSize / 2f;
                        position.y = -GridSize / 2f;
                    }
                    else if (GridWidth == 1)
                    {
                        position.x = GridSize / 2f;
                        position.y = -Mathf.Clamp(gridView.transform.position.y - globalPosition.y, GridSize / 2f, (GridSize * GridHeight) - (GridSize / 2f));
                    }
                    else if (GridHeight == 1)
                    {
                        position.x = Mathf.Clamp(globalPosition.x - gridView.transform.position.x, GridSize / 2f, (GridSize * GridWidth) - (GridSize / 2f));
                        position.y = -GridSize / 2f;
                    }
                    else
                    {
                        position.x = Mathf.Clamp(globalPosition.x - gridView.transform.position.x, GridSize / 2f, (GridSize * GridWidth) - (GridSize / 2f));
                        position.y = -Mathf.Clamp(gridView.transform.position.y - globalPosition.y, GridSize / 2f, (GridSize * GridHeight) - (GridSize / 2f));
                    }

                    dot = Vector2.Dot((globalPosition - ((Vector2)gridView.transform.position + position)).normalized, -direction);
                    distance = Vector2.Distance(globalPosition, (Vector2)gridView.transform.position + position);
                    score = Mathf.Lerp(distance, distance * 0.25f, dot);

                    if (score < bestScore && dot > 0.4f)
                    {
                        bestScore = score;
                        bestGridView = gridView;
                        bestModSlotView = null;
                        bestItemSpecificationPanel = null;
                        bestTradingTableGridView = null;
                        bestContainedGridsView = containedGridsView;
                        bestEquipmentSlotView = null;
                        bestWeaponsSlotView = null;
                        bestArmbandSlotView = null;
                        bestContainersSlotView = null;
                        bestSearchButton = null;
                        bestGridViewLocation = new Vector2Int(Mathf.RoundToInt((position.x + (GridSize / 2f)) / GridSize), -Mathf.RoundToInt((position.y - (GridSize / 2f)) / GridSize));
                    }
                }
            }
            // TradingTableGridView Blind Search
            if (tradingTableGridView != null)
            {
                Vector2 size = tradingTableGridView.GetComponent<RectTransform>().sizeDelta * ScreenRatio;
                Vector2 positionTradingTableGridView = new Vector2(tradingTableGridView.transform.position.x - (size.x / 2f), tradingTableGridView.transform.position.y + (size.y / 2f));

                GridWidth = Traverse.Create(Traverse.Create(tradingTableGridView).Field("Grid").GetValue<object>()).Property("GridWidth").GetValue<IBindable<int>>().Value;
                GridHeight = Traverse.Create(Traverse.Create(tradingTableGridView).Field("Grid").GetValue<object>()).Property("GridHeight").GetValue<IBindable<int>>().Value;
                //GridWidth = tradingTableGridView.Grid.GridWidth.Value;
                //GridHeight = tradingTableGridView.Grid.GridHeight.Value;

                position.x = Mathf.Clamp(globalPosition.x - positionTradingTableGridView.x, GridSize / 2f, (GridSize * GridWidth) - (GridSize / 2f));
                position.y = -Mathf.Clamp(positionTradingTableGridView.y - globalPosition.y, GridSize / 2f, (GridSize * GridHeight) - (GridSize / 2f));

                dot = Vector2.Dot((globalPosition - (positionTradingTableGridView + position)).normalized, -direction);
                distance = Vector2.Distance(globalPosition, positionTradingTableGridView + position);
                score = Mathf.Lerp(distance, distance * 0.25f, dot);

                if (score < bestScore && dot > 0.4f)
                {
                    bestScore = score;
                    bestGridView = null;
                    bestModSlotView = null;
                    bestItemSpecificationPanel = null;
                    bestTradingTableGridView = tradingTableGridView;
                    bestContainedGridsView = null;
                    bestEquipmentSlotView = null;
                    bestWeaponsSlotView = null;
                    bestArmbandSlotView = null;
                    bestContainersSlotView = null;
                    bestSearchButton = null;
                    bestGridViewLocation = new Vector2Int(Mathf.RoundToInt((position.x + (GridSize / 2f)) / GridSize), -Mathf.RoundToInt((position.y - (GridSize / 2f)) / GridSize));
                }
            }
            // ItemSpecificationPanel ModSlotView Blind Search
            foreach (ItemSpecificationPanel itemSpecificationPanel in itemSpecificationPanels)
            {
                if (itemSpecificationPanel == currentItemSpecificationPanel) continue;
                foreach (ModSlotView modSlotView in Traverse.Create(itemSpecificationPanel).Field("_modsContainer").GetValue<RectTransform>().GetComponentsInChildren<ModSlotView>())
                {
                    if (modSlotView == currentModSlotView) continue;

                    dot = Vector2.Dot((globalPosition - ((Vector2)modSlotView.transform.position)).normalized, -direction);
                    distance = Vector2.Distance(globalPosition, (Vector2)modSlotView.transform.position);
                    score = Mathf.Lerp(distance, distance * 0.25f, dot);

                    if (score < bestScore && dot > 0.4f)
                    {
                        bestScore = score;
                        bestGridView = null;
                        bestModSlotView = modSlotView;
                        bestItemSpecificationPanel = itemSpecificationPanel;
                        bestTradingTableGridView = null;
                        bestContainedGridsView = null;
                        bestEquipmentSlotView = null;
                        bestWeaponsSlotView = null;
                        bestArmbandSlotView = null;
                        bestContainersSlotView = null;
                        bestSearchButton = null;
                        bestGridViewLocation = new Vector2Int(1, 1);
                    }
                }
            }
            // SearchButton
            foreach (SearchButton searchButton in searchButtons)
            {
                if (searchButton == currentSearchButton) continue;
                Vector2 size = searchButton.GetComponent<RectTransform>().sizeDelta * ScreenRatio;
                position.x = searchButton.transform.position.x;// - (size.x / 2f);
                position.y = searchButton.transform.position.y;// - (size.y / 2f);

                dot = Vector2.Dot((globalPosition - position).normalized, -direction);
                distance = Vector2.Distance(globalPosition, position);
                score = Mathf.Lerp(distance, distance * 0.25f, dot);

                if (score < bestScore && dot > 0.4f)
                {
                    bestScore = score;
                    bestGridView = null;
                    bestModSlotView = null;
                    bestItemSpecificationPanel = null;
                    bestTradingTableGridView = null;
                    bestContainedGridsView = null;
                    bestEquipmentSlotView = null;
                    bestWeaponsSlotView = null;
                    bestArmbandSlotView = null;
                    bestContainersSlotView = null;
                    bestSearchButton = searchButton;
                    bestGridViewLocation = new Vector2Int(1, 1);
                }
            }

            Skip2:

            // EquipmentSlotView
            foreach (SlotView slotView in equipmentSlotViews)
            {
                if (slotView == currentEquipmentSlotView) continue;
                position = new Vector2(slotView.transform.position.x + (SlotSize / 2f), slotView.transform.position.y - (SlotSize / 2f));
                dot = Vector2.Dot((globalPosition - position).normalized, -direction);
                distance = Vector2.Distance(globalPosition, position);
                score = Mathf.Lerp(distance, distance * 0.25f, dot);

                if (score < bestScore && dot > 0.4f)
                {
                    bestScore = score;
                    bestGridView = null;
                    bestModSlotView = null;
                    bestItemSpecificationPanel = null;
                    bestTradingTableGridView = null;
                    bestContainedGridsView = null;
                    bestEquipmentSlotView = slotView;
                    bestWeaponsSlotView = null;
                    bestArmbandSlotView = null;
                    bestContainersSlotView = null;
                    bestSearchButton = null;
                    bestGridViewLocation = new Vector2Int(1, 1);
                }
            }
            // WeaponsSlotView
            foreach (SlotView slotView in weaponsSlotViews)
            {
                if (slotView == currentWeaponsSlotView) continue;
                position = new Vector2(slotView.transform.position.x + (157.0811f * ScreenRatio), slotView.transform.position.y - (SlotSize / 2f));
                dot = Vector2.Dot((globalPosition - position).normalized, -direction);
                distance = Vector2.Distance(globalPosition, position);
                score = Mathf.Lerp(distance, distance * 0.25f, dot);

                if (score < bestScore && dot > 0.4f)
                {
                    bestScore = score;
                    bestGridView = null;
                    bestModSlotView = null;
                    bestItemSpecificationPanel = null;
                    bestTradingTableGridView = null;
                    bestContainedGridsView = null;
                    bestEquipmentSlotView = null;
                    bestWeaponsSlotView = slotView;
                    bestArmbandSlotView = null;
                    bestContainersSlotView = null;
                    bestSearchButton = null;
                    bestGridViewLocation = new Vector2Int(1, 1);
                }
            }
            // ArmbandSlotView
            if (armbandSlotView != null && armbandSlotView != currentArmbandSlotView)
            {
                position = new Vector2(armbandSlotView.transform.position.x + (SlotSize / 2f), armbandSlotView.transform.position.y - (32f * ScreenRatio));
                dot = Vector2.Dot((globalPosition - position).normalized, -direction);
                distance = Vector2.Distance(globalPosition, position);
                score = Mathf.Lerp(distance, distance * 0.25f, dot);

                if (score < bestScore && dot > 0.4f)
                {
                    bestScore = score;
                    bestGridView = null;
                    bestModSlotView = null;
                    bestItemSpecificationPanel = null;
                    bestTradingTableGridView = null;
                    bestContainedGridsView = null;
                    bestEquipmentSlotView = null;
                    bestWeaponsSlotView = null;
                    bestArmbandSlotView = armbandSlotView;
                    bestContainersSlotView = null;
                    bestSearchButton = null;
                    bestGridViewLocation = new Vector2Int(1, 1);
                }
            }
            // ContainersSlotView
            foreach (SlotView slotView in containersSlotViews)
            {
                if (slotView == currentContainersSlotView) continue;
                position = new Vector2(slotView.transform.position.x + (SlotSize / 2f), slotView.transform.position.y - (SlotSize / 2f));
                dot = Vector2.Dot((globalPosition - position).normalized, -direction);
                distance = Vector2.Distance(globalPosition, position);
                score = Mathf.Lerp(distance, distance * 0.25f, dot);

                if (score < bestScore && dot > 0.4f)
                {
                    bestScore = score;
                    bestGridView = null;
                    bestModSlotView = null;
                    bestItemSpecificationPanel = null;
                    bestTradingTableGridView = null;
                    bestContainedGridsView = null;
                    bestEquipmentSlotView = null;
                    bestWeaponsSlotView = null;
                    bestArmbandSlotView = null;
                    bestContainersSlotView = slotView;
                    bestSearchButton = null;
                    bestGridViewLocation = new Vector2Int(1, 1);
                }
            }
            // LootEquipmentSlotView
            foreach (SlotView slotView in lootEquipmentSlotViews)
            {
                if (slotView == currentEquipmentSlotView) continue;
                position = new Vector2(slotView.transform.position.x + (SlotSize / 2f), slotView.transform.position.y - (SlotSize / 2f));
                dot = Vector2.Dot((globalPosition - position).normalized, -direction);
                distance = Vector2.Distance(globalPosition, position);
                score = Mathf.Lerp(distance, distance * 0.25f, dot);

                if (score < bestScore && dot > 0.4f)
                {
                    bestScore = score;
                    bestGridView = null;
                    bestModSlotView = null;
                    bestItemSpecificationPanel = null;
                    bestTradingTableGridView = null;
                    bestContainedGridsView = null;
                    bestEquipmentSlotView = slotView;
                    bestWeaponsSlotView = null;
                    bestArmbandSlotView = null;
                    bestContainersSlotView = null;
                    bestSearchButton = null;
                    bestGridViewLocation = new Vector2Int(1, 1);
                }
            }
            // LootWeaponsSlotView
            foreach (SlotView slotView in lootWeaponsSlotViews)
            {
                if (slotView == currentWeaponsSlotView) continue;
                position = new Vector2(slotView.transform.position.x + (157.0811f * ScreenRatio), slotView.transform.position.y - (SlotSize / 2f));
                dot = Vector2.Dot((globalPosition - position).normalized, -direction);
                distance = Vector2.Distance(globalPosition, position);
                score = Mathf.Lerp(distance, distance * 0.25f, dot);

                if (score < bestScore && dot > 0.4f)
                {
                    bestScore = score;
                    bestGridView = null;
                    bestModSlotView = null;
                    bestItemSpecificationPanel = null;
                    bestTradingTableGridView = null;
                    bestContainedGridsView = null;
                    bestEquipmentSlotView = null;
                    bestWeaponsSlotView = slotView;
                    bestArmbandSlotView = null;
                    bestContainersSlotView = null;
                    bestSearchButton = null;
                    bestGridViewLocation = new Vector2Int(1, 1);
                }
            }
            // LootArmbandSlotView
            if (lootArmbandSlotView != null && lootArmbandSlotView != currentArmbandSlotView)
            {
                position = new Vector2(lootArmbandSlotView.transform.position.x + (SlotSize / 2f), lootArmbandSlotView.transform.position.y - (32f * ScreenRatio));
                dot = Vector2.Dot((globalPosition - position).normalized, -direction);
                distance = Vector2.Distance(globalPosition, position);
                score = Mathf.Lerp(distance, distance * 0.25f, dot);

                if (score < bestScore && dot > 0.4f)
                {
                    bestScore = score;
                    bestGridView = null;
                    bestModSlotView = null;
                    bestItemSpecificationPanel = null;
                    bestTradingTableGridView = null;
                    bestContainedGridsView = null;
                    bestEquipmentSlotView = null;
                    bestWeaponsSlotView = null;
                    bestArmbandSlotView = lootArmbandSlotView;
                    bestContainersSlotView = null;
                    bestSearchButton = null;
                    bestGridViewLocation = new Vector2Int(1, 1);
                }
            }
            // LootContainersSlotView
            foreach (SlotView slotView in lootContainersSlotViews)
            {
                if (slotView == currentContainersSlotView) continue;
                position = new Vector2(slotView.transform.position.x + (SlotSize / 2f), slotView.transform.position.y - (SlotSize / 2f));
                dot = Vector2.Dot((globalPosition - position).normalized, -direction);
                distance = Vector2.Distance(globalPosition, position);
                score = Mathf.Lerp(distance, distance * 0.25f, dot);

                if (score < bestScore && dot > 0.4f)
                {
                    bestScore = score;
                    bestGridView = null;
                    bestModSlotView = null;
                    bestItemSpecificationPanel = null;
                    bestTradingTableGridView = null;
                    bestContainedGridsView = null;
                    bestEquipmentSlotView = null;
                    bestWeaponsSlotView = null;
                    bestArmbandSlotView = null;
                    bestContainersSlotView = slotView;
                    bestSearchButton = null;
                    bestGridViewLocation = new Vector2Int(1, 1);
                }
            }
            // Skip Include SimpleStash
            if (Skip && SimpleStashGridView != null && SimpleStashGridView != currentGridView)
            {
                GridWidth = Traverse.Create(Traverse.Create(SimpleStashGridView).Field("Grid").GetValue<object>()).Property("GridWidth").GetValue<IBindable<int>>().Value;
                GridHeight = Traverse.Create(Traverse.Create(SimpleStashGridView).Field("Grid").GetValue<object>()).Property("GridHeight").GetValue<IBindable<int>>().Value;
                //GridWidth = SimpleStashGridView.Grid.GridWidth.Value;
                //GridHeight = SimpleStashGridView.Grid.GridHeight.Value;

                if (GridWidth == 1 && GridHeight == 1)
                {
                    position.x = GridSize / 2f;
                    position.y = -GridSize / 2f;
                }
                else if (GridWidth == 1)
                {
                    position.x = GridSize / 2f;
                    position.y = -Mathf.Clamp(SimpleStashGridView.transform.position.y - globalPosition.y, GridSize / 2f, (GridSize * GridHeight) - (GridSize / 2f));
                }
                else if (GridHeight == 1)
                {
                    position.x = Mathf.Clamp(globalPosition.x - SimpleStashGridView.transform.position.x, GridSize / 2f, (GridSize * GridWidth) - (GridSize / 2f));
                    position.y = -GridSize / 2f;
                }
                else
                {
                    position.x = Mathf.Clamp(globalPosition.x - SimpleStashGridView.transform.position.x, GridSize / 2f, (GridSize * GridWidth) - (GridSize / 2f));
                    position.y = -Mathf.Clamp(SimpleStashGridView.transform.position.y - globalPosition.y, GridSize / 2f, (GridSize * GridHeight) - (GridSize / 2f));
                }

                dot = Vector2.Dot((globalPosition - ((Vector2)SimpleStashGridView.transform.position + position)).normalized, -direction);
                distance = Vector2.Distance(globalPosition, (Vector2)SimpleStashGridView.transform.position + position);
                score = Mathf.Lerp(distance, distance * 0.25f, dot);

                if (score < bestScore && dot > 0.4f)
                {
                    bestScore = score;
                    bestGridView = SimpleStashGridView;
                    bestModSlotView = null;
                    bestItemSpecificationPanel = null;
                    bestTradingTableGridView = null;
                    bestContainedGridsView = null;
                    bestEquipmentSlotView = null;
                    bestWeaponsSlotView = null;
                    bestArmbandSlotView = null;
                    bestContainersSlotView = null;
                    bestSearchButton = null;
                    bestGridViewLocation = new Vector2Int(Mathf.RoundToInt((position.x + (GridSize / 2f)) / GridSize), -Mathf.RoundToInt((position.y - (GridSize / 2f)) / GridSize));
                }
            }
            // Support

            if (bestGridView == null && bestTradingTableGridView == null && bestEquipmentSlotView == null && bestWeaponsSlotView == null && bestArmbandSlotView == null && bestContainersSlotView == null && bestModSlotView == null && bestSearchButton == null && Skip)
            {
                ControllerUIMove(direction, false);
                return;
            }
            // Set GridView/SlotView
            if (bestGridView == null && bestTradingTableGridView == null && bestEquipmentSlotView == null && bestWeaponsSlotView == null && bestArmbandSlotView == null && bestContainersSlotView == null && bestModSlotView == null && bestSearchButton == null) return;

            currentGridView = bestGridView;
            currentModSlotView = bestModSlotView;
            currentTradingTableGridView = bestTradingTableGridView;
            currentContainedGridsView = bestContainedGridsView;
            currentItemSpecificationPanel = bestItemSpecificationPanel;
            currentEquipmentSlotView = bestEquipmentSlotView;
            currentWeaponsSlotView = bestWeaponsSlotView;
            currentArmbandSlotView = bestArmbandSlotView;
            currentContainersSlotView = bestContainersSlotView;
            currentSearchButton = bestSearchButton;
            gridViewLocation = bestGridViewLocation;
            UpdateGlobalPosition();
            FindGridWindow(globalPosition);
            // OnMove
            ControllerUIOnMove(direction, globalPosition);
        }
        public void UpdateGlobalPosition()
        {
            if (currentGridView != null)
            {
                globalPosition.x = currentGridView.transform.position.x + (GridSize * gridViewLocation.x) - (GridSize / 2f);
                globalPosition.y = currentGridView.transform.position.y - (GridSize * gridViewLocation.y) + (GridSize / 2f);
            }
            else if (currentTradingTableGridView != null)
            {
                Vector2 size = currentTradingTableGridView.GetComponent<RectTransform>().sizeDelta * ScreenRatio;
                globalPosition.x = (currentTradingTableGridView.transform.position.x - (size.x / 2f)) + (GridSize * gridViewLocation.x) - (GridSize / 2f);
                globalPosition.y = (currentTradingTableGridView.transform.position.y + (size.y / 2f)) - (GridSize * gridViewLocation.y) + (GridSize / 2f);
            }
            else if (currentModSlotView != null)
            {
                globalPosition = currentModSlotView.transform.position;
            }
            else if (currentEquipmentSlotView != null)
            {
                globalPosition = new Vector2(currentEquipmentSlotView.transform.position.x + (SlotSize / 2f), currentEquipmentSlotView.transform.position.y - (SlotSize / 2f));
            }
            else if (currentWeaponsSlotView != null)
            {
                globalPosition = new Vector2(currentWeaponsSlotView.transform.position.x + (157.0811f * ScreenRatio), currentWeaponsSlotView.transform.position.y - (SlotSize / 2f));
            }
            else if (currentArmbandSlotView != null)
            {
                globalPosition = new Vector2(currentArmbandSlotView.transform.position.x + (SlotSize / 2f), currentArmbandSlotView.transform.position.y - (32f * ScreenRatio));
            }
            else if (currentContainersSlotView != null)
            {
                globalPosition = new Vector2(currentContainersSlotView.transform.position.x + (SlotSize / 2f), currentContainersSlotView.transform.position.y - (SlotSize / 2f));
            }
            else if (currentSearchButton != null)
            {
                Vector2 size = currentSearchButton.GetComponent<RectTransform>().sizeDelta * ScreenRatio;
                globalPosition.x = currentSearchButton.transform.position.x;// - (size.x / 2f);
                globalPosition.y = currentSearchButton.transform.position.y;// - (size.y / 2f);
                if (AmandsControllerSets.ContainsKey("SearchButton") && !ActiveAmandsControllerSets.Contains("SearchButton"))
                {
                    ActiveAmandsControllerSets.Add("SearchButton");
                }
            }
        }
        public void AmandsControllerUse(bool Hold)
        {
            if (pointerEventData != null)
            {
                pointerEventData.position = globalPosition;
                pointerEventData.dragging = false;
                List<RaycastResult> results = new List<RaycastResult>();
                eventSystem.RaycastAll(pointerEventData, results);
                if (results.Count > 0)
                {
                    itemView = results[0].gameObject.GetComponentInParent<ItemView>();
                    if (itemView == null)
                    {
                        itemView = results[0].gameObject.GetComponentInParent<GridItemView>();
                    }
                    if (itemView == null)
                    {
                        itemView = results[0].gameObject.GetComponentInParent<SlotItemView>();
                    }
                    if (itemView != null)
                    {
                        ItemUiContext ItemUiContext = Traverse.Create(itemView).Field("ItemUiContext").GetValue<ItemUiContext>();
                        object NewContextInteractionsObject = Traverse.Create(itemView).Property("NewContextInteractions").GetValue();
                        if (NewContextInteractionsObject != null)
                        {
                            if (ExecuteInteraction == null)
                            {
                                ExecuteInteraction = NewContextInteractionsObject.GetType().GetMethod("ExecuteInteraction", BindingFlags.Instance | BindingFlags.Public);
                            }
                            if (IsInteractionAvailable == null)
                            {
                                IsInteractionAvailable = NewContextInteractionsObject.GetType().GetMethod("IsInteractionAvailable", BindingFlags.Instance | BindingFlags.Public);
                            }
                        }
                        if (!itemView.IsSearched && ExecuteMiddleClick != null && (bool)ExecuteMiddleClick.Invoke(itemView, null)) return;
                        if (ItemUiContext == null || !itemView.IsSearched) return;
                        TraderControllerClass ItemController = Traverse.Create(itemView).Field("ItemController").GetValue<TraderControllerClass>();
                        if (ExecuteInteraction != null && IsInteractionAvailable != null)
                        {
                            if (itemView.Item is FoodClass || itemView.Item is MedsClass)
                            {
                                ExecuteInteractionInvokeParameters[0] = EItemInfoButton.Use;
                                if (!(bool)ExecuteInteraction.Invoke(NewContextInteractionsObject, ExecuteInteractionInvokeParameters))
                                {
                                    ExecuteInteractionInvokeParameters[0] = EItemInfoButton.UseAll;
                                    ExecuteInteraction.Invoke(NewContextInteractionsObject, ExecuteInteractionInvokeParameters);
                                }
                                return;
                            }
                            if (itemView.Item.IsContainer && !Hold)
                            {
                                ExecuteInteractionInvokeParameters[0] = EItemInfoButton.Open;
                                if ((bool)ExecuteInteraction.Invoke(NewContextInteractionsObject, ExecuteInteractionInvokeParameters)) return;
                            }
                            //ExecuteInteractionInvokeParameters[0] = itemView.Item.IsContainer ? EItemInfoButton.Open : EItemInfoButton.Inspect;
                            //if (itemView.Item.IsContainer && (bool)ExecuteInteraction.Invoke(NewContextInteractionsObject, ExecuteInteractionInvokeParameters)) return;

                            SimpleTooltip tooltip = ItemUiContext.Tooltip;
                            IsInteractionAvailableInvokeParameters[0] = EItemInfoButton.Equip;
                            if (!Hold && (bool)IsInteractionAvailable.Invoke(NewContextInteractionsObject, IsInteractionAvailableInvokeParameters))
                            {
                                ItemUiContext.QuickEquip(itemView.Item).HandleExceptions();
                                if (tooltip != null)
                                {
                                    tooltip.Close();
                                }
                                return;
                            }
                            else
                            {
                                bool IsBeingLoadedMagazine = Traverse.Create(Traverse.Create(itemView).Property("IsBeingLoadedMagazine").GetValue<object>()).Field("gparam_0").GetValue<bool>();
                                bool IsBeingUnloadedMagazine = Traverse.Create(Traverse.Create(itemView).Property("IsBeingUnloadedMagazine").GetValue<object>()).Field("gparam_0").GetValue<bool>();
                                if (IsBeingLoadedMagazine || IsBeingUnloadedMagazine)
                                {
                                    ItemController.StopProcesses();
                                    return;
                                }
                            }
                            ExecuteInteractionInvokeParameters[0] = EItemInfoButton.CheckMagazine;
                            if ((bool)ExecuteInteraction.Invoke(NewContextInteractionsObject, ExecuteInteractionInvokeParameters)) return;
                            if (ExecuteMiddleClick != null && (bool)ExecuteMiddleClick.Invoke(itemView, null)) return;
                        }
                    }
                }
            }
        }
        public void AmandsControllerQuickMove()
        {
            if (pointerEventData != null)
            {
                pointerEventData.position = globalPosition;
                pointerEventData.dragging = false;
                List<RaycastResult> results = new List<RaycastResult>();
                eventSystem.RaycastAll(pointerEventData, results);
                if (results.Count > 0)
                {
                    itemView = results[0].gameObject.GetComponentInParent<ItemView>();
                    if (itemView == null)
                    {
                        itemView = results[0].gameObject.GetComponentInParent<GridItemView>();
                    }
                    if (itemView == null)
                    {
                        itemView = results[0].gameObject.GetComponentInParent<SlotItemView>();
                    }
                    if (itemView != null)
                    {
                        ItemUiContext ItemUiContext = Traverse.Create(itemView).Field("ItemUiContext").GetValue<ItemUiContext>();
                        if (ItemUiContext == null || !itemView.IsSearched) return;
                        TraderControllerClass ItemController = Traverse.Create(itemView).Field("ItemController").GetValue<TraderControllerClass>();
                        SimpleTooltip tooltip = ItemUiContext.Tooltip;
                        //GStruct374 gstruct = ItemUiContext.QuickFindAppropriatePlace(itemView.ItemContext, ItemController, false, true, true);
                        object ItemContext = Traverse.Create(itemView).Property("ItemContext").GetValue<object>();
                        if (ItemContext != null)
                        {
                            object gstructObject = QuickFindAppropriatePlace.Invoke(ItemUiContext, new object[5] { ItemContext, ItemController, false, true, true });
                            if (gstructObject != null)
                            {
                                bool Failed = Traverse.Create(gstructObject).Property("Failed").GetValue<bool>();
                                if (Failed) return;
                                object Value = Traverse.Create(gstructObject).Field("Value").GetValue<object>();
                                if (Value != null)
                                {
                                    if (!(bool)CanExecute.Invoke(ItemController, new object[1] { Value }))
                                    {
                                        return;
                                    }
                                    bool ItemsDestroyRequired = Traverse.Create(Value).Field("ItemsDestroyRequired").GetValue<bool>();
                                    if (ItemsDestroyRequired)
                                    {
                                        NotificationManagerClass.DisplayWarningNotification("DiscardLimit", ENotificationDurationType.Default);
                                        return;
                                    }
                                    string itemSound = itemView.Item.ItemSound;
                                    RunNetworkTransaction.Invoke(ItemController, new object[2] { Value, null });
                                    if (tooltip != null)
                                    {
                                        tooltip.Close();
                                    }
                                    Singleton<GUISounds>.Instance.PlayItemSound(itemSound, EInventorySoundType.pickup, false);
                                }
                            }
                        }
                    }
                }
            }
        }
        public void AmandsControllerDiscard()
        {
            if (pointerEventData != null)
            {
                pointerEventData.position = globalPosition;
                pointerEventData.dragging = false;
                List<RaycastResult> results = new List<RaycastResult>();
                eventSystem.RaycastAll(pointerEventData, results);
                if (results.Count > 0)
                {
                    itemView = results[0].gameObject.GetComponentInParent<ItemView>();
                    if (itemView == null)
                    {
                        itemView = results[0].gameObject.GetComponentInParent<GridItemView>();
                    }
                    if (itemView == null)
                    {
                        itemView = results[0].gameObject.GetComponentInParent<SlotItemView>();
                    }
                    if (itemView != null)
                    {
                        ItemUiContext ItemUiContext = Traverse.Create(itemView).Field("ItemUiContext").GetValue<ItemUiContext>();
                        if (ItemUiContext == null || !itemView.IsSearched) return;
                        object NewContextInteractionsObject = Traverse.Create(itemView).Property("NewContextInteractions").GetValue();
                        if (NewContextInteractionsObject != null)
                        {
                            if (IsInteractionAvailable == null)
                            {
                                IsInteractionAvailable = NewContextInteractionsObject.GetType().GetMethod("IsInteractionAvailable", BindingFlags.Instance | BindingFlags.Public);
                            }
                        }
                        if (IsInteractionAvailable != null)
                        {
                            SimpleTooltip tooltip = ItemUiContext.Tooltip;
                            IsInteractionAvailableInvokeParameters[0] = EItemInfoButton.Discard;
                            if ((bool)IsInteractionAvailable.Invoke(NewContextInteractionsObject, IsInteractionAvailableInvokeParameters))
                            {
                                ItemUiContext.ThrowItem(itemView.Item).HandleExceptions();
                                if (tooltip != null)
                                {
                                    tooltip.Close();
                                }
                                return;
                            }
                        }
                    }
                }
            }
        }
        public void AmandsControllerBeginDrag()
        {
            if (!Dragging && pointerEventData != null)
            {
                pointerEventData.position = globalPosition;
                pointerEventData.dragging = false;
                List<RaycastResult> results = new List<RaycastResult>();
                eventSystem.RaycastAll(pointerEventData, results);
                if (results.Count > 0)
                {
                    itemView = results[0].gameObject.GetComponentInParent<ItemView>();
                    if (itemView == null)
                    {
                        itemView = results[0].gameObject.GetComponentInParent<GridItemView>();
                    }
                    if (itemView == null)
                    {
                        itemView = results[0].gameObject.GetComponentInParent<SlotItemView>();
                    }
                    if (itemView != null)
                    {
                        itemView.OnBeginDrag(pointerEventData);
                        Dragging = true;
                        pointerEventData.dragging = true;
                        if (AmandsControllerSets.ContainsKey("OnDrag") && !ActiveAmandsControllerSets.Contains("OnDrag"))
                        {
                            ActiveAmandsControllerSets.Add("OnDrag");
                        }
                        AmandsOnDrag();
                    }
                }
            }
        }
        public void AmandsOnDrag()
        {
            if (pointerEventData != null)
            {
                pointerEventData.position = globalPosition;
                if (itemView != null && itemView.BeingDragged)
                {
                    List<RaycastResult> results = new List<RaycastResult>();
                    eventSystem.RaycastAll(pointerEventData, results);
                    if (results.Count > 0)
                    {
                        pointerEventData.pointerEnter = results[0].gameObject;
                        itemView.OnDrag(pointerEventData);
                    }
                    else
                    {
                        pointerEventData.pointerEnter = null;
                        itemView.OnDrag(pointerEventData);
                    }
                }
                else if (itemView != null && !itemView.BeingDragged)
                {
                    AmandsControllerCancelDrag();
                }
            }
        }
        public void AmandsControllerEndDrag()
        {
            if (Dragging && pointerEventData != null)
            {
                List<RaycastResult> results = new List<RaycastResult>();
                eventSystem.RaycastAll(pointerEventData, results);
                if (results.Count > 0)
                {
                    Dragging = false;
                    pointerEventData.pointerEnter = results[0].gameObject;
                    pointerEventData.dragging = false;
                    if (itemView != null)
                    {
                        itemView.OnEndDrag(pointerEventData);
                    }
                    ActiveAmandsControllerSets.Remove("OnDrag");
                }
                else
                {
                    AmandsControllerCancelDrag();
                }
            }
        }
        public void AmandsControllerCancelDrag()
        {
            if (Dragging && pointerEventData != null)
            {
                Dragging = false;
                pointerEventData.pointerEnter = null;
                pointerEventData.dragging = false;
                if (itemView != null)
                {
                    itemView.OnEndDrag(pointerEventData);
                }
                ActiveAmandsControllerSets.Remove("OnDrag");
            }
        }
        public void gridslotDebug()
        {
            Vector2 position;

            int GridWidth = 1;
            int GridHeight = 1;

            gridViewsDebug.Clear();
            foreach (GridView gridView in gridViews)
            {
                GridWidth = Traverse.Create(Traverse.Create(gridView).Field("Grid").GetValue<object>()).Property("GridWidth").GetValue<IBindable<int>>().Value;
                GridHeight = Traverse.Create(Traverse.Create(gridView).Field("Grid").GetValue<object>()).Property("GridHeight").GetValue<IBindable<int>>().Value;
                //GridWidth = gridView.Grid.GridWidth.Value;
                //GridHeight = gridView.Grid.GridHeight.Value;

                if (GridWidth == 1 && GridHeight == 1)
                {
                    position.x = GridSize / 2f;
                    position.y = -GridSize / 2f;
                }
                else if (GridWidth == 1)
                {
                    position.x = GridSize / 2f;
                    position.y = -Mathf.Clamp(gridView.transform.position.y - globalPosition.y, GridSize / 2f, (GridSize * GridHeight) - (GridSize / 2f));
                }
                else if (GridHeight == 1)
                {
                    position.x = Mathf.Clamp(globalPosition.x - gridView.transform.position.x, GridSize / 2f, (GridSize * GridWidth) - (GridSize / 2f));
                    position.y = -GridSize / 2f;
                }
                else
                {
                    position.x = Mathf.Clamp(globalPosition.x - gridView.transform.position.x, GridSize / 2f, (GridSize * GridWidth) - (GridSize / 2f));
                    position.y = -Mathf.Clamp(gridView.transform.position.y - globalPosition.y, GridSize / 2f, (GridSize * GridHeight) - (GridSize / 2f));
                }
                gridViewsDebug.Add((Vector2)gridView.transform.position + position);
            }

            foreach (ContainedGridsView containedGridsView in containedGridsViews)
            {
                foreach (GridView gridView in containedGridsView.GridViews)
                {
                    GridWidth = Traverse.Create(Traverse.Create(gridView).Field("Grid").GetValue<object>()).Property("GridWidth").GetValue<IBindable<int>>().Value;
                    GridHeight = Traverse.Create(Traverse.Create(gridView).Field("Grid").GetValue<object>()).Property("GridHeight").GetValue<IBindable<int>>().Value;
                    //GridWidth = gridView.Grid.GridWidth.Value;
                    //GridHeight = gridView.Grid.GridHeight.Value;

                    if (GridWidth == 1 && GridHeight == 1)
                    {
                        position.x = GridSize / 2f;
                        position.y = -GridSize / 2f;
                    }
                    else if (GridWidth == 1)
                    {
                        position.x = GridSize / 2f;
                        position.y = -Mathf.Clamp(gridView.transform.position.y - globalPosition.y, GridSize / 2f, (GridSize * GridHeight) - (GridSize / 2f));
                    }
                    else if (GridHeight == 1)
                    {
                        position.x = Mathf.Clamp(globalPosition.x - gridView.transform.position.x, GridSize / 2f, (GridSize * GridWidth) - (GridSize / 2f));
                        position.y = -GridSize / 2f;
                    }
                    else
                    {
                        position.x = Mathf.Clamp(globalPosition.x - gridView.transform.position.x, GridSize / 2f, (GridSize * GridWidth) - (GridSize / 2f));
                        position.y = -Mathf.Clamp(gridView.transform.position.y - globalPosition.y, GridSize / 2f, (GridSize * GridHeight) - (GridSize / 2f));
                    }
                    gridViewsDebug.Add((Vector2)gridView.transform.position + position);
                }
            }

            /*slotViewsDebug.Clear();
            foreach (SlotView slotView in slotViews)
            {
                position.x = SlotSize / 2f;
                position.y = -SlotSize / 2f;

                slotViewsDebug.Add((Vector2)slotView.transform.position + position);
            }*/
        }
        public void DebugStuff(string stuff)
        {
            ConsoleScreen.Log(stuff);
        }
    }
    public struct AmandsControllerButtonBind
    {
        public List<ECommand> Commands;
        public EAmandsControllerCommand AmandsControllerCommand;
        public EAmandsControllerPressType PressType;
        public int Priority;
        public string AmandsControllerSet;

        public AmandsControllerButtonBind(List<ECommand> Commands, EAmandsControllerCommand AmandsControllerCommand, EAmandsControllerPressType PressType, int Priority, string AmandsControllerSet)
        {
            this.Commands = Commands;
            this.AmandsControllerCommand = AmandsControllerCommand;
            this.PressType = PressType;
            this.Priority = Priority;
            this.AmandsControllerSet = AmandsControllerSet;
        }
    }
    public struct AmandsControllerButtonSnapshot
    {
        public bool Pressed;
        public float Time;
        public AmandsControllerButtonBind PressBind;
        public AmandsControllerButtonBind ReleaseBind;
        public AmandsControllerButtonBind HoldBind;
        public AmandsControllerButtonBind DoubleClickBind;

        public AmandsControllerButtonSnapshot(bool Pressed, float Time, AmandsControllerButtonBind PressBind, AmandsControllerButtonBind ReleaseBind, AmandsControllerButtonBind HoldBind, AmandsControllerButtonBind DoubleClickBind)
        {
            this.Pressed = Pressed;
            this.Time = Time;
            this.PressBind = PressBind;
            this.ReleaseBind = ReleaseBind;
            this.HoldBind = HoldBind;
            this.DoubleClickBind = DoubleClickBind;
        }
    }

    public enum EAmandsControllerButton
    {
        A = 0,
        B = 1,
        X = 2,
        Y = 3,
        LeftShoulder = 4,
        RightShoulder = 5,
        LeftTrigger = 6,
        RightTrigger = 7,
        LeftThumb = 8,
        RightThumb = 9,
        UP = 10,
        DOWN = 11,
        LEFT = 12,
        RIGHT = 13,
        BACK = 14,
        MENU = 15,
    }
    public enum EAmandsControllerPressType
    {
        Press = 0,
        Release = 1,
        Hold = 2,
        DoubleClick = 3
    }
    public enum EAmandsControllerCommand
    {
        Empty = 0,
        ToggleSet = 1,
        EnableSet = 2,
        DisableSet = 3,
        InputTree = 4,
        QuickSelectWeapon = 5,
        SlowLeanLeft = 6,
        SlowLeanRight = 7,
        EndSlowLean = 8,
        RestoreLean = 9,
        InterfaceUp = 10,
        InterfaceDown = 11,
        InterfaceLeft = 12,
        InterfaceRight = 13,
        InterfaceDisableAutoMove = 14,
        BeginDrag = 15,
        EndDrag = 16,
        Search = 17,
        Use = 18,
        UseHold = 19,
        QuickMove = 20,
        Discard = 21
    }
}
