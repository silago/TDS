# TDS Project Agent Notes

## High-level architecture
- Unity + URP + Mirror (server authoritative).
- ECS: LeoECS Lite, server-only simulation.
- DI: Zenject.

## Networking rules
- Clients send input only.
- Server simulates: movement, damage, shooting, melee, bullets.
- Server sends state via SyncVars and event RPCs.

## Key gameplay model
- Players are ECS entities created in `PlayerView.OnStartServer()`.
- Movement runs in FixedUpdate (`EcsBootstrap.FixedUpdate`).
- Movement uses `Rigidbody2D.MovePosition` if available.
- Health is server authoritative; deaths trigger respawn after delay.
- Weapons are configured in `GameConfig.Weapons` (array by WeaponType).
- Pickups assign weapon config + full ammo.
- Bullets are simulated in ECS and use segment raycast per step.
- Weapon drop is server-authoritative via `DropWeaponSystem` (`G` key from client input).
- Pickup rule: same `WeaponType` is not picked up if already equipped.
- Dropped pickup has short owner-ignore window to avoid instant self-repickup.

## Visuals
- Player visuals in `PlayerView`.
- Bullet visuals are client-only and spawned via RPC.
- Server sends `RpcBulletHit(bulletId)` to despawn visuals on impact.
- Bullet visuals are pooled with `BulletPool`.

## Scene setup essentials
- `Arena` scene.
- `SceneContext` with `ArenaInstaller` binding `GameConfig`.
- `EcsBootstrap` (no NetworkIdentity).
- `ArenaNetworkManager` with transport and player prefab.
- `MatchStateView` object with NetworkIdentity.
- Player prefab: NetworkIdentity, PlayerView, NetworkTransform, Rigidbody2D, Collider2D.
- Walls: TilemapCollider2D (optional Composite), layer in GameConfig.HitMask.
- Spawn points tagged `SpawnPoint`.
- `ArenaNetworkManager.DroppedPickupPrefab` should point to the same pickup prefab used in scene.
- Optional bots: set `ArenaNetworkManager.BotCount` (`0..7`).

## Bots (A*)
- Bots are regular network-spawned player prefabs with `PlayerView.ConfigureAsBot()`.
- ECS tags/components: `BotTag`, `BotAgent`.
- AI system: `BotInputSystem` writes `InputData` on server.
- Pathfinding: grid A* built from colliders in `GameConfig.HitMask`.
- Bot tuning in `GameConfig`: `BotRepathInterval`, `BotStopDistance`, `BotFireDistance`, `BotGridCellSize`.
- Bots start with `Pistol` from `GameConfig.Weapons`.

## Known design decisions
- Bullets are not NetworkIdentity objects.
- Player UI uses events from `PlayerView` SyncVar hooks (HUD updates on change).
- `ArenaNetworkManager` remains `NetworkManager` (not `NetworkBehaviour`); synced match fields live in `MatchStateView`.
