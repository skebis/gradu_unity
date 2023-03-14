using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Tilemaps;
using Unity.Mathematics;
using TMPro;
using static UnityEngine.GraphicsBuffer;

public class Pathfinding : MonoBehaviour
{
    [SerializeField] private bool debuggable;
    [SerializeField] private Transform target;

    private GameObject areaObject;

    //private Tilemap endTileMap;
    //private Tilemap startTileMap;
    private Tilemap walkableTiles;
    private Tilemap obstacles;

    private NodeBase start;
    private NodeBase end;

    private int loopBoundary = 50000;

    [SerializeField]
    private Tile defaultTile;

    private readonly Color defaultColor = Color.white;

    private Dictionary<int2, bool> isObstacle = new Dictionary<int2, bool>();
    private Dictionary<int2, NodeBase> nodes = new Dictionary<int2, NodeBase>();
    private Dictionary<int2, NodeBase> openSet = new Dictionary<int2, NodeBase>();

    private Stack<int2> path = new Stack<int2>();

    private HashSet<int2> offsets = new HashSet<int2>();

    private float fullTime;
    private int nodeCount;

    private void Start()
    {
        areaObject = GameObject.FindGameObjectWithTag("Area");
        // hard-coded area camera properties
        if (areaObject.name == "Area1")
        {
            Camera.main.transform.position = new Vector3(0,0,-10f);
            Camera.main.orthographicSize = 5f;
        }
        else if (areaObject.name == "Area2")
        {
            Camera.main.transform.position = new Vector3(0, 0, -10f);
            Camera.main.orthographicSize = 11f;
        }
        else if (areaObject.name == "Area3")
        {
            Camera.main.transform.position = new Vector3(0f, 9f, -10f);
            Camera.main.orthographicSize = 28f;
        }

        //endTileMap = GameObject.FindGameObjectWithTag("End").GetComponent<Tilemap>();
        //startTileMap = GameObject.FindGameObjectWithTag("Start").GetComponent<Tilemap>();
        walkableTiles = GameObject.FindGameObjectWithTag("Walkable").GetComponent<Tilemap>();
        obstacles = GameObject.FindGameObjectWithTag("Col").GetComponent<Tilemap>();

        start = new NodeBase { coord = int2.zero, parent = int2.zero, G = int.MaxValue, H = int.MaxValue };
        end = new NodeBase { coord = int2.zero, parent = int2.zero, G = int.MaxValue, H = int.MaxValue };

        offsets.Add(new int2(0, 1));
        offsets.Add(new int2(0, -1));
        offsets.Add(new int2(1, 0));
        offsets.Add(new int2(-1, 0));

        ResetNodes();
        ResetTileColors();
        CompressTileMaps();
        this.transform.position = new Vector3(start.coord.x, start.coord.y, 0);
        end.coord = new int2((int)target.position.x, (int)target.position.y);
        nodeCount = 0;
        fullTime = 0f;
        FindPath(start.coord);
    }

    /*private void Update()
    {
        fullTime += Time.deltaTime;

        if (Input.GetMouseButtonDown(0))
        {
            Vector3 mouseWorldPos = Camera.main.ScreenToWorldPoint(Input.mousePosition);

            Vector3Int mouseCell = walkableTiles.WorldToCell(mouseWorldPos);
            mouseCell.z = 0;
            target.position = mouseCell;
            end.coord = new int2(mouseCell.x, mouseCell.y);
            start.coord = int2.zero;

            ResetNodes();
            ResetTileColors();
            CompressTileMaps();
            this.transform.position = new Vector3(start.coord.x, start.coord.y, 0);
            startTime = Time.realtimeSinceStartup;
            FindPath(start.coord);
        }
    }*/

    /// <summary>
    /// A* pathfinding algorithm.
    /// </summary>
    /// <param name="startCoords">Start coordinates</param>
    private void FindPath(int2 startCoords)
    {
        NodeBase current = new NodeBase();
        current.coord = startCoords;
        current.G = 0;
        current.H = ManhattanDistance(startCoords, end.coord);
        //Debug.Log(startCoords + " " + end.coord + " " + ManhattanDistance(startCoords, end.coord));
        openSet.TryAdd(current.coord, current);

        int counter = 0;

        // loop until openset is empty and current node is at end node.
        do
        {
            current = openSet[ClosestNode()];
            nodes.TryAdd(current.coord, current);
            //TextMesh nodeText = new TextMesh();
            //nodeText.transform.position = new Vector3(current.coord.x, current.coord.y, 0);
            
            foreach (int2 ints in offsets)
            {
                // check if offset node is already checked or is an obstacle
                if (!nodes.ContainsKey(current.coord + ints) && !isObstacle.ContainsKey(current.coord + ints))
                {
                    NodeBase neighbour = new NodeBase
                    {
                        coord = current.coord + ints,
                        parent = current.coord,
                        G = current.G + ManhattanDistance(current.coord, current.coord + ints),
                        H = ManhattanDistance(current.coord + ints, end.coord)
                    };

                    // update neighbor G-value if new neighbor already exists in the open set and new neighbor has lower G-value
                    if (openSet.ContainsKey(neighbour.coord) && neighbour.G < openSet[neighbour.coord].G)
                    {
                        openSet[neighbour.coord] = neighbour;
                        if (debuggable)
                        {
                            CreateTileText(openSet[neighbour.coord]);
                        }
                    }
                    // add neighbor to open set if it isnt there yet
                    else if (!openSet.ContainsKey(neighbour.coord))
                    {
                        openSet.TryAdd(neighbour.coord, neighbour);
                        if (debuggable)
                        {
                            CreateTileText(neighbour);
                        }
                    }
                }
            }
            openSet.Remove(current.coord);
            counter++;

            if (counter > loopBoundary)
                break;

        } while (openSet.Count != 0 && !current.coord.Equals(end.coord));

        // marking what nodes have been expanded and painting them yellow.
        /*NodeBase[] nodesArray = nodes.Values.ToArray();
        for (int i = 0; i < nodesArray.Length; i++)
        {
            Vector3Int currentNode = new Vector3Int(nodesArray[i].coord.x, nodesArray[i].coord.y, 0);

            if (!startCoords.Equals(nodesArray[i].coord) && !end.coord.Equals(nodesArray[i].coord) && !isObstacle.ContainsKey(nodesArray[i].coord))
            {
                walkableTiles.SetTileFlags(currentNode, TileFlags.None);
                walkableTiles.SetColor(currentNode, Color.yellow);
            }
        }*/


        // creates a path from start to end for moving purposes, painting the path blue.
        if (nodes.ContainsKey(end.coord))
        {
            //Debug.Log("painting tiles");
            int2 currentCoord = end.coord;
            path.Push(end.coord);

            while (!currentCoord.Equals(startCoords))
            {
                currentCoord = nodes[currentCoord].parent;
                Debug.Log(currentCoord);
                if (!currentCoord.Equals(int2.zero))
                {
                    path.Push(currentCoord);

                }
                /*Vector3Int currentTile = new Vector3Int(currentCoord.x, currentCoord.y, 0);
                walkableTiles.SetTileFlags(currentTile, TileFlags.None);
                walkableTiles.SetColor(currentTile, Color.blue);*/
            }
        }

        StartCoroutine(DelayedMovement(0.2f));
    }

    /// <summary>
    /// Collision detection for game object.
    /// </summary>
    /// <param name="collider">Collider2D that hit the object.</param>
    private void OnTriggerEnter2D(Collider2D collider)
    {
        if (collider.gameObject.CompareTag("End"))
        {
            //float endTime = Time.realtimeSinceStartup;
            Debug.Log("Hit the target. A* TIME: " + GlobalTimer.globalTimer + " NodeCount: " + nodeCount);

        }
    }

    /// <summary>
    /// Compress all the tilemaps so they are much more smaller.
    /// </summary>
    private void CompressTileMaps()
    {
        // start tile map only has one tile and the tile is located at bottom left corner (xmin and ymin)
        //startTileMap.CompressBounds();
        //start.coord = new int2(startTileMap.cellBounds.xMin, startTileMap.cellBounds.yMin);
        // end tile map only has one tile and the tile is located at bottom left corner (xmin and ymin)
        //endTileMap.CompressBounds();
        //end.coord = new int2(endTileMap.cellBounds.xMin, endTileMap.cellBounds.yMin);

        obstacles.CompressBounds();
        BoundsInt bint = obstacles.cellBounds;

        // go through the whole obstacle tilemap and add obstacles to isObstacle-dictionary
        for (int i = bint.xMin; i < bint.xMax; i++)
        {
            for (int j = bint.yMin; j < bint.yMax; j++)
            {
                Vector3Int pos = new Vector3Int(i, j, 0);
                if (obstacles.HasTile(pos))
                {
                    isObstacle.Add(new int2(pos.x, pos.y), true);
                }
            }
        }
    }

    /// <summary>
    /// Calculating manhattan distance (integer)
    /// </summary>
    /// <param name="coordA">first coordinate x and y</param>
    /// <param name="coordB">second coordinate x and y</param>
    /// <returns>manhattan distance (integer)</returns>
    private int ManhattanDistance(int2 coordA, int2 coordB)
    {
        int a = Mathf.Abs(coordA.x - coordB.x);
        int b = Mathf.Abs(coordA.y - coordB.y);

        return a + b;
    }

    /// <summary>
    /// Returns the best node from the open set by comparing g+h values.
    /// </summary>
    /// <returns>Best node coordinates</returns>
    private int2 ClosestNode()
    {
        NodeBase result = new NodeBase();
        int fScore = int.MaxValue;

        NodeBase[] tempNodeArray = openSet.Values.ToArray();

        for (int i = 0; i < tempNodeArray.Length; i++)
        {
            if (tempNodeArray[i].G + tempNodeArray[i].H < fScore)
            {
                result = tempNodeArray[i];
                fScore = tempNodeArray[i].G + tempNodeArray[i].H;
            }
        }
        return result.coord;
    }

    /// <summary>
    /// Coroutine to do agent movement in certain intervals.
    /// </summary>
    /// <param name="delay">Delay in seconds.</param>
    /// <returns></returns>
    private IEnumerator DelayedMovement(float delay)
    {
        while (!(this.transform.position.x.Equals(end.coord.x) && this.transform.position.y.Equals(end.coord.y)))
        {
            int2 cur = path.Pop();
            if (obstacles.HasTile(new Vector3Int(cur.x, cur.y, 0)))
            {
                ResetNodes();
                CompressTileMaps();
                ResetTileColors();
                FindPath(new int2((int)(this.transform.position.x), (int)(this.transform.position.y)));
                break;
            }
            this.transform.position = new Vector3(cur.x, cur.y);
            nodeCount++;
            yield return new WaitForSeconds(delay);
        }
        yield return null;
    }

    /// <summary>
    /// Reset all collections.
    /// </summary>
    private void ResetNodes()
    {
        isObstacle.Clear();
        nodes.Clear();
        openSet.Clear();
        path.Clear();

        foreach (Canvas canv in walkableTiles.transform.GetComponentsInChildren<Canvas>())
        {
            GameObject.Destroy(canv.gameObject);
        }
    }

    /// <summary>
    /// Resets the tile colors on walkable tiles.
    /// </summary>
    private void ResetTileColors()
    {
        walkableTiles.CompressBounds();
        BoundsInt bint = walkableTiles.cellBounds;

        // go through the whole obstacle tilemap and add obstacles to isObstacle-dictionary
        for (int i = bint.xMin; i < bint.xMax; i++)
        {
            for (int j = bint.yMin; j < bint.yMax; j++)
            {
                Vector3Int pos = new Vector3Int(i, j, 0);
                if (walkableTiles.HasTile(pos))
                {
                    walkableTiles.SetColor(pos, defaultColor);
                }
            }
        }
    }

    private void CreateTileText(NodeBase neighbour)
    {
        Canvas canv = GameObject.FindGameObjectWithTag("Canv").GetComponent<Canvas>();
        if (neighbour.nodeCanvas == null)
        {
            neighbour.nodeCanvas = Instantiate(canv);
            neighbour.nodeCanvas.transform.SetParent(walkableTiles.gameObject.transform);
        }
        neighbour.nodeCanvas.transform.position = new Vector3(neighbour.coord.x, neighbour.coord.y, 0);
        TextMeshProUGUI neighbourText = neighbour.nodeCanvas.GetComponentInChildren<TextMeshProUGUI>();
        neighbourText.SetText(neighbour.nodeText);
    }
}
