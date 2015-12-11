using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Diagnostics;

namespace Nav.ExploreEngine
{
    // This algorithm chooses nearest unexplored neighbor but prefer those with many visited neighbors to not leave unexplored islands
    public class Nearest : ExplorationEngine
    {
        public Nearest(Navmesh navmesh, NavigationEngine navigator)
            : base(navmesh, navigator)
        {
        }

        protected class ExploreCellSelector : Algorihms.IDistanceVisitor<ExploreCell>
        {
            public ExploreCellSelector()
            {
            }

            public void Visit(ExploreCell cell, float distance)
            {
                float dist = EvaluateDist(cell, distance);

                if (dist < dest_cell_distance)
                {
                    dest_cell = cell;
                    dest_cell_distance = dist;
                }
            }

            protected virtual float EvaluateDist(ExploreCell cell, float distance)
            {
                // decrease distance based on number of explored neighbors (do not leave small unexplored fragments)
                float dist = distance;

                dist -= DISTANCE_REDUCTION_EXPLORE_PCT * GetExploredNeighboursPct(cell, 1);
                //dist -= DISTANCE_REDUCTION_PER_EXPLORED_NEIGHBOUR * (float)explored_neighbours_count;
                //dist -= DISTANCE_REDUCTION_PER_MISSING_NEIGHBOUR * Math.Max(0, AVG_NEIGHBOURS_COUNT - cell.Neighbours.Count);
                //dist -= base_dist * DISTANCE_PCT_REDUCTION_PER_EXPLORED_NEIGHBOUR * (float)explored_neighbours_count;
                //dist -= base_dist * DISTANCE_PCT_REDUCTION_PER_MISSING_NEIGHBOUR * (float)missing_neighbours_count;

                return dist;
            }

            private float GetExploredNeighboursPct(ExploreCell cell, int max_depth)
            {
                HashSet<ExploreCell> cells_group = new HashSet<ExploreCell>();
                
                Algorihms.Visit(cell, ref cells_group, MovementFlag.None, true, 0, max_depth);

                //treat missing cells as explored thus explore edges to possibly load new navmesh data
                int max_cells_num = (int)Math.Pow(9, max_depth);
                int missing_cells = Math.Max(0, max_cells_num - cells_group.Count);

                return (float)(cells_group.Count(x => ((ExploreCell)x).Explored) + missing_cells) / (float)max_cells_num;
            }

            //const float DISTANCE_REDUCTION_PER_EXPLORED_NEIGHBOUR = 15;
            //const float DISTANCE_REDUCTION_PER_MISSING_NEIGHBOUR = 10;
            const float DISTANCE_REDUCTION_EXPLORE_PCT = 500;
            //const float DISTANCE_PCT_REDUCTION_PER_EXPLORED_NEIGHBOUR = 0.06f;
            //const float DISTANCE_PCT_REDUCTION_PER_MISSING_NEIGHBOUR = 0.05f;
            //const int AVG_NEIGHBOURS_COUNT = 8;

            public ExploreCell dest_cell = null;
            private float dest_cell_distance = float.MaxValue;
        }

        internal override Vec3 GetDestinationCellPosition()
        {
            Vec3 dest = base.GetDestinationCellPosition();

            if (!dest.IsEmpty)
                return dest;

            ExploreCell current_explore_cell = GetCurrentExploreCell();

            if (current_explore_cell == null)
                return Vec3.Empty;

            if (!current_explore_cell.Explored)
                return current_explore_cell.Position;

            HashSet<ExploreCell> unexplored_cells = GetUnexploredCells(current_explore_cell);

            ExploreCellSelector selector = CreateExploreCellSelector();

            Algorihms.VisitBreadth(current_explore_cell, MovementFlag.None, -1, unexplored_cells, selector);

            if (selector.dest_cell != null)
                return selector.dest_cell.Position;

            return Vec3.Empty;
        }

        protected virtual ExploreCellSelector CreateExploreCellSelector()
        {
            return new ExploreCellSelector();
        }
    }
}
