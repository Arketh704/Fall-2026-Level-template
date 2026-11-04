using System;
using UnityEngine;
using TinyInspector;

namespace LevelDesign.Systems.Player {
    [RequireComponent(typeof(Collider))]
    public class StackPickup : MonoBehaviour {
        [BoxGroup("Pickup")]
        [SerializeField] private StackType type = StackType.Ammo;
        [BoxGroup("Pickup")]
        [SerializeField] private int amount = 1;

        [BoxGroup("Respawn")]
        [SerializeField] private bool respawns = true;
        [BoxGroup("Respawn")]
        [SerializeField] private float respawnDelay = 10f;

        [BoxGroup("Scene Refs")]
        [SerializeField] private GameObject visual;

        private Collider trigger;
        private float respawnTime;

        // Raised on the collecting client. Hook networking here to tell other clients to call SetAvailable(false).
        public event Action<StackPickup, Player> Collected;

        #region Unity Calls
        private void Awake() {
            trigger = GetComponent<Collider>();
            trigger.isTrigger = true;

            if(visual == null) {
                Debug.LogWarning("StackPickup has no visual assigned; it will stay visible after being collected.", this);
            }
        }

        private void Update() {
            if(IsAvailable || !respawns) { return; }

            if(Time.time >= respawnTime) {
                SetAvailable(true);
            }
        }

        private void OnTriggerEnter(Collider other) {
            if(!IsAvailable) { return; }

            Player player = other.GetComponentInParent<Player>();
            if(player == null) { return; }
            if(!player.IsLocalPlayer) { return; }

            TryCollect(player);
        }
        #endregion

        #region Public Accessors
        public bool IsAvailable { get; private set; } = true;
        public StackType Type => type;
        public int Amount => amount;

        public bool TryCollect(Player player) {
            if(!IsAvailable) { return false; }

            StackManager stacks = player.StackM;
            if(stacks == null) { return false; }

            stacks.Add(type, amount);
            SetAvailable(false);
            respawnTime = Time.time + respawnDelay;

            Collected?.Invoke(this, player);
            return true;
        }

        public void SetAvailable(bool available) {
            if(trigger == null) {
                trigger = GetComponent<Collider>();
            }

            IsAvailable = available;
            trigger.enabled = available;

            if(visual != null) {
                visual.SetActive(available);
            }

            if(!available) {
                respawnTime = Time.time + respawnDelay;
            }
        }
        #endregion
    }
}