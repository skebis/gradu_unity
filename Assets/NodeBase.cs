using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Mathematics;

public struct NodeBase
{

    public int2 coord;
    public int2 parent;
    public int G;
    public int H;
    public string nodeText => G.ToString();

    public Canvas nodeCanvas;
}
