using UnityEngine;

using Abc.Unity;

namespace Abc.Unity.Samples.BotArena
{
    [AddComponentMenu("ABC/Samples/Bot Arena")]
    public sealed class BotArenaBootstrap : MonoBehaviour
    {
        private struct MoveAgentsAction : IActorQueryAction<ArenaPositionData, ArenaVelocityData, ArenaAgentData>
        {
            public float DeltaTime;
            public float ArenaRadius;

            public void Execute(
                ActorModel actor,
                ArenaPositionData position,
                ArenaVelocityData velocity,
                ArenaAgentData agent)
            {
                position.PreviousValue = position.Value;
                var next = position.Value + velocity.Value * DeltaTime;
                var horizontal = new Vector2(next.x, next.z);
                var limit = ArenaRadius - agent.Radius;
                if (horizontal.sqrMagnitude > limit * limit)
                {
                    horizontal = horizontal.normalized * limit;
                    next.x = horizontal.x;
                    next.z = horizontal.y;
                }

                position.Value = next;
            }
        }

        private struct MoveProjectilesAction : IActorQueryAction<ArenaProjectileData, ArenaPositionData, ArenaVelocityData>
        {
            public float DeltaTime;

            public void Execute(
                ActorModel actor,
                ArenaProjectileData projectile,
                ArenaPositionData position,
                ArenaVelocityData velocity)
            {
                position.PreviousValue = position.Value;
                position.Value += velocity.Value * Mathf.Min(DeltaTime, Mathf.Max(0f, projectile.Lifetime));
                projectile.Lifetime -= DeltaTime;
            }
        }

        private struct ResolveCollisionsAction : IActorQueryAction<ArenaProjectileData, ArenaPositionData>
        {
            public ArenaSession Session;

            public void Execute(ActorModel actor, ArenaProjectileData projectile, ArenaPositionData position) =>
                Session.ResolveProjectile(actor, projectile, position);
        }

        private struct SyncVisualsAction : IActorQueryAction<ArenaPositionData, ArenaVelocityData, ArenaVisualData>
        {
            public void Execute(
                ActorModel actor,
                ArenaPositionData position,
                ArenaVelocityData velocity,
                ArenaVisualData visual) => visual.Sync(position.Value, velocity.Value);
        }

        [SerializeField, Range(10f, 30f)] private float _arenaRadius = 16f;
        [SerializeField, Range(10, 250)] private int _stressBotCount = 100;

        private ActorWorld _world;
        private ArenaSession _session;
        private ArenaVisualFactory _visuals;
        private ArenaSceneState _sceneState;
        private ActorWorldQuery<ArenaPositionData, ArenaVelocityData, ArenaAgentData> _agentQuery;
        private ActorWorldQuery<ArenaProjectileData, ArenaPositionData, ArenaVelocityData> _projectileQuery;
        private ActorWorldQuery<ArenaProjectileData, ArenaPositionData> _collisionQuery;
        private ActorWorldQuery<ArenaPositionData, ArenaVelocityData, ArenaVisualData> _visualQuery;
        private MoveAgentsAction _moveAgents;
        private MoveProjectilesAction _moveProjectiles;
        private ResolveCollisionsAction _resolveCollisions;
        private SyncVisualsAction _syncVisuals;
        private Camera _camera;
        private Transform _entityRoot;
        private Texture2D _panelTexture;
        private GUIStyle _headerStyle;
        private GUIStyle _bodyStyle;
        private GUIStyle _smallStyle;
        private GUIStyle _centerStyle;
        private GUIStyle _buttonStyle;
        private float _smoothedFps;
        private float _hudTimer;
        private int _queryMatches;
        private string _statusText = string.Empty;
        private string _performanceText = string.Empty;
        private bool _paused;

        private void Start()
        {
            _camera = Camera.main;
            _sceneState = new ArenaSceneState(_camera);
            Application.targetFrameRate = 144;
            QualitySettings.antiAliasing = Mathf.Max(QualitySettings.antiAliasing, 4);

            if (_camera == null)
            {
                var cameraObject = new GameObject("Main Camera", typeof(Camera), typeof(AudioListener));
                cameraObject.transform.SetParent(transform);
                cameraObject.tag = "MainCamera";
                _camera = cameraObject.GetComponent<Camera>();
            }

            var environment = new GameObject("Arena Environment").transform;
            environment.SetParent(transform);
            _entityRoot = new GameObject("Runtime Actors").transform;
            _entityRoot.SetParent(transform);
            _visuals = new ArenaVisualFactory(_entityRoot);
            _visuals.BuildEnvironment(environment, _camera, _arenaRadius);
            ResetSimulation();
        }

        private void Update()
        {
#if ENABLE_LEGACY_INPUT_MANAGER
            if (Input.GetKeyDown(KeyCode.R))
                ResetSimulation();

            if (Input.GetKeyDown(KeyCode.P))
                _paused = !_paused;

            if (Input.GetKeyDown(KeyCode.B))
                _session?.SpawnStressBots(_stressBotCount);
#endif

            var unscaledDeltaTime = Time.unscaledDeltaTime;
            _smoothedFps = Mathf.Lerp(_smoothedFps, 1f / Mathf.Max(0.0001f, unscaledDeltaTime), 0.08f);

            if (!_paused && _world != null)
                Simulate(Time.deltaTime);

            _hudTimer -= unscaledDeltaTime;
            if (_hudTimer <= 0f)
            {
                _hudTimer = 0.2f;
                RefreshHud();
            }
        }

        private void OnGUI()
        {
            if (_session == null)
                return;

            var previousColor = GUI.color;
            var previousContentColor = GUI.contentColor;
            try
            {
                GUI.color = Color.white;
                GUI.contentColor = Color.white;
                DrawHud();
            }
            finally
            {
                GUI.color = previousColor;
                GUI.contentColor = previousContentColor;
            }
        }

        private void DrawHud()
        {
            EnsureStyles();
            DrawPanel(new Rect(24f, 24f, 285f, 166f));
            GUI.Label(new Rect(44f, 39f, 250f, 30f), "ABC 2.0  /  BOT ARENA", _headerStyle);
            GUI.Label(new Rect(44f, 76f, 245f, 78f), _statusText, _bodyStyle);
            GUI.Label(new Rect(44f, 145f, 245f, 30f), _performanceText, _smallStyle);

            DrawPanel(new Rect(Screen.width - 304f, 24f, 280f, 166f));
            GUI.Label(new Rect(Screen.width - 284f, 40f, 240f, 25f), "CONTROLS", _headerStyle);
#if ENABLE_LEGACY_INPUT_MANAGER
            GUI.Label(new Rect(Screen.width - 284f, 75f, 240f, 70f), "WASD / Arrows   Move\nMouse / Space   Fire\nB   + stress bots", _bodyStyle);
#else
            GUI.Label(new Rect(Screen.width - 284f, 75f, 240f, 70f), "Enable Legacy Input Manager\nto control the arena", _bodyStyle);
#endif
            if (GUI.Button(new Rect(Screen.width - 284f, 151f, 105f, 26f), _paused ? "RESUME" : "PAUSE", _buttonStyle))
                _paused = !_paused;

            if (GUI.Button(new Rect(Screen.width - 169f, 151f, 125f, 26f), "RESTART", _buttonStyle))
                ResetSimulation();

            DrawHealthBar();

            if (_session.IsGameOver)
                DrawGameOver();
            else
                DrawCrosshair();
        }

        private void OnDestroy()
        {
            _world?.Dispose();
            _visuals?.Dispose();
            _sceneState?.Dispose();

            if (_panelTexture != null)
                Destroy(_panelTexture);
        }

        private void Simulate(float deltaTime)
        {
            _world.Tick(deltaTime);
            _moveAgents.DeltaTime = deltaTime;
            _moveAgents.ArenaRadius = _arenaRadius;
            _moveProjectiles.DeltaTime = deltaTime;
            _resolveCollisions.Session = _session;
            _queryMatches = _agentQuery.For(ref _moveAgents);
            _queryMatches += _projectileQuery.For(ref _moveProjectiles);
            _queryMatches += _collisionQuery.For(ref _resolveCollisions);
            _queryMatches += _visualQuery.For(ref _syncVisuals);
            _session.Update(deltaTime);
        }

        private void ResetSimulation()
        {
            var blueprint = Resources.Load<ActorBlueprint>("BotBlueprint");

            if (blueprint == null)
            {
                Debug.LogError("Bot Arena requires Resources/BotBlueprint.asset.", this);
                enabled = false;
                return;
            }

            _world?.Dispose();
            _world = new ActorWorld("Bot Arena", 256);
            _session = new ArenaSession(_world, _visuals, blueprint, _camera, _arenaRadius);
            _agentQuery = _world.Query<ArenaPositionData, ArenaVelocityData, ArenaAgentData>().OnlyAlive();
            _projectileQuery = _world.Query<ArenaProjectileData, ArenaPositionData, ArenaVelocityData>().OnlyAlive();
            _collisionQuery = _world.Query<ArenaProjectileData, ArenaPositionData>().OnlyAlive();
            _visualQuery = _world.Query<ArenaPositionData, ArenaVelocityData, ArenaVisualData>().OnlyAlive();
            _session.Begin();
            _paused = false;
            RefreshHud();
        }

        private void RefreshHud()
        {
            if (_session == null || _world == null)
                return;

            _statusText = $"Wave  {_session.Wave:00}      Score  {_session.Score:N0}\nBots  {_session.ActiveBots:000}      Shots  {_session.ProjectilesFired:N0}";
            _performanceText = $"{_world.Count:N0} models  /  {_queryMatches:N0} query hits  /  {_smoothedFps:0} FPS";
        }

        private void EnsureStyles()
        {
            if (_headerStyle != null)
                return;

            _panelTexture = new Texture2D(1, 1) { hideFlags = HideFlags.HideAndDontSave };
            _panelTexture.SetPixel(0, 0, new Color(0.025f, 0.035f, 0.07f, 0.92f));
            _panelTexture.Apply();
            _headerStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 18,
                fontStyle = FontStyle.Bold,
                normal = { textColor = new Color(0.16f, 0.9f, 1f) }
            };
            _bodyStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 15,
                normal = { textColor = new Color(0.88f, 0.93f, 1f) }
            };
            _smallStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 11,
                normal = { textColor = new Color(0.45f, 0.7f, 0.82f) }
            };
            _centerStyle = new GUIStyle(_headerStyle)
            {
                alignment = TextAnchor.MiddleCenter,
                fontSize = 30
            };
            _buttonStyle = new GUIStyle(GUI.skin.button)
            {
                fontSize = 12,
                fontStyle = FontStyle.Bold
            };
        }

        private void DrawPanel(Rect rect)
        {
            var previous = GUI.color;
            GUI.color = Color.white;
            GUI.DrawTexture(rect, _panelTexture, ScaleMode.StretchToFill);
            GUI.color = new Color(0.1f, 0.75f, 0.95f, 0.55f);
            GUI.DrawTexture(new Rect(rect.x, rect.y, 3f, rect.height), Texture2D.whiteTexture);
            GUI.color = previous;
        }

        private void DrawHealthBar()
        {
            var width = Mathf.Min(440f, Screen.width * 0.42f);
            var rect = new Rect((Screen.width - width) * 0.5f, Screen.height - 48f, width, 16f);
            GUI.color = new Color(0.08f, 0.02f, 0.06f, 0.9f);
            GUI.DrawTexture(rect, Texture2D.whiteTexture);
            GUI.color = Color.Lerp(new Color(1f, 0.12f, 0.3f), new Color(0.1f, 0.95f, 0.75f), _session.PlayerHealth);
            GUI.DrawTexture(new Rect(rect.x + 2f, rect.y + 2f, (rect.width - 4f) * _session.PlayerHealth, rect.height - 4f), Texture2D.whiteTexture);
            GUI.color = Color.white;
            GUI.Label(new Rect(rect.x, rect.y - 40f, rect.width, 38f), "PLAYER CORE", _centerStyle);
        }

        private void DrawGameOver()
        {
            var rect = new Rect(Screen.width * 0.5f - 220f, Screen.height * 0.5f - 80f, 440f, 160f);
            DrawPanel(rect);
            GUI.Label(new Rect(rect.x, rect.y + 24f, rect.width, 42f), "CORE OFFLINE", _centerStyle);
            GUI.Label(new Rect(rect.x, rect.y + 72f, rect.width, 28f), $"Final score  {_session.Score:N0}", _centerStyle);
            if (GUI.Button(new Rect(rect.x + 145f, rect.y + 116f, 150f, 30f), "REBOOT  [R]", _buttonStyle))
                ResetSimulation();
        }

        private static void DrawCrosshair()
        {
#if ENABLE_LEGACY_INPUT_MANAGER
            var point = Input.mousePosition;
            point.y = Screen.height - point.y;
            GUI.color = new Color(0.2f, 0.95f, 1f, 0.8f);
            GUI.DrawTexture(new Rect(point.x - 9f, point.y - 1f, 18f, 2f), Texture2D.whiteTexture);
            GUI.DrawTexture(new Rect(point.x - 1f, point.y - 9f, 2f, 18f), Texture2D.whiteTexture);
            GUI.color = Color.white;
#endif
        }
    }
}
