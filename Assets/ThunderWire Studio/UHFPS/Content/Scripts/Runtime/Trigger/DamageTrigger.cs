using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UHFPS.Tools;
using Newtonsoft.Json.Linq;

namespace UHFPS.Runtime
{
    public class DamageTrigger : MonoBehaviour, ISaveable
    {
        [Flags]
        public enum DamageReceiverEnum { Player = 1, Enemy = 2, Breakable = 4 }
        public enum DamageTypeEnum { Once, MoreTimes, Stay }

        public DamageReceiverEnum DamageReceiver = DamageReceiverEnum.Player;
        public DamageTypeEnum DamageType = DamageTypeEnum.Once;

        public Tag EnemyTag;
        public bool DamageInRange;
        public bool InstantDeath;

        public uint Damage;
        public MinMaxInt DamageRange;
        public float DamageRate;

        public UnityEvent<uint> OnDamage;

        private float damageTime;
        private bool damageOnce;

        private readonly HashSet<BaseHealthEntity> damagedHealthEntities = new();
        private readonly HashSet<BaseBreakableEntity> damagedBreakables = new();

        private void OnTriggerEnter(Collider other)
        {
            if (DamageType == DamageTypeEnum.Stay)
                return;

            if (DamageType == DamageTypeEnum.Once && damageOnce)
                return;

            TryApplyDamage(other, true);
        }

        private void OnTriggerExit(Collider other)
        {
            if (DamageType != DamageTypeEnum.MoreTimes)
                return;

            BaseHealthEntity health = other.GetComponentInParent<BaseHealthEntity>();
            if (health != null)
                damagedHealthEntities.Remove(health);

            BaseBreakableEntity breakable = other.GetComponentInParent<BaseBreakableEntity>();
            if (breakable != null)
                damagedBreakables.Remove(breakable);
        }

        private void OnTriggerStay(Collider other)
        {
            if (DamageType != DamageTypeEnum.Stay || damageOnce || damageTime > 0f)
                return;

            if (TryApplyDamage(other, false))
                damageTime = DamageRate;
        }

        private bool TryApplyDamage(Collider other, bool trackEntry)
        {
            uint damage = DamageInRange ? (uint)DamageRange.Random() : Damage;

            BaseHealthEntity health = other.GetComponentInParent<BaseHealthEntity>();
            if (health != null)
            {
                GameObject target = health.gameObject;

                if (target.CompareTag("Player") && DamageReceiver.HasFlag(DamageReceiverEnum.Player))
                {
                    if (trackEntry && damagedHealthEntities.Contains(health))
                        return false;

                    if (InstantDeath) health.ApplyDamageMax(transform);
                    else health.OnApplyDamage((int)damage, transform);

                    if (trackEntry)
                        damagedHealthEntities.Add(health);

                    if (DamageType == DamageTypeEnum.Once)
                        damageOnce = true;

                    OnDamage?.Invoke(damage);
                    return true;
                }

                if (DamageReceiver.HasFlag(DamageReceiverEnum.Enemy) && target.CompareTag(EnemyTag))
                {
                    if (trackEntry && damagedHealthEntities.Contains(health))
                        return false;

                    if (InstantDeath) health.ApplyDamageMax(transform);
                    else health.OnApplyDamage((int)damage, transform);

                    if (trackEntry)
                        damagedHealthEntities.Add(health);

                    if (DamageType == DamageTypeEnum.Once)
                        damageOnce = true;

                    OnDamage?.Invoke(damage);
                    return true;
                }
            }

            BaseBreakableEntity breakable = other.GetComponentInParent<BaseBreakableEntity>();
            if (breakable != null && DamageReceiver.HasFlag(DamageReceiverEnum.Breakable))
            {
                if (trackEntry && damagedBreakables.Contains(breakable))
                    return false;

                if (InstantDeath) breakable.ApplyDamageMax(transform);
                else breakable.OnApplyDamage((int)damage, transform);

                if (trackEntry)
                    damagedBreakables.Add(breakable);

                if (DamageType == DamageTypeEnum.Once)
                    damageOnce = true;

                OnDamage?.Invoke(damage);
                return true;
            }

            return false;
        }

        private void Update()
        {
            if (DamageType == DamageTypeEnum.Stay && !damageOnce && damageTime > 0f)
                damageTime -= Time.deltaTime;
        }

        public StorableCollection OnSave()
        {
            return new StorableCollection()
            {
                { nameof(damageOnce), damageOnce },
            };
        }

        public void OnLoad(JToken data)
        {
            damageOnce = (bool)data[nameof(damageOnce)];
        }
    }
}