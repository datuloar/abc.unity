using System;

using UnityEngine;

namespace Abc.Unity.Samples.BotArena
{
    internal sealed class ArenaSceneState : IDisposable
    {
        private readonly Camera _camera;
        private readonly Vector3 _cameraPosition;
        private readonly Quaternion _cameraRotation;
        private readonly Color _cameraBackground;
        private readonly CameraClearFlags _cameraClearFlags;
        private readonly float _cameraFieldOfView;
        private readonly Color _ambientLight = RenderSettings.ambientLight;
        private readonly bool _fog = RenderSettings.fog;
        private readonly Color _fogColor = RenderSettings.fogColor;
        private readonly float _fogDensity = RenderSettings.fogDensity;
        private readonly int _targetFrameRate = Application.targetFrameRate;
        private readonly int _antiAliasing = QualitySettings.antiAliasing;

        public ArenaSceneState(Camera camera)
        {
            _camera = camera;
            if (camera == null)
                return;

            _cameraPosition = camera.transform.position;
            _cameraRotation = camera.transform.rotation;
            _cameraBackground = camera.backgroundColor;
            _cameraClearFlags = camera.clearFlags;
            _cameraFieldOfView = camera.fieldOfView;
        }

        public void Dispose()
        {
            RenderSettings.ambientLight = _ambientLight;
            RenderSettings.fog = _fog;
            RenderSettings.fogColor = _fogColor;
            RenderSettings.fogDensity = _fogDensity;
            Application.targetFrameRate = _targetFrameRate;
            QualitySettings.antiAliasing = _antiAliasing;

            if (_camera == null)
                return;

            _camera.transform.SetPositionAndRotation(_cameraPosition, _cameraRotation);
            _camera.backgroundColor = _cameraBackground;
            _camera.clearFlags = _cameraClearFlags;
            _camera.fieldOfView = _cameraFieldOfView;
        }
    }
}
