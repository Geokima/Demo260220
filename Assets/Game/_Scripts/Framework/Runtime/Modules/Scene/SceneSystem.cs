namespace Framework.Modules.Scene
{
    using System;
    using System.Collections.Generic;
    using UnityEngine;
    using UnityEngine.SceneManagement;
    using UnitySceneManager = UnityEngine.SceneManagement.SceneManager;

    public class SceneSystem : AbstractSystem
    {
        public List<string> CurrentScenes { get; private set; } = new();

        private List<string> _pendingScenes = new();
        private List<AsyncOperation> _loadingScenes = new();
        private bool _isLoading;

        public override void Init()
        {
            UnitySceneManager.sceneLoaded += OnSceneLoaded;
            UnitySceneManager.sceneUnloaded += OnSceneUnloaded;
        }

        public override void Deinit()
        {
            UnitySceneManager.sceneLoaded -= OnSceneLoaded;
            UnitySceneManager.sceneUnloaded -= OnSceneUnloaded;

            CurrentScenes.Clear();
            _pendingScenes.Clear();
            _loadingScenes.Clear();
            _isLoading = false;
        }

        private void OnSceneLoaded(Scene scene, LoadSceneMode sceneMode)
        {
            if (CurrentScenes.Count == 0) return;
            if (CurrentScenes.Find(x => x == scene.name) == null) return;

            if (scene.path == CurrentScenes[0])
            {
                UnitySceneManager.SetActiveScene(scene);
            }
        }

        private void OnSceneUnloaded(Scene scene)
        {
            if (CurrentScenes.Count == 0) return;
            if (CurrentScenes.Find(x => x == scene.path) == null) return;

            CurrentScenes.Remove(scene.path);
            if (CurrentScenes.Count == 0)
            {
                var res = this.GetSystem<Res.ResSystem>();
                res.UnloadUnusedAssets();
                LoadPendingScene();
            }
        }

        public void Update()
        {
            if (_isLoading)
            {
                Debug.Log($"[Scene] Scene loading progress: {GetSceneLoadProgress()}");
                if (GetSceneLoadProgress() == 1f)
                {
                    Debug.Log($"[Scene] Scene loading completed: {string.Join(", ", CurrentScenes)}");
                    _isLoading = false;
                    _loadingScenes.Clear();
                    this.SendEvent(new SceneLoadCompleteEvent { SceneNames = CurrentScenes.ToArray() });
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

        public void LoadScene(string scenePath)
        {
            if (_isLoading)
            {
                Debug.LogError($"[Scene] Already loading scenes: {string.Join(", ", CurrentScenes)}");
                return;
            }

            if (CurrentScenes.Contains(scenePath))
            {
                Debug.LogError($"[Scene] Duplicate scene: {scenePath}");
                return;
            }

            _isLoading = true;
            this.SendEvent(new SceneLoadStartEvent { SceneName = scenePath });
            Debug.Log($"[Scene] Loading scene: {scenePath}");
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

        public void LoadScenes(string[] scenePaths)
        {
            if (_isLoading)
            {
                Debug.LogError($"[Scene] Already loading scenes: {string.Join(", ", CurrentScenes)}");
                return;
            }

            foreach (var scenePath in scenePaths)
            {
                if (CurrentScenes.Contains(scenePath))
                {
                    Debug.LogError($"[Scene] Duplicate scene: {scenePath}");
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
                    Debug.LogError($"[Scene] Scene loading failed: {scenePath}");
                    this.SendEvent(new SceneErrorEvent { SceneName = scenePath, Error = "LoadSceneAsync returned null" });
                }
            }

            _pendingScenes.Clear();
        }

        public float GetSceneLoadProgress()
        {
            if (_loadingScenes.Count == 0) return 0f;

            float progress = 0f;
            foreach (var operation in _loadingScenes)
                progress += operation.progress;
            return progress / _loadingScenes.Count;
        }
    }
}