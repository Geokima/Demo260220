using System;
using System.Collections.Generic;
using Framework;
using Framework.Modules.Res;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnitySceneManager = UnityEngine.SceneManagement.SceneManager;
using YooAsset;
using static Framework.Logger;

namespace Framework.Modules.Scene
{
    /// <summary>
    /// 场景系统实现类
    /// </summary>
    public class SceneSystem : AbstractSystem, ISceneSystem, IUpdateable
    {
        #region Fields

        /// <inheritdoc />
        public List<string> CurrentScenes { get; private set; } = new();

        private List<string> _pendingScenes = new();
        private List<SceneHandle> _loadingSceneHandles = new();
        private List<SceneHandle> _loadedSceneHandles = new();
        private bool _isLoading;

        #endregion

        #region Lifecycle

        /// <inheritdoc />
        public override void Init()
        {
            UnitySceneManager.sceneLoaded += OnSceneLoaded;
            UnitySceneManager.sceneUnloaded += OnSceneUnloaded;
        }

        /// <inheritdoc />
        public override void Deinit()
        {
            UnitySceneManager.sceneLoaded -= OnSceneLoaded;
            UnitySceneManager.sceneUnloaded -= OnSceneUnloaded;

            CurrentScenes.Clear();
            _pendingScenes.Clear();
            _loadingSceneHandles.Clear();
            _loadedSceneHandles.Clear();
            _isLoading = false;
        }

        /// <inheritdoc />
        public void OnUpdate()
        {
            if (_isLoading)
            {
                if (GetSceneLoadProgress() >= 0.9f) // Unity AsyncOperation.progress ends at 0.9 for load
                {
                    if (CheckAllLoaded())
                    {
                        Log($"[Scene] Scene loading completed: {string.Join(", ", CurrentScenes)}");
                        _isLoading = false;
                        _loadingSceneHandles.Clear();
                        this.SendEvent(new SceneLoadCompleteEvent { SceneNames = CurrentScenes.ToArray() });
                    }
                }
                else
                {
                    this.SendEvent(new SceneLoadProgressEvent
                    {
                        SceneNames = CurrentScenes.ToArray(),
                        Progress = GetSceneLoadProgress()
                    });
                }
            }
        }

        #endregion

        #region Public Methods

        /// <inheritdoc />
        public void LoadScene(string scenePath)
        {
            if (_isLoading)
            {
                LogError($"[Scene] Already loading scenes: {string.Join(", ", CurrentScenes)}");
                return;
            }

            if (CurrentScenes.Contains(scenePath))
            {
                LogError($"[Scene] Duplicate scene: {scenePath}");
                return;
            }

            _isLoading = true;
            this.SendEvent(new SceneLoadStartEvent { SceneName = scenePath });
            Log($"[Scene] Loading scene: {scenePath}");
            _pendingScenes.Add(scenePath);

            if (CurrentScenes.Count > 0)
            {
                foreach (var handle in _loadedSceneHandles)
                {
                    if (handle != null && handle.IsValid)
                        handle.UnloadAsync();
                }
            }
            else
            {
                LoadPendingScene();
            }
        }

        /// <inheritdoc />
        public void LoadScenes(string[] scenePaths)
        {
            if (_isLoading)
            {
                LogError($"[Scene] Already loading scenes: {string.Join(", ", CurrentScenes)}");
                return;
            }

            foreach (var scenePath in scenePaths)
            {
                if (CurrentScenes.Contains(scenePath))
                {
                    LogError($"[Scene] Duplicate scene: {scenePath}");
                    return;
                }
            }

            _isLoading = true;
            this.SendEvent(new SceneLoadStartEvent { SceneName = string.Join(", ", scenePaths) });
            _pendingScenes.AddRange(scenePaths);

            if (CurrentScenes.Count > 0)
            {
                foreach (var handle in _loadedSceneHandles)
                {
                    if (handle != null && handle.IsValid)
                        handle.UnloadAsync();
                }
            }
            else
            {
                LoadPendingScene();
            }
        }

        /// <inheritdoc />
        public float GetSceneLoadProgress()
        {
            if (_loadingSceneHandles.Count == 0) return 0f;

            float progress = 0f;
            foreach (var handle in _loadingSceneHandles)
                progress += handle.Progress;
            return progress / _loadingSceneHandles.Count;
        }

        #endregion

        #region Private Methods

        private bool CheckAllLoaded()
        {
            foreach (var handle in _loadingSceneHandles)
            {
                if (!handle.IsDone) return false;
                if (handle.Status != EOperationStatus.Succeed)
                {
                    LogError($"[Scene] Scene loading failed: {handle.GetAssetInfo().AssetPath}, error: {handle.LastError}");
                    this.SendEvent(new SceneErrorEvent { SceneName = handle.GetAssetInfo().AssetPath, Error = handle.LastError });
                }
            }
            return true;
        }

        private void OnSceneLoaded(UnityEngine.SceneManagement.Scene scene, LoadSceneMode sceneMode)
        {
            if (CurrentScenes.Count == 0) return;
            if (CurrentScenes.Find(x => x == scene.path) == null) return;

            if (scene.path == CurrentScenes[0])
            {
                UnitySceneManager.SetActiveScene(scene);
            }
        }

        private void OnSceneUnloaded(UnityEngine.SceneManagement.Scene scene)
        {
            if (CurrentScenes.Count == 0) return;
            if (CurrentScenes.Find(x => x == scene.path) == null) return;

            CurrentScenes.Remove(scene.path);
            if (CurrentScenes.Count == 0)
            {
                _loadedSceneHandles.Clear();
                var res = this.GetSystem<IResSystem>();
                res.UnloadUnusedAssets();
                LoadPendingScene();
            }
        }

        private void LoadPendingScene()
        {
            CurrentScenes.Clear();
            _loadedSceneHandles.Clear();
            foreach (var scenePath in _pendingScenes)
            {
                var handle = YooAssets.LoadSceneAsync(scenePath, LoadSceneMode.Additive);
                if (handle != null)
                {
                    CurrentScenes.Add(scenePath);
                    _loadingSceneHandles.Add(handle);
                    _loadedSceneHandles.Add(handle);
                }
                else
                {
                    LogError($"[Scene] Scene loading failed: {scenePath}");
                    this.SendEvent(new SceneErrorEvent { SceneName = scenePath, Error = "LoadSceneAsync returned null" });
                }
            }

            _pendingScenes.Clear();
        }

        #endregion
    }
}
