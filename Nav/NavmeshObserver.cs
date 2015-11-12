using System;

namespace Nav
{
    public interface NavmeshObserver
    {
        void OnGridCellAdded(GridCell grid_cell);
        void OnNavDataChanged();
        void OnNavDataCleared();
    }
}
