using UnityEngine;
using TinyInspector;
using LevelDesign.Systems;

namespace LevelDesign.Systems.Player
{
    public class Player : MonoBehaviour
    {
        [BoxGroup("State Machine")]
        public PlayerStateMachine PSM;
        [BoxGroup("State Machine")]
        [SerializeField] private LocalPlayerState LPS;
        
        [BoxGroup("Controllers")]
        [SerializeField] private PlayerCamera playerCamera;
        [Space]
        [BoxGroup("Controllers")]
        [SerializeField] private _MovementController playerCharacter;
        [BoxGroup("Controllers")]
        [SerializeField] private ArmController playerArmController;

        [BoxGroup("Managers")]
        public CharacterDataManager characterDataM;
        [BoxGroup("Managers")]
        public RespawnManager respawnM;
        [BoxGroup("Managers")]
        public UIManager uiM;
        [BoxGroup("Managers")]
        public HealthManager healthM;
        [BoxGroup("Managers")]
        public StackManager StackM;

        private Transform cameraFocalTarget;
        private Transform spectatorCameraTarget;

        #region Unity Calls
        public void Initialize() {
            if(LPS.isLocalPlayer) { LocalInit(); }
            else { RemoteInit(); }
        }

        private void Update() {
            RoutineChecks();
            ProcessControllers();

            if(!LPS.isLocalPlayer) { return; }

            if(GameManager.Instance != null)
            {
                if(GameManager.Instance.localState == GameState.Playing)
                {
                    PSM.isPlaying = true;
                }
                else
                {
                    PSM.isPlaying = false;
                }
            }
        }

        private void LateUpdate()
        {
            UpdateCameraTarget();
        }

        private void OnDestroy()
        {
            healthM.Died -= OnDeath;
        }
        #endregion

        #region Initializations
        private void LocalInit() {
            if(StackM == null) {
                StackM = GetComponentInChildren<StackManager>();
            }

            healthM.Died += OnDeath;

            playerCamera.Initialize(PSM);
            characterDataM.Initialize();
            healthM.Initialize(StackM);
            respawnM.Initialize(this, PSM, characterDataM, playerCamera);
            uiM.Initialize(Player: this);

            if(GameManager.Instance != null) {
                GameManager.Instance.RegisterLocalPlayer(this);
            } else {
                Debug.LogWarning("Player could not register: no GameManager in the scene");
            }
        }
        private void RemoteInit() {
            playerCamera.RemoteInit();
        }
        #endregion

        #region Controllers
        public void ProcessControllers() {
            if(playerCamera != null)
            {
                playerCamera.UpdateCameraInput();
                playerCamera.UpdateRotation();
            }
            if(playerCharacter != null && playerCamera != null)
            {
                playerCharacter._UpdateBody(Time.deltaTime, playerCamera.gameObject.transform);
            }
        }

        public void UpdateCameraTarget()
        {
            if(playerCamera == null) { return; }

            if (!PSM.isCinematic)
            {
                if(playerCharacter == null) { return; }

                spectatorCameraTarget = null;
                cameraFocalTarget = playerCharacter._GetCameraTarget();

                if(cameraFocalTarget == null) { return; }

                playerCamera.UpdatePosition(cameraFocalTarget);
            }
            else
            {
                if (spectatorCameraTarget == null)
                {
                    return;
                }

                playerCamera.SnapToTarget(spectatorCameraTarget);
            }
        }

        public void ClearMovement() { }
        #endregion

        #region Checks
        private void RoutineChecks() {
            if(!LPS.isLocalPlayer) {
                if(playerCharacter == null) { 
                    playerCharacter = GetComponentInChildren<_MovementController>();
                    
                    if(playerCharacter != null && playerCharacter.enabled) {
                       playerCharacter._RemoteInit(); 
                    }
                }
                
                if(playerCharacter != null) {
                    RigInfo rig = playerCharacter._GetCurrentRigInfo();
                }
            }
            
            if(characterDataM.currentMovementController != null && LPS.isLocalPlayer) {
                if(!characterDataM.currentMovementController._isInitialized) {
                    playerCharacter = characterDataM.currentMovementController;

                    playerCharacter._BindStacks(StackM);
                    playerCharacter._Initialize(PSM, characterDataM.currentCharacterData);
                }
            }

            if(characterDataM.currentArmController != null && LPS.isLocalPlayer) {
                if(!characterDataM.currentArmController.isInitialized) {
                    playerArmController = characterDataM.currentArmController;
                    
                    if(playerCharacter != null)
                    {
                        playerArmController.Initialize(playerRef: this, controller: playerCharacter);
                    }
                }
            }
        }

        public void OnDeath()
        {
            characterDataM.ClearCharacter();
            respawnM.StartDeathTimer();
        }
        #endregion

        #region Public Accessors
        public bool IsLocalPlayer => LPS != null && LPS.isLocalPlayer;

        public Transform GetCameraTarget() {
            return playerCharacter != null ? playerCharacter._GetCameraTarget() : null;
        }

        public void SetAndLoadCinematic(Transform cinematicPosition) {
            spectatorCameraTarget = cinematicPosition;
            PSM.isCinematic = true;
        }

        public void ExitCinematic() {
            PSM.isCinematic = false;
        }
        #endregion
    }
}