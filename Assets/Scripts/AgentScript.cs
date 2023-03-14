using UnityEngine;
using Unity.MLAgents;
using Unity.MLAgents.Sensors;
using Unity.MLAgents.Actuators;
using UnityEngine.Tilemaps;
using TMPro;
using System.Linq;
using System;
using Random = UnityEngine.Random;

public class AgentScript : Agent
{
    [SerializeField] private bool debuggable;
    [SerializeField] private bool testingMode;

    [Tooltip("Selecting will turn on action masking. Note that a model trained with action " +
        "masking turned on may not behave optimally when action masking is turned off.")]
    [SerializeField] private bool maskActions;

    [SerializeField] private Transform target;


    private Tilemap groundTileMap;
    private Tilemap colTileMap;

    private Vector3Int startPos;

    private int minimY;
    private int maximY;

    private int minimX;
    private int maximX;

    private bool spawningTile = true;

    private Vector2 moveTo;

    private Vector2Int currentEndTilePos;

    private bool useIgnoredSpawnPositions = false;

    private readonly Color defaultColor = Color.white;

    private float fullTime;
    private int nodeCount;

    void Awake()
    {
        var tilemaps = this.gameObject.transform.parent.GetComponentsInChildren<Tilemap>();

        foreach (var tilemap in tilemaps)
        {
            if (tilemap.gameObject.CompareTag("Walkable"))
            {
                groundTileMap = tilemap;
            }
            else if (tilemap.gameObject.CompareTag("Col"))
            {
                colTileMap = tilemap;
            }
        }
    }

    // Start is called before the first frame update
    void Start()
    {
        // if current area is the difficult one with dynamic objects
        // dont let target spawn on the dynamic parts.
        if (this.gameObject.transform.parent.name.StartsWith("AreaDifficult"))
        {
            useIgnoredSpawnPositions = true;
        }

        startPos = new Vector3Int(0,0,0);

        groundTileMap.CompressBounds();
        colTileMap.CompressBounds();

        minimY = colTileMap.cellBounds.yMin;
        maximY = colTileMap.cellBounds.yMax-1; // y max value is top of the tile but we want bottom value.
        minimX = colTileMap.cellBounds.xMin;
        maximX = colTileMap.cellBounds.xMax-1; // x max value is right of the tile but we want left value.

        if (debuggable)
        {
            CreateTileText();
        }
        //Time.timeScale = 0;
        fullTime = 0f;
    }

    /*private void Update()
    {
        fullTime += Time.deltaTime;
        if (Input.GetMouseButtonDown(0) && testingMode)
        {
            Vector3 mouseWorldPos = Camera.main.ScreenToWorldPoint(Input.mousePosition);

            Vector3Int mouseCell = groundTileMap.WorldToCell(mouseWorldPos);
            mouseCell.z = 0;
            target.position = mouseCell;
            spawningTile = false;
            Time.timeScale = 1;
        }
    }*/

    public override void Heuristic(in ActionBuffers actionsOut)
    {
        // When using inference type heuristic, use arrow keys to move the agent.
        var discreteAc = actionsOut.DiscreteActions;
        
        float movementFloatHorizontal = Mathf.Clamp(Input.GetAxis("Horizontal"), -1f, 1f);
        float movementFloatVertical = Mathf.Clamp(Input.GetAxis("Vertical"), -1f, 1f);

        if (movementFloatHorizontal < 0f)
        {
            discreteAc[0] = 1;
        }
        if (movementFloatHorizontal > 0f)
        {
            discreteAc[0] = 2;
        }
        if (movementFloatVertical < 0f)
        {
            discreteAc[0] = 3;
        }
        if (movementFloatVertical > 0f)
        {
            discreteAc[0] = 4;
        }
    }

    public override void OnEpisodeBegin()
    {
        Debug.Log("episode begin");
        ResetTileColors();
        // Reset agent position.
        this.transform.localPosition = Vector3.zero;
        spawningTile = false;

        // Spawn target on a ground tile and make sure it does not collide with walls.
        while (spawningTile && !testingMode)
        {
            Time.timeScale = 1;
            LayerMask mask = LayerMask.GetMask("Collision");
            currentEndTilePos = new Vector2Int(Random.Range(minimX, maximX), Random.Range(minimY, maximY));
            Collider2D collid = Physics2D.OverlapBox(currentEndTilePos, new Vector2(1f,1f), 0f, mask);
            if (groundTileMap.HasTile((Vector3Int)currentEndTilePos) && !collid)
            {
                // Skip this iteration if using difficult area and spawnpoint is forbidden.
                if (useIgnoredSpawnPositions && MovingBlocksAreaDiff.ignoredSpawnPositions.Contains((Vector3Int)currentEndTilePos))
                {
                    Debug.Log("Forbidden spawnpoint!");
                    continue;
                }
                target.localPosition = (Vector2)currentEndTilePos;
                spawningTile = false;
            }
        }
    }

    public override void CollectObservations(VectorSensor sensor)
    {
        // Observe agent and target positions and distance between them.
        Vector2 agentPos = (Vector2)this.transform.localPosition;
        Vector2 targetPos = (Vector2)target.localPosition;
        sensor.AddObservation(agentPos);
        sensor.AddObservation(targetPos);
        sensor.AddObservation(Vector2.Distance(agentPos,targetPos));
    }

    public override void OnActionReceived(ActionBuffers actions)
    {
        // Give small negative reward for every step to encourage fast episode completion.
        if (MaxStep > 0)
        {
            AddReward((-1f) / MaxStep);
        }

        int movement = actions.DiscreteActions[0];

        switch (movement)
        {
            case 0:
                moveTo = Vector2.zero;
                break;
            case 1:
                moveTo = Vector2.left;
                break;
            case 2:
                moveTo = Vector2.right;
                break;
            case 3:
                moveTo = Vector2.down;
                break;
            case 4:
                moveTo = Vector2.up;
                break;
            default:
                throw new ArgumentException("No action value");
        }

        // Give -1 and end current episode if agent collided against a wall.
        if (!Move(moveTo))
        {
            AddReward(-1f);
            EndEpisode();
        }
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
            Debug.Log("Hit the target. AGENT TIME: " + GlobalTimer.globalTimer + " NodeCount: " + nodeCount);
            SetReward(1.0f);
            EndEpisode();
        }
    }

    /// <summary>
    /// Moves object to given direction if it is allowed.
    /// </summary>
    /// <param name="direction">Vector direction</param>
    /// <returns>True if object was moved, false otherwise.</returns>
    private bool Move(Vector3 direction)
    {
        nodeCount++;
        if (CanMove(direction))
        {
            transform.localPosition += direction;
            return true;
        }
        else return false;
    }

    /// <summary>
    /// Can object move to given direction.
    /// </summary>
    /// <param name="direction">Vector direction</param>
    /// <returns>True if can move, false otherwise.</returns>
    private bool CanMove(Vector3 direction)
    {
        Vector3Int gridPosition = groundTileMap.LocalToCell(this.transform.localPosition + direction);

        if (!groundTileMap.HasTile(gridPosition) && colTileMap.HasTile(gridPosition))
        {
            return false;
        }
        else
        {
            //PaintCurrentTile(gridPosition);
            return true;
        }
    }

    /// <summary>
    /// Sets tile color to blue.
    /// </summary>
    /// <param name="gridPosition">Current position</param>
    private void PaintCurrentTile(Vector3Int gridPosition)
    {
        groundTileMap.SetTileFlags(gridPosition, TileFlags.None);
        groundTileMap.SetColor(gridPosition, Color.blue);
    }

    /// <summary>
    /// Resets the tile colors on walkable tiles.
    /// </summary>
    private void ResetTileColors()
    {
        BoundsInt bint = groundTileMap.cellBounds;

        // go through the whole obstacle tilemap and add obstacles to isObstacle-dictionary
        for (int i = bint.xMin; i < bint.xMax; i++)
        {
            for (int j = bint.yMin; j < bint.yMax; j++)
            {
                Vector3Int pos = new Vector3Int(i, j, 0);
                if (groundTileMap.HasTile(pos))
                {
                    groundTileMap.SetColor(pos, defaultColor);
                }
            }
        }
    }

    /// <summary>
    /// Marks tile coordinates on walkable tiles for debugging purposes.
    /// </summary>
    private void CreateTileText()
    {
        Canvas canv = GameObject.FindGameObjectWithTag("Canv").GetComponent<Canvas>();

        // Loop through all the tiles and set tile indexes as text on them.
        for (int i = minimX; i < maximX; i++)
        {
            for (int j = minimY; j < maximY; j++)
            {
                if (groundTileMap.GetTile(new Vector3Int(i, j)) != null)
                {
                    Canvas tempCanv = Instantiate(canv);
                    tempCanv.transform.SetParent(groundTileMap.gameObject.transform);
                    tempCanv.transform.localPosition = new Vector3(i, j);
                    TextMeshProUGUI neighbourText = tempCanv.GetComponentInChildren<TextMeshProUGUI>();
                    neighbourText.SetText(i + ", " + j);
                }
            }
        }
    }

    public override void WriteDiscreteActionMask(IDiscreteActionMask actionMask)
    {
        // NOT IN USE IN THE END PRODUCT!
        // Mask the necessary actions if selected by the user.
        if (maskActions)
        {
            // Prevents the agent from picking an action that would make it collide with a wall
            Vector3Int gridPos = colTileMap.LocalToCell(this.transform.localPosition);

            if (colTileMap.HasTile(gridPos + Vector3Int.left))
            {
                actionMask.SetActionEnabled(0, 1, false);
            }

            if (colTileMap.HasTile(gridPos + Vector3Int.right))
            {
                actionMask.SetActionEnabled(0, 2, false);
            }

            if (colTileMap.HasTile(gridPos + Vector3Int.down))
            {
                actionMask.SetActionEnabled(0, 3, false);
            }

            if (colTileMap.HasTile(gridPos + Vector3Int.up))
            {
                actionMask.SetActionEnabled(0, 4, false);
            }
        }
    }

}
