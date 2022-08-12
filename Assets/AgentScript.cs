using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.MLAgents;
using Unity.MLAgents.Sensors;
using Unity.MLAgents.Actuators;
using UnityEngine.Tilemaps;

public class AgentScript : Agent
{
    [SerializeField] private bool useAStar;

    private Vector3 target;

    private Tilemap groundTileMap;
    private Tilemap colTileMap;
    //private Tilemap startTileMap;
    private Tilemap endTileMap;

    private Vector3 startPos;

    private int minimY;
    private int maximY;

    private int minimX;
    private int maximX;

    //private float timer;

    void Awake()
    {
        groundTileMap = GameObject.FindGameObjectWithTag("Walkable").GetComponent<Tilemap>();
        colTileMap = GameObject.FindGameObjectWithTag("Col").GetComponent<Tilemap>();
        //startTileMap = GameObject.FindGameObjectWithTag("Start").GetComponent<Tilemap>();
        endTileMap = GameObject.FindGameObjectWithTag("End").GetComponent<Tilemap>();
    }

    // Start is called before the first frame update
    void Start()
    {
        startPos = this.gameObject.transform.position;

        groundTileMap.CompressBounds();
        colTileMap.CompressBounds();
        endTileMap.CompressBounds();

        minimY = colTileMap.cellBounds.yMin;
        maximY = colTileMap.cellBounds.yMax;
        minimX = colTileMap.cellBounds.xMin;
        maximX = colTileMap.cellBounds.xMax;

        target = new Vector3(endTileMap.cellBounds.xMin, endTileMap.cellBounds.yMin, 0);
    }

    public override void Heuristic(in ActionBuffers actionsOut)
    {

        /*int dirX = 0, dirY = 0;

        var movement = actionsOut.DiscreteActions[0];

        float movementFloatHorizontal = Input.GetAxis("Horizontal");
        float movementFloatVertical = Input.GetAxis("Vertical");

        if (Mathf.Abs(movementFloatHorizontal) > Mathf.Abs(movementFloatVertical))
        {
            if (movementFloatHorizontal < 0)
            {
                dirX = -1;
            }
            else if (movementFloatHorizontal > 0)
            {
                dirX = 1;
            }
        }
        else if (Mathf.Abs(movementFloatHorizontal) < Mathf.Abs(movementFloatVertical))
        {
            if (movementFloatVertical < 0)
            {
                dirY = -1;
            }
            else if (movementFloatVertical > 0)
            {
                dirY = 1;
            }
        }
        
        var normVector = new Vector2(dirX, dirY);

        if (!normVector.Equals(Vector2.zero))
        {
            Move(normVector);
        }*/
    }

    public override void OnEpisodeBegin()
    {
        this.transform.localPosition = startPos;
        //timer = 0;
    }

    public override void CollectObservations(VectorSensor sensor)
    {
        /*int origValueX = groundTileMap.WorldToCell(this.transform.position).x;
        int origValueY = groundTileMap.WorldToCell(this.transform.position).y;

        int targetValueX = endTileMap.WorldToCell(Target.position).x;
        int targetValueY = endTileMap.WorldToCell(Target.position).y;

        // normalize all vectors
        int agentNormalizedValueX = (origValueX - minimX) / (maximX - minimX);
        int agentNormalizedValueY = (origValueY - minimY) / (maximY - minimY);

        int targetNormalizedValueX = (targetValueX - minimX) / (maximX - minimX);
        int targetNormalizedValueY = (targetValueY - minimY) / (maximY - minimY);

        int distanceX = Mathf.Abs(origValueX) + Mathf.Abs(targetValueX);
        int distanceY = Mathf.Abs(origValueY) + Mathf.Abs(targetValueY);

        float distanceNormalizedX = (distanceX - minDistanceX) / (maxDistanceX - minDistanceX);
        float distanceNormalizedY = (distanceY - minDistanceY) / (maxDistanceY - minDistanceY);*/
        var observationAgentX = (this.transform.position.x - minimX) / (maximX - minimX);
        var observationAgentY = (this.transform.position.y - minimY) / (maximY - minimY);

        var observationEndTileX = (target.x - minimX) / (maximX - minimX);
        var observationEndTileY = (target.y - minimY) / (maximY - minimY);

        sensor.AddObservation(observationAgentX);
        sensor.AddObservation(observationAgentY);
        sensor.AddObservation(observationEndTileX);
        sensor.AddObservation(observationEndTileY);
        sensor.AddObservation((Mathf.Abs(this.transform.position.x) + Mathf.Abs(target.x)) / maximX);
        sensor.AddObservation((Mathf.Abs(this.transform.position.y) + Mathf.Abs(target.y)) / maximY);
    }

    public override void OnActionReceived(ActionBuffers actions)
    {
        int dirX = 0, dirY = 0;

        var movement = actions.DiscreteActions[0];
        if (movement == 1)
        {
            dirX = -1;
        }
        if (movement == 2)
        {
            dirX = 1;
        }
        if (movement == 3)
        {
            dirY = -1;
        }
        if (movement == 4)
        {
            dirY = 1;
        }

        var normVector = new Vector2(dirX, dirY);

        AddReward(-0.01f);
        if (!Move(normVector))
        {
            AddReward(-0.001f);
        }

        if (OnEndTile())
        {
            Debug.Log("tultiin maaliin");
            AddReward(1.0f);
            EndEpisode();
        }
    }

    private bool OnEndTile()
    {
        Vector3Int gridPos = endTileMap.WorldToCell(this.transform.position);
        if (endTileMap.HasTile(gridPos))
        {
            return true;
        }
        else return false;
    }

    private bool Move(Vector2 direction)
    {
        if (CanMove(direction))
        {
            transform.position += (Vector3)direction;
            return true;
        }
        else return false;
    }

    private bool CanMove(Vector2 direction)
    {
        Vector3Int gridPosition = groundTileMap.WorldToCell(this.transform.position + (Vector3)direction);

        if (!groundTileMap.HasTile(gridPosition) || colTileMap.HasTile(gridPosition))
        {
            return false;
        }
        else
        {
            return true;
        }
    }

}
