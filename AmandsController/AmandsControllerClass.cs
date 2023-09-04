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
        private object[] InvokeParameters = new object[3] { new List<ECommand>(), null, ECursorResult.Ignore };

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

        AmandsControllerButtonBind EmptyBind = new AmandsControllerButtonBind();
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

        public LocalPlayer AimAssistLocalPlayer = null;
        public LocalPlayer HitAimAssistLocalPlayer = null;
        public float AimAssistLocalPlayerDistance = 1000000.0f;
        public float Angle;
        public float AngleMin;
        public float AngleMax;
        public float AimAssistStrength;
        public float AimAssistStrengthSmooth;
        public float AimAssistStrengthSmoothChange;

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
            GUILayout.Label("AimAssistStrength " + AimAssistStrength.ToString());*/
            if (AimAssistStrengthSmooth > 0.01)
            {
                GUILayout.Label("AimAssist " + AimAssistStrengthSmooth.ToString());
            }
            GUILayout.EndArea();
        }
        public void Start()
        {
            TranslateInput = typeof(InputTree).GetMethod("TranslateInput", BindingFlags.Instance | BindingFlags.NonPublic);

            controller = new Controller(UserIndex.One);
            connected = controller.IsConnected;

            AimAnimationCurve.keys = AimKeys;
    }
        public void Update()
        {
            if (!connected) return;

            gamepad = controller.GetState().Gamepad;

            if (localPlayer == null) return;

            if (firearmController == null)
            {
                firearmController = localPlayer.gameObject.GetComponent<FirearmController>();
            }

            //AimAssistPlayers.Clear();
            if (localPlayer != null && Camera.main != null)
            {

                AngleMin = 90.0f - AmandsControllerPlugin.AngleMin.Value;
                AngleMax = 90.0f - AmandsControllerPlugin.AngleMax.Value;

                Vector3 position = Vector3.one;
                Vector3 direction = Vector3.forward;

                if (firearmController != null)
                {
                    position = firearmController.CurrentFireport.position;
                    direction = firearmController.WeaponDirection;
                    firearmController.AdjustShotVectors(ref position, ref direction);
                }

                AimAssistStrength = 0f;
                colliders = new Collider[100];
                colliderCount = Physics.OverlapCapsuleNonAlloc(position, position + (direction * 1000f), AmandsControllerPlugin.Radius.Value, colliders, AimAssistLayerMask, QueryTriggerInteraction.Ignore);
                AimAssistLocalPlayer = null;
                HitAimAssistLocalPlayer = null;
                AimAssistLocalPlayerDistance = 1000000.0f;
                for (int i = 0; i < colliderCount; i++)
                {
                    HitAimAssistLocalPlayer = colliders[i].transform.gameObject.GetComponent<LocalPlayer>();
                    if (HitAimAssistLocalPlayer != null && Vector3.Distance(position, HitAimAssistLocalPlayer.Position) < AimAssistLocalPlayerDistance && HitAimAssistLocalPlayer != localPlayer && (Mathf.Abs(Vector3.Dot((position - HitAimAssistLocalPlayer.PlayerBones.Ribcage.position).normalized, direction)) * 90f) > AngleMin && !Physics.Raycast(position, (HitAimAssistLocalPlayer.PlayerBones.Ribcage.position - position).normalized, out hit, Vector3.Distance(HitAimAssistLocalPlayer.PlayerBones.Ribcage.position, position), HighLayerMask, QueryTriggerInteraction.Ignore))
                    {
                        AimAssistLocalPlayerDistance = Vector3.Distance(position, HitAimAssistLocalPlayer.Position);
                        AimAssistLocalPlayer = HitAimAssistLocalPlayer;
                        Angle = Mathf.Abs(Vector3.Dot((position - HitAimAssistLocalPlayer.PlayerBones.Ribcage.position).normalized, direction)) * 90f;
                    }
                }
                if (AimAssistLocalPlayer != null)
                {
                    if (Angle < AngleMin)
                    {
                        AimAssistStrength = 0f;
                        //AimAssistPlayers.Add(AimAssistLocalPlayer, AimAssistStrength);
                    }
                    else if (Angle > AngleMax)
                    {
                        AimAssistStrength = 1f;
                        //AimAssistPlayers.Add(AimAssistLocalPlayer, AimAssistStrength);
                    }
                    else
                    {
                        AimAssistStrength = (Angle - AngleMin) / (AngleMax - AngleMin);
                        //AimAssistPlayers.Add(AimAssistLocalPlayer, AimAssistStrength);
                    }

                    //float Angle = Mathf.Abs(Mathf.Tan(Vector3.Angle((HitLocalPlayer.Position + new Vector3(0, AmandsControllerPlugin.TargetHeight.Value, 0)) - position, direction) * Mathf.Deg2Rad) * Vector3.Distance((HitLocalPlayer.Position + new Vector3(0, AmandsControllerPlugin.TargetHeight.Value, 0)), position));

                    //AimAssistStrength = Angle;//Mathf.Lerp(1.0f,0.0f,Mathf.Clamp(Angle, 0.0f, 1.0f));
                }
            }
            AimAssistStrengthSmoothChange = ((AimAssistStrength - AimAssistStrengthSmooth) * AmandsControllerPlugin.SticknessSmooth.Value) * Time.deltaTime;
            if (AimAssistStrengthSmoothChange > 0f)
            {
                AimAssistStrengthSmooth += AimAssistStrengthSmoothChange * 2f;
            }
            else
            {
                AimAssistStrengthSmooth += AimAssistStrengthSmoothChange * 0.5f;
            }

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

            leftThumb.x = (float)gamepad.LeftThumbX / maxValue;
            leftThumb.y = (float)gamepad.LeftThumbY / maxValue;
            rightThumb.x = (float)gamepad.RightThumbX / maxValue;
            rightThumb.y = (float)gamepad.RightThumbY / maxValue;

            leftTrigger = (float)gamepad.LeftTrigger / 255f;
            rightTrigger = (float)gamepad.RightTrigger / 255f;

            leftThumbXYSqrt = Mathf.Sqrt(Mathf.Pow(leftThumb.x, 2) + Mathf.Pow(leftThumb.y, 2));
            rightThumbXYSqrt = Mathf.Sqrt(Mathf.Pow(rightThumb.x, 2) + Mathf.Pow(rightThumb.y, 2));
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

            if (rightThumbXYSqrt > AmandsControllerPlugin.RDeadzone.Value)
            {
                Aim.x = rightThumb.x * AimAnimationCurve.Evaluate(rightThumbXYSqrt);
                Aim.y = rightThumb.y * AimAnimationCurve.Evaluate(rightThumbXYSqrt);
                localPlayer.Rotate(Aim * AmandsControllerPlugin.Sensitivity.Value * Time.deltaTime * 100f * Mathf.Lerp(1f, AmandsControllerPlugin.Stickness.Value, AimAssistStrengthSmooth), false);
            }

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
            if (SlowLeanLeft || SlowLeanRight)
            {
                localPlayer.SlowLean(((SlowLeanLeft ? -AmandsControllerPlugin.LeanSensitivity.Value: 0) + (SlowLeanRight ? AmandsControllerPlugin.LeanSensitivity.Value : 0)) * Time.deltaTime);
            }
        }
        public void UpdateController(LocalPlayer Player)
        {
            localPlayer = Player;
            //movementContext = localPlayer.MovementContext;

            AmandsControllerSets.Add("LB", new Dictionary<EAmandsControllerButton, List<AmandsControllerButtonBind>>());
            AmandsControllerSets["LB"].Add(EAmandsControllerButton.UP, new List<AmandsControllerButtonBind> { new AmandsControllerButtonBind(ECommand.ThrowGrenade, EAmandsControllerCommand.InputTree, EAmandsControllerPressType.Press, 2, "") });
            AmandsControllerSets["LB"].Add(EAmandsControllerButton.DOWN, new List<AmandsControllerButtonBind> { new AmandsControllerButtonBind(ECommand.SelectSecondaryWeapon, EAmandsControllerCommand.InputTree, EAmandsControllerPressType.Press, 2, "") });
            AmandsControllerSets["LB"][EAmandsControllerButton.DOWN].Add(new AmandsControllerButtonBind(ECommand.QuickSelectSecondaryWeapon, EAmandsControllerCommand.InputTree, EAmandsControllerPressType.DoubleClick, 2, ""));
            AmandsControllerSets["LB"].Add(EAmandsControllerButton.LEFT, new List<AmandsControllerButtonBind> { new AmandsControllerButtonBind(ECommand.SelectFirstPrimaryWeapon, EAmandsControllerCommand.InputTree, EAmandsControllerPressType.Press, 2, "") });
            AmandsControllerSets["LB"].Add(EAmandsControllerButton.RIGHT, new List<AmandsControllerButtonBind> { new AmandsControllerButtonBind(ECommand.SelectSecondPrimaryWeapon, EAmandsControllerCommand.InputTree, EAmandsControllerPressType.Press, 2, "") });

            AmandsControllerSets["LB"].Add(EAmandsControllerButton.A, new List<AmandsControllerButtonBind> { new AmandsControllerButtonBind(ECommand.SelectFastSlot4, EAmandsControllerCommand.InputTree, EAmandsControllerPressType.Press, 2, "") });
            AmandsControllerSets["LB"].Add(EAmandsControllerButton.B, new List<AmandsControllerButtonBind> { new AmandsControllerButtonBind(ECommand.SelectFastSlot5, EAmandsControllerCommand.InputTree, EAmandsControllerPressType.Press, 2, "") });
            AmandsControllerSets["LB"].Add(EAmandsControllerButton.X, new List<AmandsControllerButtonBind> { new AmandsControllerButtonBind(ECommand.SelectFastSlot6, EAmandsControllerCommand.InputTree, EAmandsControllerPressType.Press, 2, "") });
            AmandsControllerSets["LB"].Add(EAmandsControllerButton.Y, new List<AmandsControllerButtonBind> { new AmandsControllerButtonBind(ECommand.SelectFastSlot7, EAmandsControllerCommand.InputTree, EAmandsControllerPressType.Press, 2, "") });

            AmandsControllerSets["LB"].Add(EAmandsControllerButton.LeftThumb, new List<AmandsControllerButtonBind> { new AmandsControllerButtonBind(ECommand.DropBackpack, EAmandsControllerCommand.InputTree, EAmandsControllerPressType.Press, 2, "") });

            AmandsControllerSets["LB"].Add(EAmandsControllerButton.BACK, new List<AmandsControllerButtonBind> { new AmandsControllerButtonBind(ECommand.ToggleGoggles, EAmandsControllerCommand.InputTree, EAmandsControllerPressType.Press, 2, "") });

            AmandsControllerSets.Add("RB", new Dictionary<EAmandsControllerButton, List<AmandsControllerButtonBind>>());
            //AmandsControllerSets["RB"].Add(EAmandsControllerButton.A, new List<AmandsControllerButtonBind> { new AmandsControllerButtonBind(ECommand.DropBackpack, EAmandsControllerCommand.GamePlayerOwner, EAmandsControllerPressType.Press, 1, "") });
            AmandsControllerSets["RB"].Add(EAmandsControllerButton.X, new List<AmandsControllerButtonBind> { new AmandsControllerButtonBind(ECommand.ChamberUnload, EAmandsControllerCommand.InputTree, EAmandsControllerPressType.Press, 1, "") });
            AmandsControllerSets["RB"][EAmandsControllerButton.X].Add(new AmandsControllerButtonBind(ECommand.CheckChamber, EAmandsControllerCommand.InputTree, EAmandsControllerPressType.Hold, 1, "") );
            AmandsControllerSets["RB"][EAmandsControllerButton.X].Add(new AmandsControllerButtonBind(ECommand.UnloadMagazine, EAmandsControllerCommand.InputTree, EAmandsControllerPressType.DoubleClick, 1, ""));

            AmandsControllerSets["RB"].Add(EAmandsControllerButton.B, new List<AmandsControllerButtonBind> { new AmandsControllerButtonBind(ECommand.None, EAmandsControllerCommand.EnableSet, EAmandsControllerPressType.Press, 1, "Movement") });
            AmandsControllerSets["RB"][EAmandsControllerButton.B].Add(new AmandsControllerButtonBind(ECommand.DisplayTimer, EAmandsControllerCommand.DisableSet, EAmandsControllerPressType.Release, 1, "Movement"));

            AmandsControllerSets["RB"].Add(EAmandsControllerButton.Y, new List<AmandsControllerButtonBind> { new AmandsControllerButtonBind(ECommand.FoldStock, EAmandsControllerCommand.InputTree, EAmandsControllerPressType.Press, 1, "") });
            AmandsControllerSets["RB"][EAmandsControllerButton.Y].Add(new AmandsControllerButtonBind(ECommand.CheckChamber, EAmandsControllerCommand.InputTree, EAmandsControllerPressType.Hold, 1, ""));

            AmandsControllerSets["RB"].Add(EAmandsControllerButton.LEFT, new List<AmandsControllerButtonBind> { new AmandsControllerButtonBind(ECommand.ToggleLeanLeft, EAmandsControllerCommand.InputTree, EAmandsControllerPressType.Press, 1, "") });
            AmandsControllerSets["RB"].Add(EAmandsControllerButton.RIGHT, new List<AmandsControllerButtonBind> { new AmandsControllerButtonBind(ECommand.ToggleLeanRight, EAmandsControllerCommand.InputTree, EAmandsControllerPressType.Press, 1, "") });

            AmandsControllerSets["RB"].Add(EAmandsControllerButton.BACK, new List<AmandsControllerButtonBind> { new AmandsControllerButtonBind(ECommand.SwitchHeadLight, EAmandsControllerCommand.InputTree, EAmandsControllerPressType.Press, 1, "") });
            AmandsControllerSets["RB"][EAmandsControllerButton.BACK].Add(new AmandsControllerButtonBind(ECommand.ToggleHeadLight, EAmandsControllerCommand.InputTree, EAmandsControllerPressType.Hold, 1, ""));

            //AmandsControllerSets["RB"].Add(EAmandsControllerButton.LeftThumb, new List<AmandsControllerButtonBind> { new AmandsControllerButtonBind(ECommand., EAmandsControllerCommand.GamePlayerOwner, EAmandsControllerPressType.Press, 1, "") });

            AmandsControllerSets.Add("LB_RB", new Dictionary<EAmandsControllerButton, List<AmandsControllerButtonBind>>());
            AmandsControllerSets["LB_RB"].Add(EAmandsControllerButton.UP, new List<AmandsControllerButtonBind> { new AmandsControllerButtonBind(ECommand.ToggleBlindAbove, EAmandsControllerCommand.InputTree, EAmandsControllerPressType.Press, 3, "") });
            AmandsControllerSets["LB_RB"][EAmandsControllerButton.UP].Add(new AmandsControllerButtonBind(ECommand.BlindShootEnd, EAmandsControllerCommand.InputTree, EAmandsControllerPressType.Release, 3, ""));
            AmandsControllerSets["LB_RB"].Add(EAmandsControllerButton.DOWN, new List<AmandsControllerButtonBind> { new AmandsControllerButtonBind(ECommand.ToggleBlindRight, EAmandsControllerCommand.InputTree, EAmandsControllerPressType.Press, 3, "") });
            AmandsControllerSets["LB_RB"][EAmandsControllerButton.DOWN].Add(new AmandsControllerButtonBind(ECommand.BlindShootEnd, EAmandsControllerCommand.InputTree, EAmandsControllerPressType.Release, 3, ""));
            AmandsControllerSets["LB_RB"].Add(EAmandsControllerButton.LEFT, new List<AmandsControllerButtonBind> { new AmandsControllerButtonBind(ECommand.ToggleStepLeft, EAmandsControllerCommand.InputTree, EAmandsControllerPressType.Press, 3, "") });
            AmandsControllerSets["LB_RB"][EAmandsControllerButton.LEFT].Add(new AmandsControllerButtonBind(ECommand.ReturnFromLeftStep, EAmandsControllerCommand.InputTree, EAmandsControllerPressType.Release, 3, ""));
            AmandsControllerSets["LB_RB"].Add(EAmandsControllerButton.RIGHT, new List<AmandsControllerButtonBind> { new AmandsControllerButtonBind(ECommand.ToggleStepRight, EAmandsControllerCommand.InputTree, EAmandsControllerPressType.Press, 3, "") });
            AmandsControllerSets["LB_RB"][EAmandsControllerButton.RIGHT].Add(new AmandsControllerButtonBind(ECommand.ReturnFromRightStep, EAmandsControllerCommand.InputTree, EAmandsControllerPressType.Release, 3, ""));

            AmandsControllerSets["LB_RB"].Add(EAmandsControllerButton.A, new List<AmandsControllerButtonBind> { new AmandsControllerButtonBind(ECommand.SelectFastSlot8, EAmandsControllerCommand.InputTree, EAmandsControllerPressType.Press, 3, "") });
            AmandsControllerSets["LB_RB"].Add(EAmandsControllerButton.B, new List<AmandsControllerButtonBind> { new AmandsControllerButtonBind(ECommand.SelectFastSlot9, EAmandsControllerCommand.InputTree, EAmandsControllerPressType.Press, 3, "") });
            AmandsControllerSets["LB_RB"].Add(EAmandsControllerButton.X, new List<AmandsControllerButtonBind> { new AmandsControllerButtonBind(ECommand.SelectFastSlot0, EAmandsControllerCommand.InputTree, EAmandsControllerPressType.Press, 3, "") });
            AmandsControllerSets["LB_RB"].Add(EAmandsControllerButton.Y, new List<AmandsControllerButtonBind> { new AmandsControllerButtonBind(ECommand.DisplayTimer, EAmandsControllerCommand.InputTree, EAmandsControllerPressType.Press, 3, "") });
            AmandsControllerSets["LB_RB"][EAmandsControllerButton.Y].Add(new AmandsControllerButtonBind(ECommand.DisplayTimerAndExits, EAmandsControllerCommand.InputTree, EAmandsControllerPressType.DoubleClick, 3, ""));

            AmandsControllerSets.Add("ActionPanel", new Dictionary<EAmandsControllerButton, List<AmandsControllerButtonBind>>());
            AmandsControllerSets["ActionPanel"].Add(EAmandsControllerButton.X, new List<AmandsControllerButtonBind> { new AmandsControllerButtonBind(ECommand.BeginInteracting, EAmandsControllerCommand.InputTree, EAmandsControllerPressType.Press, 10, "") });
            AmandsControllerSets["ActionPanel"][EAmandsControllerButton.X].Add(new AmandsControllerButtonBind(ECommand.EndInteracting, EAmandsControllerCommand.InputTree, EAmandsControllerPressType.Release, 10, ""));
            AmandsControllerSets["ActionPanel"].Add(EAmandsControllerButton.UP, new List<AmandsControllerButtonBind> { new AmandsControllerButtonBind(ECommand.ScrollPrevious, EAmandsControllerCommand.InputTree, EAmandsControllerPressType.Press, 10, "") });
            AmandsControllerSets["ActionPanel"].Add(EAmandsControllerButton.DOWN, new List<AmandsControllerButtonBind> { new AmandsControllerButtonBind(ECommand.ScrollNext, EAmandsControllerCommand.InputTree, EAmandsControllerPressType.Press, 10, "") });

            AmandsControllerSets.Add("HealingLimbSelector", new Dictionary<EAmandsControllerButton, List<AmandsControllerButtonBind>>());
            AmandsControllerSets["HealingLimbSelector"].Add(EAmandsControllerButton.UP, new List<AmandsControllerButtonBind> { new AmandsControllerButtonBind(ECommand.ScrollNext, EAmandsControllerCommand.InputTree, EAmandsControllerPressType.Press, 9, "") });
            AmandsControllerSets["HealingLimbSelector"].Add(EAmandsControllerButton.DOWN, new List<AmandsControllerButtonBind> { new AmandsControllerButtonBind(ECommand.ScrollPrevious, EAmandsControllerCommand.InputTree, EAmandsControllerPressType.Press, 9, "") });

            AmandsControllerSets.Add("Movement", new Dictionary<EAmandsControllerButton, List<AmandsControllerButtonBind>>());
            AmandsControllerSets["Movement"].Add(EAmandsControllerButton.UP, new List<AmandsControllerButtonBind> { new AmandsControllerButtonBind(ECommand.NextWalkPose, EAmandsControllerCommand.InputTree, EAmandsControllerPressType.Press, 3, "") });
            AmandsControllerSets["Movement"].Add(EAmandsControllerButton.DOWN, new List<AmandsControllerButtonBind> { new AmandsControllerButtonBind(ECommand.PreviousWalkPose, EAmandsControllerCommand.InputTree, EAmandsControllerPressType.Press, 3, "") });
            AmandsControllerSets["Movement"].Add(EAmandsControllerButton.LEFT, new List<AmandsControllerButtonBind> { new AmandsControllerButtonBind(ECommand.None, EAmandsControllerCommand.SlowLeanLeft, EAmandsControllerPressType.Press, 3, "") });
            AmandsControllerSets["Movement"][EAmandsControllerButton.LEFT].Add(new AmandsControllerButtonBind(ECommand.DisplayTimer, EAmandsControllerCommand.EndSlowLean, EAmandsControllerPressType.Release, 3, ""));
            AmandsControllerSets["Movement"].Add(EAmandsControllerButton.RIGHT, new List<AmandsControllerButtonBind> { new AmandsControllerButtonBind(ECommand.None, EAmandsControllerCommand.SlowLeanRight, EAmandsControllerPressType.Press, 3, "") });
            AmandsControllerSets["Movement"][EAmandsControllerButton.RIGHT].Add(new AmandsControllerButtonBind(ECommand.DisplayTimer, EAmandsControllerCommand.EndSlowLean, EAmandsControllerPressType.Release, 3, ""));
            AmandsControllerSets["Movement"].Add(EAmandsControllerButton.LeftThumb, new List<AmandsControllerButtonBind> { new AmandsControllerButtonBind(ECommand.None, EAmandsControllerCommand.RestoreLean, EAmandsControllerPressType.Press, 3, "") });

            AmandsControllerSets.Add("Aiming", new Dictionary<EAmandsControllerButton, List<AmandsControllerButtonBind>>());
            AmandsControllerSets["Aiming"].Add(EAmandsControllerButton.RightThumb, new List<AmandsControllerButtonBind> { new AmandsControllerButtonBind(ECommand.ToggleBreathing, EAmandsControllerCommand.InputTree, EAmandsControllerPressType.Press, 4, "") });
            AmandsControllerSets["Aiming"].Add(EAmandsControllerButton.RightShoulder, new List<AmandsControllerButtonBind> { new AmandsControllerButtonBind(ECommand.None, EAmandsControllerCommand.EnableSet, EAmandsControllerPressType.Press, 4, "Aiming_RB") });
            AmandsControllerSets["Aiming"][EAmandsControllerButton.RightShoulder].Add(new AmandsControllerButtonBind(ECommand.DisplayTimer, EAmandsControllerCommand.DisableSet, EAmandsControllerPressType.Release, 4, "Aiming_RB"));

            AmandsControllerSets.Add("Aiming_RB", new Dictionary<EAmandsControllerButton, List<AmandsControllerButtonBind>>());
            AmandsControllerSets["Aiming_RB"].Add(EAmandsControllerButton.UP, new List<AmandsControllerButtonBind> { new AmandsControllerButtonBind(ECommand.OpticCalibrationSwitchUp, EAmandsControllerCommand.InputTree, EAmandsControllerPressType.Press, 4, "") });
            AmandsControllerSets["Aiming_RB"].Add(EAmandsControllerButton.DOWN, new List<AmandsControllerButtonBind> { new AmandsControllerButtonBind(ECommand.OpticCalibrationSwitchDown, EAmandsControllerCommand.InputTree, EAmandsControllerPressType.Press, 4, "") });

            AmandsControllerButtonBinds.Add(EAmandsControllerButton.LeftTrigger, new List<AmandsControllerButtonBind> { new AmandsControllerButtonBind(ECommand.ToggleAlternativeShooting, EAmandsControllerCommand.InputTree, EAmandsControllerPressType.Press, -1, "") });
            AmandsControllerButtonBinds[EAmandsControllerButton.LeftTrigger].Add(new AmandsControllerButtonBind(ECommand.FinishLowThrow, EAmandsControllerCommand.InputTree, EAmandsControllerPressType.Release, -1, ""));
            AmandsControllerButtonBinds.Add(EAmandsControllerButton.RightTrigger, new List<AmandsControllerButtonBind> { new AmandsControllerButtonBind(ECommand.ToggleShooting, EAmandsControllerCommand.InputTree, EAmandsControllerPressType.Press, -1, "") });
            AmandsControllerButtonBinds[EAmandsControllerButton.RightTrigger].Add(new AmandsControllerButtonBind(ECommand.EndShooting, EAmandsControllerCommand.InputTree, EAmandsControllerPressType.Release, -1, ""));
            AmandsControllerButtonBinds.Add(EAmandsControllerButton.A, new List<AmandsControllerButtonBind> { new AmandsControllerButtonBind(ECommand.Jump, EAmandsControllerCommand.InputTree, EAmandsControllerPressType.Press, -1, "") });
            AmandsControllerButtonBinds.Add(EAmandsControllerButton.B, new List<AmandsControllerButtonBind> { new AmandsControllerButtonBind(ECommand.ToggleDuck, EAmandsControllerCommand.InputTree, EAmandsControllerPressType.Press, -1, "") });
            AmandsControllerButtonBinds[EAmandsControllerButton.B].Add(new AmandsControllerButtonBind(ECommand.ToggleProne, EAmandsControllerCommand.InputTree, EAmandsControllerPressType.Hold, -1, ""));
            AmandsControllerButtonBinds.Add(EAmandsControllerButton.X, new List<AmandsControllerButtonBind> { new AmandsControllerButtonBind(ECommand.ReloadWeapon, EAmandsControllerCommand.InputTree, EAmandsControllerPressType.Press, -1, "") });
            AmandsControllerButtonBinds[EAmandsControllerButton.X].Add(new AmandsControllerButtonBind(ECommand.CheckAmmo, EAmandsControllerCommand.InputTree, EAmandsControllerPressType.Hold, -1, ""));
            AmandsControllerButtonBinds[EAmandsControllerButton.X].Add(new AmandsControllerButtonBind(ECommand.QuickReloadWeapon, EAmandsControllerCommand.InputTree, EAmandsControllerPressType.DoubleClick, -1, ""));
            AmandsControllerButtonBinds.Add(EAmandsControllerButton.Y, new List<AmandsControllerButtonBind> { new AmandsControllerButtonBind(ECommand.None, EAmandsControllerCommand.QuickSelectWeapon, EAmandsControllerPressType.Press, -1, "") });
            AmandsControllerButtonBinds[EAmandsControllerButton.Y].Add(new AmandsControllerButtonBind(ECommand.ExamineWeapon, EAmandsControllerCommand.InputTree, EAmandsControllerPressType.Hold, -1, ""));
            AmandsControllerButtonBinds.Add(EAmandsControllerButton.LeftThumb, new List<AmandsControllerButtonBind> { new AmandsControllerButtonBind(ECommand.ToggleSprinting, EAmandsControllerCommand.InputTree, EAmandsControllerPressType.Press, -1, "") });
            AmandsControllerButtonBinds.Add(EAmandsControllerButton.RightThumb, new List<AmandsControllerButtonBind> { new AmandsControllerButtonBind(ECommand.QuickKnifeKick, EAmandsControllerCommand.InputTree, EAmandsControllerPressType.Press, -1, "") });
            AmandsControllerButtonBinds[EAmandsControllerButton.RightThumb].Add(new AmandsControllerButtonBind(ECommand.SelectKnife, EAmandsControllerCommand.InputTree, EAmandsControllerPressType.Hold, -1, ""));
            AmandsControllerButtonBinds.Add(EAmandsControllerButton.UP, new List<AmandsControllerButtonBind> { new AmandsControllerButtonBind(ECommand.NextTacticalDevice, EAmandsControllerCommand.InputTree, EAmandsControllerPressType.Press, -1, "") });
            AmandsControllerButtonBinds[EAmandsControllerButton.UP].Add(new AmandsControllerButtonBind(ECommand.ToggleTacticalDevice, EAmandsControllerCommand.InputTree, EAmandsControllerPressType.Hold, -1, ""));
            AmandsControllerButtonBinds.Add(EAmandsControllerButton.DOWN, new List<AmandsControllerButtonBind> { new AmandsControllerButtonBind(ECommand.ChangeWeaponMode, EAmandsControllerCommand.InputTree, EAmandsControllerPressType.Press, -1, "") });
            AmandsControllerButtonBinds[EAmandsControllerButton.DOWN].Add(new AmandsControllerButtonBind(ECommand.CheckFireMode, EAmandsControllerCommand.InputTree, EAmandsControllerPressType.Hold, -1, ""));
            AmandsControllerButtonBinds[EAmandsControllerButton.DOWN].Add(new AmandsControllerButtonBind(ECommand.ForceAutoWeaponMode, EAmandsControllerCommand.InputTree, EAmandsControllerPressType.DoubleClick, -1, ""));
            AmandsControllerButtonBinds.Add(EAmandsControllerButton.LEFT, new List<AmandsControllerButtonBind> { new AmandsControllerButtonBind(ECommand.ChangeScopeMagnification, EAmandsControllerCommand.InputTree, EAmandsControllerPressType.Press, -1, "") });
            AmandsControllerButtonBinds[EAmandsControllerButton.LEFT].Add(new AmandsControllerButtonBind(ECommand.ChangeScope, EAmandsControllerCommand.InputTree, EAmandsControllerPressType.Hold, -1, ""));
            AmandsControllerButtonBinds.Add(EAmandsControllerButton.RIGHT, new List<AmandsControllerButtonBind> { new AmandsControllerButtonBind(ECommand.ChangeScope, EAmandsControllerCommand.InputTree, EAmandsControllerPressType.Press, -1, "") });
            AmandsControllerButtonBinds.Add(EAmandsControllerButton.BACK, new List<AmandsControllerButtonBind> { new AmandsControllerButtonBind(ECommand.ToggleInventory, EAmandsControllerCommand.InputTree, EAmandsControllerPressType.Press, -1, "") });

            MovementContextObject = Traverse.Create(localPlayer).Property("MovementContext").GetValue<object>();
            MovementContextType = MovementContextObject.GetType();
            SetCharacterMovementSpeed = MovementContextType.GetMethod("SetCharacterMovementSpeed", BindingFlags.Instance | BindingFlags.Public);
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
            if (AmandsControllerButtonSnapshots.ContainsKey(Button))
            { 
                AmandsControllerButtonSnapshot AmandsControllerButtonSnapshot = AmandsControllerButtonSnapshots[Button];
                if (Pressed)
                {
                    if (AmandsControllerButtonSnapshot.DoubleClickBind.Command != ECommand.None && Time.time - AmandsControllerButtonSnapshot.Time <= AmandsControllerPlugin.DoubleClickDelay.Value)
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
                    if (AmandsControllerButtonSnapshot.ReleaseBind.Command != ECommand.None)
                    {
                        AmandsControllerButton(AmandsControllerButtonSnapshot.ReleaseBind);
                        AmandsControllerButtonSnapshots.Remove(Button);
                    }
                    else
                    {
                        // Temp
                        if (AmandsControllerButtonSnapshot.HoldBind.Command == ECommand.None && AmandsControllerButtonSnapshot.DoubleClickBind.Command == ECommand.None)
                        {
                            if (AmandsControllerButtonSnapshot.ReleaseBind.Command != ECommand.None)
                            {
                                AmandsControllerButton(AmandsControllerButtonSnapshot.ReleaseBind);
                            }
                            AmandsControllerButtonSnapshots.Remove(Button);
                        }
                        else if (AmandsControllerButtonSnapshot.HoldBind.Command != ECommand.None || AmandsControllerButtonSnapshot.DoubleClickBind.Command != ECommand.None)
                        {
                            AsyncHold.Remove(Button.ToString() + AmandsControllerButtonSnapshot.Time.ToString());
                        }
                        if (AmandsControllerButtonSnapshot.DoubleClickBind.Command == ECommand.None && AmandsControllerButtonSnapshot.ReleaseBind.Command == ECommand.None)
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
                if (Binds[1].Command != ECommand.None)
                {
                    AmandsControllerButtonSnapshots.Add(Button, new AmandsControllerButtonSnapshot(true, time, Binds[0], Binds[1], Binds[2], Binds[3]));
                    AmandsControllerButton(Binds[0]);
                }
                else
                {
                    // Temp
                    if (Binds[2].Command == ECommand.None && Binds[3].Command == ECommand.None)
                    {
                        AmandsControllerButton(Binds[0]);
                    }
                    else if (Binds[2].Command != ECommand.None || Binds[3].Command != ECommand.None)
                    {
                        AmandsControllerButtonTimer(Button.ToString() + time.ToString(), Button);
                    }
                    if (Binds[2].Command != ECommand.None || Binds[3].Command != ECommand.None)
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
            ECommand PriorityCommand = ECommand.None;
            EAmandsControllerCommand PriorityAmandsControllerCommand = EAmandsControllerCommand.Empty;
            string PriorityAmandsControllerSet = "";
            if (AmandsControllerButtonBinds.ContainsKey(Button))
            {
                foreach (AmandsControllerButtonBind Bind in AmandsControllerButtonBinds[Button])
                {
                    if (Bind.PressType == PressType && Bind.Priority > BindPriority)
                    {
                        BindPriority = Bind.Priority;
                        PriorityCommand = Bind.Command;
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
                            PriorityCommand = Bind.Command;
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
                    if (Bind.Command != ECommand.None && inputTree)
                    {
                        ConsoleScreen.Log(Bind.Command.ToString());

                        commands.Clear();
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
                        }
                        InvokeParameters[0] = commands;
                        TranslateInput.Invoke(inputTree, InvokeParameters);
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
                        InvokeParameters[0] = commands;
                        TranslateInput.Invoke(inputTree, InvokeParameters);
                    }
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
    }
    public struct AmandsControllerButtonBind
    {
        public ECommand Command;
        public EAmandsControllerCommand AmandsControllerCommand;
        public EAmandsControllerPressType PressType;
        public int Priority;
        public string AmandsControllerSet;

        public AmandsControllerButtonBind(ECommand Command, EAmandsControllerCommand AmandsControllerCommand, EAmandsControllerPressType PressType, int Priority, string AmandsControllerSet)
        {
            this.Command = Command;
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
        RestoreLean = 9
    }
}
