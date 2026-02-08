namespace TDS.Ecs.Components
{
    public struct Weapon
    {
        public WeaponType Type;
        public float Range;
        public int Damage;
        public float FireCooldown;
        public float NextFireTime;
        public int Ammo;
        public int MagSize;
        public int Pellets;
        public float SpreadDeg;
        public float BulletSpeed;
    }
}
