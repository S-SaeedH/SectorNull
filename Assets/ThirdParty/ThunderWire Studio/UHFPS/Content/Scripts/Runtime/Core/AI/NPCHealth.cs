using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UHFPS.Tools;
using Newtonsoft.Json.Linq;

namespace UHFPS.Runtime
{
    public class NPCHealth : BaseHealthEntity, ISaveable
    {
        [System.Serializable]
        public struct BodySegment
        {
            public Rigidbody Rigidbody;
            public Collider Collider;
            public NPCBodyPart BodyPart;

            public BodySegment(Rigidbody rigidbody, Collider collider, NPCBodyPart bodyPart)
            {
                Rigidbody = rigidbody;
                Collider = collider;
                BodyPart = bodyPart;
            }
        }

        public List<BodySegment> BodySegments = new();
        public List<Component> DisableComponents;

        public Transform Hips;
        public Collider Head;
        public Layer BodyPartLayer;

        public uint MaxHealth = 100;
        public uint StartHealth = 100;
        public float HeadshotMultiplier = 2f;
        public bool AllowHeadhsot = true;

        [Header("Corpse")]
        public bool RemoveCorpse;
        public bool DisableCorpse;
        public float CorpseRemoveTime = 10f;

        [Header("Save Settings")]
        [Tooltip("If true, killed enemies stay dead after loading. If false, enemies respawn alive when loading.")]
        public bool SaveDeathState = true;

        [Tooltip("If true, NPC initializes itself if no save data was found for it.")]
        public bool InitializeIfMissingFromSave = true;

        public AudioClip[] DamageSounds;
        [Range(0f, 1f)] public float DamageVolume = 1f;

        public SoundClip DeathSound;

        public UnityEvent<int> OnTakeDamage;
        public UnityEvent OnDeath;
        public UnityEvent OnCorpseRemove;

        private int lastDamageSound;
        private float corpseTime;
        private bool corpseRemoved;
        private bool loadedFromSave;

        private void Awake()
        {
            // Normal new game / normal scene load.
            if (!SaveGameManager.GameWillLoad)
            {
                InitializeNPCFresh();
            }
        }

        private void Start()
        {
            /*
             * When loading a save, Awake skips initialization because we expect OnLoad()
             * to restore the NPC.
             *
             * But if this enemy is not found in the save data, OnLoad() never runs.
             * Without this fallback, the NPC can be left in a broken/uninitialized state.
             */
            if (SaveGameManager.GameWillLoad && !loadedFromSave && InitializeIfMissingFromSave)
            {
                InitializeNPCFresh();
                Debug.LogWarning($"{name} was not found in save data, initialized as a fresh NPC.");
            }
        }

        private void Update()
        {
            if (!IsDead || corpseRemoved)
                return;

            // Do nothing if corpse removal/disable is not enabled.
            if (!RemoveCorpse && !DisableCorpse)
                return;

            if (corpseTime > 0)
            {
                corpseTime -= Time.deltaTime;
            }
            else
            {
                if (DisableCorpse)
                    gameObject.SetActive(false);

                OnCorpseRemove?.Invoke();

                corpseTime = 0;
                corpseRemoved = true;
            }
        }

        public override void OnApplyDamage(int damage, Transform sender = null)
        {
            if (IsDead || corpseRemoved)
                return;

            base.OnApplyDamage(damage, sender);
            OnTakeDamage?.Invoke(damage);

            if (DamageSounds != null && DamageSounds.Length > 0)
            {
                int damageSound = GameTools.RandomUnique(0, DamageSounds.Length, lastDamageSound);
                GameTools.PlayOneShot3D(transform.position, DamageSounds[damageSound], DamageVolume, "ZombieDamageAudio");
                lastDamageSound = damageSound;
            }
        }

        public override void OnHealthZero()
        {
            EnableRagdoll(true);

            OnDeath?.Invoke();

            corpseTime = CorpseRemoveTime;
            corpseRemoved = false;

            foreach (var component in DisableComponents)
            {
                if (component == null)
                    continue;

                if (component is Behaviour behaviour)
                    behaviour.enabled = false;
                else if (component is Collider collider)
                    collider.enabled = false;
            }

            if (DeathSound != null)
                GameTools.PlayOneShot3D(transform.position, DeathSound, "ZombieDeathAudio");
        }

        private void InitializeNPCFresh()
        {
            gameObject.SetActive(true);

            IsDead = false;
            corpseRemoved = false;
            corpseTime = 0f;

            InitializeHealth((int)StartHealth, (int)MaxHealth);
            EnableRagdoll(false);
            EnableDisabledComponents(true);
        }

        private void EnableDisabledComponents(bool enabled)
        {
            foreach (var component in DisableComponents)
            {
                if (component == null)
                    continue;

                if (component is Behaviour behaviour)
                    behaviour.enabled = enabled;
                else if (component is Collider collider)
                    collider.enabled = enabled;
            }
        }

        private void EnableRagdoll(bool enabled)
        {
            foreach (BodySegment bodyPart in BodySegments)
            {
                if (bodyPart.Rigidbody == null || bodyPart.Collider == null)
                    continue;

                if (enabled)
                {
                    bodyPart.Rigidbody.isKinematic = false;
                    bodyPart.Rigidbody.useGravity = true;
                    bodyPart.Collider.isTrigger = false;
                }
                else
                {
                    bodyPart.Rigidbody.isKinematic = true;
                    bodyPart.Rigidbody.useGravity = false;
                    bodyPart.Collider.isTrigger = true;
                }
            }
        }

        public StorableCollection OnSave()
        {
            return new StorableCollection()
            {
                { "position", transform.position.ToSaveable() },
                { "rotation", transform.eulerAngles.ToSaveable() },
                { "health", EntityHealth },
                { "isDead", IsDead },
                { "corpseRemoved", corpseRemoved }
            };
        }

        public void OnLoad(JToken data)
        {
            loadedFromSave = true;

            if (data == null)
            {
                InitializeNPCFresh();
                return;
            }

            if (data["position"] != null)
                transform.position = data["position"].ToObject<Vector3>();

            if (data["rotation"] != null)
                transform.eulerAngles = data["rotation"].ToObject<Vector3>();

            int health = data["health"] != null ? (int)data["health"] : (int)StartHealth;
            bool savedIsDead = data["isDead"] != null ? (bool)data["isDead"] : health <= 0;
            bool savedCorpseRemoved = data["corpseRemoved"] != null && (bool)data["corpseRemoved"];

            /*
             * If death state should be saved:
             * - killed enemies stay gone/dead after load.
             *
             * If death state should NOT be saved:
             * - enemy respawns alive even if it was dead in the save.
             */
            if (SaveDeathState && (savedIsDead || health <= 0 || savedCorpseRemoved))
            {
                IsDead = true;
                EntityHealth = 0;
                corpseRemoved = savedCorpseRemoved;

                EnableDisabledComponents(false);
                EnableRagdoll(false);

                gameObject.SetActive(false);
                return;
            }

            // Enemy should load alive.
            gameObject.SetActive(true);

            IsDead = false;
            corpseRemoved = false;
            corpseTime = 0f;

            int loadedHealth = health <= 0 ? (int)StartHealth : health;

            InitializeHealth(loadedHealth, (int)MaxHealth);
            EnableRagdoll(false);
            EnableDisabledComponents(true);
        }

        /*
         * Temporary debug helpers.
         * Keep these while testing. Remove later if you want.
         */
        private void OnEnable()
        {
            Debug.Log($"{name} enabled.");
        }

        private void OnDisable()
        {
            Debug.LogWarning($"{name} disabled.");
        }

        private void OnDestroy()
        {
            Debug.LogError($"{name} destroyed.");
        }
    }
}