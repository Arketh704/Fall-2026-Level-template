using System;
using System.Collections;
using UnityEngine;
using TinyInspector;
using LevelDesign.Data;
using LevelDesign.Systems.Player;
using LevelDesign.Systems;
using LevelDesign.UI;

namespace LevelDesign
{
    public class RespawnManager : MonoBehaviour
    {
        #region Fields
        [BoxGroup("Config")]
        [SerializeField] private float deathLength;

        [BoxGroup("Scene Refs")]
        [SerializeField] private CharacterDataSO characterData;

        [BoxGroup("Debug")]
        public PlayerStateMachine PSM;
        [BoxGroup("Debug")]
        [SerializeField] private RespawnPoint activePoint;
        [BoxGroup("Debug")]
        public float deathTimeRemaining;

        private Player player;
        private PlayerCamera playerCamera;
        private CharacterDataManager characterDataM;
        private Coroutine spawnRoutine;

        public event Action<RespawnPoint> OnConfirmed;
        #endregion

        #region Unity
        private void Update()
        {
            if(PSM.isDead)
            {
                deathTimeRemaining -= Time.deltaTime;

                if(deathTimeRemaining <= 0f) { 
                    PSM.isDead = false;
                    PSM.awaitingRespawn = true;
                    player.healthM.ResetHealth();
                }
            }
        }
        #endregion

        #region Core
        private IEnumerator SpawnRoutine(RespawnPoint point) {
            characterDataM.LoadCharacterData(characterData);

            _MovementController controller = characterDataM.currentMovementController;

            if(controller == null) {
                spawnRoutine = null;
                yield break;
            }

            // Player.RoutineChecks initializes the new controller on the next Update
            while(controller != null && !controller._isInitialized) {
                yield return null;
            }

            if(controller == null) {
                spawnRoutine = null;
                yield break;
            }

            Quaternion spawnRotation = GetFlatRotation(point.transform);

            controller._Teleport(point.transform.position);
            controller._SetRotation(spawnRotation);

            if(playerCamera != null) {
                playerCamera.SetRotation(new Vector3(0f, spawnRotation.eulerAngles.y, 0f));
            }

            player.ExitCinematic();
            OnConfirmed?.Invoke(point);

            spawnRoutine = null;
        }

        public void StartDeathTimer()
        {
            deathTimeRemaining = deathLength;
            PSM.isDead = true;
        }

        private Quaternion GetFlatRotation(Transform point) {
            Vector3 forward = Vector3.ProjectOnPlane(point.forward, Vector3.up);

            if(forward.sqrMagnitude < 0.0001f) { return Quaternion.identity; }

            return Quaternion.LookRotation(forward.normalized, Vector3.up);
        }
        #endregion

        #region Public Accessors
        public void Initialize(Player playerRef, PlayerStateMachine psm, CharacterDataManager dataManager, PlayerCamera cameraRef) {
            player = playerRef;
            PSM = psm;
            characterDataM = dataManager;
            playerCamera = cameraRef;
        }

        public void SetActivePoint(RespawnPoint point) {
            activePoint = point;
        }

        public void SetCharacterData(CharacterDataSO newCharacterData) {
            characterData = newCharacterData;
        }

        public void Confirm() {
            if(spawnRoutine != null) { return; }

            if(activePoint == null) {
                Debug.LogWarning("Respawn Manager has no active respawn point to confirm");
                return;
            }

            if(characterData == null) {
                Debug.LogWarning("Respawn Manager has no character data assigned");
                return;
            }

            if(player == null || characterDataM == null) {
                Debug.LogWarning("Respawn Manager was confirmed before it was initialized");
                return;
            }

            PSM.awaitingRespawn = false;
            spawnRoutine = StartCoroutine(SpawnRoutine(activePoint));
        }
        #endregion
    }
}