using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using TMPro;

public class View : MonoBehaviour
{
    [SerializeField] private Transform pipe_visual_template_;
    [SerializeField] private Transform cell_visual_template_;
    [SerializeField] private float start_pos_y_ = 14f;

    private Main model_;
    private Grid<Main.GridCell> grid_;
    private Dictionary<Main.Pipe, PipeVisual> pipe_dict_; //linker between model and view
    private State state_;
    private float busy_timer_;

    private Transform cam_;
    private int start_drag_x_;
    private int start_drag_y_;
    private int dest_drag_x_;
    private int dest_drag_y_;

    [SerializeField] private TMP_Text level_txt_;
    [SerializeField] private TMP_Text timer_txt_;
    [SerializeField] private TMP_Text score_txt_;
    [SerializeField] private GameObject game_over_panel_;
    [SerializeField] private Text game_over_txt_;

    private Action OnEndBusyAction;

    private AudioSource audio_;
    private VfxManager vfx_manager_;

    public enum State
    {
        kBusy,
        kAvailable,
        kProcessing,
        kGameOver
    }

    private void Awake()
    {
        cam_ = Camera.main.transform;
        audio_ = GetComponent<AudioSource>();
        vfx_manager_ = FindObjectOfType<VfxManager>();
        level_txt_.text = SceneManager.GetActiveScene().name;
        game_over_panel_.SetActive(false);
    }

    private void Start()
    {
        state_ = State.kBusy;
        Init(FindObjectOfType<Main>(), FindObjectOfType<Main>().GetMainGrid());

        model_.OnScoreChanged += HandleScoreChangedEvent;
        model_.OnTimerChanged += HandleTimerChangedEvent;
    }

    private void Update()
    {
        DoUpdateView();

        switch (state_)
        {
            case State.kBusy:
                if (busy_timer_ > 0)
                {
                    busy_timer_ -= Time.deltaTime;
                }
                else
                {
                    OnEndBusyAction();
                }
                break;
            case State.kAvailable:
                if (Input.GetMouseButtonDown(0))
                {
                    Vector3 world_pos = Camera.main.ScreenToWorldPoint(Input.mousePosition);
                    Vector2Int coords = grid_.GetGridCoords(world_pos);
                    start_drag_x_ = coords.x;
                    start_drag_y_ = coords.y;

                    vfx_manager_.GetVfx(new Vector3(start_drag_x_, start_drag_y_), GlobalEnums.VfxType.HIT);
                }

                if (Input.GetMouseButtonUp(0))
                {
                    Vector3 world_pos = Camera.main.ScreenToWorldPoint(Input.mousePosition);
                    Vector2Int coords = grid_.GetGridCoords(world_pos);
                    dest_drag_x_ = coords.x;
                    dest_drag_y_ = coords.y;

                    if (dest_drag_x_ != start_drag_x_)
                    {
                        dest_drag_y_ = start_drag_y_;
                        if (dest_drag_x_ < start_drag_x_)
                        {
                            dest_drag_x_ = start_drag_x_ - 1;
                        }
                        else
                        {
                            dest_drag_x_ = start_drag_x_ + 1;
                        }
                    }
                    else
                    {
                        dest_drag_x_ = start_drag_x_;
                        if (dest_drag_y_ < start_drag_y_)
                        {
                            dest_drag_y_ = start_drag_y_ - 1;
                        }
                        else
                        {
                            dest_drag_y_ = start_drag_y_ + 1;
                        }
                    }

                    if (model_.TrySwapGridCells(start_drag_x_, start_drag_y_, dest_drag_x_, dest_drag_y_))
                    {
                        SetBusyState(0.5f, () => state_ = State.kProcessing);
                    }
                }
                break;
            case State.kProcessing:
                if (model_.TryProcessAllMatches())
                {
                    SetBusyState(0.33f, () =>
                    {
                        model_.DoGemsFall();
                        SetBusyState(0.33f, () =>
                        {
                            model_.DoSpawnNewGems();
                            SetBusyState(0.5f, () => state_ = (State.kProcessing));
                        });
                    });
                }
                else
                {
                    //SetBusyState(0.5f, () => state_ = (State.kAvailable));
                    TryReturnToAvailableState();
                }
                break;
            case State.kGameOver:
                break;
        }
    }

    public void Init(Main model, Grid<Main.GridCell> grid)
    {
        model_ = model;
        grid_ = grid;

        float cam_offset_y = 0.1f;
        cam_.position = new Vector3(grid_.GetWidth() *.5f, grid_.GetHeight() * .5f + cam_offset_y, cam_.position.z);

        model_.OnGridCellDestroyed += HandleGridCellDestroyedEvent;
        model_.OnNewPipeSpawned += HandleNewPipeSpawnedEvent;
        model_.OnBombSpawned += HandleBombSpawnedEvent;
        model_.OnWin += HandleWinEvent;
        model_.OnLoss += HandleLossEvent;

        pipe_dict_ = new Dictionary<Main.Pipe, PipeVisual>();
        for (int x = 0; x < grid_.GetWidth(); x++)
        {
            for (int y = 0; y < grid_.GetHeight(); y++)
            {
                Main.GridCell cell = grid_.GetValue(x, y);
                Main.Pipe gem = cell.GetCellItem();

                CreatePipeVisualAtWorldPos(grid_.GetWorldPos(x, y), gem);

                Instantiate(cell_visual_template_, grid_.GetWorldPos(x, y), Quaternion.identity);
            }
        }
        SetBusyState(1.35f, () => state_ = State.kProcessing);
    }

    /// <summary>
    /// Instantiate gem_visual_template_ at pos and link new GemVisual to gem
    /// </summary>
    /// <param name="pos"></param>
    /// <param name="pipe"></param>
    /// <returns></returns>
    private Transform CreatePipeVisualAtWorldPos(Vector3 pos, Main.Pipe pipe)
    {
        Vector3 position = pos;
        position = new Vector3(position.x, start_pos_y_); //move gem way up at the start

        Transform scene_pipe = Instantiate(pipe_visual_template_, position, Quaternion.identity);
        scene_pipe.Find("Sprite").GetComponent<SpriteRenderer>().sprite = pipe.GetPipeSO().prefab.GetComponent<SpriteRenderer>().sprite;
        scene_pipe.Find("Sprite").GetComponent<SpriteRenderer>().material = pipe.GetPipeSO().prefab.GetComponent<SpriteRenderer>().sharedMaterial;
        if (pipe.GetPipeSO().prefab.GetComponent<Animator>().runtimeAnimatorController != null)
        {
            scene_pipe.Find("Sprite").GetComponent<Animator>().runtimeAnimatorController = pipe.GetPipeSO().prefab.GetComponent<Animator>().runtimeAnimatorController;
        }
        
        PipeVisual gem_visual = new PipeVisual(scene_pipe, pipe);

        pipe_dict_[pipe] = gem_visual;

        return scene_pipe;
    }

    private void DoUpdateView()
    {
        foreach (Main.Pipe gem in pipe_dict_.Keys)
        {
            pipe_dict_[gem].DoUpdate();
        }
    }

    private void SetBusyState(float wait_time, Action NewAction)
    {
        state_ = State.kBusy;
        busy_timer_ = wait_time;
        OnEndBusyAction = NewAction;
    }

    public void SwapGridCells(int start_x, int start_y, int dest_x, int dest_y)
    {
        model_.SwapGridCells(start_x, start_y, dest_x, dest_y);

        SetBusyState(0.5f, () => state_ = State.kProcessing);
    }

    private void TryReturnToAvailableState()
    {
        if (model_.GetScore() >= 10000)
        {
            state_ = State.kGameOver;
            DoWin();
        }
        else
        {
            state_ = State.kAvailable;
        }
    }

    private void DoWin()
    {
        game_over_panel_.SetActive(true);
        game_over_txt_.text = "You Win!";
    }

    public void DoLoadLevel1()
    {
        SceneManager.LoadScene("Level1");
    }
    public void DoLoadLevel2()
    {
        SceneManager.LoadScene("Level2");
    }
    public void DoLoadLevel3()
    {
        SceneManager.LoadScene("Level3");
    }
    public void DoQuit()
    {
        Application.Quit();
    }

    private void HandleGridCellDestroyedEvent(object sender, System.EventArgs e)
    {
        Main.GridCell cell = sender as Main.GridCell;
        if (cell != null && cell.GetCellItem() != null)
        {
            pipe_dict_.Remove(cell.GetCellItem());
        }
    }

    private void HandleNewPipeSpawnedEvent(object sender, Main.OnNewPipeSpawnedEventArgs e)
    {
        CreatePipeVisualAtWorldPos(e.cell.GetWorldPos(), e.gem);
    }

    private void HandleBombSpawnedEvent(object sender, Main.OnBombSpawnedEventArgs e)
    {
        vfx_manager_.GetVfx(new Vector3(e.x, e.y), GlobalEnums.VfxType.BOMB);
    }

    private void HandleScoreChangedEvent(object sender, System.EventArgs e)
    {
        score_txt_.text = "Score: " + model_.GetScore().ToString();
        audio_.PlayOneShot(audio_.clip);
    }

    private void HandleTimerChangedEvent(object sender, System.EventArgs e)
    {
        timer_txt_.text = "Timer: " + model_.GetTimer().ToString();
    }
    
    private void HandleWinEvent(object sender, System.EventArgs e)
    {
        DoWin();
        Time.timeScale = 0.0f;
    }

    private void HandleLossEvent(object sender, System.EventArgs e)
    {
        game_over_panel_.SetActive(true);
        game_over_txt_.text = "Time's Up";
        Time.timeScale = 0.0f;
    }



    public class PipeVisual
    {
        private Transform transform_;
        private Main.Pipe pipe_;
        private bool is_destroyed;
        private VfxManager vfx_manager_; //[TODO] can be made into event

        public PipeVisual(Transform t, Main.Pipe pipe)
        {
            transform_ = t;
            pipe_ = pipe;
            is_destroyed = false;

            pipe_.OnDestroyed += HandlePipeDestroyedEvent;

            vfx_manager_ = FindObjectOfType<VfxManager>();
        }

        public void DoUpdate()
        {
            if (is_destroyed)
            {
                return;
            }
            Vector3 target = pipe_.GetWorldPos();
            Vector3 dir = target - transform_.position;
            float speed = 4.5f;
            transform_.position += dir * speed * Time.deltaTime;
        }

        private void HandlePipeDestroyedEvent(object sender, System.EventArgs e)
        {
            is_destroyed = true;
            vfx_manager_.GetVfx(transform_.position, GlobalEnums.VfxType.GEM_CLEAR);
            transform_.GetComponent<Rigidbody2D>().isKinematic = false;
            transform_.GetComponent<Rigidbody2D>().bodyType = RigidbodyType2D.Dynamic;
            transform_.GetComponent<Rigidbody2D>().AddForce(new Vector2(UnityEngine.Random.Range(-0.5f, 0.5f), 0.5f).normalized * UnityEngine.Random.Range(4.8f, 12.8f), ForceMode2D.Impulse);
            transform_.GetComponent<Rigidbody2D>().AddTorque(UnityEngine.Random.Range(-2.5f, 2.5f), ForceMode2D.Impulse);
            Destroy(transform_.gameObject, 3.0f);
        }
    }
}
