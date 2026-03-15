using Framework;
using UnityEngine;
using Game.Match3.Logic;
using Game.Match3;
using System.Collections.Generic;

namespace Game.Gameplay.Match3
{
    /// <summary>
    /// 纯表现器（显示器）- 负责将逻辑事件转化为动画序列
    /// </summary>
    public class Match3BoardVisualizer : MonoBehaviour, IController
    {
        // 核心状态：是否正在播放动画
        public bool IsAnimating { get; private set; }

        private void Start()
        {
            // 监听逻辑层事件
            this.RegisterEvent<Match3BoardInitializedEvent>(OnBoardInit);
            this.RegisterEvent<Match3MatchEvent>(OnMatch);
            this.RegisterEvent<Match3RefillEvent>(OnRefill);
        }

        private void OnBoardInit(Match3BoardInitializedEvent e)
        {
            Debug.Log("[Visualizer] 棋盘初始化表现...");
            // TODO: 生成所有宝石的 GameObject
        }

        private void OnMatch(Match3MatchEvent e)
        {
            Debug.Log($"[Visualizer] 执行 {e.Matches.Count} 处消除动画...");
            // 这里可以启动一个协程，设置 IsAnimating = true，播放完设为 false
        }

        private void OnRefill(Match3RefillEvent e)
        {
            Debug.Log($"[Visualizer] 执行 {e.Falls.Count} 个方块落位动画...");
        }

        public IArchitecture Architecture 
        { 
            get => Match3Architecture.Instance; 
            set { } 
        }
    }
}
