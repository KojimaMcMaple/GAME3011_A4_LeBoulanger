using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.SceneManagement;

public class Main : MonoBehaviour
{
    public event EventHandler<OnGridCellChangedEventArgs> OnGridCellChanged; //event to send command from model to view
    public event EventHandler OnGridCellDestroyed; //event to send command from model to view
    public event EventHandler<OnNewPipeSpawnedEventArgs> OnNewPipeSpawned; //event to send command from model to view
    public event EventHandler<OnBombSpawnedEventArgs> OnBombSpawned; //event to send command from model to view
    public event EventHandler OnScoreChanged;
    public event EventHandler OnTimerChanged;
    public event EventHandler OnWin; //unused
    public event EventHandler OnLoss;
    public event EventHandler<Pipe> OnPipeSoChanged; //unused

    public class OnGridCellChangedEventArgs : EventArgs
    {
        public Pipe pipe;
        public int x;
        public int y;
        public Color color;
    }

    public class OnNewPipeSpawnedEventArgs : EventArgs
    {
        public Pipe pipe;
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
    public List<PipeSO> pipe_so_list_;
    [SerializeField] PipeSO startPipeSO, endPipeSo; 
    private List<GridCell> processing_list_;
    private GridCell start_cell_;
    private GridCell end_cell_;

    private int score_;
    private float level_time_limit_ = 300f;
    private float timer_ = 300f;
    private int player_level_ = 1;
    private int max_immoveables_ = 5;
    private int num_immoveables_ = 0;
    [SerializeField] private bool can_spawn_bomb_ = false;

    private void Awake()
    {
        Scene curr_scene = SceneManager.GetActiveScene();
        if (curr_scene.name == "Level1")
        {
            width_ = 6;
            height_ = 6;
            level_time_limit_ = 300f;
            timer_ = level_time_limit_;
            timer_ += GetExtraTimeFromPlayerLevel(timer_);
        }
        else if (curr_scene.name == "Level2")
        {
            width_ = 8;
            height_ = 8;
            level_time_limit_ = 250f;
            timer_ = level_time_limit_;
            timer_ += GetExtraTimeFromPlayerLevel(timer_);
        }
        else
        {
            width_ = 10;
            height_ = 10;
            level_time_limit_ = 200f;
            timer_ = level_time_limit_;
            timer_ += GetExtraTimeFromPlayerLevel(timer_);
        }

        grid_ = new Grid<GridCell>(width_, height_, 1f, Vector3.zero, 
            (Grid<GridCell> grid_, int x, int y) => new GridCell(grid_,x,y));

        for (int x = 0; x < width_; x++)
        {
            for (int y = 0; y < height_; y++)
            {
                int max_count = pipe_so_list_.Count;
                int idx = UnityEngine.Random.Range(0, max_count);
                PipeSO pipe_so = pipe_so_list_[idx];
                Pipe pipe = new Pipe(pipe_so, x, y, (GlobalEnums.RotType)UnityEngine.Random.Range((int)GlobalEnums.RotType.Rot0, (int)GlobalEnums.RotType.NUM_OF_TYPES)); //rand rot);
                grid_.GetGridObj(x,y).SetCellItem(pipe);
            }
        }

        Vector2Int start_coords = new Vector2Int(0, UnityEngine.Random.Range(0, height_));
        Vector2Int end_coords = new Vector2Int();
        List<Vector2Int> path = new List<Vector2Int>();
        List<Vector2Int> dir = new List<Vector2Int>();
        (path, dir) = PathGenerator.GeneratePath(start_coords, width_, height_, UnityEngine.Random.Range(height_, width_ * height_), out end_coords);
        
        PathGenerator.GeneratePipesFromPath(dir, this, start_coords);

        start_cell_ = grid_.GetGridObj(start_coords.x, start_coords.y);
        start_cell_.GetCellItem().SetIsStartPoint(true);
        start_cell_.GetCellItem().SetPipeSo(startPipeSO);

        end_cell_ = grid_.GetGridObj(end_coords.x, end_coords.y);
        end_cell_.GetCellItem().SetIsEndPoint(true);
        end_cell_.GetCellItem().SetPipeSo(endPipeSo);

        score_ = 0;
    }

    private void Start()
    {
        OnGridCellChanged?.Invoke(this, new OnGridCellChangedEventArgs
        {
            pipe = start_cell_.GetCellItem(),
            x = start_cell_.GetX(),
            y = start_cell_.GetY(),
            color = Color.blue
        });
        OnGridCellChanged?.Invoke(this, new OnGridCellChangedEventArgs
        {
            pipe = end_cell_.GetCellItem(),
            x = end_cell_.GetX(),
            y = end_cell_.GetY(),
            color = Color.green
        });
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

    public float GetLevelTimeLimit()
    {
        return level_time_limit_;
    }

    public float GetTimer()
    {
        return timer_;
    }

    public int GetPlayerLevel()
    {
        return player_level_;
    }

    public void IncrementPlayerLevel()
    {
        player_level_ = player_level_ < 3 ? player_level_ + 1 : player_level_;
    }

    public float GetExtraTimeFromPlayerLevel(float base_time)
    {
        if (player_level_ == 1)
        {
            return (base_time * 10 / 100);
        }
        else if (player_level_ == 2)
        {
            return (base_time * 33 / 100);
        }
        else if (player_level_ == 3)
        {
            return (base_time * 50 / 100);
        }
        return 0;
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
        GridCell cell = grid_.GetGridObj(x, y);
        return cell.GetCellItem().GetPipeSO();
    }

    public List<GridCell> GetSurroundGridObj(int x, int y)
    {
        if (!IsValidCoords(x, y))
        {
            return null;
        }

        List<GridCell> result = new List<GridCell>();
        if (x > 0) { result.Add(grid_.GetGridObj(x - 1, y)); }
        if (x < width_-1) { result.Add(grid_.GetGridObj(x + 1, y)); }
        if (y > 0) { result.Add(grid_.GetGridObj(x, y - 1)); }
        if (y < height_ - 1) { result.Add(grid_.GetGridObj(x, y + 1)); }

        return result;
    }

    private GlobalEnums.PipeMatchType GetMatchTypeBetweenPipes(int start_x, int start_y, int dest_x, int dest_y)
    {
        GridCell start_cell = grid_.GetGridObj(start_x, start_y);
        GridCell dest_cell = grid_.GetGridObj(dest_x, dest_y);
        Pipe start_pipe = start_cell.GetCellItem();
        Pipe dest_pipe = dest_cell.GetCellItem();

        int start_bitmask = start_pipe.GetBitmask();
        int dest_bitmask = dest_pipe.GetBitmask();

        if (dest_y < start_y)
        {
            start_bitmask &= GlobalEnums.kBitmaskBottom;
            dest_bitmask &= GlobalEnums.kBitmaskTop;
        }
        else if (dest_y > start_y)
        {
            start_bitmask &= GlobalEnums.kBitmaskTop;
            dest_bitmask &= GlobalEnums.kBitmaskBottom;
        }
        else if (dest_x < start_x)
        {
            start_bitmask &= GlobalEnums.kBitmaskLeft;
            dest_bitmask &= GlobalEnums.kBitmaskRight;
        }
        else if (dest_x > start_x)
        {
            start_bitmask &= GlobalEnums.kBitmaskRight;
            dest_bitmask &= GlobalEnums.kBitmaskLeft;
        }

        if (start_bitmask == 0)
        {
            return GlobalEnums.PipeMatchType.ValidWithOpenSide;
        }
        else if (dest_bitmask != 0)
        {
            return GlobalEnums.PipeMatchType.ValidWithSolidMatch;
        }
        return GlobalEnums.PipeMatchType.Invalid;
    }

    private bool HasPath(int start_x, int start_y, List<GridCell> all_matches, List<GridCell> checked_cells)
    {
        GridCell curr_cell = grid_.GetGridObj(start_x, start_y);
        checked_cells.Add(curr_cell);
        //Debug.Log(">>> curr_cell: (" + curr_cell.GetX() + ", " + curr_cell.GetY() + ")");
        List<GridCell> matches = new List<GridCell>();
        List<GridCell> surround_cells = new List<GridCell>();
        surround_cells = GetSurroundGridObj(curr_cell.GetX(), curr_cell.GetY());
        foreach (GridCell sc in surround_cells)
        {
            if (!checked_cells.Contains(sc))
            {
                GlobalEnums.PipeMatchType match_type = GetMatchTypeBetweenPipes(curr_cell.GetX(), curr_cell.GetY(),
                                                                            sc.GetX(), sc.GetY());
                if (match_type == GlobalEnums.PipeMatchType.ValidWithSolidMatch)
                {
                    matches.Add(sc);
                    all_matches.Add(sc);
                }
            }
            else
            {
                //Debug.Log("> sc checked: (" + sc.GetX() + ", " + sc.GetY() + ")");
            }
        }
        if (matches.Contains(end_cell_))
        {
            return true;
        }
        else
        {
            if (matches.Count > 0)
            {
                foreach (GridCell mc in matches)
                {
                    if (HasPath(mc.GetX(), mc.GetY(), all_matches, checked_cells))
                    {
                        return true;
                    }
                }
            }
            else
            {
                return false;
            }
        }
        return false;
    }

    public bool TryProcessAllMatches()
    {
        Debug.Log(">>> TryProcessAllMatches...");
        List<GridCell> matches = new List<GridCell> ();
        List<GridCell> checked_cells = new List<GridCell> ();
        bool result = HasPath(start_cell_.GetX(), start_cell_.GetY(), matches, checked_cells);
        
        // RESET MATCHES
        for (int x = 0; x < width_; x++)
        {
            for (int y = 0; y < height_; y++)
            {
                GridCell c = grid_.GetGridObj(x, y); //ci = cell_item
                c.GetCellItem().SetHasMatch(false);
                if (!c.GetCellItem().IsStartPoint() && !c.GetCellItem().IsEndPoint())
                {
                    OnGridCellChanged?.Invoke(this, new OnGridCellChangedEventArgs
                    {
                        pipe = c.GetCellItem(),
                        x = c.GetX(),
                        y = c.GetY(),
                        color = Color.white
                    });
                }
                
            }
        }
        // SET MATCHES
        foreach (GridCell m in matches)
        {
            m.GetCellItem().SetHasMatch(true);
            OnGridCellChanged?.Invoke(this, new OnGridCellChangedEventArgs
            {
                pipe = m.GetCellItem(),
                x = m.GetX(),
                y = m.GetY(),
                color = Color.blue
            });
        }

        return result;
    }

    public bool HasMatch(int x, int y)
    {
        List<GridCell> result = GetMatchesAtCoords(x, y);
        return result != null && result.Count > 2;
    }

    public void SwapGridCellPipes(int start_x, int start_y, int dest_x, int dest_y)
    {
        if (!IsValidCoords(start_x, start_y) || !IsValidCoords(dest_x, dest_y)) 
        {
            return;
        }
        if (start_x == dest_x && start_y == dest_y)
        {
            return;
        }

        GridCell start_cell = grid_.GetGridObj(start_x, start_y);
        GridCell dest_cell = grid_.GetGridObj(dest_x, dest_y);
        Pipe start_gem = start_cell.GetCellItem();
        Pipe dest_gem = dest_cell.GetCellItem();

        start_gem.SetPipeCoords(dest_x, dest_y);
        dest_gem.SetPipeCoords(start_x, start_y);
        start_cell.SetCellItem(dest_gem);
        dest_cell.SetCellItem(start_gem);
    }

    public bool TrySwapGridCellPipes(int start_x, int start_y, int dest_x, int dest_y)
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

        SwapGridCellPipes(start_x, start_y, dest_x, dest_y);
        bool has_match = HasMatch(start_x, start_y) || HasMatch(dest_x, dest_y);
        if (!has_match)
        {
            SwapGridCellPipes(start_x, start_y, dest_x, dest_y);
        }

        return has_match;
    }

    public void RotGridCellPipe(int coord_x, int coord_y)
    {
        if (!IsValidCoords(coord_x, coord_y))
        {
            return;
        }

        GridCell cell = grid_.GetGridObj(coord_x, coord_y);
        Pipe pipe = cell.GetCellItem();
        pipe.DoIncrementRot();
    }

    public bool TryRotGridCellPipe(int coord_x, int coord_y)
    {
        if (!IsValidCoords(coord_x, coord_y))
        {
            return false;
        }
        if (GetPipeSOAtCoords(coord_x, coord_y).is_immovable)
        {
            return false;
        }

        RotGridCellPipe(coord_x, coord_y);


        return true;
    }

    public bool TryProcessMatchesAtCoords(int start_x, int start_y, int dest_x, int dest_y)
    {
        return false;
        
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

    public bool TryProcessAllMatches111()
    {
        return false;
        
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
                        bombed_cells.Add(grid_.GetGridObj(x+i, y+j));
                    }
                }
                foreach (GridCell cell in bombed_cells)
                {
                    TryDestroyGem(cell);
                }
                OnBombSpawned?.Invoke(this, new OnBombSpawnedEventArgs
                {
                    x = x,
                    y = y
                });
            }
        }

        OnScoreChanged?.Invoke(this, EventArgs.Empty);

        return true;
    }

    public void Invoke_ChangedPipeSO(Pipe pipe)
    {
       
        OnPipeSoChanged?.Invoke(this, pipe);
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

    public void DoPipesFall()
    {
        for (int x = 0; x < width_; x++)
        {
            for (int y = 0; y < height_; y++)
            {
                GridCell cell = grid_.GetGridObj(x, y);
                if (cell.HasCellItem())
                {
                    for (int i = y-1; i >=0; i--)
                    {
                        GridCell cell_below = grid_.GetGridObj(x, i);
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

    public void DoSpawnNewPipes()
    {
        for (int x = 0; x < width_; x++)
        {
            for (int y = 0; y < height_; y++)
            {
                GridCell cell = grid_.GetGridObj(x, y);
                if (!cell.HasCellItem())
                {
                    PipeSO pipe_so = pipe_so_list_[UnityEngine.Random.Range(0, pipe_so_list_.Count-1)]; //no immoveable when spawning new
                    Pipe pipe = new Pipe(pipe_so, x, y, (GlobalEnums.RotType)UnityEngine.Random.Range((int)GlobalEnums.RotType.Rot0, (int)GlobalEnums.RotType.NUM_OF_TYPES)); //rand rot
                    cell.SetCellItem(pipe);

                    OnNewPipeSpawned?.Invoke(pipe, new OnNewPipeSpawnedEventArgs
                    {
                        pipe = pipe,
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
                    result.Add(grid_.GetGridObj(bound_left + i, y));
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
                    result.Add(grid_.GetGridObj(x, bound_down + i));
                }
            }
        }
        if (result.Count != 0)
        {
            result.Add(grid_.GetGridObj(x, y));
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

        private PipeSO pipe_; //contains LineTileType 
        private int x_;
        private int y_;
        private bool is_dead_;
        private bool is_start_point_;
        private bool is_end_point_;
        private bool has_match_;

        private GlobalEnums.RotType rot_type_;
        private int bitmask_;

        public int Ycoord { get => y_; private set => y_ = value; }
        public int Xcoord { get => x_; private set => x_ = value; }

        public Pipe(int x, int y)
        {
            x_ = x;
            y_ = y;
        }
        public Pipe(PipeSO pipe, int x, int y, GlobalEnums.RotType rot_type)
        {
            pipe_ = pipe;
            x_ = x;
            y_ = y;
            is_dead_ = false;

            rot_type_ = rot_type;
            UpdateBitmask();
        }

        public PipeSO GetPipeSO()
        {
            return pipe_;
        }
        public void SetPipeSo(PipeSO pipeType) 
        { 
            pipe_ = pipeType;
            UpdateBitmask();
        }

        public Vector3 GetWorldPos()
        {
            return new Vector3(x_, y_);
        }

        public Vector2Int GetGridCoord()
        {
            return new Vector2Int(x_, y_);
        }

        public void SetPipeCoords(int x, int y)
        {
            x_ = x;
            y_ = y;
        }

        public bool IsStartPoint()
        {
            return is_start_point_;
        }

        public void SetIsStartPoint(bool value)
        {
            is_start_point_ = value;
        }

        public bool IsEndPoint()
        {
            return is_end_point_;
        }

        public void SetIsEndPoint(bool value)
        {
            is_end_point_ = value;
        }

        public bool HasMatch()
        {
            return has_match_;
        }

        public void SetHasMatch(bool value)
        {
            has_match_ = true;
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

        public GlobalEnums.RotType GetRotType()
        {
            return rot_type_;
        }

        public void SetRotType(GlobalEnums.RotType value)
        {
            rot_type_ = value;
            UpdateBitmask();
        }

        public void DoIncrementRot()
        {
            int curr_rot = (int)rot_type_;
            curr_rot++;
            curr_rot = curr_rot < (int)GlobalEnums.RotType.NUM_OF_TYPES ? curr_rot : 0;
            SetRotType((GlobalEnums.RotType)curr_rot);
        }

        public int GetBitmask()
        {
            return bitmask_;
        }

        public void SetBitmask(int mask)
        {
            bitmask_ = mask;
        }

        private void UpdateBitmask()
        {
            switch (pipe_.line_type)
            {
                case GlobalEnums.LineTileType.Nub:
                    if (rot_type_ == GlobalEnums.RotType.Rot0) bitmask_ = GlobalEnums.kBitmaskBottom;
                    if (rot_type_ == GlobalEnums.RotType.Rot90) bitmask_ = GlobalEnums.kBitmaskLeft;
                    if (rot_type_ == GlobalEnums.RotType.Rot180) bitmask_ = GlobalEnums.kBitmaskTop;
                    if (rot_type_ == GlobalEnums.RotType.Rot270) bitmask_ = GlobalEnums.kBitmaskRight;
                    break;
                case GlobalEnums.LineTileType.Line:
                    if (rot_type_ == GlobalEnums.RotType.Rot0 || rot_type_ == GlobalEnums.RotType.Rot180) bitmask_ = GlobalEnums.kBitmaskTop | GlobalEnums.kBitmaskBottom;
                    if (rot_type_ == GlobalEnums.RotType.Rot90 || rot_type_ == GlobalEnums.RotType.Rot270) bitmask_ = GlobalEnums.kBitmaskRight | GlobalEnums.kBitmaskLeft;
                    break;
                case GlobalEnums.LineTileType.Corner:
                    if (rot_type_ == GlobalEnums.RotType.Rot0) bitmask_ = GlobalEnums.kBitmaskTop | GlobalEnums.kBitmaskRight;
                    if (rot_type_ == GlobalEnums.RotType.Rot90) bitmask_ = GlobalEnums.kBitmaskRight | GlobalEnums.kBitmaskBottom;
                    if (rot_type_ == GlobalEnums.RotType.Rot180) bitmask_ = GlobalEnums.kBitmaskBottom | GlobalEnums.kBitmaskLeft;
                    if (rot_type_ == GlobalEnums.RotType.Rot270) bitmask_ = GlobalEnums.kBitmaskLeft | GlobalEnums.kBitmaskTop;
                    break;
                case GlobalEnums.LineTileType.Threeway:
                    if (rot_type_ == GlobalEnums.RotType.Rot0) bitmask_ = GlobalEnums.kBitmaskTop | GlobalEnums.kBitmaskRight | GlobalEnums.kBitmaskBottom;
                    if (rot_type_ == GlobalEnums.RotType.Rot90) bitmask_ = GlobalEnums.kBitmaskRight | GlobalEnums.kBitmaskBottom | GlobalEnums.kBitmaskLeft;
                    if (rot_type_ == GlobalEnums.RotType.Rot180) bitmask_ = GlobalEnums.kBitmaskBottom | GlobalEnums.kBitmaskLeft | GlobalEnums.kBitmaskTop;
                    if (rot_type_ == GlobalEnums.RotType.Rot270) bitmask_ = GlobalEnums.kBitmaskLeft | GlobalEnums.kBitmaskTop | GlobalEnums.kBitmaskRight;
                    break;
                case GlobalEnums.LineTileType.Cross:
                    bitmask_ = GlobalEnums.kBitmaskTop | GlobalEnums.kBitmaskRight | GlobalEnums.kBitmaskBottom | GlobalEnums.kBitmaskLeft;
                    break;
                default:
                    break;
            }
        }
    }
}
