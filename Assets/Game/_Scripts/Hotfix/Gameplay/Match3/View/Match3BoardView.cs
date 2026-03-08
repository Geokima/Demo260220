using System.Collections.Generic;
using Framework;
using Game.Shared.Logic.Match3;
using UnityEngine;
using Cysharp.Threading.Tasks;
using Framework.Modules.Routine;

namespace Game.Gameplay.Match3
{
    /// <summary>
    /// 三消棋盘表现层
    /// 负责监听 Match3Architecture 的事件并转化为视觉动作序列（IRoutine）
    /// </summary>
    public class Match3BoardView : MonoBehaviour, IController
    {
        [Header("Settings")]
        public Transform GridRoot;
        public GameObject CellPrefab;
        public float CellSize = 1.0f;

        private Match3Model _model;
        private Dictionary<(int, int), GameObject> _cellViews = new Dictionary<(int, int), GameObject>();

        public IArchitecture GetArchitecture() => Match3Architecture.Interface;

        private void Start()
        {
            _model = this.GetModel<Match3Model>();

            // 监听初始化
            this.RegisterEvent<Match3BoardInitializedEvent>(e => CreateBoard());
            
            // 监听交换
            this.RegisterEvent<Match3SwapSuccessEvent>(e => OnSwapSuccess(e).Forget());
            this.RegisterEvent<Match3SwapFailEvent>(e => OnSwapFail(e).Forget());

            // 监听消除和下落
            this.RegisterEvent<Match3MatchEvent>(e => OnMatch(e).Forget());
            this.RegisterEvent<Match3RefillEvent>(e => OnRefill(e).Forget());
        }

        private void CreateBoard()
        {
            // 清理旧的
            foreach (var view in _cellViews.Values) Destroy(view);
            _cellViews.Clear();

            // 生成新的
            for (int x = 0; x < _model.Width; x++)
            {
                for (int y = 0; y < _model.Height; y++)
                {
                    var cellType = _model.Grid[x, y];
                    var pos = GetWorldPosition(x, y);
                    var go = Instantiate(CellPrefab, pos, Quaternion.identity, GridRoot);
                    go.name = $"Cell_{x}_{y}";
                    // 这里通常要把 cellType 传给视觉组件去设置颜色
                    _cellViews[(x, y)] = go;
                }
            }
        }

        private async UniTaskVoid OnSwapSuccess(Match3SwapSuccessEvent e)
        {
            // 创建交换动画序列
            var seq = new SequenceRoutine();
            // ... 添加具体的移动 Routine
            await seq.PlayAsync();
        }

        private async UniTaskVoid OnSwapFail(Match3SwapFailEvent e)
        {
            // 创建交换失败（来回晃一下）动画
            await UniTask.Yield();
        }

        private async UniTaskVoid OnMatch(Match3MatchEvent e)
        {
            var parallel = new ParallelRoutine();
            // ... 遍历 e.MatchedCells 添加所有的消除动画
            await parallel.PlayAsync();
        }

        private async UniTaskVoid OnRefill(Match3RefillEvent e)
        {
            var parallel = new ParallelRoutine();
            // ... 遍历 e.Falls 添加所有的掉落动画
            await parallel.PlayAsync();
        }

        private Vector3 GetWorldPosition(int x, int y)
        {
            return new Vector3(x * CellSize, y * CellSize, 0);
        }
    }
}
