using System;
using System.Collections.Generic;

using UnityEngine;

using Abc.Unity;

namespace Abc.Unity.Samples.BotArena
{
    internal sealed class ArenaSession
    {
        private readonly struct Target
        {
            public Target(ActorModel actor)
            {
                Actor = actor;
                Agent = actor.GetData<ArenaAgentData>();
                Health = actor.GetData<ArenaHealthData>();
                Position = actor.GetData<ArenaPositionData>();
            }

            public ActorModel Actor { get; }
            public ArenaAgentData Agent { get; }
            public ArenaHealthData Health { get; }
            public ArenaPositionData Position { get; }
        }

        private const float GoldenAngle = 2.39996323f;

        private readonly ActorWorld _world;
        private readonly ArenaVisualFactory _visuals;
        private readonly ActorBlueprint _botBlueprint;
        private readonly Camera _camera;
        private readonly float _arenaRadius;
        private readonly List<Target> _targets = new List<Target>(128);

        private ActorModel _player;
        private ArenaPositionData _playerPosition;
        private ArenaHealthData _playerHealth;
        private float _nextWaveDelay = -1f;
        private int _botSerial;
        private int _projectileSerial;

        public ArenaSession(
            ActorWorld world,
            ArenaVisualFactory visuals,
            ActorBlueprint botBlueprint,
            Camera camera,
            float arenaRadius)
        {
            _world = world ?? throw new ArgumentNullException(nameof(world));
            _visuals = visuals ?? throw new ArgumentNullException(nameof(visuals));
            _botBlueprint = botBlueprint ?? throw new ArgumentNullException(nameof(botBlueprint));
            _camera = camera ?? throw new ArgumentNullException(nameof(camera));
            _arenaRadius = arenaRadius;
        }

        public int ActiveBots { get; private set; }
        public int Score { get; private set; }
        public int Wave { get; private set; }
        public int ProjectilesFired { get; private set; }
        public bool IsGameOver { get; private set; }
        public float PlayerHealth => _playerHealth?.Normalized ?? 0f;

        public void Begin()
        {
            SpawnPlayer();
            StartNextWave();
        }

        public void Update(float deltaTime)
        {
            if (IsGameOver || _nextWaveDelay < 0f)
                return;

            _nextWaveDelay -= deltaTime;
            if (_nextWaveDelay <= 0f)
                StartNextWave();
        }

        public bool TryGetPlayerPosition(out Vector3 position)
        {
            if (_player != null && _playerHealth is { IsAlive: true })
            {
                position = _playerPosition.Value;
                return true;
            }

            position = default;
            return false;
        }

        public Vector3 GetAimDirection(Vector3 origin)
        {
#if ENABLE_LEGACY_INPUT_MANAGER
            var ray = _camera.ScreenPointToRay(Input.mousePosition);
            var plane = new Plane(Vector3.up, new Vector3(0f, origin.y, 0f));
            if (plane.Raycast(ray, out var distance))
            {
                var direction = ray.GetPoint(distance) - origin;
                direction.y = 0f;
                if (direction.sqrMagnitude > 0.001f)
                    return direction.normalized;
            }
#endif

            return Vector3.forward;
        }

        public void SpawnProjectile(IActor owner, Vector3 origin, Vector3 direction, ArenaWeaponData weapon)
        {
            if (IsGameOver || !owner.TryGetData<ArenaAgentData>(out var agent))
                return;

            origin.y = 0.75f;
            var visual = _visuals.CreateProjectile($"Projectile {++_projectileSerial}", origin, agent.IsPlayer);
            visual.Heading = direction;
            var projectile = new ActorModel($"Projectile {_projectileSerial}", ArenaTags.Projectile)
                .WithData(new ArenaPositionData(origin))
                .WithData(new ArenaVelocityData(direction * weapon.ProjectileSpeed))
                .WithData(new ArenaProjectileData(agent.IsPlayer, weapon.Damage, 2.3f))
                .WithData(visual);
            _world.Add(projectile);
            ProjectilesFired++;
        }

        public void ResolveProjectile(ActorModel projectile, ArenaProjectileData shot, ArenaPositionData position)
        {
            ActorModel closest = null;
            var closestFraction = float.PositiveInfinity;

            for (var i = 0; i < _targets.Count; i++)
            {
                var target = _targets[i];
                if (target.Agent.IsPlayer == shot.FromPlayer || !target.Health.IsAlive || !_world.Contains(target.Actor))
                    continue;

                var start = position.PreviousValue - target.Position.PreviousValue;
                var end = position.Value - target.Position.Value;
                if (!ArenaHitTest.TryHit(start, end, target.Agent.Radius + 0.22f, out var fraction) ||
                    fraction >= closestFraction)
                    continue;

                closest = target.Actor;
                closestFraction = fraction;
            }

            if (closest != null)
                closest.SendCommand(new ArenaDamageCommand(shot.Damage));

            if (closest != null || shot.Lifetime <= 0f || position.Value.sqrMagnitude > _arenaRadius * _arenaRadius * 1.8f)
                _world.Despawn(projectile);
        }

        public void OnAgentKilled(ActorModel actor)
        {
            if (actor == null || !actor.TryGetData<ArenaAgentData>(out var agent) ||
                !actor.TryGetData<ArenaPositionData>(out var position) ||
                !actor.TryGetData<ArenaVisualData>(out var visual))
                return;

            _visuals.CreateBurst(position.Value, visual.Accent);
            RemoveTarget(actor);

            if (agent.IsPlayer)
            {
                _player = null;
                IsGameOver = true;
            }
            else
            {
                ActiveBots--;
                Score += 100 * Wave;
                if (ActiveBots == 0)
                    _nextWaveDelay = 2.2f;
            }

            _world.Despawn(actor);
        }

        public void SpawnStressBots(int count)
        {
            if (IsGameOver)
                return;

            count = Mathf.Clamp(count, 1, 250);
            for (var i = 0; i < count; i++)
                SpawnBot();
        }

        private void SpawnPlayer()
        {
            var position = new ArenaPositionData(new Vector3(0f, 0.75f, -2f));
            var health = new ArenaHealthData();
            health.Configure(300f);
            var agent = new ArenaAgentData();
            agent.Configure(true, 8.5f, 0.68f);
            var weapon = new ArenaWeaponData();
            weapon.Configure(0.12f, 24f, 19f);
            var visual = _visuals.CreateAgent("Player", position.Value, true);

            _player = new ActorModel("Player", ArenaTags.Player)
                .WithData(position)
                .WithData(new ArenaVelocityData(Vector3.zero))
                .WithData(agent)
                .WithData(health)
                .WithData(weapon)
                .WithData(visual)
                .WithBehaviour(new ArenaHealthBehaviour(this))
                .WithBehaviour(new ArenaWeaponBehaviour(this))
                .WithBehaviour(new ArenaPlayerBehaviour(this));

            _world.Add(_player);
            _targets.Add(new Target(_player));
            _playerPosition = position;
            _playerHealth = health;
        }

        private void StartNextWave()
        {
            Wave++;
            _nextWaveDelay = -1f;
            var count = Mathf.Min(8 + Wave * 2, 36);

            for (var i = 0; i < count; i++)
                SpawnBot();
        }

        private void SpawnBot()
        {
            var actor = new ActorModel($"Bot {++_botSerial}", ArenaTags.Bot)
                .WithBlueprint(_botBlueprint);
            var agent = actor.GetData<ArenaAgentData>();
            var health = actor.GetData<ArenaHealthData>();
            var weapon = actor.GetData<ArenaWeaponData>();
            var bot = actor.GetData<ArenaBotData>();
            var difficulty = Mathf.Clamp(Wave - 1, 0, 14);
            agent.Configure(false, agent.MoveSpeed * (1f + difficulty * 0.035f), agent.Radius);
            health.Configure(health.Maximum * (1f + difficulty * 0.14f));
            weapon.Configure(weapon.FireInterval / (1f + difficulty * 0.04f),
                weapon.Damage * (1f + difficulty * 0.12f), weapon.ProjectileSpeed);
            weapon.Delay(1.1f + _botSerial % 7 * 0.17f);
            bot.Configure(bot.PreferredRange, bot.ShootRange,
                bot.OrbitDirection * (_botSerial % 2 == 0 ? 1f : -1f));

            var angle = (_botSerial + Wave * 0.4f) * GoldenAngle;
            var distance = _arenaRadius * (0.68f + (_botSerial % 5) * 0.035f);
            var spawn = new Vector3(Mathf.Sin(angle) * distance, 0.68f, Mathf.Cos(angle) * distance);
            var visual = _visuals.CreateAgent(actor.Name, spawn, false);
            actor.WithData(new ArenaPositionData(spawn))
                .WithData(new ArenaVelocityData(Vector3.zero))
                .WithData(visual)
                .WithBehaviour(new ArenaHealthBehaviour(this))
                .WithBehaviour(new ArenaWeaponBehaviour(this))
                .WithBehaviour(new ArenaBotBehaviour(this));

            _world.Add(actor);
            _targets.Add(new Target(actor));
            ActiveBots++;
        }

        private void RemoveTarget(ActorModel actor)
        {
            for (var i = 0; i < _targets.Count; i++)
            {
                if (!ReferenceEquals(_targets[i].Actor, actor))
                    continue;

                var last = _targets.Count - 1;
                _targets[i] = _targets[last];
                _targets.RemoveAt(last);
                return;
            }
        }
    }
}
