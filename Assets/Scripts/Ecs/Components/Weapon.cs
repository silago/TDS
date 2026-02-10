using TDS.Config;
using UnityEngine;

namespace TDS.Ecs.Components
{
    public struct Weapon
    {
        public WeaponType Type;
        public WeaponFireMode FireMode;
        public float Range;
        public int Damage;
        public float FireCooldown;
        public float NextFireTime;
        public int Ammo;
        public int MagSize;
        public int Pellets;
        public float SpreadDeg;
        public float BulletSpeed;
        public float ShootOffset;
        public bool CanDrop;
        
        public void Clear()
        {
            Type = WeaponType.None;
            FireMode = WeaponFireMode.Melee;
            Range = 0f;
            Damage = 0;
            FireCooldown = 0f;
            NextFireTime = 0f;
            Ammo = 0;
            MagSize = 0;
            Pellets = 1;
            SpreadDeg = 0f;
            BulletSpeed = 0f;
            ShootOffset = 0f;
            CanDrop = false;
        }

        public void Apply(in GameConfig.WeaponConfig cfg)
        {
            Type = cfg.Id;
            FireMode = cfg.FireMode;
            Range = cfg.Range;
            Damage = cfg.Damage;
            FireCooldown = 1f / Mathf.Max(0.01f, cfg.FireRate);
            NextFireTime = 0f;
            MagSize = FireMode == WeaponFireMode.Melee ? 0 : Mathf.Max(1, cfg.MagSize);
            Ammo = MagSize;
            Pellets = Mathf.Max(1, cfg.Pellets);
            SpreadDeg = Mathf.Max(0f, cfg.SpreadDeg);
            BulletSpeed = Mathf.Max(0.1f, cfg.BulletSpeed);
            ShootOffset = cfg.ShootOffset;
            CanDrop = cfg.CanDrop;
        }
    }
}
