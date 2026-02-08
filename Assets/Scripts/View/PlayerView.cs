using Leopotam.EcsLite;
using Mirror;
using UnityEngine;
using TDS.Bootstrap;
using TDS.Config;
using TDS.Ecs.Components;
using TDS.Net;

namespace TDS.View
{
    [RequireComponent(typeof(Collider2D))]
    public class PlayerView : NetworkBehaviour
    {
        [SyncVar] private int _health;
        [SyncVar(hook = nameof(OnAliveChanged))] private bool _alive = true;
        [SyncVar] private WeaponType _weaponType = WeaponType.None;
        [SyncVar] private int _ammo;
        [SyncVar] private int _magSize;

        private SpriteRenderer _sprite;
        private Collider2D _collider;
        private int _entity = -1;

        public int Health => _health;
        public WeaponType WeaponType => _weaponType;
        public int Ammo => _ammo;
        public int MagSize => _magSize;

        private void Awake()
        {
            _sprite = GetComponentInChildren<SpriteRenderer>();
            _collider = GetComponent<Collider2D>();
        }

        public override void OnStartServer()
        {
            base.OnStartServer();

            var ctx = EcsBootstrap.Instance.Context;
            if (!ctx.ServerInitialized)
                ctx.InitializeServer();

            var world = ctx.World;
            var config = ctx.Config;
            var registry = ctx.Registry;

            _entity = world.NewEntity();

            world.GetPool<PlayerTag>().Add(_entity);
            ref var netIdComp = ref world.GetPool<NetworkId>().Add(_entity);
            netIdComp.Value = netId;

            world.GetPool<InputData>().Add(_entity);
            world.GetPool<AimData>().Add(_entity);

            ref var speed = ref world.GetPool<MoveSpeed>().Add(_entity);
            speed.Value = config.MoveSpeed;

            ref var tr = ref world.GetPool<Transform2D>().Add(_entity);
            tr.Position = transform.position;
            tr.RotationDeg = transform.eulerAngles.z;

            ref var health = ref world.GetPool<Health>().Add(_entity);
            health.Max = config.MaxHealth;
            health.Current = config.MaxHealth;
            health.LastDamager = 0;

            ref var weapon = ref world.GetPool<Weapon>().Add(_entity);
            weapon.Type = WeaponType.None;
            weapon.Range = 0f;
            weapon.Damage = 0;
            weapon.FireCooldown = 0f;
            weapon.NextFireTime = 0f;
            weapon.Ammo = 0;
            weapon.MagSize = 0;
            weapon.Pellets = 1;
            weapon.SpreadDeg = 0f;
            weapon.BulletSpeed = 0f;

            ref var melee = ref world.GetPool<Melee>().Add(_entity);
            melee.Range = config.MeleeRange;
            melee.Damage = config.MeleeDamage;
            melee.Cooldown = config.MeleeCooldown;
            melee.NextTime = 0f;

            ref var view = ref world.GetPool<ViewRef>().Add(_entity);
            view.Transform = transform;
            view.View = this;

            registry.RegisterPlayer(netId, _entity, this);
            ServerSetHealth(health.Current);
            ServerSetAlive(true);
            ServerSetWeapon(WeaponType.None, 0, 0);
        }

        public override void OnStopServer()
        {
            base.OnStopServer();

            var ctx = EcsBootstrap.Instance != null ? EcsBootstrap.Instance.Context : null;
            if (ctx != null && _entity >= 0)
            {
                var world = ctx.World;
                if (world != null)
                    world.DelEntity(_entity);
            }

            if (ctx != null)
                ctx.Registry.UnregisterPlayer(netId);

            _entity = -1;
        }

        private void Update()
        {
            if (!isLocalPlayer)
                return;

            var move = new Vector2(Input.GetAxisRaw("Horizontal"), Input.GetAxisRaw("Vertical"));
            bool fire = Input.GetMouseButton(0);
            bool melee = Input.GetMouseButton(1);

            Vector2 aim = Vector2.right;
            var cam = Camera.main;
            if (cam != null)
            {
                Vector3 mouseWorld = cam.ScreenToWorldPoint(Input.mousePosition);
                aim = (mouseWorld - transform.position);
            }

            CmdSendInput(move, aim, fire, melee);
        }

        [Command(channel = Channels.Unreliable)]
        private void CmdSendInput(Vector2 move, Vector2 aim, bool fire, bool melee)
        {
            var ctx = EcsBootstrap.Instance.Context;
            if (ctx == null || _entity < 0)
                return;

            var world = ctx.World;
            if (world == null)
                return;

            var pool = world.GetPool<InputData>();
            ref var input = ref pool.Get(_entity);
            input.Move = move;
            input.Aim = aim;
            input.Fire = fire;
            input.Melee = melee;
        }

        public void ServerSetHealth(int value)
        {
            _health = value;
        }

        public void ServerSetAlive(bool value)
        {
            _alive = value;
            UpdateAliveVisual();
        }

        public void ServerSetWeapon(WeaponType type, int ammo, int magSize)
        {
            _weaponType = type;
            _ammo = ammo;
            _magSize = magSize;
        }

        [ClientRpc]
        public void RpcSpawnBullet(WeaponType weaponType, Vector2 origin, Vector2 dir, float speed, float range)
        {
            var ctx = EcsBootstrap.Instance != null ? EcsBootstrap.Instance.Context : null;
            if (ctx == null || !ctx.Config.TryGetWeapon(weaponType, out var cfg))
                return;

            if (cfg.BulletPrefab == null)
                return;

            var bullet = BulletPool.Get(cfg.BulletPrefab);
            bullet.Init(origin, dir, speed, range);
        }

        [ClientRpc]
        public void RpcHit(uint sourceNetId, int damage)
        {
        }

        [ClientRpc]
        public void RpcDied(uint killerNetId)
        {
        }

        private void UpdateAliveVisual()
        {
            if (_sprite != null)
                _sprite.enabled = _alive;
            if (_collider != null)
                _collider.enabled = _alive;
        }

        private void OnEnable()
        {
            UpdateAliveVisual();
        }

        private void OnAliveChanged(bool oldValue, bool newValue)
        {
            UpdateAliveVisual();
        }
    }
}
