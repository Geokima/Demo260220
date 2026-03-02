using System;
using System.Collections.Generic;
using Framework;
using Framework.Modules.Res;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnitySceneManager = UnityEngine.SceneManagement.SceneManager;
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
        private List<AsyncOperation> _loadingScenes = new();
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
            _loadingScenes.Clear();
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
                        _loadingScenes.Clear();
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
                foreach (var scene in CurrentScenes)
                    UnitySceneManager.UnloadSceneAsync(scene);
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
                foreach (var scene in CurrentScenes)
                    UnitySceneManager.UnloadSceneAsync(scene);
            }
            else
            {
                LoadPendingScene();
            }
        }

        /// <inheritdoc />
        public float GetSceneLoadProgress()
        {
            if (_loadingScenes.Count == 0) return 0f;

            float progress = 0f;
            foreach (var operation in _loadingScenes)
                progress += operation.progress;
            return progress / _loadingScenes.Count;
        }

        #endregion

        #region Private Methods

        private bool CheckAllLoaded()
        {
            foreach (var op in _loadingScenes)
            {
                if (!op.isDone) return false;
            }
            return true;
        }

        private void OnSceneLoaded(UnityEngine.SceneManagement.Scene scene, LoadSceneMode sceneMode)
        {
            if (CurrentScenes.Count == 0) return;
            if (CurrentScenes.Find(x => x == scene.name) == null) return;

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
                var res = this.GetSystem<IResSystem>();
                res.UnloadUnusedAssets();
                LoadPendingScene();
            }
        }

        private void LoadPendingScene()
        {
            CurrentScenes.Clear();
            foreach (var scenePath in _pendingScenes)
            {
                AsyncOperation operation = UnitySceneManager.LoadSceneAsync(scenePath, LoadSceneMode.Additive);
                if (operation != null)
                {
                    CurrentScenes.Add(scenePath);
                    _loadingScenes.Add(operation);
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
