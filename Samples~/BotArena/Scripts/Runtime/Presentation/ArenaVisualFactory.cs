using System;
using System.Collections.Generic;

using UnityEngine;

namespace Abc.Unity.Samples.BotArena
{
    internal sealed class ArenaVisualFactory : IDisposable
    {
        private static readonly Color PlayerColor = new Color(0.08f, 0.9f, 1f);
        private static readonly Color BotColor = new Color(1f, 0.14f, 0.52f);
        private static readonly Color DarkColor = new Color(0.025f, 0.035f, 0.065f);

        private readonly Transform _entityRoot;
        private readonly List<Material> _materials = new List<Material>(12);
        private readonly Material _playerMaterial;
        private readonly Material _botMaterial;
        private readonly Material _playerProjectileMaterial;
        private readonly Material _botProjectileMaterial;
        private readonly Material _healthMaterial;
        private readonly Material _healthBackMaterial;
        private readonly Material _floorMaterial;
        private readonly Material _gridMaterial;
        private readonly Material _particleMaterial;
        private readonly Shader _fallbackShader;
        private readonly Material _materialTemplate;

        public ArenaVisualFactory(Transform entityRoot, Material materialTemplate)
        {
            _entityRoot = entityRoot;
            _materialTemplate = materialTemplate;
            _fallbackShader = materialTemplate != null ? materialTemplate.shader : Shader.Find("Standard");

            if (_fallbackShader == null)
                throw new InvalidOperationException("Bot Arena could not resolve a compatible shader.");

            _playerMaterial = CreateMaterial(PlayerColor, PlayerColor * 1.7f);
            _botMaterial = CreateMaterial(BotColor, BotColor * 1.4f);
            _playerProjectileMaterial = CreateMaterial(Color.white, PlayerColor * 3f);
            _botProjectileMaterial = CreateMaterial(new Color(1f, 0.75f, 0.2f), BotColor * 2.5f);
            _healthMaterial = CreateMaterial(new Color(0.25f, 1f, 0.48f), new Color(0.1f, 0.6f, 0.2f));
            _healthBackMaterial = CreateMaterial(new Color(0.12f, 0.04f, 0.09f), Color.black);
            _floorMaterial = CreateMaterial(DarkColor, new Color(0.01f, 0.015f, 0.04f));
            _gridMaterial = CreateMaterial(PlayerColor * 0.42f, PlayerColor * 0.7f);
            _particleMaterial = CreateParticleMaterial();
        }

        public ArenaVisualData CreateAgent(string name, Vector3 position, bool isPlayer)
        {
            var color = isPlayer ? PlayerColor : BotColor;
            var material = isPlayer ? _playerMaterial : _botMaterial;
            var body = CreatePrimitive(PrimitiveType.Capsule, name, _entityRoot, material);
            body.transform.position = position;
            body.transform.localScale = isPlayer
                ? new Vector3(1.15f, 0.85f, 1.15f)
                : new Vector3(0.9f, 0.72f, 0.9f);

            var core = CreatePrimitive(PrimitiveType.Sphere, "Energy Core", body.transform, material);
            core.transform.localPosition = new Vector3(0f, 0.32f, 0.16f);
            core.transform.localScale = Vector3.one * (isPlayer ? 0.48f : 0.38f);

            var weapon = CreatePrimitive(PrimitiveType.Cube, "Emitter", body.transform, material);
            weapon.transform.localPosition = new Vector3(0f, 0.15f, 0.78f);
            weapon.transform.localScale = new Vector3(0.18f, 0.18f, 0.9f);

            var healthBack = CreatePrimitive(PrimitiveType.Cube, "Health Back", body.transform, _healthBackMaterial);
            healthBack.transform.localPosition = new Vector3(0f, 1.55f, 0f);
            healthBack.transform.localScale = new Vector3(1.48f, 0.12f, 0.12f);

            var healthFill = CreatePrimitive(PrimitiveType.Cube, "Health", body.transform, _healthMaterial);
            healthFill.transform.localPosition = new Vector3(0f, 1.55f, -0.07f);
            healthFill.transform.localScale = new Vector3(1.35f, 0.075f, 0.075f);

            return new ArenaVisualData(body.transform, healthFill.transform, color);
        }

        public ArenaVisualData CreateProjectile(string name, Vector3 position, bool fromPlayer)
        {
            var material = fromPlayer ? _playerProjectileMaterial : _botProjectileMaterial;
            var color = fromPlayer ? PlayerColor : BotColor;
            var projectile = CreatePrimitive(PrimitiveType.Sphere, name, _entityRoot, material);
            projectile.transform.position = position;
            projectile.transform.localScale = Vector3.one * (fromPlayer ? 0.3f : 0.25f);
            var trail = projectile.AddComponent<TrailRenderer>();
            trail.time = 0.2f;
            trail.startWidth = fromPlayer ? 0.2f : 0.16f;
            trail.endWidth = 0f;
            trail.sharedMaterial = material;
            trail.startColor = color;
            trail.endColor = new Color(color.r, color.g, color.b, 0f);
            trail.minVertexDistance = 0.08f;
            return new ArenaVisualData(projectile.transform, null, color);
        }

        public void CreateBurst(Vector3 position, Color color)
        {
            var burst = new GameObject("Impact Burst");
            burst.transform.SetParent(_entityRoot);
            burst.transform.position = position;
            var particles = burst.AddComponent<ParticleSystem>();
            particles.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            var main = particles.main;
            main.duration = 0.3f;
            main.loop = false;
            main.startLifetime = 0.45f;
            main.startSpeed = 5f;
            main.startSize = 0.16f;
            main.startColor = color;
            main.maxParticles = 24;
            var emission = particles.emission;
            emission.rateOverTime = 0f;
            emission.SetBursts(new[] { new ParticleSystem.Burst(0f, 18) });
            var shape = particles.shape;
            shape.shapeType = ParticleSystemShapeType.Sphere;
            shape.radius = 0.28f;
            var renderer = particles.GetComponent<ParticleSystemRenderer>();
            renderer.sharedMaterial = _particleMaterial;
            particles.Play();
            UnityEngine.Object.Destroy(burst, 1.2f);
        }

        public void BuildEnvironment(Transform root, Camera camera, float radius)
        {
            RenderSettings.ambientLight = new Color(0.16f, 0.19f, 0.3f);
            RenderSettings.fog = true;
            RenderSettings.fogColor = new Color(0.018f, 0.025f, 0.05f);
            RenderSettings.fogDensity = 0.009f;
            camera.backgroundColor = new Color(0.01f, 0.014f, 0.032f);
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.fieldOfView = 50f;
            camera.transform.position = new Vector3(0f, 30f, -22f) * (radius / 16f);
            camera.transform.LookAt(Vector3.zero);

            var floor = CreatePrimitive(PrimitiveType.Cylinder, "Arena Floor", root, _floorMaterial);
            floor.transform.position = new Vector3(0f, -0.18f, 0f);
            floor.transform.localScale = new Vector3(radius * 2.15f, 0.12f, radius * 2.15f);

            BuildGrid(root, radius);
            BuildWall(root, radius);
            BuildLights(root);
            BuildTitle(root, radius);
        }

        public void Dispose()
        {
            for (var i = 0; i < _materials.Count; i++)
            {
                if (_materials[i] != null)
                    UnityEngine.Object.Destroy(_materials[i]);
            }

            _materials.Clear();
        }

        private void BuildGrid(Transform root, float radius)
        {
            const int divisions = 10;
            var extent = radius * 0.9f;

            for (var i = -divisions; i <= divisions; i++)
            {
                var offset = extent * i / divisions;
                var lineExtent = Mathf.Sqrt(extent * extent - offset * offset);
                CreateLine(root, new Vector3(-lineExtent, 0.02f, offset), new Vector3(lineExtent, 0.02f, offset));
                CreateLine(root, new Vector3(offset, 0.02f, -lineExtent), new Vector3(offset, 0.02f, lineExtent));
            }
        }

        private void BuildWall(Transform root, float radius)
        {
            const int segments = 40;

            for (var i = 0; i < segments; i++)
            {
                var angle = Mathf.PI * 2f * i / segments;
                var point = new Vector3(Mathf.Sin(angle), 0f, Mathf.Cos(angle)) * radius;
                var wall = CreatePrimitive(PrimitiveType.Cube, $"Wall {i + 1}", root, i % 2 == 0 ? _gridMaterial : _floorMaterial);
                wall.transform.position = point + Vector3.up * 0.45f;
                wall.transform.rotation = Quaternion.Euler(0f, angle * Mathf.Rad2Deg + 90f, 0f);
                wall.transform.localScale = new Vector3(0.2f, 0.9f, radius * 0.16f);
            }
        }

        private void BuildLights(Transform root)
        {
            var key = new GameObject("Key Light", typeof(Light));
            key.transform.SetParent(root);
            key.transform.rotation = Quaternion.Euler(55f, -28f, 0f);
            var light = key.GetComponent<Light>();
            light.type = LightType.Directional;
            light.color = new Color(0.62f, 0.75f, 1f);
            light.intensity = 1.15f;

            var rim = new GameObject("Arena Glow", typeof(Light));
            rim.transform.SetParent(root);
            rim.transform.position = new Vector3(0f, 7f, 3f);
            light = rim.GetComponent<Light>();
            light.type = LightType.Point;
            light.color = PlayerColor;
            light.range = 32f;
            light.intensity = 1.7f;
        }

        private static void BuildTitle(Transform root, float radius)
        {
            var title = new GameObject("Arena Title", typeof(TextMesh));
            title.transform.SetParent(root);
            title.transform.position = new Vector3(0f, 0.04f, radius * 0.72f);
            title.transform.rotation = Quaternion.Euler(90f, 0f, 0f);
            var text = title.GetComponent<TextMesh>();
            text.text = "ABC  /  BOT ARENA";
            text.anchor = TextAnchor.MiddleCenter;
            text.alignment = TextAlignment.Center;
            text.characterSize = 0.24f;
            text.fontSize = 64;
            text.color = new Color(0.16f, 0.85f, 1f, 0.5f);
        }

        private void CreateLine(Transform root, Vector3 from, Vector3 to)
        {
            var lineObject = new GameObject("Grid Line", typeof(LineRenderer));
            lineObject.transform.SetParent(root);
            var line = lineObject.GetComponent<LineRenderer>();
            line.useWorldSpace = true;
            line.positionCount = 2;
            line.SetPosition(0, from);
            line.SetPosition(1, to);
            line.startWidth = 0.025f;
            line.endWidth = 0.025f;
            line.sharedMaterial = _gridMaterial;
            line.startColor = new Color(0.08f, 0.58f, 0.72f, 0.45f);
            line.endColor = line.startColor;
        }

        private GameObject CreatePrimitive(PrimitiveType type, string name, Transform parent, Material material)
        {
            var instance = GameObject.CreatePrimitive(type);
            instance.name = name;
            instance.transform.SetParent(parent);
            instance.GetComponent<Renderer>().sharedMaterial = material;
            var collider = instance.GetComponent<Collider>();
            if (collider != null)
                UnityEngine.Object.Destroy(collider);

            return instance;
        }

        private Material CreateMaterial(Color color, Color emission)
        {
            var material = _materialTemplate != null
                ? new Material(_materialTemplate)
                : new Material(_fallbackShader);
            material.color = color;

            if (material.HasProperty("_BaseColor"))
                material.SetColor("_BaseColor", color);

            if (material.HasProperty("_EmissionColor"))
            {
                material.EnableKeyword("_EMISSION");
                material.SetColor("_EmissionColor", emission);
            }

            _materials.Add(material);
            return material;
        }

        private Material CreateParticleMaterial()
        {
            var shader = Shader.Find("Particles/Standard Unlit") ??
                         Shader.Find("Sprites/Default") ??
                         _fallbackShader;
            var material = new Material(shader);
            _materials.Add(material);
            return material;
        }
    }
}
