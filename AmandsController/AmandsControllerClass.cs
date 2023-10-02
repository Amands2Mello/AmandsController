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
using static EFT.Player;
using EFT.UI.DragAndDrop;
using UnityEngine.EventSystems;
using Comfort.Common;
using SharpDX;
using EFT.Communications;
using EFT.InventoryLogic;
using Cutscene;
using Diz.Binding;
using System.Linq;
using EFT.Hideout;

namespace AmandsController
{
    public class AmandsControllerClass : MonoBehaviour
    {
        public InputTree inputTree;
        public LocalPlayer localPlayer;

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
        public Vector2 LS, RS, Aim = new Vector2(0, 0);
        public float LTSensitivity, RTSensitivity, LSXYSqrt, RSXYSqrt;
        public bool resetCharacterMovementSpeed = false;
        public AnimationCurve AimAnimationCurve = new AnimationCurve();
        public Keyframe[] AimKeys = new Keyframe[3] { new Keyframe(0f,0f), new Keyframe(0.75f,0.5f, 0.75f, 0.5f), new Keyframe(1f, 1f), };
        public bool SlowLeanLeft;
        public bool SlowLeanRight;

        bool LSINPUT = false;
        bool LSUP = false;
        bool LSDOWN = false;
        bool LSLEFT = false;
        bool LSRIGHT = false;

        bool RSINPUT = false;
        bool RSUP = false;
        bool RSDOWN = false;
        bool RSLEFT = false;
        bool RSRIGHT = false;

        bool A = false;
        bool B = false;
        bool X = false;
        bool Y = false;

        bool LB = false;
        bool RB = false;
        bool LB_RB = false;
        bool Interface_LB_RB = false;

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

        AmandsControllerButtonBind EmptyBind = new AmandsControllerButtonBind(new AmandsControllerCommand(EAmandsControllerCommand.None), EAmandsControllerPressType.Press, -100);
        Dictionary<string,Dictionary<EAmandsControllerButton, List<AmandsControllerButtonBind>>> AmandsControllerSets = new Dictionary<string, Dictionary<EAmandsControllerButton, List<AmandsControllerButtonBind>>>();
        List<string> ActiveAmandsControllerSets = new List<string>();

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
        public SlotView dogtagSlotView;
        public List<SlotView> specialSlotSlotViews = new List<SlotView>();

        public List<SearchButton> searchButtons = new List<SearchButton>();

        public List<SimpleContextMenuButton> simpleContextMenuButtons = new List<SimpleContextMenuButton>();

        public SplitDialog splitDialog;


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
        public SlotView currentDogtagSlotView;
        public SlotView currentSpecialSlotSlotView;
        public SearchButton currentSearchButton;
        public SimpleContextMenuButton currentSimpleContextMenuButton;
        public ScrollRectNoDrag currentScrollRectNoDrag;
        public RectTransform currentScrollRectNoDragRectTransform;

        public GridView snapshotGridView;
        public ModSlotView snapshotModSlotView;
        public TradingTableGridView snapshotTradingTableGridView;
        public ContainedGridsView snapshotContainedGridsView;
        public ItemSpecificationPanel snapshotItemSpecificationPanel;
        public SlotView snapshotEquipmentSlotView;
        public SlotView snapshotWeaponsSlotView;
        public SlotView snapshotArmbandSlotView;
        public SlotView snapshotContainersSlotView;
        public SlotView snapshotDogtagSlotView;
        public SlotView snapshotSpecialSlotSlotView;
        public SearchButton snapshotSearchButton;
        public SimpleContextMenuButton snapshotSimpleContextMenuButton;


        public Vector2 globalPosition = Vector2.zero;
        public Vector2 tglobalPosition = Vector2.zero;
        public Vector2Int gridViewLocation = Vector2Int.one;
        public Vector2Int SnapshotGridViewLocation = Vector2Int.one;
        public Vector2Int lastDirection = Vector2Int.zero;
        public Vector2 debugglobalPosition;

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

        //public ItemView itemView = null;
        public ItemView DraggingItemView = null;
        public PointerEventData pointerEventData = null;
        public EventSystem eventSystem = null;
        public bool Dragging = false;

        public bool Interface = false;
        public bool AutoMove = false;
        public bool SplitDialogAutoMove = false;
        public float AutoMoveTime = 0f;
        public float AutoMoveTimeDelay = 0.2f;
        public float InterfaceStickMoveTime = 0f;
        public float InterfaceStickMoveTimeDelay = 0.3f;
        public float InterfaceSkipStickMoveTime = 0f;
        public float InterfaceSkipStickMoveTimeDelay = 0.3f;

        public bool ContextMenu = false;

        public MethodInfo ExecuteInteraction;
        private object[] ExecuteInteractionInvokeParameters = new object[1] { EItemInfoButton.Inspect };
        public MethodInfo IsInteractionAvailable;
        private object[] IsInteractionAvailableInvokeParameters = new object[1] { EItemInfoButton.Inspect };
        public MethodInfo ExecuteMiddleClick;

        public MethodInfo QuickFindAppropriatePlace;
        public MethodInfo CanExecute;
        public MethodInfo RunNetworkTransaction;

        public MethodInfo ItemUIContextMethod_0;
        private object[] ItemUIContextMethod_0InvokeParameters = new object[2] { typeof(Item), EBoundItem.Item4 };

        private ItemView onPointerEnterItemView;
        private SimpleContextMenuButton onPointerEnterSimpleContextMenuButton;

        public MethodInfo ShowContextMenu;
        public object[] ShowContextMenuInvokeParameters = new object[1] { Vector2.zero };

        public int lastIntSliderValue;

        public bool InRaidOnly = true;

        public MethodInfo CalculateRotatedSize;

        public bool LSButtons = false;
        public bool RSButtons = false;
        public EAmandsControllerUseStick InterfaceStick = EAmandsControllerUseStick.None;
        public EAmandsControllerUseStick InterfaceSkipStick = EAmandsControllerUseStick.RS;
        public EAmandsControllerUseStick ScrollStick = EAmandsControllerUseStick.LS;
        public EAmandsControllerUseStick WindowStick = EAmandsControllerUseStick.LS;

        public bool QuickSkipStick = false;

        public MethodInfo DraggedItemViewMethod_2;

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
            if (Interface && currentSimpleContextMenuButton == null)
            {
                GUI.Box(new Rect(new Vector2(globalPosition.x - (GridSize / 2), (Screen.height) - globalPosition.y - (GridSize / 2)), new Vector2(GridSize, GridSize)), gUIContent);
                //GUI.Box(new Rect(new Vector2(debugglobalPosition.x, (Screen.height) - debugglobalPosition.y), new Vector2(GridSize, GridSize)), gUIContent);
            }
            /*RectTransform rectTransform;
            foreach (SlotView specialSlotSlotView in specialSlotSlotViews)
            {
                rectTransform = specialSlotSlotView.GetComponent<RectTransform>();
                GUI.Box(new Rect(new Vector2(rectTransform.position.x, (Screen.height) - rectTransform.position.y), new Vector2(GridSize, GridSize)), gUIContent);
            }*/
            /*RectTransform rectTransform;
            foreach (ScrollRectNoDrag scrollRectNoDrag in scrollRectNoDrags)
            {
                rectTransform = scrollRectNoDrag.GetComponent<RectTransform>();
                rectTransform = scrollRectNoDrag.content;
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
                    DebugStuff("count " + modSlotViews.Count());
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
            ItemUIContextMethod_0 = typeof(ItemUiContext).GetMethod("method_0", BindingFlags.Instance | BindingFlags.NonPublic);
            TranslateInput = typeof(InputTree).GetMethod("TranslateInput", BindingFlags.Instance | BindingFlags.NonPublic);
            Press = typeof(Button).GetMethod("Press", BindingFlags.Instance | BindingFlags.NonPublic);
            ExecuteMiddleClick = typeof(ItemView).GetMethod("ExecuteMiddleClick", BindingFlags.Instance | BindingFlags.NonPublic);
            QuickFindAppropriatePlace = typeof(ItemUiContext).GetMethod("QuickFindAppropriatePlace", BindingFlags.Instance | BindingFlags.Public);
            CanExecute = typeof(TraderControllerClass).GetMethod("CanExecute", BindingFlags.Instance | BindingFlags.Public);
            RunNetworkTransaction = typeof(TraderControllerClass).GetMethod("RunNetworkTransaction", BindingFlags.Instance | BindingFlags.Public);
            ShowContextMenu = typeof(ItemView).GetMethod("ShowContextMenu", BindingFlags.Instance | BindingFlags.NonPublic);
            CalculateRotatedSize = typeof(Item).GetMethod("CalculateRotatedSize", BindingFlags.Instance | BindingFlags.Public);
            DraggedItemViewMethod_2 = typeof(DraggedItemView).GetMethod("method_2", BindingFlags.Instance | BindingFlags.NonPublic);

            AimAnimationCurve.keys = AimKeys;
        }
        public void Update()
        {
            if (Input.GetKeyDown(KeyCode.PageUp))
            {
                InRaidOnly = false;
                UpdateController(null);
                UpdateInterfaceBinds(true);
            }
            if (Input.GetKeyDown(KeyCode.PageDown))
            {
                InRaidOnly = true;
                connected = false;
            }
            if (!connected) return;

            if (localPlayer == null && InRaidOnly) return;

            gamepad = controller.GetState().Gamepad;

            if (LTSensitivity > 0.25)
            {
                if (!LT)
                {
                    LT = true;
                    AmandsControllerGeneratePressType(EAmandsControllerButton.LT, true);
                }
            }
            else
            {
                if (LT)
                {
                    LT = false;
                    AmandsControllerGeneratePressType(EAmandsControllerButton.LT, false);
                }
            }
            if (RTSensitivity > 0.25)
            {
                if (!RT)
                {
                    RT = true;
                    AmandsControllerGeneratePressType(EAmandsControllerButton.RT, true);
                }
            }
            else
            {
                if (RT)
                {
                    RT = false;
                    AmandsControllerGeneratePressType(EAmandsControllerButton.RT, false);
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
                    AmandsControllerGeneratePressType(EAmandsControllerButton.LB, true);
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
                    AmandsControllerGeneratePressType(EAmandsControllerButton.LB, false);
                    ActiveAmandsControllerSets.Remove("LB");
                }
            }
            if (gamepad.Buttons.HasFlag(GamepadButtonFlags.RightShoulder))
            {
                if (!RB)
                {
                    RB = true;
                    AmandsControllerGeneratePressType(EAmandsControllerButton.RB, true);
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
                    AmandsControllerGeneratePressType(EAmandsControllerButton.RB, false);
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
            if (gamepad.Buttons.HasFlag(GamepadButtonFlags.LeftShoulder | GamepadButtonFlags.RightShoulder) && Interface)
            {
                if (!Interface_LB_RB)
                {
                    Interface_LB_RB = true;
                    if (AmandsControllerSets.ContainsKey("Interface_LB_RB") && !ActiveAmandsControllerSets.Contains("Interface_LB_RB"))
                    {
                        ActiveAmandsControllerSets.Add("Interface_LB_RB");
                    }
                }
            }
            else
            {
                if (Interface_LB_RB)
                {
                    Interface_LB_RB = false;
                    ActiveAmandsControllerSets.Remove("Interface_LB_RB");
                }
            }
            if (gamepad.Buttons.HasFlag(GamepadButtonFlags.LeftThumb))
            {
                if (!L)
                {
                    L = true;
                    AmandsControllerGeneratePressType(EAmandsControllerButton.LS, true);
                }
            }
            else
            {
                if (L)
                {
                    L = false;
                    AmandsControllerGeneratePressType(EAmandsControllerButton.LS, false);
                }
            }
            if (gamepad.Buttons.HasFlag(GamepadButtonFlags.RightThumb))
            {
                if (!R)
                {
                    R = true;
                    AmandsControllerGeneratePressType(EAmandsControllerButton.RS, true);
                }
            }
            else
            {
                if (R)
                {
                    R = false;
                    AmandsControllerGeneratePressType(EAmandsControllerButton.RS, false);
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

            LTSensitivity = (float)gamepad.LeftTrigger / 255f;
            RTSensitivity = (float)gamepad.RightTrigger / 255f;

            LS.x = (float)gamepad.LeftThumbX / maxValue;
            LS.y = (float)gamepad.LeftThumbY / maxValue;
            LSXYSqrt = Mathf.Sqrt(Mathf.Pow(LS.x, 2) + Mathf.Pow(LS.y, 2));

            RS.x = (float)gamepad.RightThumbX / maxValue;
            RS.y = (float)gamepad.RightThumbY / maxValue;
            RSXYSqrt = Mathf.Sqrt(Mathf.Pow(RS.x, 2) + Mathf.Pow(RS.y, 2));

            // LSButtons
            if (LSXYSqrt > 0.25)
            {
                if (!LSINPUT)
                {
                    LSINPUT = true;
                }
            }
            else
            {
                if (LSINPUT)
                {
                    LSINPUT = false;
                }
            }
            if (LS.y > 0.25)
            {
                if (!LSUP)
                {
                    LSUP = true;
                    AmandsControllerGeneratePressType(EAmandsControllerButton.LSUP, true);
                    if (!LSButtons && InterfaceStick == EAmandsControllerUseStick.LS)
                    {
                        ControllerUIMove(new Vector2Int(0, 1), false);
                        InterfaceStickMoveTime = 0f;
                        InterfaceStickMoveTimeDelay = 0.15f;
                    }
                    if (QuickSkipStick && !LSButtons && InterfaceSkipStick == EAmandsControllerUseStick.LS)
                    {
                        ControllerUIMove(new Vector2Int(0, 1), true);
                        InterfaceSkipStickMoveTime = 0f;
                        InterfaceSkipStickMoveTimeDelay = 0.15f;
                    }
                }
            }
            else
            {
                if (LSUP)
                {
                    LSUP = false;
                    AmandsControllerGeneratePressType(EAmandsControllerButton.LSUP, false);
                }
            }
            if (LS.y < -0.25)
            {
                if (!LSDOWN)
                {
                    LSDOWN = true;
                    AmandsControllerGeneratePressType(EAmandsControllerButton.LSDOWN, true);
                    if (!LSButtons && InterfaceStick == EAmandsControllerUseStick.LS)
                    {
                        ControllerUIMove(new Vector2Int(0, -1), false);
                        InterfaceStickMoveTime = 0f;
                        InterfaceStickMoveTimeDelay = 0.15f;
                    }
                    if (QuickSkipStick && !LSButtons && InterfaceSkipStick == EAmandsControllerUseStick.LS)
                    {
                        ControllerUIMove(new Vector2Int(0, -1), true);
                        InterfaceSkipStickMoveTime = 0f;
                        InterfaceSkipStickMoveTimeDelay = 0.15f;
                    }
                }
            }
            else
            {
                if (LSDOWN)
                {
                    LSDOWN = false;
                    AmandsControllerGeneratePressType(EAmandsControllerButton.LSDOWN, false);
                }
            }
            if (LS.x > 0.25)
            {
                if (!LSRIGHT)
                {
                    LSRIGHT = true;
                    AmandsControllerGeneratePressType(EAmandsControllerButton.LSRIGHT, true);
                    if (!LSButtons && InterfaceStick == EAmandsControllerUseStick.LS)
                    {
                        ControllerUIMove(new Vector2Int(1, 0), false);
                        InterfaceStickMoveTime = 0f;
                        InterfaceStickMoveTimeDelay = 0.15f;
                    }
                    if (QuickSkipStick && !LSButtons && InterfaceSkipStick == EAmandsControllerUseStick.LS)
                    {
                        ControllerUIMove(new Vector2Int(1, 0), true);
                        InterfaceSkipStickMoveTime = 0f;
                        InterfaceSkipStickMoveTimeDelay = 0.15f;
                    }
                }
            }
            else
            {
                if (LSRIGHT)
                {
                    LSRIGHT = false;
                    AmandsControllerGeneratePressType(EAmandsControllerButton.LSRIGHT, false);
                }
            }
            if (LS.x < -0.25)
            {
                if (!LSLEFT)
                {
                    LSLEFT = true;
                    AmandsControllerGeneratePressType(EAmandsControllerButton.LSLEFT, true);
                    if (!LSButtons && InterfaceStick == EAmandsControllerUseStick.LS)
                    {
                        ControllerUIMove(new Vector2Int(-1, 0), false);
                        InterfaceStickMoveTime = 0f;
                        InterfaceStickMoveTimeDelay = 0.15f;
                    }
                    if (QuickSkipStick && !LSButtons && InterfaceSkipStick == EAmandsControllerUseStick.LS)
                    {
                        ControllerUIMove(new Vector2Int(-1, 0), true);
                        InterfaceSkipStickMoveTime = 0f;
                        InterfaceSkipStickMoveTimeDelay = 0.15f;
                    }
                }
            }
            else
            {
                if (LSLEFT)
                {
                    LSLEFT = false;
                    AmandsControllerGeneratePressType(EAmandsControllerButton.LSLEFT, false);
                }
            }
            // RSButtons
            if (RSXYSqrt > 0.25)
            {
                if (!RSINPUT)
                {
                    RSINPUT = true;
                }
            }
            else
            {
                if (RSINPUT)
                {
                    RSINPUT = false;
                }
            }
            if (RS.y > 0.25)
            {
                if (!RSUP)
                {
                    RSUP = true;
                    AmandsControllerGeneratePressType(EAmandsControllerButton.RSUP, true);
                    if (!RSButtons && InterfaceStick == EAmandsControllerUseStick.RS)
                    {
                        ControllerUIMove(new Vector2Int(0, 1), false);
                        InterfaceStickMoveTime = 0f;
                        InterfaceStickMoveTimeDelay = 0.15f;
                    }
                    if (QuickSkipStick && !RSButtons && InterfaceSkipStick == EAmandsControllerUseStick.RS)
                    {
                        ControllerUIMove(new Vector2Int(0, 1), true);
                        InterfaceSkipStickMoveTime = 0f;
                        InterfaceSkipStickMoveTimeDelay = 0.15f;
                    }
                }
            }
            else
            {
                if (RSUP)
                {
                    RSUP = false;
                    AmandsControllerGeneratePressType(EAmandsControllerButton.RSUP, false);
                }
            }
            if (RS.y < -0.25)
            {
                if (!RSDOWN)
                {
                    RSDOWN = true;
                    AmandsControllerGeneratePressType(EAmandsControllerButton.RSDOWN, true);
                    if (!RSButtons && InterfaceStick == EAmandsControllerUseStick.RS)
                    {
                        ControllerUIMove(new Vector2Int(0, -1), false);
                        InterfaceStickMoveTime = 0f;
                        InterfaceStickMoveTimeDelay = 0.15f;
                    }
                    if (QuickSkipStick && !RSButtons && InterfaceSkipStick == EAmandsControllerUseStick.RS)
                    {
                        ControllerUIMove(new Vector2Int(0, -1), true);
                        InterfaceSkipStickMoveTime = 0f;
                        InterfaceSkipStickMoveTimeDelay = 0.15f;
                    }
                }
            }
            else
            {
                if (RSDOWN)
                {
                    RSDOWN = false;
                    AmandsControllerGeneratePressType(EAmandsControllerButton.RSDOWN, false);
                }
            }
            if (RS.x > 0.25)
            {
                if (!RSRIGHT)
                {
                    RSRIGHT = true;
                    AmandsControllerGeneratePressType(EAmandsControllerButton.RSRIGHT, true);
                    if (!RSButtons && InterfaceStick == EAmandsControllerUseStick.RS)
                    {
                        ControllerUIMove(new Vector2Int(1, 0), false);
                        InterfaceStickMoveTime = 0f;
                        InterfaceStickMoveTimeDelay = 0.15f;
                    }
                    if (QuickSkipStick && !RSButtons && InterfaceSkipStick == EAmandsControllerUseStick.RS)
                    {
                        ControllerUIMove(new Vector2Int(1, 0), true);
                        InterfaceSkipStickMoveTime = 0f;
                        InterfaceSkipStickMoveTimeDelay = 0.15f;
                    }
                }
            }
            else
            {
                if (RSRIGHT)
                {
                    RSRIGHT = false;
                    AmandsControllerGeneratePressType(EAmandsControllerButton.RSRIGHT, false);
                }
            }
            if (RS.x < -0.25)
            {
                if (!RSLEFT)
                {
                    RSLEFT = true;
                    AmandsControllerGeneratePressType(EAmandsControllerButton.RSLEFT, true);
                    if (!RSButtons && InterfaceStick == EAmandsControllerUseStick.RS)
                    {
                        ControllerUIMove(new Vector2Int(-1, 0), false);
                        InterfaceStickMoveTime = 0f;
                        InterfaceStickMoveTimeDelay = 0.15f;
                    }
                    if (QuickSkipStick && !RSButtons && InterfaceSkipStick == EAmandsControllerUseStick.RS)
                    {
                        ControllerUIMove(new Vector2Int(-1, 0), true);
                        InterfaceSkipStickMoveTime = 0f;
                        InterfaceSkipStickMoveTimeDelay = 0.15f;
                    }
                }
            }
            else
            {
                if (RSLEFT)
                {
                    RSLEFT = false;
                    AmandsControllerGeneratePressType(EAmandsControllerButton.RSLEFT, false);
                }
            }

            // Interface
            if (Interface)
            {
                // Auto Move
                if (AutoMove || SplitDialogAutoMove)
                {
                    AutoMoveTime += Time.deltaTime;
                    if (AutoMoveTime > AutoMoveTimeDelay)
                    {
                        AutoMoveTime = 0f;
                        AutoMoveTimeDelay = 0.1f;
                        if (SplitDialogAutoMove)
                        {
                            AmandsControllerSplitDialogAdd(lastIntSliderValue);
                        }
                        else if (AutoMove)
                        {
                            ControllerUIMove(lastDirection, false);
                        }
                    }
                }
                else
                {
                    AutoMoveTimeDelay = 0.2f;
                }

                // Window Move
                bool WindowLS = false;
                bool WindowRS = false;
                switch (WindowStick)
                {
                    case EAmandsControllerUseStick.LS:
                        if (!LSButtons && currentContainedGridsView != null && LSXYSqrt > 0.3f)
                        {
                            currentContainedGridsView.transform.parent.position += new Vector3(LS.x, LS.y, 0f) * 1000f * Time.deltaTime;
                            UpdateGlobalPosition();
                            WindowLS = true;
                        }
                        break;
                    case EAmandsControllerUseStick.RS:
                        if (!RSButtons && currentContainedGridsView != null && RSXYSqrt > 0.3f)
                        {
                            currentContainedGridsView.transform.parent.position += new Vector3(RS.x, RS.y, 0f) * 1000f * Time.deltaTime;
                            UpdateGlobalPosition();
                            WindowRS = true;
                        }
                        break;
                }

                // Stick Move
                switch (InterfaceStick)
                {
                    case EAmandsControllerUseStick.LS:
                        if (LSButtons || WindowLS) break;
                        InterfaceStickMoveTime += Time.deltaTime;
                        if (InterfaceStickMoveTime > InterfaceStickMoveTimeDelay && (Mathf.Abs(LS.x) > 0.3f || Mathf.Abs(LS.y) > 0.3f))
                        {
                            InterfaceStickMoveTime = 0f;
                            InterfaceStickMoveTimeDelay = 0.15f;
                            ControllerUIMove(new Vector2Int(LS.x > 0.3f ? 1 : LS.x < -0.3f ? -1 : 0, LS.y > 0.3f ? 1 : LS.y < -0.3f ? -1 : 0), false);
                        }
                        else if (!(Mathf.Abs(LS.x) > 0.3f || Mathf.Abs(LS.y) > 0.3f))
                        {
                            InterfaceStickMoveTime = 1f;
                            InterfaceStickMoveTimeDelay = 0.3f;
                        }
                        break;
                    case EAmandsControllerUseStick.RS:
                        if (RSButtons || WindowRS) break;
                        InterfaceStickMoveTime += Time.deltaTime;
                        if (InterfaceStickMoveTime > InterfaceStickMoveTimeDelay && (Mathf.Abs(RS.x) > 0.3f || Mathf.Abs(RS.y) > 0.3f))
                        {
                            InterfaceStickMoveTime = 0f;
                            InterfaceStickMoveTimeDelay = 0.15f;
                            ControllerUIMove(new Vector2Int(RS.x > 0.3f ? 1 : RS.x < -0.3f ? -1 : 0, RS.y > 0.3f ? 1 : RS.y < -0.3f ? -1 : 0), false);
                        }
                        else if (!(Mathf.Abs(RS.x) > 0.3f || Mathf.Abs(RS.y) > 0.3f))
                        {
                            InterfaceStickMoveTime = 1f;
                            InterfaceStickMoveTimeDelay = 0.3f;
                        }
                        break;
                }

                // Stick Skip Move
                switch (InterfaceSkipStick)
                {
                    case EAmandsControllerUseStick.LS:
                        if (LSButtons || WindowLS) break;
                        InterfaceSkipStickMoveTime += Time.deltaTime;
                        if (InterfaceSkipStickMoveTime > InterfaceSkipStickMoveTimeDelay && (Mathf.Abs(LS.x) > 0.3f || Mathf.Abs(LS.y) > 0.3f))
                        {
                            InterfaceSkipStickMoveTime = 0f;
                            InterfaceSkipStickMoveTimeDelay = 0.15f;
                            ControllerUIMove(new Vector2Int(LS.x > 0.3f ? 1 : LS.x < -0.3f ? -1 : 0, LS.y > 0.3f ? 1 : LS.y < -0.3f ? -1 : 0), true);
                        }
                        else if (!(Mathf.Abs(LS.x) > 0.3f || Mathf.Abs(LS.y) > 0.3f))
                        {
                            InterfaceSkipStickMoveTime = 1f;
                            InterfaceSkipStickMoveTimeDelay = 0.3f;
                        }
                        break;
                    case EAmandsControllerUseStick.RS:
                        if (RSButtons || WindowRS) break;
                        InterfaceSkipStickMoveTime += Time.deltaTime;
                        if (InterfaceSkipStickMoveTime > InterfaceSkipStickMoveTimeDelay && (Mathf.Abs(RS.x) > 0.3f || Mathf.Abs(RS.y) > 0.3f))
                        {
                            InterfaceSkipStickMoveTime = 0f;
                            InterfaceSkipStickMoveTimeDelay = 0.15f;
                            ControllerUIMove(new Vector2Int(RS.x > 0.3f ? 1 : RS.x < -0.3f ? -1 : 0, RS.y > 0.3f ? 1 : RS.y < -0.3f ? -1 : 0), true);
                        }
                        else if (!(Mathf.Abs(RS.x) > 0.3f || Mathf.Abs(RS.y) > 0.3f))
                        {
                            InterfaceSkipStickMoveTime = 1f;
                            InterfaceSkipStickMoveTimeDelay = 0.3f;
                        }
                        break;
                }

                // Scroll
                if (currentScrollRectNoDrag != null && currentScrollRectNoDragRectTransform != null && !ContextMenu)
                {
                    switch (ScrollStick)
                    {
                        case EAmandsControllerUseStick.None:
                            AmandsControllerAutoScroll();
                            break;
                        case EAmandsControllerUseStick.LS:
                            if (Mathf.Abs(LS.y) > 0.3f && !LSButtons && !WindowLS)
                            {
                                AmandsControllerScroll(LS.y);
                            }
                            else
                            {
                                AmandsControllerAutoScroll();
                            }
                            break;
                        case EAmandsControllerUseStick.RS:
                            if (Mathf.Abs(RS.y) > 0.3f && !RSButtons && !WindowRS)
                            {
                                AmandsControllerScroll(RS.y);
                            }
                            else
                            {
                                AmandsControllerAutoScroll();
                            }
                            break;
                    }
                }
            }

            if (localPlayer == null || Interface) return;

            // Aiming
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

            // Movement
            if (!LSButtons && LSXYSqrt > AmandsControllerPlugin.LDeadzone.Value)
            {
                localPlayer.Move(LS.normalized);
                CharacterMovementSpeed = 0f;
                if (MovementContextObject != null)
                {
                    StateSpeedLimit = Traverse.Create(MovementContextObject).Property("StateSpeedLimit").GetValue<float>();
                    MaxSpeed = Traverse.Create(MovementContextObject).Property("MaxSpeed").GetValue<float>();
                    CharacterMovementSpeed = Mathf.Lerp(-AmandsControllerPlugin.LDeadzone.Value - AmandsControllerPlugin.DeadzoneBuffer.Value, 1f, LSXYSqrt) * Mathf.Min(StateSpeedLimit, MaxSpeed);
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

            // Look
            Magnetism = false;
            Stickiness = 0;
            AutoAim = Vector2.zero;
            if (firearmController == null)
            {
                firearmController = localPlayer.HandsController as FirearmController;
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
                        AutoAim = Vector2.Lerp(Vector2.Lerp(Vector2.zero, Vector2.Lerp(new Vector2(Mathf.Clamp(AimAssistTarget2DPoint.x * 10f, -0.5f, 0.5f), Mathf.Clamp(AimAssistTarget2DPoint.y * -5f, -0.5f, 0.5f)) * 100f * Time.deltaTime, Vector2.zero, (Mathf.Clamp(AimAssistAngle / AmandsControllerPlugin.AutoAimRadius.Value, 0.5f, 1f) - 0.5f) / (1f - 0.5f)) * AmandsControllerPlugin.AutoAim.Value, 1f) / firearmController.AimingSensitivity * (firearmController.IsAiming ? 2f : 1f), Vector2.zero, RSXYSqrt);
                    }
                }
            }
            StickinessSmooth += ((Stickiness - StickinessSmooth) * AmandsControllerPlugin.StickinessSmooth.Value) * Time.deltaTime;
            AutoAimSmooth += ((AutoAim - AutoAimSmooth) * AmandsControllerPlugin.AutoAimSmooth.Value) * Time.deltaTime;
            if (!RSButtons && RSXYSqrt > AmandsControllerPlugin.RDeadzone.Value || Mathf.Sqrt(Mathf.Pow(AutoAimSmooth.x, 2) + Mathf.Pow(AutoAimSmooth.y, 2)) > AmandsControllerPlugin.RDeadzone.Value)
            {
                Aim.x = RS.x * AimAnimationCurve.Evaluate(RSXYSqrt);
                Aim.y = RS.y * AimAnimationCurve.Evaluate(RSXYSqrt);
                localPlayer.Rotate(((Aim * AmandsControllerPlugin.Sensitivity.Value * 100f * Time.deltaTime) * Mathf.Lerp(1f, AmandsControllerPlugin.Stickiness.Value, StickinessSmooth)) + AutoAimSmooth, false);
            }

            // Lean
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
            AmandsControllerSets["LB"].Add(EAmandsControllerButton.UP, new List<AmandsControllerButtonBind> { new AmandsControllerButtonBind(new AmandsControllerCommand(ECommand.ThrowGrenade), EAmandsControllerPressType.Press, 2) });
            AmandsControllerSets["LB"].Add(EAmandsControllerButton.DOWN, new List<AmandsControllerButtonBind> { new AmandsControllerButtonBind(new AmandsControllerCommand(ECommand.SelectSecondaryWeapon), EAmandsControllerPressType.Press, 2) });
            AmandsControllerSets["LB"][EAmandsControllerButton.DOWN].Add(new AmandsControllerButtonBind(new AmandsControllerCommand(ECommand.SelectSecondaryWeapon), EAmandsControllerPressType.DoubleClick, 2));
            AmandsControllerSets["LB"].Add(EAmandsControllerButton.LEFT, new List<AmandsControllerButtonBind> { new AmandsControllerButtonBind(new AmandsControllerCommand(ECommand.SelectSecondPrimaryWeapon), EAmandsControllerPressType.Press, 2) });
            AmandsControllerSets["LB"].Add(EAmandsControllerButton.RIGHT, new List<AmandsControllerButtonBind> { new AmandsControllerButtonBind(new AmandsControllerCommand(ECommand.SelectFirstPrimaryWeapon), EAmandsControllerPressType.Press, 2) });

            /*AmandsControllerSets["LB"].Add(EAmandsControllerButton.A, new List<AmandsControllerButtonBind> { new AmandsControllerButtonBind(new AmandsControllerCommand(ECommand.PressSlot4), EAmandsControllerPressType.Press, 2) });
            AmandsControllerSets["LB"].Add(EAmandsControllerButton.B, new List<AmandsControllerButtonBind> { new AmandsControllerButtonBind(new AmandsControllerCommand(ECommand.PressSlot5), EAmandsControllerPressType.Press, 2) });
            AmandsControllerSets["LB"].Add(EAmandsControllerButton.X, new List<AmandsControllerButtonBind> { new AmandsControllerButtonBind(new AmandsControllerCommand(ECommand.PressSlot6), EAmandsControllerPressType.Press, 2) });
            AmandsControllerSets["LB"].Add(EAmandsControllerButton.Y, new List<AmandsControllerButtonBind> { new AmandsControllerButtonBind(new AmandsControllerCommand(ECommand.PressSlot7), EAmandsControllerPressType.Press, 2) });*/

            AmandsControllerSets["LB"].Add(EAmandsControllerButton.A, new List<AmandsControllerButtonBind> { new AmandsControllerButtonBind(new List<AmandsControllerCommand> { new AmandsControllerCommand(ECommand.PressSlot4), new AmandsControllerCommand(EAmandsControllerCommand.EnableSet, "HealingLimbSelector") }, EAmandsControllerPressType.Press, 2) });
            AmandsControllerSets["LB"][EAmandsControllerButton.A].Add(new AmandsControllerButtonBind(new List<AmandsControllerCommand> { new AmandsControllerCommand(ECommand.SelectFastSlot4), new AmandsControllerCommand(EAmandsControllerCommand.DisableSet, "HealingLimbSelector") }, EAmandsControllerPressType.Release, 2));
            AmandsControllerSets["LB"].Add(EAmandsControllerButton.B, new List<AmandsControllerButtonBind> { new AmandsControllerButtonBind(new List<AmandsControllerCommand> { new AmandsControllerCommand(ECommand.PressSlot5), new AmandsControllerCommand(EAmandsControllerCommand.EnableSet, "HealingLimbSelector") }, EAmandsControllerPressType.Press, 2) });
            AmandsControllerSets["LB"][EAmandsControllerButton.B].Add(new AmandsControllerButtonBind(new List<AmandsControllerCommand> { new AmandsControllerCommand(ECommand.SelectFastSlot5), new AmandsControllerCommand(EAmandsControllerCommand.DisableSet, "HealingLimbSelector") }, EAmandsControllerPressType.Release, 2));
            AmandsControllerSets["LB"].Add(EAmandsControllerButton.X, new List<AmandsControllerButtonBind> { new AmandsControllerButtonBind(new List<AmandsControllerCommand> { new AmandsControllerCommand(ECommand.PressSlot6), new AmandsControllerCommand(EAmandsControllerCommand.EnableSet, "HealingLimbSelector") }, EAmandsControllerPressType.Press, 2) });
            AmandsControllerSets["LB"][EAmandsControllerButton.X].Add(new AmandsControllerButtonBind(new List<AmandsControllerCommand> { new AmandsControllerCommand(ECommand.SelectFastSlot6), new AmandsControllerCommand(EAmandsControllerCommand.DisableSet, "HealingLimbSelector") }, EAmandsControllerPressType.Release, 2));
            AmandsControllerSets["LB"].Add(EAmandsControllerButton.Y, new List<AmandsControllerButtonBind> { new AmandsControllerButtonBind(new List<AmandsControllerCommand> { new AmandsControllerCommand(ECommand.PressSlot7), new AmandsControllerCommand(EAmandsControllerCommand.EnableSet, "HealingLimbSelector") }, EAmandsControllerPressType.Press, 2) });
            AmandsControllerSets["LB"][EAmandsControllerButton.Y].Add(new AmandsControllerButtonBind(new List<AmandsControllerCommand> { new AmandsControllerCommand(ECommand.SelectFastSlot7), new AmandsControllerCommand(EAmandsControllerCommand.DisableSet, "HealingLimbSelector") }, EAmandsControllerPressType.Release, 2));

            AmandsControllerSets["LB"].Add(EAmandsControllerButton.LS, new List<AmandsControllerButtonBind> { new AmandsControllerButtonBind(new AmandsControllerCommand(ECommand.DropBackpack), EAmandsControllerPressType.Press, 2) });
            AmandsControllerSets["LB"].Add(EAmandsControllerButton.BACK, new List<AmandsControllerButtonBind> { new AmandsControllerButtonBind(new AmandsControllerCommand(ECommand.ToggleGoggles), EAmandsControllerPressType.Press, 2) });

            AmandsControllerSets.Add("RB", new Dictionary<EAmandsControllerButton, List<AmandsControllerButtonBind>>());
            AmandsControllerSets["RB"].Add(EAmandsControllerButton.X, new List<AmandsControllerButtonBind> { new AmandsControllerButtonBind(new AmandsControllerCommand(ECommand.ChamberUnload), EAmandsControllerPressType.Press, 1) });
            AmandsControllerSets["RB"][EAmandsControllerButton.X].Add(new AmandsControllerButtonBind(new AmandsControllerCommand(ECommand.CheckChamber), EAmandsControllerPressType.Hold, 1));
            AmandsControllerSets["RB"][EAmandsControllerButton.X].Add(new AmandsControllerButtonBind(new AmandsControllerCommand(ECommand.UnloadMagazine), EAmandsControllerPressType.DoubleClick, 1));

            AmandsControllerSets["RB"].Add(EAmandsControllerButton.B, new List<AmandsControllerButtonBind> { new AmandsControllerButtonBind(new AmandsControllerCommand(EAmandsControllerCommand.EnableSet, "Movement"), EAmandsControllerPressType.Press, 100) });
            AmandsControllerSets["RB"][EAmandsControllerButton.B].Add(new AmandsControllerButtonBind(new AmandsControllerCommand(EAmandsControllerCommand.DisableSet, "Movement"), EAmandsControllerPressType.Release, 100));

            AmandsControllerSets["RB"].Add(EAmandsControllerButton.Y, new List<AmandsControllerButtonBind> { new AmandsControllerButtonBind(new AmandsControllerCommand(ECommand.FoldStock), EAmandsControllerPressType.Press, 1) });
            AmandsControllerSets["RB"][EAmandsControllerButton.Y].Add(new AmandsControllerButtonBind(new AmandsControllerCommand(ECommand.CheckChamber), EAmandsControllerPressType.Hold, 1));

            AmandsControllerSets["RB"].Add(EAmandsControllerButton.LEFT, new List<AmandsControllerButtonBind> { new AmandsControllerButtonBind(new AmandsControllerCommand(ECommand.ToggleLeanLeft), EAmandsControllerPressType.Press, 1) });
            AmandsControllerSets["RB"].Add(EAmandsControllerButton.RIGHT, new List<AmandsControllerButtonBind> { new AmandsControllerButtonBind(new AmandsControllerCommand(ECommand.ToggleLeanRight), EAmandsControllerPressType.Press, 1) });

            AmandsControllerSets["RB"].Add(EAmandsControllerButton.BACK, new List<AmandsControllerButtonBind> { new AmandsControllerButtonBind(new AmandsControllerCommand(ECommand.SwitchHeadLight), EAmandsControllerPressType.Press, 1) });
            AmandsControllerSets["RB"][EAmandsControllerButton.BACK].Add(new AmandsControllerButtonBind(new AmandsControllerCommand(ECommand.ToggleHeadLight), EAmandsControllerPressType.Hold, 1));

            AmandsControllerSets.Add("LB_RB", new Dictionary<EAmandsControllerButton, List<AmandsControllerButtonBind>>());
            AmandsControllerSets["LB_RB"].Add(EAmandsControllerButton.UP, new List<AmandsControllerButtonBind> { new AmandsControllerButtonBind(new AmandsControllerCommand(ECommand.ToggleBlindAbove), EAmandsControllerPressType.Press, 3) });
            AmandsControllerSets["LB_RB"][EAmandsControllerButton.UP].Add(new AmandsControllerButtonBind(new AmandsControllerCommand(ECommand.BlindShootEnd), EAmandsControllerPressType.Release, 3));
            AmandsControllerSets["LB_RB"].Add(EAmandsControllerButton.DOWN, new List<AmandsControllerButtonBind> { new AmandsControllerButtonBind(new AmandsControllerCommand(ECommand.ToggleBlindRight), EAmandsControllerPressType.Press, 3) });
            AmandsControllerSets["LB_RB"][EAmandsControllerButton.DOWN].Add(new AmandsControllerButtonBind(new AmandsControllerCommand(ECommand.BlindShootEnd), EAmandsControllerPressType.Release, 3));
            AmandsControllerSets["LB_RB"].Add(EAmandsControllerButton.LEFT, new List<AmandsControllerButtonBind> { new AmandsControllerButtonBind(new AmandsControllerCommand(ECommand.ToggleStepLeft), EAmandsControllerPressType.Press, 3) });
            AmandsControllerSets["LB_RB"][EAmandsControllerButton.LEFT].Add(new AmandsControllerButtonBind(new AmandsControllerCommand(ECommand.ReturnFromLeftStep), EAmandsControllerPressType.Release, 3));
            AmandsControllerSets["LB_RB"].Add(EAmandsControllerButton.RIGHT, new List<AmandsControllerButtonBind> { new AmandsControllerButtonBind(new AmandsControllerCommand(ECommand.ToggleStepRight), EAmandsControllerPressType.Press, 3) });
            AmandsControllerSets["LB_RB"][EAmandsControllerButton.RIGHT].Add(new AmandsControllerButtonBind(new AmandsControllerCommand(ECommand.ReturnFromRightStep), EAmandsControllerPressType.Release, 3));

            /*AmandsControllerSets["LB_RB"].Add(EAmandsControllerButton.A, new List<AmandsControllerButtonBind> { new AmandsControllerButtonBind(new AmandsControllerCommand(ECommand.PressSlot8), EAmandsControllerPressType.Press, 2) });
            AmandsControllerSets["LB_RB"].Add(EAmandsControllerButton.B, new List<AmandsControllerButtonBind> { new AmandsControllerButtonBind(new AmandsControllerCommand(ECommand.PressSlot9), EAmandsControllerPressType.Press, 2) });
            AmandsControllerSets["LB_RB"].Add(EAmandsControllerButton.X, new List<AmandsControllerButtonBind> { new AmandsControllerButtonBind(new AmandsControllerCommand(ECommand.PressSlot0), EAmandsControllerPressType.Press, 2) });*/

            AmandsControllerSets["LB_RB"].Add(EAmandsControllerButton.A, new List<AmandsControllerButtonBind> { new AmandsControllerButtonBind(new List<AmandsControllerCommand> { new AmandsControllerCommand(ECommand.PressSlot8), new AmandsControllerCommand(EAmandsControllerCommand.EnableSet, "HealingLimbSelector") }, EAmandsControllerPressType.Press, 3) });
            AmandsControllerSets["LB_RB"][EAmandsControllerButton.A].Add(new AmandsControllerButtonBind(new List<AmandsControllerCommand> { new AmandsControllerCommand(ECommand.SelectFastSlot8), new AmandsControllerCommand(EAmandsControllerCommand.DisableSet, "HealingLimbSelector") }, EAmandsControllerPressType.Release, 3));
            AmandsControllerSets["LB_RB"].Add(EAmandsControllerButton.B, new List<AmandsControllerButtonBind> { new AmandsControllerButtonBind(new List<AmandsControllerCommand> { new AmandsControllerCommand(ECommand.PressSlot9), new AmandsControllerCommand(EAmandsControllerCommand.EnableSet, "HealingLimbSelector") }, EAmandsControllerPressType.Press, 3) });
            AmandsControllerSets["LB_RB"][EAmandsControllerButton.B].Add(new AmandsControllerButtonBind(new List<AmandsControllerCommand> { new AmandsControllerCommand(ECommand.SelectFastSlot9), new AmandsControllerCommand(EAmandsControllerCommand.DisableSet, "HealingLimbSelector") }, EAmandsControllerPressType.Release, 3));
            AmandsControllerSets["LB_RB"].Add(EAmandsControllerButton.X, new List<AmandsControllerButtonBind> { new AmandsControllerButtonBind(new List<AmandsControllerCommand> { new AmandsControllerCommand(ECommand.PressSlot0), new AmandsControllerCommand(EAmandsControllerCommand.EnableSet, "HealingLimbSelector") }, EAmandsControllerPressType.Press, 3) });
            AmandsControllerSets["LB_RB"][EAmandsControllerButton.X].Add(new AmandsControllerButtonBind(new List<AmandsControllerCommand> { new AmandsControllerCommand(ECommand.SelectFastSlot0), new AmandsControllerCommand(EAmandsControllerCommand.DisableSet, "HealingLimbSelector") }, EAmandsControllerPressType.Release, 3));
            AmandsControllerSets["LB_RB"].Add(EAmandsControllerButton.Y, new List<AmandsControllerButtonBind> { new AmandsControllerButtonBind(new AmandsControllerCommand(ECommand.DisplayTimer), EAmandsControllerPressType.Press, 3) });
            AmandsControllerSets["LB_RB"][EAmandsControllerButton.Y].Add(new AmandsControllerButtonBind(new AmandsControllerCommand(ECommand.DisplayTimerAndExits), EAmandsControllerPressType.DoubleClick, 3));

            AmandsControllerSets.Add("ActionPanel", new Dictionary<EAmandsControllerButton, List<AmandsControllerButtonBind>>());
            AmandsControllerSets["ActionPanel"].Add(EAmandsControllerButton.X, new List<AmandsControllerButtonBind> { new AmandsControllerButtonBind(new AmandsControllerCommand(ECommand.BeginInteracting), EAmandsControllerPressType.Press, 10) });
            AmandsControllerSets["ActionPanel"][EAmandsControllerButton.X].Add(new AmandsControllerButtonBind(new AmandsControllerCommand(ECommand.EndInteracting), EAmandsControllerPressType.Release, 10));
            AmandsControllerSets["ActionPanel"].Add(EAmandsControllerButton.UP, new List<AmandsControllerButtonBind> { new AmandsControllerButtonBind(new AmandsControllerCommand(ECommand.ScrollPrevious), EAmandsControllerPressType.Press, 10) });
            AmandsControllerSets["ActionPanel"].Add(EAmandsControllerButton.DOWN, new List<AmandsControllerButtonBind> { new AmandsControllerButtonBind(new AmandsControllerCommand(ECommand.ScrollNext), EAmandsControllerPressType.Press, 10) });

            AmandsControllerSets.Add("HealingLimbSelector", new Dictionary<EAmandsControllerButton, List<AmandsControllerButtonBind>>());
            AmandsControllerSets["HealingLimbSelector"].Add(EAmandsControllerButton.UP, new List<AmandsControllerButtonBind> { new AmandsControllerButtonBind(new AmandsControllerCommand(ECommand.ScrollNext), EAmandsControllerPressType.Press, 11) });
            AmandsControllerSets["HealingLimbSelector"].Add(EAmandsControllerButton.DOWN, new List<AmandsControllerButtonBind> { new AmandsControllerButtonBind(new AmandsControllerCommand(ECommand.ScrollPrevious), EAmandsControllerPressType.Press, 11) });

            AmandsControllerSets.Add("Movement", new Dictionary<EAmandsControllerButton, List<AmandsControllerButtonBind>>());
            AmandsControllerSets["Movement"].Add(EAmandsControllerButton.LSLEFT, new List<AmandsControllerButtonBind> { new AmandsControllerButtonBind(new AmandsControllerCommand(EAmandsControllerCommand.SlowLeanLeft), EAmandsControllerPressType.Press, 3) });
            AmandsControllerSets["Movement"][EAmandsControllerButton.LSLEFT].Add(new AmandsControllerButtonBind(new AmandsControllerCommand(EAmandsControllerCommand.EndSlowLean), EAmandsControllerPressType.Release, 3));
            AmandsControllerSets["Movement"].Add(EAmandsControllerButton.LSRIGHT, new List<AmandsControllerButtonBind> { new AmandsControllerButtonBind(new AmandsControllerCommand(EAmandsControllerCommand.SlowLeanRight), EAmandsControllerPressType.Press, 3) });
            AmandsControllerSets["Movement"][EAmandsControllerButton.LSRIGHT].Add(new AmandsControllerButtonBind(new AmandsControllerCommand(EAmandsControllerCommand.EndSlowLean), EAmandsControllerPressType.Release, 3));
            /*AmandsControllerSets["Movement"].Add(EAmandsControllerButton.UP, new List<AmandsControllerButtonBind> { new AmandsControllerButtonBind(new List<ECommand> { ECommand.NextWalkPose }, EAmandsControllerCommand.InputTree, EAmandsControllerPressType.Press, 3, "") });
            AmandsControllerSets["Movement"].Add(EAmandsControllerButton.DOWN, new List<AmandsControllerButtonBind> { new AmandsControllerButtonBind(new List<ECommand> { ECommand.PreviousWalkPose }, EAmandsControllerCommand.InputTree, EAmandsControllerPressType.Press, 3, "") });
            AmandsControllerSets["Movement"].Add(EAmandsControllerButton.LEFT, new List<AmandsControllerButtonBind> { new AmandsControllerButtonBind(new List<ECommand>(), EAmandsControllerCommand.SlowLeanLeft, EAmandsControllerPressType.Press, 3, "") });
            AmandsControllerSets["Movement"][EAmandsControllerButton.LEFT].Add(new AmandsControllerButtonBind(new List<ECommand> { ECommand.None }, EAmandsControllerCommand.EndSlowLean, EAmandsControllerPressType.Release, 3, ""));
            AmandsControllerSets["Movement"].Add(EAmandsControllerButton.RIGHT, new List<AmandsControllerButtonBind> { new AmandsControllerButtonBind(new List<ECommand>(), EAmandsControllerCommand.SlowLeanRight, EAmandsControllerPressType.Press, 3, "") });
            AmandsControllerSets["Movement"][EAmandsControllerButton.RIGHT].Add(new AmandsControllerButtonBind(new List<ECommand> { ECommand.None }, EAmandsControllerCommand.EndSlowLean, EAmandsControllerPressType.Release, 3, ""));
            AmandsControllerSets["Movement"].Add(EAmandsControllerButton.LeftThumb, new List<AmandsControllerButtonBind> { new AmandsControllerButtonBind(new List<ECommand>(), EAmandsControllerCommand.RestoreLean, EAmandsControllerPressType.Press, 3, "") });*/

            AmandsControllerSets.Add("Aiming", new Dictionary<EAmandsControllerButton, List<AmandsControllerButtonBind>>());
            AmandsControllerSets["Aiming"].Add(EAmandsControllerButton.RS, new List<AmandsControllerButtonBind> { new AmandsControllerButtonBind(new AmandsControllerCommand(ECommand.ToggleBreathing), EAmandsControllerPressType.Press, 4) });
            AmandsControllerSets["Aiming"].Add(EAmandsControllerButton.RB, new List<AmandsControllerButtonBind> { new AmandsControllerButtonBind(new AmandsControllerCommand(EAmandsControllerCommand.EnableSet, "Aiming_RB"), EAmandsControllerPressType.Press, 4) });
            AmandsControllerSets["Aiming"][EAmandsControllerButton.RB].Add(new AmandsControllerButtonBind(new AmandsControllerCommand(EAmandsControllerCommand.DisableSet, "Aiming_RB"), EAmandsControllerPressType.Release, 4));

            AmandsControllerSets.Add("Aiming_RB", new Dictionary<EAmandsControllerButton, List<AmandsControllerButtonBind>>());
            AmandsControllerSets["Aiming_RB"].Add(EAmandsControllerButton.UP, new List<AmandsControllerButtonBind> { new AmandsControllerButtonBind(new AmandsControllerCommand(ECommand.OpticCalibrationSwitchUp), EAmandsControllerPressType.Press, 4) });
            AmandsControllerSets["Aiming_RB"].Add(EAmandsControllerButton.DOWN, new List<AmandsControllerButtonBind> { new AmandsControllerButtonBind(new AmandsControllerCommand(ECommand.OpticCalibrationSwitchDown), EAmandsControllerPressType.Press, 4) });

            AmandsControllerSets.Add("Interface", new Dictionary<EAmandsControllerButton, List<AmandsControllerButtonBind>>());
            AmandsControllerSets["Interface"].Add(EAmandsControllerButton.UP, new List<AmandsControllerButtonBind> { new AmandsControllerButtonBind(new AmandsControllerCommand(EAmandsControllerCommand.InterfaceUp), EAmandsControllerPressType.Press, 20) });
            AmandsControllerSets["Interface"][EAmandsControllerButton.UP].Add(new AmandsControllerButtonBind(new AmandsControllerCommand(EAmandsControllerCommand.InterfaceDisableAutoMove), EAmandsControllerPressType.Release, 20));
            AmandsControllerSets["Interface"].Add(EAmandsControllerButton.DOWN, new List<AmandsControllerButtonBind> { new AmandsControllerButtonBind(new AmandsControllerCommand(EAmandsControllerCommand.InterfaceDown), EAmandsControllerPressType.Press, 20) });
            AmandsControllerSets["Interface"][EAmandsControllerButton.DOWN].Add(new AmandsControllerButtonBind(new AmandsControllerCommand(EAmandsControllerCommand.InterfaceDisableAutoMove), EAmandsControllerPressType.Release, 20));
            AmandsControllerSets["Interface"].Add(EAmandsControllerButton.LEFT, new List<AmandsControllerButtonBind> { new AmandsControllerButtonBind(new AmandsControllerCommand(EAmandsControllerCommand.InterfaceLeft), EAmandsControllerPressType.Press, 20) });
            AmandsControllerSets["Interface"][EAmandsControllerButton.LEFT].Add(new AmandsControllerButtonBind(new AmandsControllerCommand(EAmandsControllerCommand.InterfaceDisableAutoMove), EAmandsControllerPressType.Release, 20));
            AmandsControllerSets["Interface"].Add(EAmandsControllerButton.RIGHT, new List<AmandsControllerButtonBind> { new AmandsControllerButtonBind(new AmandsControllerCommand(EAmandsControllerCommand.InterfaceRight), EAmandsControllerPressType.Press, 20) });
            AmandsControllerSets["Interface"][EAmandsControllerButton.RIGHT].Add(new AmandsControllerButtonBind(new AmandsControllerCommand(EAmandsControllerCommand.InterfaceDisableAutoMove), EAmandsControllerPressType.Release, 20));

            AmandsControllerSets["Interface"].Add(EAmandsControllerButton.A, new List<AmandsControllerButtonBind> { new AmandsControllerButtonBind(new AmandsControllerCommand(EAmandsControllerCommand.BeginDrag), EAmandsControllerPressType.Press, 20) });
            AmandsControllerSets["Interface"][EAmandsControllerButton.A].Add(new AmandsControllerButtonBind(new AmandsControllerCommand(EAmandsControllerCommand.ShowContextMenu), EAmandsControllerPressType.Hold, 20));
            AmandsControllerSets["Interface"].Add(EAmandsControllerButton.B, new List<AmandsControllerButtonBind> { new AmandsControllerButtonBind(new AmandsControllerCommand(ECommand.Escape), EAmandsControllerPressType.Press, 20) });
            AmandsControllerSets["Interface"].Add(EAmandsControllerButton.X, new List<AmandsControllerButtonBind> { new AmandsControllerButtonBind(new AmandsControllerCommand(EAmandsControllerCommand.Use), EAmandsControllerPressType.Press, 20) });
            AmandsControllerSets["Interface"][EAmandsControllerButton.X].Add(new AmandsControllerButtonBind(new AmandsControllerCommand(EAmandsControllerCommand.UseHold), EAmandsControllerPressType.Hold, 20));
            AmandsControllerSets["Interface"].Add(EAmandsControllerButton.Y, new List<AmandsControllerButtonBind> { new AmandsControllerButtonBind(new AmandsControllerCommand(EAmandsControllerCommand.QuickMove), EAmandsControllerPressType.Press, 20) });

            AmandsControllerSets["Interface"].Add(EAmandsControllerButton.RS, new List<AmandsControllerButtonBind> { new AmandsControllerButtonBind(new AmandsControllerCommand(EAmandsControllerCommand.Discard), EAmandsControllerPressType.Press, 20) });

            AmandsControllerSets["Interface"].Add(EAmandsControllerButton.LB, new List<AmandsControllerButtonBind> { new AmandsControllerButtonBind(new AmandsControllerCommand(EAmandsControllerCommand.EnableSet, "Interface_LB"), EAmandsControllerPressType.Press, 20) });
            AmandsControllerSets["Interface"][EAmandsControllerButton.LB].Add(new AmandsControllerButtonBind(new AmandsControllerCommand(EAmandsControllerCommand.DisableSet, "Interface_LB"), EAmandsControllerPressType.Release, 20));

            AmandsControllerSets.Add("OnDrag", new Dictionary<EAmandsControllerButton, List<AmandsControllerButtonBind>>());
            AmandsControllerSets["OnDrag"].Add(EAmandsControllerButton.A, new List<AmandsControllerButtonBind> { new AmandsControllerButtonBind(new AmandsControllerCommand(EAmandsControllerCommand.EndDrag), EAmandsControllerPressType.Press, 22) });
            AmandsControllerSets["OnDrag"].Add(EAmandsControllerButton.B, new List<AmandsControllerButtonBind> { new AmandsControllerButtonBind(new AmandsControllerCommand(EAmandsControllerCommand.CancelDrag), EAmandsControllerPressType.Press, 22) });
            AmandsControllerSets["OnDrag"].Add(EAmandsControllerButton.X, new List<AmandsControllerButtonBind> { new AmandsControllerButtonBind(new AmandsControllerCommand(EAmandsControllerCommand.RotateDragged), EAmandsControllerPressType.Press, 22) });
            AmandsControllerSets["OnDrag"].Add(EAmandsControllerButton.Y, new List<AmandsControllerButtonBind> { new AmandsControllerButtonBind(new AmandsControllerCommand(EAmandsControllerCommand.SplitDragged), EAmandsControllerPressType.Press, 22) });

            AmandsControllerSets.Add("Interface_LB", new Dictionary<EAmandsControllerButton, List<AmandsControllerButtonBind>>());
            AmandsControllerSets["Interface_LB"].Add(EAmandsControllerButton.A, new List<AmandsControllerButtonBind> { new AmandsControllerButtonBind(new AmandsControllerCommand(EAmandsControllerCommand.InterfaceBind4), EAmandsControllerPressType.Press, 21) });
            AmandsControllerSets["Interface_LB"].Add(EAmandsControllerButton.B, new List<AmandsControllerButtonBind> { new AmandsControllerButtonBind(new AmandsControllerCommand(EAmandsControllerCommand.InterfaceBind5), EAmandsControllerPressType.Press, 21) });
            AmandsControllerSets["Interface_LB"].Add(EAmandsControllerButton.X, new List<AmandsControllerButtonBind> { new AmandsControllerButtonBind(new AmandsControllerCommand(EAmandsControllerCommand.InterfaceBind6), EAmandsControllerPressType.Press, 21) });
            AmandsControllerSets["Interface_LB"].Add(EAmandsControllerButton.Y, new List<AmandsControllerButtonBind> { new AmandsControllerButtonBind(new AmandsControllerCommand(EAmandsControllerCommand.InterfaceBind7), EAmandsControllerPressType.Press, 21) });

            AmandsControllerSets.Add("Interface_LB_RB", new Dictionary<EAmandsControllerButton, List<AmandsControllerButtonBind>>());
            AmandsControllerSets["Interface_LB_RB"].Add(EAmandsControllerButton.A, new List<AmandsControllerButtonBind> { new AmandsControllerButtonBind(new AmandsControllerCommand(EAmandsControllerCommand.InterfaceBind8), EAmandsControllerPressType.Press, 22) });
            AmandsControllerSets["Interface_LB_RB"].Add(EAmandsControllerButton.B, new List<AmandsControllerButtonBind> { new AmandsControllerButtonBind(new AmandsControllerCommand(EAmandsControllerCommand.InterfaceBind9), EAmandsControllerPressType.Press, 22) });
            AmandsControllerSets["Interface_LB_RB"].Add(EAmandsControllerButton.X, new List<AmandsControllerButtonBind> { new AmandsControllerButtonBind(new AmandsControllerCommand(EAmandsControllerCommand.InterfaceBind10), EAmandsControllerPressType.Press, 22) });

            AmandsControllerSets.Add("SearchButton", new Dictionary<EAmandsControllerButton, List<AmandsControllerButtonBind>>());
            AmandsControllerSets["SearchButton"].Add(EAmandsControllerButton.A, new List<AmandsControllerButtonBind> { new AmandsControllerButtonBind(new AmandsControllerCommand(EAmandsControllerCommand.Search), EAmandsControllerPressType.Press, 23) });

            AmandsControllerSets.Add("ContextMenu", new Dictionary<EAmandsControllerButton, List<AmandsControllerButtonBind>>());
            AmandsControllerSets["ContextMenu"].Add(EAmandsControllerButton.A, new List<AmandsControllerButtonBind> { new AmandsControllerButtonBind(new AmandsControllerCommand(EAmandsControllerCommand.ContextMenuUse), EAmandsControllerPressType.Press, 30) });
            AmandsControllerSets["ContextMenu"].Add(EAmandsControllerButton.X, new List<AmandsControllerButtonBind> { new AmandsControllerButtonBind(new AmandsControllerCommand(EAmandsControllerCommand.None), EAmandsControllerPressType.Press, 30) });
            AmandsControllerSets["ContextMenu"].Add(EAmandsControllerButton.Y, new List<AmandsControllerButtonBind> { new AmandsControllerButtonBind(new AmandsControllerCommand(EAmandsControllerCommand.None), EAmandsControllerPressType.Press, 30) });
            AmandsControllerSets["ContextMenu"].Add(EAmandsControllerButton.RS, new List<AmandsControllerButtonBind> { new AmandsControllerButtonBind(new AmandsControllerCommand(EAmandsControllerCommand.None), EAmandsControllerPressType.Press, 30) });

            AmandsControllerSets.Add("SplitDialog", new Dictionary<EAmandsControllerButton, List<AmandsControllerButtonBind>>());
            AmandsControllerSets["SplitDialog"].Add(EAmandsControllerButton.A, new List<AmandsControllerButtonBind> { new AmandsControllerButtonBind(new AmandsControllerCommand(EAmandsControllerCommand.SplitDialogAccept), EAmandsControllerPressType.Press, 50) });
            AmandsControllerSets["SplitDialog"].Add(EAmandsControllerButton.UP, new List<AmandsControllerButtonBind> { new AmandsControllerButtonBind(new AmandsControllerCommand(EAmandsControllerCommand.SplitDialogAdd), EAmandsControllerPressType.Press, 50) });
            AmandsControllerSets["SplitDialog"][EAmandsControllerButton.UP].Add(new AmandsControllerButtonBind(new AmandsControllerCommand(EAmandsControllerCommand.SplitDialogDisableAutoMove), EAmandsControllerPressType.Release, 50));
            AmandsControllerSets["SplitDialog"].Add(EAmandsControllerButton.DOWN, new List<AmandsControllerButtonBind> { new AmandsControllerButtonBind(new AmandsControllerCommand(EAmandsControllerCommand.SplitDialogSubtract), EAmandsControllerPressType.Press, 50) });
            AmandsControllerSets["SplitDialog"][EAmandsControllerButton.DOWN].Add(new AmandsControllerButtonBind(new AmandsControllerCommand(EAmandsControllerCommand.SplitDialogDisableAutoMove), EAmandsControllerPressType.Release, 50));
            AmandsControllerSets["SplitDialog"].Add(EAmandsControllerButton.LEFT, new List<AmandsControllerButtonBind> { new AmandsControllerButtonBind(new AmandsControllerCommand(EAmandsControllerCommand.SplitDialogSubtract), EAmandsControllerPressType.Press, 50) });
            AmandsControllerSets["SplitDialog"][EAmandsControllerButton.LEFT].Add(new AmandsControllerButtonBind(new AmandsControllerCommand(EAmandsControllerCommand.SplitDialogDisableAutoMove), EAmandsControllerPressType.Release, 50));
            AmandsControllerSets["SplitDialog"].Add(EAmandsControllerButton.RIGHT, new List<AmandsControllerButtonBind> { new AmandsControllerButtonBind(new AmandsControllerCommand(EAmandsControllerCommand.SplitDialogAdd), EAmandsControllerPressType.Press, 50) });
            AmandsControllerSets["SplitDialog"][EAmandsControllerButton.RIGHT].Add(new AmandsControllerButtonBind(new AmandsControllerCommand(EAmandsControllerCommand.SplitDialogDisableAutoMove), EAmandsControllerPressType.Release, 50));

            AmandsControllerButtonBinds.Clear();
            AmandsControllerButtonBinds.Add(EAmandsControllerButton.LT, new List<AmandsControllerButtonBind> { new AmandsControllerButtonBind(new List<AmandsControllerCommand> { new AmandsControllerCommand(ECommand.ToggleAlternativeShooting), new AmandsControllerCommand(ECommand.EndSprinting), new AmandsControllerCommand(ECommand.TryLowThrow) }, EAmandsControllerPressType.Press, -1) });
            AmandsControllerButtonBinds[EAmandsControllerButton.LT].Add(new AmandsControllerButtonBind(new AmandsControllerCommand(ECommand.FinishLowThrow), EAmandsControllerPressType.Release, -1));
            AmandsControllerButtonBinds.Add(EAmandsControllerButton.RT, new List<AmandsControllerButtonBind> { new AmandsControllerButtonBind(new List<AmandsControllerCommand> { new AmandsControllerCommand(ECommand.ToggleShooting), new AmandsControllerCommand(ECommand.EndSprinting), new AmandsControllerCommand(ECommand.TryHighThrow) }, EAmandsControllerPressType.Press, -1) });
            AmandsControllerButtonBinds[EAmandsControllerButton.RT].Add(new AmandsControllerButtonBind(new List<AmandsControllerCommand> { new AmandsControllerCommand(ECommand.EndShooting), new AmandsControllerCommand(ECommand.FinishHighThrow) }, EAmandsControllerPressType.Release, -1));

            AmandsControllerButtonBinds.Add(EAmandsControllerButton.A, new List<AmandsControllerButtonBind> { new AmandsControllerButtonBind(new AmandsControllerCommand(ECommand.Jump), EAmandsControllerPressType.Press, -1) });
            AmandsControllerButtonBinds.Add(EAmandsControllerButton.B, new List<AmandsControllerButtonBind> { new AmandsControllerButtonBind(new AmandsControllerCommand(ECommand.ToggleDuck), EAmandsControllerPressType.Press, -1) });
            AmandsControllerButtonBinds[EAmandsControllerButton.B].Add(new AmandsControllerButtonBind(new AmandsControllerCommand(ECommand.ToggleProne), EAmandsControllerPressType.Hold, -1));
            AmandsControllerButtonBinds.Add(EAmandsControllerButton.X, new List<AmandsControllerButtonBind> { new AmandsControllerButtonBind(new AmandsControllerCommand(ECommand.ReloadWeapon), EAmandsControllerPressType.Press, -1) });
            AmandsControllerButtonBinds[EAmandsControllerButton.X].Add(new AmandsControllerButtonBind(new AmandsControllerCommand(ECommand.CheckAmmo), EAmandsControllerPressType.Hold, -1));
            AmandsControllerButtonBinds[EAmandsControllerButton.X].Add(new AmandsControllerButtonBind(new AmandsControllerCommand(ECommand.QuickReloadWeapon), EAmandsControllerPressType.DoubleClick, -1));
            AmandsControllerButtonBinds.Add(EAmandsControllerButton.Y, new List<AmandsControllerButtonBind> { new AmandsControllerButtonBind(new AmandsControllerCommand(EAmandsControllerCommand.QuickSelectWeapon), EAmandsControllerPressType.Press, -1) });
            AmandsControllerButtonBinds[EAmandsControllerButton.Y].Add(new AmandsControllerButtonBind(new AmandsControllerCommand(ECommand.ExamineWeapon), EAmandsControllerPressType.Hold, -1));
            AmandsControllerButtonBinds.Add(EAmandsControllerButton.LS, new List<AmandsControllerButtonBind> { new AmandsControllerButtonBind(new AmandsControllerCommand(ECommand.ToggleSprinting), EAmandsControllerPressType.Press, -1) });
            AmandsControllerButtonBinds.Add(EAmandsControllerButton.RS, new List<AmandsControllerButtonBind> { new AmandsControllerButtonBind(new AmandsControllerCommand(ECommand.QuickKnifeKick), EAmandsControllerPressType.Press, -1) });
            AmandsControllerButtonBinds[EAmandsControllerButton.RS].Add(new AmandsControllerButtonBind(new AmandsControllerCommand(ECommand.SelectKnife), EAmandsControllerPressType.Hold, -1));

            AmandsControllerButtonBinds.Add(EAmandsControllerButton.UP, new List<AmandsControllerButtonBind> { new AmandsControllerButtonBind(new AmandsControllerCommand(ECommand.NextTacticalDevice), EAmandsControllerPressType.Press, -1) });
            AmandsControllerButtonBinds[EAmandsControllerButton.UP].Add(new AmandsControllerButtonBind(new AmandsControllerCommand(ECommand.ToggleTacticalDevice), EAmandsControllerPressType.Hold, -1));
            AmandsControllerButtonBinds.Add(EAmandsControllerButton.DOWN, new List<AmandsControllerButtonBind> { new AmandsControllerButtonBind(new AmandsControllerCommand(ECommand.ChangeWeaponMode), EAmandsControllerPressType.Press, -1) });
            AmandsControllerButtonBinds[EAmandsControllerButton.DOWN].Add(new AmandsControllerButtonBind(new AmandsControllerCommand(ECommand.CheckFireMode), EAmandsControllerPressType.Hold, -1));
            AmandsControllerButtonBinds[EAmandsControllerButton.DOWN].Add(new AmandsControllerButtonBind(new AmandsControllerCommand(ECommand.ForceAutoWeaponMode), EAmandsControllerPressType.DoubleClick, -1));
            AmandsControllerButtonBinds.Add(EAmandsControllerButton.LEFT, new List<AmandsControllerButtonBind> { new AmandsControllerButtonBind(new AmandsControllerCommand(ECommand.ChangeScopeMagnification), EAmandsControllerPressType.Press, -1) });
            AmandsControllerButtonBinds[EAmandsControllerButton.LEFT].Add(new AmandsControllerButtonBind(new AmandsControllerCommand(ECommand.ChangeScope), EAmandsControllerPressType.Hold, -1));
            AmandsControllerButtonBinds.Add(EAmandsControllerButton.RIGHT, new List<AmandsControllerButtonBind> { new AmandsControllerButtonBind(new AmandsControllerCommand(ECommand.ChangeScope), EAmandsControllerPressType.Press, -1) });
            AmandsControllerButtonBinds.Add(EAmandsControllerButton.BACK, new List<AmandsControllerButtonBind> { new AmandsControllerButtonBind(new AmandsControllerCommand(ECommand.ToggleInventory), EAmandsControllerPressType.Press, -1) });
            
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
                ActiveAmandsControllerSets.Remove("Interface_LB");
                ActiveAmandsControllerSets.Remove("Interface_LB_RB");
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
        public void UpdateSplitDialogBinds(bool Enabled)
        {
            if (Enabled)
            {
                if (AmandsControllerSets.ContainsKey("SplitDialog") && !ActiveAmandsControllerSets.Contains("SplitDialog"))
                {
                    ActiveAmandsControllerSets.Add("SplitDialog");
                }
            }
            else
            {
                ActiveAmandsControllerSets.Remove("SplitDialog");
            }
        }
        public void UpdateHealingLimbSelectorBinds(bool Enabled)
        {
            /*if (Enabled)
            {
                if (AmandsControllerSets.ContainsKey("HealingLimbSelector") && !ActiveAmandsControllerSets.Contains("HealingLimbSelector"))
                {
                    ActiveAmandsControllerSets.Add("HealingLimbSelector");
                }
            }
            else
            {
                ActiveAmandsControllerSets.Remove("HealingLimbSelector");
            }*/
        }
        public void UpdateContextMenuBinds(bool Enabled)
        {
            ContextMenu = Enabled;
            if (Enabled)
            {
                if (AmandsControllerSets.ContainsKey("ContextMenu") && !ActiveAmandsControllerSets.Contains("ContextMenu"))
                {
                    ActiveAmandsControllerSets.Add("ContextMenu");
                }
            }
            else
            {
                ActiveAmandsControllerSets.Remove("ContextMenu");
            }
        }
        public void AmandsControllerGeneratePressType(EAmandsControllerButton Button, bool Pressed)
        {
            if (AmandsControllerButtonSnapshots.ContainsKey(Button))
            {
                AmandsControllerButtonSnapshot AmandsControllerButtonSnapshot = AmandsControllerButtonSnapshots[Button];
                if (Pressed)
                {
                    if (AmandsControllerButtonSnapshot.DoubleClickBind.Priority != -100 && Time.time - AmandsControllerButtonSnapshot.Time <= AmandsControllerPlugin.DoubleClickDelay.Value)
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
                    if (AmandsControllerButtonSnapshot.ReleaseBind.Priority != -100)
                    {
                        AmandsControllerButton(AmandsControllerButtonSnapshot.ReleaseBind);
                        AmandsControllerButtonSnapshots.Remove(Button);
                    }
                    else
                    {
                        // Temp
                        if (AmandsControllerButtonSnapshot.HoldBind.Priority == -100 && AmandsControllerButtonSnapshot.DoubleClickBind.Priority == -100)
                        {
                            if (AmandsControllerButtonSnapshot.ReleaseBind.Priority != -100)
                            {
                                AmandsControllerButton(AmandsControllerButtonSnapshot.ReleaseBind);
                            }
                            AmandsControllerButtonSnapshots.Remove(Button);
                        }
                        else if (AmandsControllerButtonSnapshot.HoldBind.Priority != -100 || AmandsControllerButtonSnapshot.DoubleClickBind.Priority != -100)
                        {
                            AsyncHold.Remove(Button.ToString() + AmandsControllerButtonSnapshot.Time.ToString());
                        }
                        if (AmandsControllerButtonSnapshot.DoubleClickBind.Priority == -100 && AmandsControllerButtonSnapshot.ReleaseBind.Priority == -100)
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
                if (Binds[1].Priority != -100)
                {
                    AmandsControllerButtonSnapshots.Add(Button, new AmandsControllerButtonSnapshot(true, time, Binds[0], Binds[1], Binds[2], Binds[3]));
                    AmandsControllerButton(Binds[0]);
                }
                else
                {
                    // Temp
                    if (Binds[2].Priority == -100 && Binds[3].Priority == -100)
                    {
                        AmandsControllerButton(Binds[0]);
                    }
                    else if (Binds[2].Priority != -100 || Binds[3].Priority != -100)
                    {
                        AmandsControllerButtonTimer(Button.ToString() + time.ToString(), Button);
                    }
                    if (Binds[2].Priority != -100 || Binds[3].Priority != -100)
                    {
                        AmandsControllerButtonSnapshots.Add(Button, new AmandsControllerButtonSnapshot(true, time, Binds[0], Binds[1], Binds[2], Binds[3]));
                    }
                }
            }
            /*if (AmandsControllerButtonSnapshots.ContainsKey(Button))
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
            }*/
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
        public void AmandsControllerButton(AmandsControllerButtonBind Bind)
        {
            List<ECommand> Commands = new List<ECommand>();
            foreach (AmandsControllerCommand AmandsControllerCommand in Bind.AmandsControllerCommands)
            {
                if (AmandsControllerCommand.Command == EAmandsControllerCommand.None) continue;
                DebugStuff("AmandsControllerCommand " + AmandsControllerCommand.Command.ToString() + " InputTree " + AmandsControllerCommand.InputTree.ToString() + " " + Bind.PressType.ToString());
                switch (AmandsControllerCommand.Command)
                {
                    case EAmandsControllerCommand.ToggleSet:
                        if (ActiveAmandsControllerSets.Contains(AmandsControllerCommand.AmandsControllerSet))
                        {
                            DebugStuff("ToggleSet Remove " + AmandsControllerCommand.AmandsControllerSet);
                            ActiveAmandsControllerSets.Remove(AmandsControllerCommand.AmandsControllerSet);
                            AmandsControllerLSRSButtonsCheck();
                        }
                        else if (AmandsControllerSets.ContainsKey(AmandsControllerCommand.AmandsControllerSet))
                        {
                            DebugStuff("ToggleSet Add " + AmandsControllerCommand.AmandsControllerSet);
                            ActiveAmandsControllerSets.Add(AmandsControllerCommand.AmandsControllerSet);
                            AmandsControllerLSRSButtonsCheck();
                        }
                        break;
                    case EAmandsControllerCommand.EnableSet:
                        if (AmandsControllerSets.ContainsKey(AmandsControllerCommand.AmandsControllerSet) && !ActiveAmandsControllerSets.Contains(AmandsControllerCommand.AmandsControllerSet))
                        {
                            DebugStuff("EnableSet " + AmandsControllerCommand.AmandsControllerSet);
                            ActiveAmandsControllerSets.Add(AmandsControllerCommand.AmandsControllerSet);
                            AmandsControllerLSRSButtonsCheck();
                        }
                        break;
                    case EAmandsControllerCommand.DisableSet:
                        DebugStuff("DisableSet " + AmandsControllerCommand.AmandsControllerSet);
                        ActiveAmandsControllerSets.Remove(AmandsControllerCommand.AmandsControllerSet);
                        AmandsControllerLSRSButtonsCheck();
                        break;
                    case EAmandsControllerCommand.InputTree:
                        if (inputTree != null)
                        {
                            Commands.Add(AmandsControllerCommand.InputTree);
                        }
                        break;
                    case EAmandsControllerCommand.QuickSelectWeapon:
                        DebugStuff("QuickSelectWeapon");
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
                        if (inputTree != null)
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
                        break;
                    case EAmandsControllerCommand.InterfaceDown:
                        ControllerUIMove(new Vector2Int(0, -1), false);
                        AutoMove = true;
                        AutoMoveTime = 0f;
                        break;
                    case EAmandsControllerCommand.InterfaceLeft:
                        ControllerUIMove(new Vector2Int(-1, 0), false);
                        AutoMove = true;
                        AutoMoveTime = 0f;
                        break;
                    case EAmandsControllerCommand.InterfaceRight:
                        ControllerUIMove(new Vector2Int(1, 0), false);
                        AutoMove = true;
                        AutoMoveTime = 0f;
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
                    case EAmandsControllerCommand.RotateDragged:
                        AmandsControllerRotateDragged();
                        break;
                    case EAmandsControllerCommand.SplitDragged:
                        AmandsControllerSplitDragged();
                        break;
                    case EAmandsControllerCommand.CancelDrag:
                        AmandsControllerCancelDrag();
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
                    case EAmandsControllerCommand.InterfaceBind4:
                        AmandsControllerInterfaceBind(EBoundItem.Item4);
                        break;
                    case EAmandsControllerCommand.InterfaceBind5:
                        AmandsControllerInterfaceBind(EBoundItem.Item5);
                        break;
                    case EAmandsControllerCommand.InterfaceBind6:
                        AmandsControllerInterfaceBind(EBoundItem.Item6);
                        break;
                    case EAmandsControllerCommand.InterfaceBind7:
                        AmandsControllerInterfaceBind(EBoundItem.Item7);
                        break;
                    case EAmandsControllerCommand.InterfaceBind8:
                        AmandsControllerInterfaceBind(EBoundItem.Item8);
                        break;
                    case EAmandsControllerCommand.InterfaceBind9:
                        AmandsControllerInterfaceBind(EBoundItem.Item9);
                        break;
                    case EAmandsControllerCommand.InterfaceBind10:
                        AmandsControllerInterfaceBind(EBoundItem.Item10);
                        break;
                    case EAmandsControllerCommand.ShowContextMenu:
                        AmandsControllerShowContextMenu();
                        break;
                    case EAmandsControllerCommand.ContextMenuUse:
                        AmandsControllerContextMenuUse();
                        break;
                    case EAmandsControllerCommand.SplitDialogAccept:
                        AmandsControllerSplitDialogAccept();
                        break;
                    case EAmandsControllerCommand.SplitDialogAdd:
                        AmandsControllerSplitDialogAdd(1);
                        break;
                    case EAmandsControllerCommand.SplitDialogSubtract:
                        AmandsControllerSplitDialogAdd(-1);
                        break;
                    case EAmandsControllerCommand.SplitDialogDisableAutoMove:
                        SplitDialogAutoMove = false;
                        break;
                }
            }
            if (Commands.Count != 0 && inputTree != null)
            {
                TranslateInputInvokeParameters[0] = Commands;
                TranslateInput.Invoke(inputTree, TranslateInputInvokeParameters);
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
        public void AmandsControllerLSRSButtonsCheck()
        {
            LSButtons = false;
            RSButtons = false;
            foreach (string ActiveSet in ActiveAmandsControllerSets)
            {
                if (AmandsControllerSets[ActiveSet].ContainsKey(EAmandsControllerButton.LSUP) || AmandsControllerSets[ActiveSet].ContainsKey(EAmandsControllerButton.LSDOWN) || AmandsControllerSets[ActiveSet].ContainsKey(EAmandsControllerButton.LSLEFT) || AmandsControllerSets[ActiveSet].ContainsKey(EAmandsControllerButton.LSRIGHT)) LSButtons = true;
                if (AmandsControllerSets[ActiveSet].ContainsKey(EAmandsControllerButton.RSUP) || AmandsControllerSets[ActiveSet].ContainsKey(EAmandsControllerButton.RSDOWN) || AmandsControllerSets[ActiveSet].ContainsKey(EAmandsControllerButton.RSLEFT) || AmandsControllerSets[ActiveSet].ContainsKey(EAmandsControllerButton.RSRIGHT)) RSButtons = true;
            }
            if (!LSButtons)
            {
                if (LSUP)
                {
                    LSUP = false;
                    AmandsControllerGeneratePressType(EAmandsControllerButton.LSUP, false);
                }
                if (LSDOWN)
                {
                    LSDOWN = false;
                    AmandsControllerGeneratePressType(EAmandsControllerButton.LSRIGHT, false);
                }
                if (LSLEFT)
                {
                    LSLEFT = false;
                    AmandsControllerGeneratePressType(EAmandsControllerButton.LSLEFT, false);
                }
                if (LSRIGHT)
                {
                    LSRIGHT = false;
                    AmandsControllerGeneratePressType(EAmandsControllerButton.LSRIGHT, false);
                }
            }
            if (!RSButtons)
            {
                if (RSUP)
                {
                    RSUP = false;
                    AmandsControllerGeneratePressType(EAmandsControllerButton.RSUP, false);
                }
                if (RSDOWN)
                {
                    RSDOWN = false;
                    AmandsControllerGeneratePressType(EAmandsControllerButton.RSRIGHT, false);
                }
                if (RSLEFT)
                {
                    RSLEFT = false;
                    AmandsControllerGeneratePressType(EAmandsControllerButton.RSLEFT, false);
                }
                if (RSRIGHT)
                {
                    RSRIGHT = false;
                    AmandsControllerGeneratePressType(EAmandsControllerButton.RSRIGHT, false);
                }
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
            currentDogtagSlotView = null;
            currentSpecialSlotSlotView = null;
            currentSearchButton = null;
            currentSimpleContextMenuButton = null;
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
                        DebugStuff(gridViewLocation.ToString());
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
                    DebugStuff(gridViewLocation.ToString());
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
                        DebugStuff("Hitdection hit Equipment Slot");
                        // Debug end
                        ResetAllCurrent();
                        currentEquipmentSlotView = slotView;
                        gridViewLocation = new Vector2Int(1, 1);
                        DebugStuff(gridViewLocation.ToString());
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
                        DebugStuff("Hitdection hit Weapon Slot");
                        // Debug end
                        ResetAllCurrent();
                        currentWeaponsSlotView = slotView;
                        gridViewLocation = new Vector2Int(1, 1);
                        DebugStuff(gridViewLocation.ToString());
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
                        DebugStuff("Hitdection hit Armband Slot");
                        // Debug end
                        ResetAllCurrent();
                        currentArmbandSlotView = armbandSlotView;
                        gridViewLocation = new Vector2Int(1, 1);
                        DebugStuff(gridViewLocation.ToString());
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
                        DebugStuff("Hitdection hit Container Slot");
                        // Debug end
                        ResetAllCurrent();
                        currentContainersSlotView = slotView;
                        gridViewLocation = new Vector2Int(1, 1);
                        DebugStuff(gridViewLocation.ToString());
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
                        DebugStuff("Hitdection hit Equipment Slot");
                        // Debug end
                        ResetAllCurrent();
                        currentEquipmentSlotView = slotView;
                        gridViewLocation = new Vector2Int(1, 1);
                        DebugStuff(gridViewLocation.ToString());
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
                        DebugStuff("Hitdection hit Weapon Slot");
                        // Debug end
                        ResetAllCurrent();
                        currentWeaponsSlotView = slotView;
                        gridViewLocation = new Vector2Int(1, 1);
                        DebugStuff(gridViewLocation.ToString());
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
                        DebugStuff("Hitdection hit Armband Slot");
                        // Debug end
                        ResetAllCurrent();
                        currentArmbandSlotView = lootArmbandSlotView;
                        gridViewLocation = new Vector2Int(1, 1);
                        DebugStuff(gridViewLocation.ToString());
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
                        DebugStuff("Hitdection hit Container Slot");
                        // Debug end
                        ResetAllCurrent();
                        currentContainersSlotView = slotView;
                        gridViewLocation = new Vector2Int(1, 1);
                        DebugStuff(gridViewLocation.ToString());
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
            currentScrollRectNoDrag = null;
            currentScrollRectNoDragRectTransform = null;

            RectTransform rectTransform;
            Vector2 position;
            foreach (ScrollRectNoDrag scrollRectNoDrag in scrollRectNoDrags)
            {
                DebugStuff("scrollRectNoDrag " + scrollRectNoDrag.name);
                rectTransform = scrollRectNoDrag.GetComponent<RectTransform>();
                DebugStuff("pivot.x " + rectTransform.pivot.x);
                position = new Vector2(rectTransform.position.x + rectTransform.rect.x, rectTransform.position.y - (rectTransform.rect.height * (rectTransform.pivot.y - 1f)));
                if (Position.x > position.x && Position.x < (position.x + (rectTransform.rect.width * ScreenRatio)) && Position.y < position.y && Position.y > (position.y - (rectTransform.rect.height * ScreenRatio)))
                {
                    currentScrollRectNoDrag = scrollRectNoDrag;
                    currentScrollRectNoDragRectTransform = rectTransform;
                    return true;
                }
                else
                {
                    RectTransform rectTransform2 = scrollRectNoDrag.content;
                    DebugStuff("pivot.x " + rectTransform2.pivot.x);
                    position = new Vector2(rectTransform2.position.x + rectTransform2.rect.x, rectTransform2.position.y - (rectTransform2.rect.height * (rectTransform2.pivot.y - 1f)));
                    if (Position.x > position.x && Position.x < (position.x + (rectTransform2.rect.width * ScreenRatio)) && Position.y < position.y && Position.y > (position.y - (rectTransform2.rect.height * ScreenRatio)))
                    {
                        currentScrollRectNoDrag = scrollRectNoDrag;
                        currentScrollRectNoDragRectTransform = rectTransform;
                        return true;
                    }
                }
            }
            return false;
        }
        public void ControllerUIMove(Vector2Int direction, bool Skip)
        {
            lastDirection = direction;

            ActiveAmandsControllerSets.Remove("SearchButton");

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
            SlotView bestDogtagSlotView = null;
            SlotView bestSpecialSlotSlotView = null;
            ItemSpecificationPanel bestItemSpecificationPanel = null;
            TradingTableGridView bestTradingTableGridView = null;
            ContainedGridsView bestContainedGridsView = null;
            SearchButton bestSearchButton = null;
            SimpleContextMenuButton bestSimpleContextMenuButton = null;
            Vector2Int bestGridViewLocation = Vector2Int.one;

            // Exclusive SimpleContextMenuButton Blind Search
            if (simpleContextMenuButtons.Count > 0)
            {
                UpdateGlobalPosition();
                foreach (SimpleContextMenuButton simpleContextMenuButton in simpleContextMenuButtons)
                {
                    if (simpleContextMenuButton == currentSimpleContextMenuButton) continue;

                    position.x = simpleContextMenuButton.transform.position.x;
                    position.y = simpleContextMenuButton.transform.position.y;

                    dot = direction == Vector2Int.zero ? 1f : Vector2.Dot((globalPosition - position).normalized, -direction);
                    distance = Vector2.Distance(globalPosition, position);
                    score = Mathf.Lerp(distance, distance * 0.25f, dot);

                    if (score < bestScore && dot > 0.4f)
                    {
                        bestScore = score;
                        bestSimpleContextMenuButton = simpleContextMenuButton;
                    }
                }
                if (bestSimpleContextMenuButton == null) return;
                if (currentSimpleContextMenuButton != null)
                {
                    currentSimpleContextMenuButton.OnPointerExit(null);
                }
                currentSimpleContextMenuButton = bestSimpleContextMenuButton;
                UpdateGlobalPosition();
                currentSimpleContextMenuButton.OnPointerEnter(null);
                return;
            }

            if (currentGridView == null && currentTradingTableGridView == null && currentEquipmentSlotView == null && currentWeaponsSlotView == null && currentArmbandSlotView == null && currentContainersSlotView == null && currentDogtagSlotView == null && currentSpecialSlotSlotView == null && currentModSlotView == null && currentSearchButton == null)
            {
                if (gridViews.Count == 0) return;
                currentGridView = gridViews[0];
            }

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
                    bestEquipmentSlotView = null;
                    bestWeaponsSlotView = null;
                    bestArmbandSlotView = null;
                    bestContainersSlotView = null;
                    bestDogtagSlotView = null;
                    bestSpecialSlotSlotView = null;
                    bestItemSpecificationPanel = null;
                    bestTradingTableGridView = null;
                    bestContainedGridsView = null;
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
                        bestEquipmentSlotView = null;
                        bestWeaponsSlotView = null;
                        bestArmbandSlotView = null;
                        bestContainersSlotView = null;
                        bestDogtagSlotView = null;
                        bestSpecialSlotSlotView = null;
                        bestItemSpecificationPanel = null;
                        bestTradingTableGridView = null;
                        bestContainedGridsView = containedGridsView;
                        bestSearchButton = null;
                        bestSimpleContextMenuButton = null;
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
                    bestEquipmentSlotView = null;
                    bestWeaponsSlotView = null;
                    bestArmbandSlotView = null;
                    bestContainersSlotView = null;
                    bestDogtagSlotView = null;
                    bestSpecialSlotSlotView = null;
                    bestItemSpecificationPanel = null;
                    bestTradingTableGridView = tradingTableGridView;
                    bestContainedGridsView = null;
                    bestSearchButton = null;
                    bestSimpleContextMenuButton = null;
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
                        bestEquipmentSlotView = null;
                        bestWeaponsSlotView = null;
                        bestArmbandSlotView = null;
                        bestContainersSlotView = null;
                        bestDogtagSlotView = null;
                        bestSpecialSlotSlotView = null;
                        bestItemSpecificationPanel = itemSpecificationPanel;
                        bestTradingTableGridView = null;
                        bestContainedGridsView = null;
                        bestSearchButton = null;
                        bestGridViewLocation = new Vector2Int(1, 1);
                    }
                }
            }
            // SearchButton Blind Search
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
                    bestEquipmentSlotView = null;
                    bestWeaponsSlotView = null;
                    bestArmbandSlotView = null;
                    bestContainersSlotView = null;
                    bestDogtagSlotView = null;
                    bestSpecialSlotSlotView = null;
                    bestItemSpecificationPanel = null;
                    bestTradingTableGridView = null;
                    bestContainedGridsView = null;
                    bestSearchButton = searchButton;
                    bestGridViewLocation = new Vector2Int(1, 1);
                }
            }
            // SpecialSlot Blind Search
            foreach (SlotView slotView in specialSlotSlotViews)
            {
                if (slotView == currentSpecialSlotSlotView) continue;
                position = new Vector2(slotView.transform.position.x + (GridSize / 2f), slotView.transform.position.y - (GridSize / 2f));
                dot = Vector2.Dot((globalPosition - position).normalized, -direction);
                distance = Vector2.Distance(globalPosition, position);
                score = Mathf.Lerp(distance, distance * 0.25f, dot);

                if (score < bestScore && dot > 0.4f)
                {
                    bestScore = score;
                    bestGridView = null;
                    bestModSlotView = null;
                    bestEquipmentSlotView = null;
                    bestWeaponsSlotView = null;
                    bestArmbandSlotView = null;
                    bestContainersSlotView = null;
                    bestDogtagSlotView = null;
                    bestSpecialSlotSlotView = slotView;
                    bestItemSpecificationPanel = null;
                    bestTradingTableGridView = null;
                    bestContainedGridsView = null;
                    bestSearchButton = null;
                    bestGridViewLocation = new Vector2Int(1, 1);
                }
            }

        Skip2:

            // EquipmentSlotView Blind Search
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
                    bestEquipmentSlotView = slotView;
                    bestWeaponsSlotView = null;
                    bestArmbandSlotView = null;
                    bestContainersSlotView = null;
                    bestDogtagSlotView = null;
                    bestSpecialSlotSlotView = null;
                    bestItemSpecificationPanel = null;
                    bestTradingTableGridView = null;
                    bestContainedGridsView = null;
                    bestSearchButton = null;
                    bestGridViewLocation = new Vector2Int(1, 1);
                }
            }
            // WeaponsSlotView Blind Search
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
                    bestEquipmentSlotView = null;
                    bestWeaponsSlotView = slotView;
                    bestArmbandSlotView = null;
                    bestContainersSlotView = null;
                    bestDogtagSlotView = null;
                    bestSpecialSlotSlotView = null;
                    bestItemSpecificationPanel = null;
                    bestTradingTableGridView = null;
                    bestContainedGridsView = null;
                    bestSearchButton = null;
                    bestGridViewLocation = new Vector2Int(1, 1);
                }
            }
            // ArmbandSlotView Blind Search
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
                    bestEquipmentSlotView = null;
                    bestWeaponsSlotView = null;
                    bestArmbandSlotView = armbandSlotView;
                    bestContainersSlotView = null;
                    bestDogtagSlotView = null;
                    bestSpecialSlotSlotView = null;
                    bestItemSpecificationPanel = null;
                    bestTradingTableGridView = null;
                    bestContainedGridsView = null;
                    bestSearchButton = null;
                    bestGridViewLocation = new Vector2Int(1, 1);
                }
            }
            // ContainersSlotView Blind Search
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
                    bestEquipmentSlotView = null;
                    bestWeaponsSlotView = null;
                    bestArmbandSlotView = null;
                    bestContainersSlotView = slotView;
                    bestDogtagSlotView = null;
                    bestSpecialSlotSlotView = null;
                    bestItemSpecificationPanel = null;
                    bestTradingTableGridView = null;
                    bestContainedGridsView = null;
                    bestSearchButton = null;
                    bestGridViewLocation = new Vector2Int(1, 1);
                }
            }
            // LootEquipmentSlotView Blind Search
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
                    bestEquipmentSlotView = slotView;
                    bestWeaponsSlotView = null;
                    bestArmbandSlotView = null;
                    bestContainersSlotView = null;
                    bestDogtagSlotView = null;
                    bestSpecialSlotSlotView = null;
                    bestItemSpecificationPanel = null;
                    bestTradingTableGridView = null;
                    bestContainedGridsView = null;
                    bestSearchButton = null;
                    bestGridViewLocation = new Vector2Int(1, 1);
                }
            }
            // LootWeaponsSlotView Blind Search
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
                    bestEquipmentSlotView = null;
                    bestWeaponsSlotView = slotView;
                    bestArmbandSlotView = null;
                    bestContainersSlotView = null;
                    bestDogtagSlotView = null;
                    bestSpecialSlotSlotView = null;
                    bestItemSpecificationPanel = null;
                    bestTradingTableGridView = null;
                    bestContainedGridsView = null;
                    bestSearchButton = null;
                    bestGridViewLocation = new Vector2Int(1, 1);
                }
            }
            // LootArmbandSlotView Blind Search
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
                    bestEquipmentSlotView = null;
                    bestWeaponsSlotView = null;
                    bestArmbandSlotView = lootArmbandSlotView;
                    bestContainersSlotView = null;
                    bestDogtagSlotView = null;
                    bestSpecialSlotSlotView = null;
                    bestItemSpecificationPanel = null;
                    bestTradingTableGridView = null;
                    bestContainedGridsView = null;
                    bestSearchButton = null;
                    bestGridViewLocation = new Vector2Int(1, 1);
                }
            }
            // LootContainersSlotView Blind Search
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
                    bestEquipmentSlotView = null;
                    bestWeaponsSlotView = null;
                    bestArmbandSlotView = null;
                    bestContainersSlotView = slotView;
                    bestDogtagSlotView = null;
                    bestSpecialSlotSlotView = null;
                    bestItemSpecificationPanel = null;
                    bestTradingTableGridView = null;
                    bestContainedGridsView = null;
                    bestSearchButton = null;
                    bestGridViewLocation = new Vector2Int(1, 1);
                }
            }
            // Dogtag Blind Search
            if (dogtagSlotView != null)
            {
                position.x = dogtagSlotView.transform.position.x - GridSize - (GridSize / 2f);
                position.y = dogtagSlotView.transform.position.y - (GridSize / 2f);

                dot = Vector2.Dot((globalPosition - position).normalized, -direction);
                distance = Vector2.Distance(globalPosition, position);
                score = Mathf.Lerp(distance, distance * 0.25f, dot);

                if (score < bestScore && dot > 0.4f)
                {
                    bestScore = score;
                    bestGridView = null;
                    bestModSlotView = null;
                    bestEquipmentSlotView = null;
                    bestWeaponsSlotView = null;
                    bestArmbandSlotView = null;
                    bestContainersSlotView = null;
                    bestDogtagSlotView = dogtagSlotView;
                    bestSpecialSlotSlotView = null;
                    bestItemSpecificationPanel = null;
                    bestTradingTableGridView = null;
                    bestContainedGridsView = null;
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
                    bestEquipmentSlotView = null;
                    bestWeaponsSlotView = null;
                    bestArmbandSlotView = null;
                    bestContainersSlotView = null;
                    bestDogtagSlotView = null;
                    bestSpecialSlotSlotView = null;
                    bestItemSpecificationPanel = null;
                    bestTradingTableGridView = null;
                    bestContainedGridsView = null;
                    bestSearchButton = null;
                    bestGridViewLocation = new Vector2Int(Mathf.RoundToInt((position.x + (GridSize / 2f)) / GridSize), -Mathf.RoundToInt((position.y - (GridSize / 2f)) / GridSize));
                }
            }
            // Support

            if (bestGridView == null && bestTradingTableGridView == null && bestEquipmentSlotView == null && bestWeaponsSlotView == null && bestArmbandSlotView == null && bestContainersSlotView == null && bestDogtagSlotView == null && bestSpecialSlotSlotView == null && bestModSlotView == null && bestSearchButton == null && Skip)
            {
                ControllerUIMove(direction, false);
                return;
            }
            // Set GridView/SlotView
            if (bestGridView == null && bestTradingTableGridView == null && bestEquipmentSlotView == null && bestWeaponsSlotView == null && bestArmbandSlotView == null && bestContainersSlotView == null && bestDogtagSlotView == null && bestSpecialSlotSlotView == null && bestModSlotView == null && bestSearchButton == null) return;

            currentGridView = bestGridView;
            currentModSlotView = bestModSlotView;
            currentTradingTableGridView = bestTradingTableGridView;
            currentContainedGridsView = bestContainedGridsView;
            currentItemSpecificationPanel = bestItemSpecificationPanel;
            currentEquipmentSlotView = bestEquipmentSlotView;
            currentWeaponsSlotView = bestWeaponsSlotView;
            currentArmbandSlotView = bestArmbandSlotView;
            currentContainersSlotView = bestContainersSlotView;
            currentDogtagSlotView = bestDogtagSlotView;
            currentSpecialSlotSlotView = bestSpecialSlotSlotView;
            currentSearchButton = bestSearchButton;
            gridViewLocation = bestGridViewLocation;
            UpdateGlobalPosition();
            FindGridWindow(globalPosition);
            // OnMove
            ControllerUIOnMove(direction, globalPosition);
        }
        public void ControllerUIOnMove(Vector2Int direction, Vector2 position)
        {
            AmandsControllerOnPointerMove();
            AmandsControllerOnDrag();
            FindScrollRectNoDrang(position);
        }
        public void ControllerUIMoveSnapshot()
        {
            snapshotGridView = currentGridView;
            snapshotModSlotView = currentModSlotView;
            snapshotTradingTableGridView = currentTradingTableGridView;
            snapshotContainedGridsView = currentContainedGridsView;
            snapshotItemSpecificationPanel = currentItemSpecificationPanel;
            snapshotEquipmentSlotView = currentEquipmentSlotView;
            snapshotWeaponsSlotView = currentWeaponsSlotView;
            snapshotArmbandSlotView = currentArmbandSlotView;
            snapshotContainersSlotView = currentContainersSlotView;
            snapshotDogtagSlotView = currentDogtagSlotView;
            snapshotSpecialSlotSlotView = currentSpecialSlotSlotView;
            snapshotSearchButton = currentSearchButton;
            SnapshotGridViewLocation = gridViewLocation;
        }
        public void ControllerUIMoveToSnapshot()
        {
            currentGridView = snapshotGridView;
            currentModSlotView = snapshotModSlotView;
            currentTradingTableGridView = snapshotTradingTableGridView;
            currentContainedGridsView = snapshotContainedGridsView;
            currentItemSpecificationPanel = snapshotItemSpecificationPanel;
            currentEquipmentSlotView = snapshotEquipmentSlotView;
            currentWeaponsSlotView = snapshotWeaponsSlotView;
            currentArmbandSlotView = snapshotArmbandSlotView;
            currentContainersSlotView = snapshotContainersSlotView;
            currentDogtagSlotView = snapshotDogtagSlotView;
            currentSpecialSlotSlotView = snapshotSpecialSlotSlotView;
            currentSearchButton = snapshotSearchButton;
            gridViewLocation = SnapshotGridViewLocation;
            UpdateGlobalPosition();
            FindGridWindow(globalPosition);
            // OnMove
            ControllerUIOnMove(Vector2Int.zero, globalPosition);
        }
        public void UpdateGlobalPosition()
        {
            if (currentSimpleContextMenuButton != null)
            {
                globalPosition.x = currentSimpleContextMenuButton.transform.position.x;
                globalPosition.y = currentSimpleContextMenuButton.transform.position.y;
                return;
            }
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
            else if (currentDogtagSlotView != null)
            {
                globalPosition = new Vector2(currentDogtagSlotView.transform.position.x - GridSize - (GridSize / 2f), currentDogtagSlotView.transform.position.y - (GridSize / 2f));
            }
            else if (currentSpecialSlotSlotView != null)
            {
                globalPosition.x = currentSpecialSlotSlotView.transform.position.x + (GridSize / 2f);
                globalPosition.y = currentSpecialSlotSlotView.transform.position.y - (GridSize / 2f);
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
        public void ControllerUISelect()
        {
            ResetAllCurrent();
            gridViewLocation = Vector2Int.one;
            UpdateGlobalPosition();
            FindGridWindow(globalPosition);
            ControllerUIOnMove(Vector2Int.zero, globalPosition);
        }
        public void ControllerUISelect(GridView gridView, ItemView itemView)
        {
            ResetAllCurrent();
            currentGridView = gridView;
            gridViewLocation = AmandsControllerCalculateItemLocation(gridView, itemView) + Vector2Int.one;
            UpdateGlobalPosition();
            FindGridWindow(globalPosition);
            ControllerUIOnMove(Vector2Int.zero, globalPosition);
        }
        public void ControllerUISelect(GridView gridView)
        {
            ResetAllCurrent();
            currentGridView = gridView;
            gridViewLocation = Vector2Int.one;
            UpdateGlobalPosition();
            FindGridWindow(globalPosition);
            ControllerUIOnMove(Vector2Int.zero, globalPosition);
        }
        public void ControllerUISelect(SimpleContextMenuButton simpleContextMenuButton)
        {
            if (currentSimpleContextMenuButton != null)
            {
                currentSimpleContextMenuButton.OnPointerExit(null);
            }
            currentSimpleContextMenuButton = simpleContextMenuButton;
            UpdateGlobalPosition();
            currentSimpleContextMenuButton.OnPointerEnter(null);
        }
        //public LocationInGrid AmandsControllerCalculateItemLocation(GridView gridView, ItemView itemView)
        public Vector2Int AmandsControllerCalculateItemLocation(GridView gridView, ItemView itemView)
        {
            int GridWidth = Traverse.Create(Traverse.Create(gridView).Field("Grid").GetValue<object>()).Property("GridWidth").GetValue<IBindable<int>>().Value;
            int GridHeight = Traverse.Create(Traverse.Create(gridView).Field("Grid").GetValue<object>()).Property("GridHeight").GetValue<IBindable<int>>().Value;

            RectTransform rectTransform = gridView.transform.GetComponent<RectTransform>();
            Vector2 size = rectTransform.rect.size;
            Vector2 pivot = rectTransform.pivot;
            Vector2 b = size * pivot;
            Vector2 vector = rectTransform.InverseTransformPoint((Vector2)itemView.transform.position + new Vector2(0f,-64f));
            vector += b;

            //GStruct23 gstruct = itemView.Item.CalculateRotatedSize(itemView.ItemRotation);
            object gstruct23 = CalculateRotatedSize.Invoke(itemView.Item, new object[1] { itemView.ItemRotation });

            vector /= 63f;
            vector.y = (float)GridHeight - vector.y;

            //vector.y -= (float)gstruct.Y;
            vector.y -= (float)Traverse.Create(gstruct23).Field("Y").GetValue<int>();

            //return new LocationInGrid(Mathf.Clamp(Mathf.RoundToInt(vector.x), 0, GridWidth), Mathf.Clamp(Mathf.RoundToInt(vector.y), 0, GridHeight), itemView.ItemRotation);
            return new Vector2Int(Mathf.Clamp(Mathf.RoundToInt(vector.x), 0, GridWidth), Mathf.Clamp(Mathf.RoundToInt(vector.y), 0, GridHeight));
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
        public void AmandsControllerOnPointerMove()
        {
            if (pointerEventData != null)
            {
                pointerEventData.position = globalPosition;
                if (onPointerEnterItemView != null && onPointerEnterItemView.gameObject.activeSelf)
                {
                    onPointerEnterItemView.OnPointerExit(pointerEventData);
                }
                List<RaycastResult> results = new List<RaycastResult>();
                eventSystem.RaycastAll(pointerEventData, results);
                if (results.Count > 0)
                {
                    pointerEventData.pointerEnter = results[0].gameObject;
                    onPointerEnterItemView = results[0].gameObject.GetComponentInParent<ItemView>();
                    if (onPointerEnterItemView == null)
                    {
                        onPointerEnterItemView = results[0].gameObject.GetComponentInParent<GridItemView>();
                    }
                    if (onPointerEnterItemView == null)
                    {
                        onPointerEnterItemView = results[0].gameObject.GetComponentInParent<SlotItemView>();
                    }
                    if (onPointerEnterItemView != null && onPointerEnterItemView.gameObject.activeSelf)
                    {
                        onPointerEnterItemView.OnPointerEnter(pointerEventData);
                    }
                }
                else
                {
                    pointerEventData.pointerEnter = null;
                    onPointerEnterItemView = null;
                }
            }
        }
        public void AmandsControllerUse(bool Hold)
        {
            if (onPointerEnterItemView == null || (onPointerEnterItemView != null && !onPointerEnterItemView.gameObject.activeSelf))
            {
                AmandsControllerOnPointerMove();
            }
            if (pointerEventData != null && onPointerEnterItemView != null && onPointerEnterItemView.gameObject.activeSelf)
            {
                pointerEventData.position = globalPosition;
                ItemUiContext ItemUiContext = ItemUiContext.Instance;//Traverse.Create(onPointerEnterItemView).Field("ItemUiContext").GetValue<ItemUiContext>();
                object NewContextInteractionsObject = Traverse.Create(onPointerEnterItemView).Property("NewContextInteractions").GetValue();
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
                if (!onPointerEnterItemView.IsSearched && ExecuteMiddleClick != null && (bool)ExecuteMiddleClick.Invoke(onPointerEnterItemView, null)) return;
                if (ItemUiContext == null || !onPointerEnterItemView.IsSearched) return;
                TraderControllerClass ItemController = Traverse.Create(onPointerEnterItemView).Field("ItemController").GetValue<TraderControllerClass>();
                if (ExecuteInteraction != null && IsInteractionAvailable != null)
                {
                    if (onPointerEnterItemView.Item is FoodClass || onPointerEnterItemView.Item is MedsClass)
                    {
                        ExecuteInteractionInvokeParameters[0] = EItemInfoButton.Use;
                        if (!(bool)ExecuteInteraction.Invoke(NewContextInteractionsObject, ExecuteInteractionInvokeParameters))
                        {
                            ExecuteInteractionInvokeParameters[0] = EItemInfoButton.UseAll;
                            ExecuteInteraction.Invoke(NewContextInteractionsObject, ExecuteInteractionInvokeParameters);
                        }
                        return;
                    }
                    if (onPointerEnterItemView.Item.IsContainer && !Hold)
                    {
                        ExecuteInteractionInvokeParameters[0] = EItemInfoButton.Open;
                        if ((bool)ExecuteInteraction.Invoke(NewContextInteractionsObject, ExecuteInteractionInvokeParameters)) return;
                    }
                    //ExecuteInteractionInvokeParameters[0] = onPointerEnterItemView.Item.IsContainer ? EItemInfoButton.Open : EItemInfoButton.Inspect;
                    //if (onPointerEnterItemView.Item.IsContainer && (bool)ExecuteInteraction.Invoke(NewContextInteractionsObject, ExecuteInteractionInvokeParameters)) return;
                    if (Hold && ExecuteMiddleClick != null && (bool)ExecuteMiddleClick.Invoke(onPointerEnterItemView, null)) return;
                    SimpleTooltip tooltip = ItemUiContext.Tooltip;
                    IsInteractionAvailableInvokeParameters[0] = EItemInfoButton.Equip;
                    if ((bool)IsInteractionAvailable.Invoke(NewContextInteractionsObject, IsInteractionAvailableInvokeParameters))
                    {
                        ItemUiContext.QuickEquip(onPointerEnterItemView.Item).HandleExceptions();
                        if (tooltip != null)
                        {
                            tooltip.Close();
                        }
                        AmandsControllerOnPointerMove();
                        return;
                    }
                    else
                    {
                        bool IsBeingLoadedMagazine = Traverse.Create(Traverse.Create(onPointerEnterItemView).Property("IsBeingLoadedMagazine").GetValue<object>()).Field("gparam_0").GetValue<bool>();
                        bool IsBeingUnloadedMagazine = Traverse.Create(Traverse.Create(onPointerEnterItemView).Property("IsBeingUnloadedMagazine").GetValue<object>()).Field("gparam_0").GetValue<bool>();
                        if (IsBeingLoadedMagazine || IsBeingUnloadedMagazine)
                        {
                            ItemController.StopProcesses();
                            return;
                        }
                    }
                    ExecuteInteractionInvokeParameters[0] = EItemInfoButton.CheckMagazine;
                    if ((bool)ExecuteInteraction.Invoke(NewContextInteractionsObject, ExecuteInteractionInvokeParameters)) return;
                    if (ExecuteMiddleClick != null && (bool)ExecuteMiddleClick.Invoke(onPointerEnterItemView, null)) return;
                }
            }
        }
        public void AmandsControllerQuickMove()
        {
            if (onPointerEnterItemView == null || (onPointerEnterItemView != null && !onPointerEnterItemView.gameObject.activeSelf))
            {
                AmandsControllerOnPointerMove();
            }
            if (pointerEventData != null && onPointerEnterItemView != null && onPointerEnterItemView.gameObject.activeSelf)
            {
                pointerEventData.position = globalPosition;
                ItemUiContext ItemUiContext = ItemUiContext.Instance;//Traverse.Create(onPointerEnterItemView).Field("ItemUiContext").GetValue<ItemUiContext>();
                if (ItemUiContext == null || !onPointerEnterItemView.IsSearched) return;
                TraderControllerClass ItemController = Traverse.Create(onPointerEnterItemView).Field("ItemController").GetValue<TraderControllerClass>();
                SimpleTooltip tooltip = ItemUiContext.Tooltip;
                //GStruct374 gstruct = ItemUiContext.QuickFindAppropriatePlace(onPointerEnterItemView.ItemContext, ItemController, false, true, true);
                object ItemContext = Traverse.Create(onPointerEnterItemView).Property("ItemContext").GetValue<object>();
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
                            string itemSound = onPointerEnterItemView.Item.ItemSound;
                            RunNetworkTransaction.Invoke(ItemController, new object[2] { Value, null });
                            if (tooltip != null)
                            {
                                tooltip.Close();
                            }
                            Singleton<GUISounds>.Instance.PlayItemSound(itemSound, EInventorySoundType.pickup, false);
                            AmandsControllerOnPointerMove();
                        }
                    }
                }
            }
        }
        public void AmandsControllerDiscard()
        {
            if (onPointerEnterItemView == null || (onPointerEnterItemView != null && !onPointerEnterItemView.gameObject.activeSelf))
            {
                AmandsControllerOnPointerMove();
            }
            if (pointerEventData != null && onPointerEnterItemView != null && onPointerEnterItemView.gameObject.activeSelf)
            {
                pointerEventData.position = globalPosition;
                ItemUiContext ItemUiContext = ItemUiContext.Instance;//Traverse.Create(onPointerEnterItemView).Field("ItemUiContext").GetValue<ItemUiContext>();
                if (ItemUiContext == null || !onPointerEnterItemView.IsSearched) return;
                object NewContextInteractionsObject = Traverse.Create(onPointerEnterItemView).Property("NewContextInteractions").GetValue();
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
                        ItemUiContext.ThrowItem(onPointerEnterItemView.Item).HandleExceptions();
                        if (tooltip != null)
                        {
                            tooltip.Close();
                        }
                        AmandsControllerOnPointerMove();
                        return;
                    }
                }
            }
        }
        public void AmandsControllerBeginDrag()
        {
            if (onPointerEnterItemView == null || (onPointerEnterItemView != null && !onPointerEnterItemView.gameObject.activeSelf))
            {
                AmandsControllerOnPointerMove();
            }
            if (!Dragging && pointerEventData != null && onPointerEnterItemView != null && onPointerEnterItemView.gameObject.activeSelf)
            {
                TraderControllerClass ItemController = Traverse.Create(onPointerEnterItemView).Field("ItemController").GetValue<TraderControllerClass>();
                if (ItemController != null)
                {
                    bool IsBeingLoadedMagazine = Traverse.Create(Traverse.Create(onPointerEnterItemView).Property("IsBeingLoadedMagazine").GetValue<object>()).Field("gparam_0").GetValue<bool>();
                    bool IsBeingUnloadedMagazine = Traverse.Create(Traverse.Create(onPointerEnterItemView).Property("IsBeingUnloadedMagazine").GetValue<object>()).Field("gparam_0").GetValue<bool>();
                    if (IsBeingLoadedMagazine || IsBeingUnloadedMagazine)
                    {
                        ItemController.StopProcesses();
                        return;
                    }
                }
                ControllerUIMoveSnapshot();
                pointerEventData.position = globalPosition;
                pointerEventData.dragging = false;
                DraggingItemView = onPointerEnterItemView;
                DraggingItemView.OnBeginDrag(pointerEventData);
                pointerEventData.dragging = true;
                Dragging = true;
                if (AmandsControllerSets.ContainsKey("OnDrag") && !ActiveAmandsControllerSets.Contains("OnDrag"))
                {
                    ActiveAmandsControllerSets.Add("OnDrag");
                }
                AmandsControllerOnPointerMove();
                AmandsControllerOnDrag();
            }
        }
        public void AmandsControllerOnDrag()
        {
            if (Dragging && pointerEventData != null)
            {
                pointerEventData.position = globalPosition;
                if (DraggingItemView != null && DraggingItemView.gameObject.activeSelf && DraggingItemView.BeingDragged)
                {
                    DraggingItemView.OnDrag(pointerEventData);
                }
                else if ((DraggingItemView != null && !DraggingItemView.BeingDragged) || !DraggingItemView.gameObject.activeSelf)
                {
                    AmandsControllerCancelDrag();
                }
            }
        }
        public void AmandsControllerEndDrag()
        {
            if (Dragging && pointerEventData != null)
            {
                pointerEventData.position = globalPosition;
                if (pointerEventData.pointerEnter != null)
                {
                    Dragging = false;
                    ActiveAmandsControllerSets.Remove("OnDrag");
                    pointerEventData.dragging = false;
                    if (DraggingItemView != null && DraggingItemView.gameObject.activeSelf)
                    {
                        DraggingItemView.OnEndDrag(pointerEventData);
                        DraggingItemView = null;
                        AmandsControllerOnPointerMove();
                    }
                }
                else
                {
                    AmandsControllerCancelDrag();
                }
            }
        }
        public void AmandsControllerCancelDrag()
        {
            if (Dragging)
            {
                Dragging = false;
                ActiveAmandsControllerSets.Remove("OnDrag");
                PointerEventData pointerEventData = new PointerEventData(eventSystem);
                pointerEventData.button = PointerEventData.InputButton.Left;
                pointerEventData.position = globalPosition;
                pointerEventData.pointerEnter = null;
                pointerEventData.dragging = false;
                if (DraggingItemView != null && DraggingItemView.gameObject.activeSelf)
                {
                    DraggingItemView.OnEndDrag(pointerEventData);
                    DraggingItemView = null;
                    ControllerUIMoveToSnapshot();
                }
            }
        }
        public void AmandsControllerRotateDragged()
        {
            if (Dragging && DraggingItemView != null && DraggingItemView.gameObject.activeSelf)
            {
                DraggedItemView DraggedItemView = Traverse.Create(DraggingItemView).Property("DraggedItemView").GetValue<DraggedItemView>();
                if (DraggedItemView != null)
                {
                    object ItemContext = Traverse.Create(DraggedItemView).Property("ItemContext").GetValue<object>();
                    if (ItemContext != null)
                    {
                        ItemRotation ItemRotation = Traverse.Create(ItemContext).Field("ItemRotation").GetValue<ItemRotation>();
                        DraggedItemViewMethod_2.Invoke(DraggedItemView, new object[1] { (ItemRotation == ItemRotation.Horizontal ? ItemRotation.Vertical : ItemRotation.Horizontal) });
                        AmandsControllerOnDrag();
                    }
                }
            }
        }
        public void AmandsControllerSplitDragged()
        {
            if (Dragging && pointerEventData != null)
            {
                AmandsControllerPlugin.LeftControl.Enable();
                pointerEventData.position = globalPosition;
                if (pointerEventData.pointerEnter != null)
                {
                    Dragging = false;
                    ActiveAmandsControllerSets.Remove("OnDrag");
                    pointerEventData.dragging = false;
                    if (DraggingItemView != null && DraggingItemView.gameObject.activeSelf)
                    {
                        DraggingItemView.OnEndDrag(pointerEventData);
                        DraggingItemView = null;
                        AmandsControllerOnPointerMove();
                    }
                }
                else
                {
                    AmandsControllerCancelDrag();
                }
                AmandsControllerPlugin.LeftControl.Disable();
            }
        }
        public void AmandsControllerInterfaceBind(EBoundItem bindIndex)
        {
            if (onPointerEnterItemView == null || (onPointerEnterItemView != null && !onPointerEnterItemView.gameObject.activeSelf))
            {
                AmandsControllerOnPointerMove();
            }
            if (pointerEventData != null && onPointerEnterItemView != null && onPointerEnterItemView.gameObject.activeSelf)
            {
                pointerEventData.position = globalPosition;
                ItemUiContext ItemUiContext = ItemUiContext.Instance;//Traverse.Create(onPointerEnterItemView).Field("ItemUiContext").GetValue<ItemUiContext>();
                if (ItemUiContext != null && onPointerEnterItemView.Item != null && ItemUIContextMethod_0 != null)
                {
                    ItemUIContextMethod_0InvokeParameters[0] = onPointerEnterItemView.Item;
                    ItemUIContextMethod_0InvokeParameters[1] = bindIndex;
                    ItemUIContextMethod_0.Invoke(ItemUiContext, ItemUIContextMethod_0InvokeParameters);
                }
                /*pointerEventData.position = globalPosition;
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
                        if (ItemUiContext != null && itemView.Item != null && ItemUIContextMethod_0 != null)
                        {
                            ItemUIContextMethod_0InvokeParameters[0] = itemView.Item;
                            ItemUIContextMethod_0InvokeParameters[1] = bindIndex;
                            ItemUIContextMethod_0.Invoke(ItemUiContext, ItemUIContextMethod_0InvokeParameters);
                        }
                    }
                }*/
            }
        }
        public void AmandsControllerShowContextMenu()
        {
            if (!ContextMenu && onPointerEnterItemView != null && onPointerEnterItemView.gameObject.activeSelf)
            {
                UpdateGlobalPosition();
                ShowContextMenuInvokeParameters[0] = globalPosition;
                ShowContextMenu.Invoke(onPointerEnterItemView, ShowContextMenuInvokeParameters);
            }
        }
        public void AmandsControllerContextMenuUse()
        {
            if (currentSimpleContextMenuButton != null)
            {
                Button _button = Traverse.Create(currentSimpleContextMenuButton).Field("_button").GetValue<Button>();
                if (_button != null)
                {
                    _button.onClick.Invoke();
                }
            }
        }
        public void AmandsControllerSplitDialogAccept()
        {
            if (splitDialog != null)
            {
                splitDialog.Accept();
            }
        }
        public void AmandsControllerSplitDialogAdd(int Value)
        {
            if (splitDialog != null)
            {
                IntSlider _intSlider = Traverse.Create(splitDialog).Field("_intSlider").GetValue<IntSlider>();
                if (_intSlider != null && _intSlider.gameObject.activeSelf)
                {
                    lastIntSliderValue = Value;
                    SplitDialogAutoMove = true;
                    int int_1 = Traverse.Create(_intSlider).Field("int_1").GetValue<int>() - 1;
                    _intSlider.UpdateValue((_intSlider.CurrentValue() + int_1) + ((LB || RB) ? Value * 10 : Value));
                }
                StepSlider _stepSlider = Traverse.Create(splitDialog).Field("_stepSlider").GetValue<StepSlider>();
                if (_stepSlider != null && _stepSlider.gameObject.activeSelf)
                {
                    lastIntSliderValue = Value;
                    SplitDialogAutoMove = true;
                    //Temp
                    int int_0 = Traverse.Create(_stepSlider).Field("int_0").GetValue<int>();
                    int int_1 = Traverse.Create(_stepSlider).Field("int_1").GetValue<int>();
                    _stepSlider.Show(int_0, int_1, (int)_stepSlider.CurrentValue() + ((LB || RB) ? Value * 10 : Value));
                }
            }
        }
        public void AmandsControllerScroll(float Value)
        {
            float height = currentScrollRectNoDrag.content.rect.height;
            Vector2 position = new Vector2(currentScrollRectNoDragRectTransform.position.x + currentScrollRectNoDragRectTransform.rect.x, currentScrollRectNoDragRectTransform.position.y - (currentScrollRectNoDragRectTransform.rect.height * (currentScrollRectNoDragRectTransform.pivot.y - 1f)));
            currentScrollRectNoDrag.verticalNormalizedPosition = currentScrollRectNoDrag.verticalNormalizedPosition + ((((Value * 1000f) / height) / height) * 10000f * Time.deltaTime * AmandsControllerPlugin.ScrollSensitivity.Value);
            UpdateGlobalPosition();
            if ((globalPosition.y + (GridSize / 2f)) > position.y)
            {
                ControllerUIMove(new Vector2Int(0, -1), false);
            }
            else if ((globalPosition.y - (GridSize / 2f)) < (position.y - (currentScrollRectNoDragRectTransform.rect.height * ScreenRatio)))
            {
                ControllerUIMove(new Vector2Int(0, 1), false);
            }
        }
        public void AmandsControllerAutoScroll()
        {
            float height = currentScrollRectNoDrag.content.rect.height;
            Vector2 position = new Vector2(currentScrollRectNoDragRectTransform.position.x + currentScrollRectNoDragRectTransform.rect.x, currentScrollRectNoDragRectTransform.position.y - (currentScrollRectNoDragRectTransform.rect.height * (currentScrollRectNoDragRectTransform.pivot.y - 1f)));
            if (globalPosition.x > position.x && globalPosition.x < (position.x + (currentScrollRectNoDragRectTransform.rect.width * ScreenRatio)))
            {
                if ((globalPosition.y + (GridSize / 2f)) > position.y)
                {
                    currentScrollRectNoDrag.verticalNormalizedPosition = currentScrollRectNoDrag.verticalNormalizedPosition + (((1000f / height) / height) * 10000f * Time.deltaTime * AmandsControllerPlugin.ScrollSensitivity.Value);
                    UpdateGlobalPosition();
                }
                else if ((globalPosition.y - (GridSize / 2f)) < (position.y - (currentScrollRectNoDragRectTransform.rect.height * ScreenRatio)))
                {
                    currentScrollRectNoDrag.verticalNormalizedPosition = currentScrollRectNoDrag.verticalNormalizedPosition + (((-1000f / height) / height) * 10000f * Time.deltaTime * AmandsControllerPlugin.ScrollSensitivity.Value);
                    UpdateGlobalPosition();
                }
            }
        }
        public void DebugStuff(string stuff)
        {
            //ConsoleScreen.Log(stuff);
        }
    }
    public struct AmandsControllerCommand
    {
        public EAmandsControllerCommand Command;
        public ECommand InputTree;
        public string AmandsControllerSet;
        public AmandsControllerCommand(EAmandsControllerCommand Command, ECommand InputTree, string AmandsControllerSet)
        {
            this.Command = Command;
            this.InputTree = InputTree;
            this.AmandsControllerSet = AmandsControllerSet;
        }
        public AmandsControllerCommand(EAmandsControllerCommand Command, ECommand InputTree)
        {
            this.Command = Command;
            this.InputTree = InputTree;
            this.AmandsControllerSet = "";
        }
        public AmandsControllerCommand(EAmandsControllerCommand Command, string AmandsControllerSet)
        {
            this.Command = Command;
            this.InputTree = ECommand.None;
            this.AmandsControllerSet = AmandsControllerSet;
        }
        public AmandsControllerCommand(EAmandsControllerCommand Command)
        {
            this.Command = Command;
            this.InputTree = ECommand.None;
            this.AmandsControllerSet = "";
        }
        public AmandsControllerCommand(ECommand InputTree)
        {
            this.Command = EAmandsControllerCommand.InputTree;
            this.InputTree = InputTree;
            this.AmandsControllerSet = "";
        }
    }

    public struct AmandsControllerButtonBind
    {
        public List<AmandsControllerCommand> AmandsControllerCommands;
        public EAmandsControllerPressType PressType;
        public int Priority;

        public AmandsControllerButtonBind(List<AmandsControllerCommand> AmandsControllerCommands, EAmandsControllerPressType PressType, int Priority)
        {
            this.AmandsControllerCommands = AmandsControllerCommands;
            this.PressType = PressType;
            this.Priority = Priority;
        }
        public AmandsControllerButtonBind(AmandsControllerCommand AmandsControllerCommands, EAmandsControllerPressType PressType, int Priority)
        {
            this.AmandsControllerCommands = new List<AmandsControllerCommand> { AmandsControllerCommands };
            this.PressType = PressType;
            this.Priority = Priority;
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
        LB = 4,
        RB = 5,
        LT = 6,
        RT = 7,
        LS = 8,
        RS = 9,
        UP = 10,
        DOWN = 11,
        LEFT = 12,
        RIGHT = 13,
        LSUP = 14,
        LSDOWN = 15,
        LSLEFT = 16,
        LSRIGHT = 17,
        RSUP = 18,
        RSDOWN = 19,
        RSLEFT = 20,
        RSRIGHT = 21,
        BACK = 24,
        MENU = 25,
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
        None = 0,
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
        Discard = 21,
        InterfaceBind4 = 22,
        InterfaceBind5 = 23,
        InterfaceBind6 = 24,
        InterfaceBind7 = 25,
        InterfaceBind8 = 26,
        InterfaceBind9 = 27,
        InterfaceBind10 = 28,
        ShowContextMenu = 29,
        ContextMenuUse = 30,
        SplitDialogAccept = 31,
        SplitDialogAdd = 32,
        SplitDialogSubtract = 33,
        SplitDialogDisableAutoMove = 34,
        RotateDragged = 35,
        CancelDrag = 36,
        SplitDragged = 37
    }
    public enum EAmandsControllerUseStick
    {
        None = 0,
        LS = 1,
        RS = 2
    }
    /*public enum EAmandsControllerStickMode
    {
        None = 0,
        Movement = 1,
        Look = 2,
        Freelook = 3,
        Interface = 4,
        InterfaceSkip = 5,
        Buttons = 6,
        Scroll = 7
    }*/
}
