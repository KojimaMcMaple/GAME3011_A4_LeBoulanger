using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class Main : MonoBehaviour
{
    public event EventHandler OnGridCellDestroyed; //event to send command from model to view
    public event EventHandler<OnNewPipeSpawnedEventArgs> OnNewPipeSpawned; //event to send command from model to view
    public event EventHandler<OnBombSpawnedEventArgs> OnBombSpawned; //event to send command from model to view
    public event EventHandler OnScoreChanged;
    public event EventHandler OnTimerChanged;
    public event EventHandler OnWin;
    public event EventHandler OnLoss;

    public class OnNewPipeSpawnedEventArgs : EventArgs
    {
        public Pipe gem;
        public GridCell cell;
    }

    public class OnBombSpawnedEventArgs : EventArgs
    {
        public int x;
        public int y;
    }

    private Grid<GridCell> grid_;
    private int width_;
    private int height_;
    [SerializeField] private List<PipeSO> pipe_so_list_;
    private List<GridCell> processing_list_;

    private int score_;
    private float timer_ = 300f;
    private int max_immoveables_ = 5;
    private int num_immoveables_ = 0;
    [SerializeField] private bool can_spawn_bomb_ = false;

    private void Awake()
    {
        width_ = 10;
        height_ = 10;
        grid_ = new Grid<GridCell>(width_, height_, 1f, Vector3.zero, 
            (Grid<GridCell> grid_, int x, int y) => new GridCell(grid_,x,y));

        for (int x = 0; x < width_; x++)
        {
            for (int y = 0; y < height_; y++)
            {
                int max_count = pipe_so_list_.Count;
                if (num_immoveables_ >= max_immoveables_)
                {
                    max_count -= 1;
                }
                int idx = UnityEngine.Random.Range(0, max_count);
                if (num_immoveables_ < max_immoveables_)
                {
                    if (idx == pipe_so_list_.Count-1)
                    {
                        num_immoveables_++;
                    }
                }
                PipeSO gem_so = pipe_so_list_[idx];
                Pipe gem = new Pipe(gem_so, x, y);
                grid_.GetValue(x,y).SetCellItem(gem);
            }
        }
        score_ = 0;
    }

    private void FixedUpdate()
    {
        if (timer_ > 0)
        {
            timer_ -= Time.deltaTime;
            OnTimerChanged?.Invoke(this, EventArgs.Empty);
        }
        else
        {
            OnLoss?.Invoke(this, EventArgs.Empty);
        }
    }

    public Grid<GridCell> GetMainGrid()
    {
        return grid_;
    }

    public int GetScore()
    {
        return score_;
    }

    public float GetTimer()
    {
        return timer_;
    }

    private bool IsValidCoords(int x, int y)
    {
        if (x < 0 || y < 0 || x >= width_ || y >= height_)
        {
            return false;
        }
        return true;
    }

    private PipeSO GetPipeSOAtCoords(int x, int y)
    {
        if (!IsValidCoords(x, y))
        {
            return null;
        }
        GridCell cell = grid_.GetValue(x, y);
        return cell.GetCellItem().GetPipeSO();
    }

    public bool HasMatch(int x, int y)
    {
        List<GridCell> result = GetMatchesAtCoords(x, y);
        return result != null && result.Count > 2;
    }

    public void SwapGridCells(int start_x, int start_y, int dest_x, int dest_y)
    {
        if (!IsValidCoords(start_x, start_y) || !IsValidCoords(dest_x, dest_y)) 
        {
            return;
        }
        if (start_x == dest_x && start_y == dest_y)
        {
            return;
        }

        GridCell start_cell = grid_.GetValue(start_x, start_y);
        GridCell dest_cell = grid_.GetValue(dest_x, dest_y);
        Pipe start_gem = start_cell.GetCellItem();
        Pipe dest_gem = dest_cell.GetCellItem();

        start_gem.SetPipeCoords(dest_x, dest_y);
        dest_gem.SetPipeCoords(start_x, start_y);
        start_cell.SetCellItem(dest_gem);
        dest_cell.SetCellItem(start_gem);
    }

    public bool TrySwapGridCells(int start_x, int start_y, int dest_x, int dest_y)
    {
        if (!IsValidCoords(start_x, start_y) || !IsValidCoords(dest_x, dest_y))
        {
            return false;
        }
        if (start_x == dest_x && start_y == dest_y)
        {
            return false;
        }
        if (GetPipeSOAtCoords(start_x, start_y) == GetPipeSOAtCoords(dest_x, dest_y))
        {
            return false;
        }
        if (GetPipeSOAtCoords(start_x, start_y).is_immovable || GetPipeSOAtCoords(dest_x, dest_y).is_immovable)
        {
            return false;
        }

        SwapGridCells(start_x, start_y, dest_x, dest_y);
        bool has_match = HasMatch(start_x, start_y) || HasMatch(dest_x, dest_y);
        if (!has_match)
        {
            SwapGridCells(start_x, start_y, dest_x, dest_y);
        }

        return has_match;
    }

    public bool TryProcessMatchesAtCoords(int start_x, int start_y, int dest_x, int dest_y)
    {
        processing_list_ = new List<GridCell>();
        List<GridCell> list1 = GetMatchesAtCoords(start_x, start_y);
        List<GridCell> list2 = GetMatchesAtCoords(dest_x, dest_y);
        processing_list_ = list1.Union(list2).ToList();

        if (processing_list_.Count == 0)
        {
            return false;
        }

        foreach (GridCell cell in processing_list_)
        {
            TryDestroyGem(cell);
        }
        return true;
    }

    public bool TryProcessAllMatches()
    {
        processing_list_ = new List<GridCell>();
        for (int x = 0; x < width_; x++)
        {
            for (int y = 0; y < height_; y++)
            {
                List<GridCell> list1 = GetMatchesAtCoords(x, y);
                if (list1 != null)
                {
                    List<GridCell> list2 = processing_list_;
                    processing_list_ = list1.Union(list2).ToList();
                }
            }
        }

        if (processing_list_.Count == 0)
        {
            return false;
        }

        foreach (GridCell cell in processing_list_)
        {
            TryDestroyGem(cell);
        }

        if (can_spawn_bomb_)
        {
            if (processing_list_.Count > 3 && UnityEngine.Random.Range(0, 100) < 50) //50% chance bomb
            {
                int idx = UnityEngine.Random.Range(0, processing_list_.Count);
                int x = processing_list_[idx].GetX();
                int y = processing_list_[idx].GetY();
                List<GridCell> bombed_cells = new List<GridCell>();
                for (int i = -1; i < 2; i++)
                {
                    for (int j = -1; j < 2; j++)
                    {
                        bombed_cells.Add(grid_.GetValue(x+i, y+j));
                    }
                }
                foreach (GridCell cell in bombed_cells)
                {
                    TryDestroyGem(cell);
                }
                OnBombSpawned?.Invoke(this, new OnBombSpawnedEventArgs
                {
                    x = x,
                    y = y,
                });
            }
        }

        OnScoreChanged?.Invoke(this, EventArgs.Empty);

        return true;
    }

    private void TryDestroyGem(GridCell cell)
    {
        if (cell.HasCellItem())
        {
            cell.DestroyCellItem();
            OnGridCellDestroyed?.Invoke(cell, EventArgs.Empty);
            cell.ClearCellItem();

            score_ += 100;
        }
    }

    public void DoGemsFall()
    {
        for (int x = 0; x < width_; x++)
        {
            for (int y = 0; y < height_; y++)
            {
                GridCell cell = grid_.GetValue(x, y);
                if (cell.HasCellItem())
                {
                    for (int i = y-1; i >=0; i--)
                    {
                        GridCell cell_below = grid_.GetValue(x, i);
                        if (!cell_below.HasCellItem()) //move cell down
                        {
                            cell.GetCellItem().SetPipeCoords(x, i);
                            cell_below.SetCellItem(cell.GetCellItem());
                            cell.ClearCellItem();
                            cell = cell_below;
                        }
                        else
                        {
                            break;
                        }
                    }
                }
            }
        }
    }

    public void DoSpawnNewGems()
    {
        for (int x = 0; x < width_; x++)
        {
            for (int y = 0; y < height_; y++)
            {
                GridCell cell = grid_.GetValue(x, y);
                if (!cell.HasCellItem())
                {
                    PipeSO gem_so = pipe_so_list_[UnityEngine.Random.Range(0, pipe_so_list_.Count-1)]; //no immoveable when spawning new
                    Pipe gem = new Pipe(gem_so, x, y);
                    cell.SetCellItem(gem);

                    OnNewPipeSpawned?.Invoke(gem, new OnNewPipeSpawnedEventArgs
                    {
                        gem = gem,
                        cell = cell,
                    });
                }
            }
        }
    }

    public List<GridCell> GetMatchesAtCoords(int x, int y) //main check
    {
        PipeSO gem_so = GetPipeSOAtCoords(x, y);
        if (gem_so == null)
        {
            return null;
        }
        if (gem_so.is_immovable)
        {
            return null;
        }

        // RIGHT
        int matches_right = 0;
        for (int i = 1; i < width_; i++)
        {
            if (!IsValidCoords(x + i, y))
            {
                break;
            }
            PipeSO next_gem_so = GetPipeSOAtCoords(x + i, y);
            if (next_gem_so != gem_so || next_gem_so.is_immovable)
            {
                break;
            }
            matches_right++;
        }


        // LEFT
        int matches_left = 0;
        for (int i = 1; i < width_; i++)
        {
            if (!IsValidCoords(x - i, y))
            {
                break;
            }
            PipeSO next_gem_so = GetPipeSOAtCoords(x - i, y);
            if (next_gem_so != gem_so || next_gem_so.is_immovable)
            {
                break;
            }
            matches_left++;
        }

        // UP
        int matches_up = 0;
        for (int i = 1; i < height_; i++)
        {
            if (!IsValidCoords(x, y + i))
            {
                break;
            }
            PipeSO next_gem_so = GetPipeSOAtCoords(x, y + i);
            if (next_gem_so != gem_so || next_gem_so.is_immovable)
            {
                break;
            }
            matches_up++;
        }

        // DOWN
        int matches_down = 0;
        for (int i = 1; i < height_; i++)
        {
            if (!IsValidCoords(x, y - i))
            {
                break;
            }
            PipeSO next_gem_so = GetPipeSOAtCoords(x, y - i);
            if (next_gem_so != gem_so || next_gem_so.is_immovable)
            {
                break;
            }
            matches_down++;
        }

        int matches_horizontal = 1 + matches_right + matches_left;
        int matches_vertical = 1 + matches_up + matches_down;
        List < GridCell > result = new List < GridCell >(); 
        if (matches_horizontal > 2)
        {
            int bound_left = x - matches_left;
            for (int i = 0; i < matches_horizontal; i++)
            {
                if (bound_left + i != x)
                {
                    result.Add(grid_.GetValue(bound_left + i, y));
                }
            }
        }
        if (matches_vertical > 2)
        {
            int bound_down = y - matches_down;
            for (int i = 0; i < matches_vertical; i++)
            {
                if (bound_down + i != y)
                {
                    result.Add(grid_.GetValue(x, bound_down + i));
                }
            }
        }
        if (result.Count != 0)
        {
            result.Add(grid_.GetValue(x, y));
        }
        return result;
    }



    public class GridCell //the Grid will be populated with GridCell, the item on a GridCell is a cell_item_ (class Pipe) 
    {
        private Pipe cell_item_; //item on cell

        private Grid<GridCell> grid_;
        private int x_;
        private int y_;

        public GridCell(Grid<GridCell> grid, int x, int y)
        {
            grid_ = grid;
            x_ = x;
            y_ = y;
        }

        public Pipe GetCellItem()
        {
            return cell_item_;
        }
        public void SetCellItem(Pipe cell_item)
        {
            cell_item_ = cell_item;
            grid_.DoTriggerGridObjChanged(x_, y_);
        }
        public bool HasCellItem()
        {
            return cell_item_ != null;
        }
        public void ClearCellItem()
        {
            cell_item_ = null;
        }
        public void DestroyCellItem()
        {
            cell_item_?.Destroy();
            grid_.DoTriggerGridObjChanged(x_, y_);
        }

        public Grid<GridCell> GetGrid()
        {
            return grid_;
        }

        public int GetX()
        {
            return x_;
        }
        public int GetY()
        {
            return y_;
        }
        public Vector3 GetWorldPos()
        {
            return grid_.GetWorldPos(x_, y_);
        }
    }



    public class Pipe //the item on a GridCell
    {
        public event EventHandler OnDestroyed;

        private PipeSO pipe_;
        private int x_;
        private int y_;
        private bool is_dead_;

        public Pipe(PipeSO pipe, int x, int y)
        {
            pipe_ = pipe;
            x_ = x;
            y_ = y;
            is_dead_ = false;
        }

        public PipeSO GetPipeSO()
        {
            return pipe_;
        }

        public Vector3 GetWorldPos()
        {
            return new Vector3(x_, y_);
        }

        public void SetPipeCoords(int x, int y)
        {
            x_ = x;
            y_ = y;
        }

        public void Destroy()
        {
            is_dead_ = true;
            OnDestroyed?.Invoke(this, EventArgs.Empty);
        }

        public override string ToString()
        {
            return is_dead_.ToString();
        }
    }
}
