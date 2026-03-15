using Framework;
using Game.Match3;
using UnityEngine;

namespace Game.Gameplay.Match3
{
    /// <summary>
    /// 纯物理层采集器（传感器）- 负责将屏幕操作翻译为逻辑指令
    /// </summary>
    public class Match3InputSensor : MonoBehaviour, IController
    {
        public Match3BoardVisualizer Visualizer; // 关联表现器以检查状态
        
        public float CellSize = 1.0f;
        public Vector3 Origin; // 棋盘左下角原点

        private Vector2Int mDragStart;
        private bool mIsDragging;

        private void Update()
        {
            // 1. 拦截检查：如果显示器正在忙，直接屏蔽所有输入
            if (Visualizer != null && Visualizer.IsAnimating) return;

            // 2. 简单的鼠标/点击输入处理
            if (Input.GetMouseButtonDown(0))
            {
                var worldPos = Camera.main.ScreenToWorldPoint(Input.mousePosition);
                mDragStart = WorldToLogic(worldPos);
                mIsDragging = true;
            }
            else if (Input.GetMouseButtonUp(0) && mIsDragging)
            {
                var worldPos = Camera.main.ScreenToWorldPoint(Input.mousePosition);
                var dragEnd = WorldToLogic(worldPos);
                
                // 判断方向并发射交换命令
                if (dragEnd != mDragStart)
                {
                    this.SendCommand(new Match3SwapCommand { 
                        X1 = mDragStart.x, Y1 = mDragStart.y, 
                        X2 = dragEnd.x, Y2 = dragEnd.y 
                    });
                }
                mIsDragging = false;
            }
        }

        private Vector2Int WorldToLogic(Vector3 worldPos)
        {
            int x = Mathf.FloorToInt((worldPos.x - Origin.x) / CellSize);
            int y = Mathf.FloorToInt((worldPos.y - Origin.y) / CellSize);
            return new Vector2Int(x, y);
        }

        public IArchitecture Architecture 
        { 
            get => Match3Architecture.Instance; 
            set { } 
        }
    }
}
