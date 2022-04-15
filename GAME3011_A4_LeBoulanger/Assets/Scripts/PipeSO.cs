using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu()]
public class PipeSO : ScriptableObject
{
    public string pipe_name;
    public GameObject prefab;
    public Sprite sprite; //redundant because prefab
    public Material mat; //redundant because prefab
    public Animator anim; //redundant because prefab
    public GlobalEnums.LineTileType line_type;
    public bool is_immovable = false;
}
