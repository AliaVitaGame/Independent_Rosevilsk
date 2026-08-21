using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

namespace HEAVYART.TopDownShooter.Netcode
{
    public class PlayerBehaviour : NetworkBehaviour
    {
        private float movementSpeed = 5;
        private float sprintSpeedMultiplier = 1.6f;
        private float jumpVelocity = 7.5f;
        private float smoothMovementTime = 0.1f;

        private readonly NetworkVariable<bool> isSprinting = new(
            false,
            NetworkVariableReadPermission.Everyone,
            NetworkVariableWritePermission.Owner);
        private readonly NetworkVariable<int> jumpSequence = new(
            0,
            NetworkVariableReadPermission.Everyone,
            NetworkVariableWritePermission.Owner);

        private WeaponControlSystem weaponControlSystem;
        private ShooterInputControls inputActions;
        private HealthController healthController;
        private ModifiersControlSystem modifiersControlSystem;
        private RigidbodyCharacterController rigidbodyCharacterController;
        private CharacterIdentityControl identityControl;
        private CharacterAnimationController animationController;

        private Camera mainCamera;
        private Plane plane;

        private bool isMobile = false;
        private Vector3 movementVelocity = Vector3.zero;
        private Vector3 currentMovementInput;
        private bool jumpQueued;

        public bool IsSprinting => isSprinting.Value;
        public float SprintSpeedMultiplier => sprintSpeedMultiplier;

        private void Awake()
        {
            //Register spawned player object (bots need it to find player)
            GameManager.Instance.userControl.AddPlayerObject(NetworkObject);
        }

        private void Start()
        {
            //Basic components
            weaponControlSystem = GetComponent<WeaponControlSystem>();
            healthController = GetComponent<HealthController>();
            modifiersControlSystem = GetComponent<ModifiersControlSystem>();
            rigidbodyCharacterController = GetComponent<RigidbodyCharacterController>();
            identityControl = GetComponent<CharacterIdentityControl>();
            animationController = GetComponent<CharacterAnimationController>();

            //Camera and aiming
            plane = new Plane(Vector3.up, weaponControlSystem.lineOfSightTransform.localPosition);
            mainCamera = Camera.main;
            mainCamera.GetComponent<GameCameraController>().ActivateCameraMovement();

            //Settings
            int modelIndex = identityControl.spawnParameters.Value.modelIndex;
            PlayerConfig config = SettingsManager.Instance.player.configs[modelIndex];

            //Health
            healthController.Initialize(config.health);
            healthController.OnDeath += () =>
            {
                GetComponent<CharacterEffectsController>().RunDestroyScenario(true);

                if (IsOwner == true) GameManager.Instance.UI.ShowEndOfGamePopup();
            };

            //Inputs
            inputActions = new ShooterInputControls();
            inputActions.Player.Look.Enable();
            inputActions.Player.Move.Enable();
            inputActions.Player.Fire.Enable();
            inputActions.Player.Sprint.Enable();
            inputActions.Player.Jump.Enable();

            movementSpeed = config.movementSpeed;
            sprintSpeedMultiplier = config.sprintSpeedMultiplier;
            jumpVelocity = config.jumpVelocity;

            isMobile = Application.isMobilePlatform;

            gameObject.name = "Player: " + identityControl.spawnParameters.Value.name;
        }

        private void OnDestroy()
        {
            if (inputActions == null)
                return;

            inputActions.Disable();
            inputActions.Dispose();
            inputActions = null;
        }

        public override void OnNetworkSpawn()
        {
            jumpSequence.OnValueChanged += HandleJumpSequenceChanged;
        }

        public override void OnNetworkDespawn()
        {
            jumpSequence.OnValueChanged -= HandleJumpSequenceChanged;
        }

        private void Update()
        {
            if (!IsOwner || isMobile || inputActions == null)
                return;

            if (inputActions.Player.Jump.WasPressedThisFrame())
                jumpQueued = true;
        }

        void FixedUpdate()
        {
            if (IsOwner == false) return;

            //Stop any movement when game ends
            if (GameManager.Instance.gameState == GameState.GameIsOver)
            {
                SetSprinting(false);
                rigidbodyCharacterController.Stop();
            }

            //Stop any movement when player is dead
            if (healthController.isAlive == false)
            {
                SetSprinting(false);
                rigidbodyCharacterController.Stop();
            }

            //Wait for game to start
            if (GameManager.Instance.gameState != GameState.ActiveGame)
            {
                SetSprinting(false);
                jumpQueued = false;
                return;
            }

            if (isMobile)
                HandleMobileInput();
            else
                HandleKeyboardInput();
        }

        private void HandleKeyboardInput()
        {
            Vector2 mouseInput = inputActions.Player.Look.ReadValue<Vector2>();
            Ray ray = mainCamera.ScreenPointToRay(mouseInput);

            if (plane.Raycast(ray, out float enter))
            {
                Vector3 hitPoint = ray.GetPoint(enter);
                Vector3 lookDirection = hitPoint - transform.position;
                lookDirection.y = 0;

                //Point line of sight in direction of cursor
                weaponControlSystem.lineOfSightTransform.localRotation = Quaternion.LookRotation(lookDirection);

                //Draw line of sight direction
                if (Application.isEditor)
                    Debug.DrawRay(weaponControlSystem.lineOfSightTransform.position, weaponControlSystem.lineOfSightTransform.forward, Color.red);
            }

            //Keyboard inputs
            Vector2 positionInput = inputActions.Player.Move.ReadValue<Vector2>().normalized;
            currentMovementInput = Vector3.SmoothDamp(currentMovementInput, positionInput, ref movementVelocity, smoothMovementTime);

            SetSprinting(inputActions.Player.Sprint.IsPressed() && positionInput.sqrMagnitude > 0.01f);

            //Update movement speed according to currently active modifiers
            float currentSpeed = modifiersControlSystem.CalculateSpeedMultiplier() * movementSpeed;
            if (IsSprinting)
                currentSpeed *= sprintSpeedMultiplier;

            //Move (using physics)
            rigidbodyCharacterController.Move(new Vector3(currentMovementInput.x, 0, currentMovementInput.y), currentSpeed);

            if (jumpQueued)
            {
                jumpQueued = false;
                if (rigidbodyCharacterController.Jump(jumpVelocity))
                {
                    animationController.PlayJumpAnimation();
                    jumpSequence.Value++;
                }
            }

            //Fire weapon
            if (inputActions.Player.Fire.inProgress)
                weaponControlSystem.Fire();
        }

        private void HandleMobileInput()
        {
            if (inputActions.Player.Look.inProgress)
            {
                //Screen joystick inputs
                Vector2 lookJoystickInput = inputActions.Player.Look.ReadValue<Vector2>();
                weaponControlSystem.lineOfSightTransform.localRotation = Quaternion.LookRotation(new Vector3(lookJoystickInput.x, 0, lookJoystickInput.y));

                //Fire weapon
                weaponControlSystem.Fire();
            }

            //Screen joystick inputs
            Vector2 moveJoystickInput = inputActions.Player.Move.ReadValue<Vector2>().normalized;
            currentMovementInput = Vector3.SmoothDamp(currentMovementInput, moveJoystickInput, ref movementVelocity, smoothMovementTime);

            //Update movement speed according to currently active modifiers
            float currentSpeed = modifiersControlSystem.CalculateSpeedMultiplier() * movementSpeed;

            //Move (using physics)
            rigidbodyCharacterController.Move(new Vector3(currentMovementInput.x, 0, currentMovementInput.y), currentSpeed);
        }

        private void SetSprinting(bool value)
        {
            if (IsSpawned && isSprinting.Value != value)
                isSprinting.Value = value;
        }

        private void HandleJumpSequenceChanged(int previousValue, int newValue)
        {
            if (!IsOwner && animationController != null)
                animationController.PlayJumpAnimation();
        }
    }
}
