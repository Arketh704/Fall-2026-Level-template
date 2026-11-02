using System;
using System.Collections.Generic;
using PurrNet;
using UnityEngine;
using TinyInspector;
using LevelDesign.Tags;
using PlayerController = LevelDesign.Systems.Player.Player;
using TMPro;
using UnityEngine.SocialPlatforms;

namespace LevelDesign.Systems
{
    public enum GameState
    {
        LoadingIn,
        Playing,
        GameOver
    }

    public class GameManager : NetworkBehaviour
    {
        [SerializeField] private GameObject gameStartingUI;
        [SerializeField] private TMP_Text gameCountdownText;
        [SerializeField] private float loadInDuration = 20f;
        [SerializeField] private float matchDuration = 0f;

        private readonly SyncVar<GameState> state = new SyncVar<GameState>(GameState.LoadingIn);
        private readonly HashSet<PlayerID> confirmedPlayers = new HashSet<PlayerID>();
        private readonly List<TAG_LoadInLook> looks = new List<TAG_LoadInLook>();

        public GameState localState = GameState.LoadingIn;
        private PlayerController localPlayer;
        private int lookIndex;
        private bool clientSpawned;

        private bool loadInRunning;
        private float loadInElapsed;
        private float loadInTotal;

        private bool serverLoadInStarted;
        private float serverTimer;
        
        #region Events
        public static GameManager Instance { get; private set; }

        public event Action<GameState> OnStateEntered;
        public event Action<float> OnCountdownStarted;
        public event Action OnCanPlay;
        #endregion

        #region Unity Calls
        private void Awake() {
            Instance = this;

            RefreshLooks();

            if(looks.Count == 0) {
                Debug.LogWarning("GameManager found no TAG_LoadInLook objects in the scene");
            }
        }

        private void Update() {
            TickLookCycle();

            if(isSpawned && isServer) {
                TickServer();
            }

            if(localState == GameState.LoadingIn)
            {
                gameStartingUI.SetActive(true);
                gameCountdownText.text = (Mathf.RoundToInt(LoadInTimeRemaining).ToString() + "s");
            }
            else
            {
                gameStartingUI.SetActive(false);
            }
        }

        private void OnDestroy() {
            if(Instance == this) {
                Instance = null;
            }
        }
        #endregion

        #region Network Callbacks
        protected override void OnSpawned(bool asServer) {
            if(asServer) {
                confirmedPlayers.Clear();
                serverLoadInStarted = false;
                return;
            }

            clientSpawned = true;
            EnterLocalState(state.value);

            if(localPlayer != null) {
                ConfirmInitializedServer();
            }
        }
        #endregion

        #region State Machine
        private void ChangeState(GameState next) {
            if(!isServer) { return; }
            if(state.value == next) { return; }

            state.value = next;
            serverLoadInStarted = false;
            serverTimer = next == GameState.Playing ? matchDuration : 0f;

            RpcStateChanged(next);
        }

        [ObserversRpc(runLocally: true)]
        private void RpcStateChanged(GameState next) {
            EnterLocalState(next);
        }

        private void EnterLocalState(GameState next) {
            localState = next;
            lookIndex = 0;
            loadInElapsed = 0f;
            loadInTotal = loadInDuration;
            loadInRunning = next == GameState.GameOver;

            ApplyCinematicForState();

            OnStateEntered?.Invoke(next);

            if(next == GameState.Playing) {
                OnCanPlay?.Invoke();
            }
        }

        private void TickServer() {
            switch(state.value) {
                case GameState.LoadingIn:
                    TickLoadingIn();
                    break;
                case GameState.Playing:
                    TickPlaying();
                    break;
            }
        }

        private void TickLoadingIn() {
            if(!serverLoadInStarted) {
                if(!AllPlayersConfirmed()) { return; }

                serverLoadInStarted = true;
                serverTimer = loadInDuration;
                RpcLoadInStarted(loadInDuration);
                return;
            }

            serverTimer -= Time.deltaTime;

            if(serverTimer <= 0f) {
                ChangeState(GameState.Playing);
            }
        }

        private void TickPlaying() {
            if(matchDuration <= 0f) { return; }

            serverTimer -= Time.deltaTime;

            if(serverTimer <= 0f) {
                ChangeState(GameState.GameOver);
            }
        }

        private bool AllPlayersConfirmed() {
            int playerCount = 0;

            foreach(PlayerID player in networkManager.players) {
                playerCount++;

                if(!confirmedPlayers.Contains(player)) {
                    return false;
                }
            }

            return playerCount > 0;
        }

        [ObserversRpc(runLocally: true)]
        private void RpcLoadInStarted(float duration) {
            loadInTotal = duration;
            loadInElapsed = 0f;
            loadInRunning = true;

            OnCountdownStarted?.Invoke(duration);
        }
        #endregion

        #region Cinematic
        private void RefreshLooks() {
            looks.Clear();
            looks.AddRange(FindObjectsByType<TAG_LoadInLook>(FindObjectsInactive.Include, FindObjectsSortMode.InstanceID));
            looks.Sort((a, b) => string.Compare(a.name, b.name, StringComparison.Ordinal));
            lookIndex = 0;
        }

        private void ApplyCinematicForState() {
            if(localPlayer == null) { return; }

            if(localState == GameState.Playing) {
                localPlayer.ExitCinematic();
                return;
            }

            if(looks.Count == 0) {
                RefreshLooks();
            }

            ShowLook(lookIndex);
        }

        private void TickLookCycle() {
            if(localPlayer == null) { return; }
            if(localState == GameState.Playing) { return; }
            if(!loadInRunning) { return; }
            if(loadInTotal <= 0f) { return; }
            if(looks.Count == 0) { return; }

            loadInElapsed += Time.deltaTime;

            if(localState == GameState.GameOver) {
                loadInElapsed %= loadInTotal;
            } else {
                loadInElapsed = Mathf.Min(loadInElapsed, loadInTotal);
            }

            int nextIndex = Mathf.Min(Mathf.FloorToInt(LoadInPercent * looks.Count), looks.Count - 1);

            if(nextIndex == lookIndex) { return; }

            lookIndex = nextIndex;
            ShowLook(lookIndex);
        }

        private void ShowLook(int index) {
            if(looks.Count == 0) { return; }

            localPlayer.SetAndLoadCinematic(looks[index].transform);
        }
        #endregion

        #region Initialization Confirmation
        [ServerRpc(requireOwnership: false)]
        private void ConfirmInitializedServer(RPCInfo info = default) {
            confirmedPlayers.Add(info.sender);
        }
        #endregion

        #region Public Accessors
        public GameState CurrentState => state.value;
        public bool LoadInRunning => loadInRunning;
        public float LoadInPercent => loadInTotal > 0f ? loadInElapsed / loadInTotal : 0f;
        public float LoadInTimeRemaining => Mathf.Max(0f, loadInTotal - loadInElapsed);

        public void RegisterLocalPlayer(PlayerController player) {
            localPlayer = player;


            ApplyCinematicForState();

            if(clientSpawned) {
                ConfirmInitializedServer();
            }
        }

        public void EndGame() {
            ChangeState(GameState.GameOver);
        }
        #endregion
    }
}