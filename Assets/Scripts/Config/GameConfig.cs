using System;
using UnityEngine;
using TDS.Ecs.Components;
using TDS.View;

namespace TDS.Config
{
    [CreateAssetMenu(menuName = "TDS/Game Config", fileName = "GameConfig")]
    public class GameConfig : ScriptableObject
    {
        [Serializable]
        public struct WeaponConfig
        {
            public WeaponType Id;
            public BulletVisual BulletPrefab;
            public float Range;
            public int Damage;
            public float FireRate;
            public int MagSize;
            public int Pellets;
            public float SpreadDeg;
            public float BulletSpeed;
            public float ShootOffset;
        }

        [Header("Player")]
        public float MoveSpeed = 6f;
        public int MaxHealth = 100;
        public float RespawnDelay = 3f;

        [Header("Melee")]
        public float MeleeRange = 0.9f;
        public int MeleeDamage = 25;
        public float MeleeCooldown = 0.5f;

        [Header("Weapons")]
        public WeaponConfig[] Weapons;

        [Header("Pickups")]
        public float PickupRadius = 0.8f;

        [Header("Bots")]
        public float BotRepathInterval = 0.35f;
        public float BotStopDistance = 1.4f;
        public float BotFireDistance = 10f;
        public float BotGridCellSize = 0.5f;

        [Header("Match")]
        public float MatchDuration = 180f;

        [Header("Physics")]
        public LayerMask HitMask;

        public bool TryGetWeapon(WeaponType id, out WeaponConfig config)
        {
            if (Weapons != null)
            {
                for (int i = 0; i < Weapons.Length; i++)
                {
                    if (Weapons[i].Id == id)
                    {
                        config = Weapons[i];
                        return true;
                    }
                }
            }

            config = default;
            return false;
        }
    }
}
