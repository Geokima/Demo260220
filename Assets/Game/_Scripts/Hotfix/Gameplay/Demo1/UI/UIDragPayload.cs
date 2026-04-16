using Game.Gameplay.Demo1;
using UnityEngine;

namespace Game.Gameplay.Demo1.UI
{
    public sealed class UIDragPayload
    {
        public UI_CardView View { get; }
        public CardModel Model { get; }
        public int WidthInCells { get; }
        public Vector2 PointerOffsetFromLeftEdgeScreen { get; }
        public Vector2 PointerOffsetFromCenterScreen { get; }

        public UIDragPayload(
            UI_CardView view,
            CardModel model,
            int widthInCells,
            Vector2 pointerOffsetFromLeftEdgeScreen,
            Vector2 pointerOffsetFromCenterScreen)
        {
            View = view;
            Model = model;
            WidthInCells = widthInCells;
            PointerOffsetFromLeftEdgeScreen = pointerOffsetFromLeftEdgeScreen;
            PointerOffsetFromCenterScreen = pointerOffsetFromCenterScreen;
        }
    }
}
