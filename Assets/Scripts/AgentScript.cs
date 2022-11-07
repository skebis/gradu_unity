using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.MLAgents;
using Unity.MLAgents.Sensors;
using Unity.MLAgents.Actuators;
using UnityEngine.Tilemaps;
using TMPro;
using System.Linq;
using System;
using UnityEngine.Rendering;
using Unity.Mathematics;
using Random = UnityEngine.Random;
using System.Threading;

public class AgentScript : Agent
{
    [SerializeField] private bool debuggable;

    [Tooltip("Selecting will turn on action masking. Note that a model trained with action " +
        "masking turned on may not behave optimally when action masking is turned off.")]
    [SerializeField] private bool maskActions;

    [SerializeField] private Transform target;


    private Tilemap groundTileMap;
    private Tilemap colTileMap;

    private Vector3 startPos;

    private int minimY;
    private int maximY;

    private int minimX;
    private int maximX;

    private bool spawningTile = false;
    private Vector2Int currentEndTilePos;

    private Vector2 moveTo;

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
        startPos = this.gameObject.transform.localPosition;

        groundTileMap.CompressBounds();
        colTileMap.CompressBounds();

        minimY = colTileMap.cellBounds.yMin;
        maximY = colTileMap.cellBounds.yMax-1;
        minimX = colTileMap.cellBounds.xMin;
        maximX = colTileMap.cellBounds.xMax-1;

        if (debuggable)
        {
            CreateTileText();
        }
    }

    public override void Heuristic(in ActionBuffers actionsOut)
    {
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
        // Reset agent position.
        this.transform.localPosition = startPos;
        spawningTile = true;

        // Spawn target on a ground tile and make sure it does not collide with walls.
        while (spawningTile)
        {
            LayerMask mask = LayerMask.GetMask("Collision");
            currentEndTilePos = new Vector2Int(Random.Range(minimX, maximX), Random.Range(minimY, maximY));
            Collider2D collid = Physics2D.OverlapBox(currentEndTilePos, new Vector2(2f,2f), 0f, mask);
            if (groundTileMap.HasTile((Vector3Int)currentEndTilePos) && !collid && !MovingBlocksAreaDiff.ignoredSpawnPositions.Contains((Vector3Int)currentEndTilePos))
            {
                target.localPosition = (Vector2)currentEndTilePos;
                spawningTile = false;
            }
        }
    }

    public override void CollectObservations(VectorSensor sensor)
    {
        // Observe agent and target positions.
        sensor.AddObservation((Vector2)this.transform.localPosition);
        sensor.AddObservation((Vector2)target.localPosition);
    }

    public override void WriteDiscreteActionMask(IDiscreteActionMask actionMask)
    {
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

    public override void OnActionReceived(ActionBuffers actions)
    {
        // Give small negative reward for every step to encourage fast episode completion.
        if (MaxStep > 0)
        {
            AddReward((-1f) / MaxStep);
        }

        int movement = actions.DiscreteActions[0];

        switch(movement)
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

        // Set reward to -1 if character tried to move somewhere it cant and end the episode.
        if (!Move(moveTo))
        {
            SetReward(-1.0f);
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
            Debug.Log("tultiin maaliin");
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
            return true;
        }
    }

    /// <summary>
    /// Marks tile coordinates on walkable tiles for debugging purposes.
    /// </summary>
    private void CreateTileText()
    {
        Canvas canv = GameObject.FindGameObjectWithTag("Canv").GetComponent<Canvas>();

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

}
