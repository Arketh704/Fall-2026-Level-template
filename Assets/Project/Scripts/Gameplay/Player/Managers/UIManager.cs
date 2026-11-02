using TinyInspector;
using UnityEngine;
using TMPro;
using UnityEngine.UI;
using LevelDesign.UI;

namespace LevelDesign.Systems.Player
{
    public class UIManager : MonoBehaviour
    {
        [BoxGroup("Scene Refs/Gameplay")]
        [SerializeField] private GameObject gamePage;
        [BoxGroup("Scene Refs/Gameplay")]
        [SerializeField] private TMP_Text healthBarText;
        [BoxGroup("Scene Refs/Gameplay")]
        [SerializeField] private TMP_Text healthBarLowerText;
        [BoxGroup("Scene Refs/Gameplay")]
        [SerializeField] private RawImage healthBarFill;
        [BoxGroup("Scene Refs/Gameplay")]
        public TMP_Text ammoText;
        
        [BoxGroup("Scene Refs/Respawn")]
        [SerializeField] private GameObject respawnPage;
        [BoxGroup("Scene Refs/Respawn")]
        [SerializeField] private RespawnButtonConstructor respawnButtonConstructor;

        [BoxGroup("Scene Refs/Death")]
        [SerializeField] private GameObject deathPage;
        [BoxGroup("Scene Refs/Death")]
        [SerializeField] private TMP_Text countdownText;
        
        private Player player;
        private HealthManager healthM;
        private PlayerStateMachine PSM;

        private static readonly int FillId = Shader.PropertyToID("_Fill");
        
        private bool initialized;
        private bool respawnButtonsBuilt;
    
        #region Unity Calls
        private void Awake() {
            healthBarFill.material = new Material(healthBarFill.material);
        }

        void Update()
        {
            if(!initialized) { return; }

            DriveGameplay();
            DriveRespawn();
            DriveDeath();
        }
        #endregion

        #region Core
        void DriveGameplay()
        {
            if(!PSM.isPlaying || PSM.isDead || PSM.awaitingRespawn)
            {
                gamePage.SetActive(false);
                return;
            }
            else
            {
                gamePage.SetActive(true);
            }

            healthBarText.text = healthM.currentHealth.ToString();
            healthBarLowerText.text = healthM.currentHealth.ToString();
            
            healthBarFill.material.SetFloat(FillId, Mathf.Clamp01((float)healthM.currentHealth / healthM.maxHealth));
        }

        void DriveRespawn() {
            if(!PSM.isPlaying || !PSM.awaitingRespawn) {
                respawnPage.SetActive(false);
                respawnButtonsBuilt = false;
                return;
            }

            respawnPage.SetActive(true);

            if(!respawnButtonsBuilt && respawnButtonConstructor != null) {
                respawnButtonConstructor.Build(player);
                respawnButtonsBuilt = true;
            }
        }

        void DriveDeath()
        {
            if(!PSM.isPlaying || !PSM.isDead)
            {
                deathPage.SetActive(false);
                return;
            }
            else
            {
                deathPage.SetActive(true);
            }
        }
        #endregion

        #region Public Accessors
        public void Initialize(Player Player)
        {
            player = Player;
            PSM = Player.PSM;
            healthM = player.healthM;
            initialized = true;
        }
        #endregion
    }
}