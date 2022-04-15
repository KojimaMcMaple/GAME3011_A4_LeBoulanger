using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu()]
public class GemSO : ScriptableObject
{
    public string gem_name;
    public GameObject prefab;
    public Sprite sprite;
    public Material mat;
    public Animator anim;
    public bool is_immovable = false;
}
