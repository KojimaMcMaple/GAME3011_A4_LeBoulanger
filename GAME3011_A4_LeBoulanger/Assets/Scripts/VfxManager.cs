using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
///  The Source file name: VfxManager.cs
///  Author's name: Trung Le (Kyle Hunter)
///  Student Number: 101264698
///  Program description: Manages the queue
///  Date last Modified: See GitHub
///  Revision History: See GitHub
/// </summary>
[System.Serializable]
public class VfxManager : MonoBehaviour
{
    private Queue<GameObject> hit_vfx_pool_;
    private Queue<GameObject> gem_clear_vfx_pool_;
    private Queue<GameObject> bomb_vfx_pool_;
    [SerializeField] private int hit_vfx_num_;
    [SerializeField] private int gem_clear_vfx_num_;
    [SerializeField] private int bomb_vfx_num_;
    //public GameObject vfx_obj;

    private VfxFactory factory_;

    private void Awake()
    {
        hit_vfx_pool_ = new Queue<GameObject>();
        gem_clear_vfx_pool_ = new Queue<GameObject>();
        bomb_vfx_pool_ = new Queue<GameObject>();
        factory_ = GetComponent<VfxFactory>();
        BuildVfxPool(); //pre-build a certain num of vfxs to improve performance
    }

    /// <summary>
    /// Builds a pool of vfxs in vfx_num amount
    /// </summary>
    private void BuildVfxPool()
    {
        for (int i = 0; i < hit_vfx_num_; i++)
        {
            PreAddVfx(GlobalEnums.VfxType.HIT);
        }
        for (int i = 0; i < gem_clear_vfx_num_; i++)
        {
            PreAddVfx(GlobalEnums.VfxType.GEM_CLEAR);
        }
        for (int i = 0; i < bomb_vfx_num_; i++)
        {
            PreAddVfx(GlobalEnums.VfxType.BOMB);
        }
    }

    /// <summary>
    /// Based on AddVfx() without num++, otherwise would cause infinite loop
    /// </summary>
    private void PreAddVfx(GlobalEnums.VfxType type = GlobalEnums.VfxType.HIT)
    {
        //var temp = Instantiate(vfx_obj, this.transform);
        var temp = factory_.CreateVfx(type);

        switch (type)
        {
            case GlobalEnums.VfxType.HIT:
                hit_vfx_pool_.Enqueue(temp);
                break;
            case GlobalEnums.VfxType.GEM_CLEAR:
                gem_clear_vfx_pool_.Enqueue(temp);
                break;
            case GlobalEnums.VfxType.BOMB:
                bomb_vfx_pool_.Enqueue(temp);
                break;
            default:
                break;
        }
    }

    /// <summary>
    /// Uses the factory to spawn one object, add it to the queue, and increase the pool size 
    /// </summary>
    private void AddVfx(GlobalEnums.VfxType type = GlobalEnums.VfxType.HIT)
    {
        //var temp = Instantiate(vfx_obj, this.transform);
        var temp = factory_.CreateVfx(type);

        switch (type)
        {
            case GlobalEnums.VfxType.HIT:
                //temp.SetActive(false);
                hit_vfx_pool_.Enqueue(temp);
                hit_vfx_num_++;
                break;
            case GlobalEnums.VfxType.GEM_CLEAR:
                //temp.SetActive(false);
                gem_clear_vfx_pool_.Enqueue(temp);
                gem_clear_vfx_num_++;
                break;
            case GlobalEnums.VfxType.BOMB:
                //temp.SetActive(false);
                bomb_vfx_pool_.Enqueue(temp);
                bomb_vfx_num_++;
                break;
            default:
                break;
        }
    }

    /// <summary>
    /// // Removes a vfx from the pool and return a ref to it
    /// </summary>
    /// <param name="position"></param>
    /// <param name="type"></param>
    /// <returns></returns>
    public GameObject GetVfx(Vector3 position,
                                GlobalEnums.VfxType type = GlobalEnums.VfxType.HIT)
    {
        //Debug.Log(">>> Spawning Vfx...");
        GameObject temp = null;
        GameObject temp2 = null;
        switch (type)
        {
            case GlobalEnums.VfxType.HIT:
                if (hit_vfx_pool_.Count < 1) //add one vfx if pool empty
                {
                    AddVfx(GlobalEnums.VfxType.HIT);
                }
                temp = hit_vfx_pool_.Dequeue();
                break;
            case GlobalEnums.VfxType.GEM_CLEAR:
                if (gem_clear_vfx_pool_.Count < 1) //add one vfx if pool empty
                {
                    AddVfx(GlobalEnums.VfxType.GEM_CLEAR);
                }
                temp = gem_clear_vfx_pool_.Dequeue();
                break;
            case GlobalEnums.VfxType.BOMB:
                if (bomb_vfx_pool_.Count < 1) //add one vfx if pool empty
                {
                    AddVfx(GlobalEnums.VfxType.BOMB);
                }
                temp = bomb_vfx_pool_.Dequeue();
                break;
            default:
                break;
        }
        temp.transform.position = position;
        temp.SetActive(true);
        temp.GetComponent<VfxController>().DoSpawn();

        return temp;
    }

    /// <summary>
    /// Returns a vfx back into the pool
    /// </summary>
    /// <param name="returned_vfx"></param>
    /// <param name="type"></param>
    public void ReturnVfx(GameObject returned_vfx, GlobalEnums.VfxType type = GlobalEnums.VfxType.HIT)
    {
        returned_vfx.SetActive(false);

        switch (type)
        {
            case GlobalEnums.VfxType.HIT:
                hit_vfx_pool_.Enqueue(returned_vfx);
                break;
            case GlobalEnums.VfxType.GEM_CLEAR:
                gem_clear_vfx_pool_.Enqueue(returned_vfx);
                break;
            case GlobalEnums.VfxType.BOMB:
                bomb_vfx_pool_.Enqueue(returned_vfx);
                break;
            default:
                break;
        }
    }
}
