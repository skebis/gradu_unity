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

public class AgentScript : Agent
{
    [SerializeField] private bool debuggable;

    public float timeBetweenDecisionsAtInference;
    float m_TimeSinceDecision;

    [Tooltip("Selecting will turn on action masking. Note that a model trained with action " +
        "masking turned on may not behave optimally when action masking is turned off.")]
    public bool maskActions = true;

    private GameObject areaObject;

    private Vector3 target;

    private Tilemap groundTileMap;
    private Tilemap colTileMap;
    private Tilemap endTileMap;

    private Vector3 startPos;

    private int minimY;
    private int maximY;

    private int minimX;
    private int maximX;

    private Queue<Vector2> waypointQueue = new Queue<Vector2>();

    //private float timer;

    void Awake()
    {
        //groundTileMap = GameObject.FindGameObjectWithTag("Walkable").GetComponent<Tilemap>();
        //colTileMap = GameObject.FindGameObjectWithTag("Col").GetComponent<Tilemap>();
        //startTileMap = GameObject.FindGameObjectWithTag("Start").GetComponent<Tilemap>();
        //endTileMap = GameObject.FindGameObjectWithTag("End").GetComponent<Tilemap>();

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
            else if (tilemap.gameObject.CompareTag("End"))
            {
                endTileMap = tilemap;
            }
        }
    }

    // Start is called before the first frame update
    void Start()
    {
        areaObject = GameObject.FindGameObjectWithTag("Area");
        // hard-coded area camera properties
        if (areaObject.name == "Area1")
        {
            Camera.main.transform.position = new Vector3(0, 0, -10f);
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
            Camera.main.orthographicSize = 40f;
            
        }

        startPos = this.gameObject.transform.localPosition;

        groundTileMap.CompressBounds();
        colTileMap.CompressBounds();
        endTileMap.CompressBounds();

        minimY = colTileMap.cellBounds.yMin;
        maximY = colTileMap.cellBounds.yMax-1;
        minimX = colTileMap.cellBounds.xMin;
        maximX = colTileMap.cellBounds.xMax-1;

        target = groundTileMap.CellToLocal(new Vector3Int(endTileMap.cellBounds.xMin, endTileMap.cellBounds.yMin));

        Debug.Log(target.x + " " + target.y);

        if (debuggable)
        {
            CreateTileText();
        }

        /*Physics2D.IgnoreCollision(gameObject.GetComponent<BoxCollider2D>(), groundTileMap.GetComponent<CompositeCollider2D>());
        Physics2D.IgnoreCollision(gameObject.GetComponent<BoxCollider2D>(), groundTileMap.GetComponent<TilemapCollider2D>());
        Physics2D.IgnoreCollision(gameObject.GetComponent<BoxCollider2D>(), colTileMap.GetComponent<CompositeCollider2D>());
        Physics2D.IgnoreCollision(gameObject.GetComponent<BoxCollider2D>(), colTileMap.GetComponent<TilemapCollider2D>());*/
    }

    public override void Heuristic(in ActionBuffers actionsOut)
    {

        //int dirX = 0, dirY = 0;
        var discreteAc = actionsOut.DiscreteActions;
        
        float movementFloatHorizontal = Mathf.Clamp(Input.GetAxis("Horizontal"), -1f, 1f);
        float movementFloatVertical = Mathf.Clamp(Input.GetAxis("Vertical"), -1f, 1f);
        //Debug.Log(movementFloatHorizontal + " " + movementFloatVertical);

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

        /*float movementFloatHorizontal = Input.GetAxis("Horizontal");
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
        }*/

        //Vector2 normVector = new Vector2(dirX, dirY);

        /*if (!normVector.Equals(Vector2.zero))
        {
            Move(normVector);
        }*/
    }

    public override void OnEpisodeBegin()
    {
        //waypointQueue.Clear();
        this.transform.localPosition = startPos;
        //timer = 0;
    }

    public override void CollectObservations(VectorSensor sensor)
    {
        /*float origValueX = groundTileMap.LocalToCell(this.transform.localPosition).x;
        float origValueY = groundTileMap.LocalToCell(this.transform.localPosition).y;

        float targetValueX = groundTileMap.LocalToCell(target).x;
        float targetValueY = groundTileMap.LocalToCell(target).y;


        // normalize all vectors
        float agentNormalizedValueX = (origValueX - minimX) / (maximX - minimX);
        float agentNormalizedValueY = (origValueY - minimY) / (maximY - minimY);

        float targetNormalizedValueX = (targetValueX - minimX) / (maximX - minimX);
        float targetNormalizedValueY = (targetValueY - minimY) / (maximY - minimY);

        float distanceXNormalized = Mathf.Abs(origValueX - targetValueX);
        float distanceYNormalized = Mathf.Abs(origValueY - targetValueY);*/

        /*float distanceNormalizedX = (distanceX - minDistanceX) / (maxDistanceX - minDistanceX);
        float distanceNormalizedY = (distanceY - minDistanceY) / (maxDistanceY - minDistanceY);
        float observationAgentX = (this.transform.localPosition.x - minimX) / (maximX - minimX);
        float observationAgentY = (this.transform.localPosition.y - minimY) / (maximY - minimY);
        
        float observationEndTileX = (target.x - minimX) / (maximX - minimX);
        float observationEndTileY = (target.y - minimY) / (maximY - minimY);
        
        float observationDistanceX = Mathf.Abs(target.x - this.transform.localPosition.x) / (maximX - minimX);
        float observationDistanceY = Mathf.Abs(target.y - this.transform.localPosition.y) / (maximY - minimY);*/
        //float dist = Vector3.Distance(transform.localPosition, target)/100f;

        //float observationDistanceNormalized = (distanceX + distanceY)/100;

        sensor.AddObservation(this.transform.localPosition);
        sensor.AddObservation(Vector3.Distance(this.transform.localPosition, (target + new Vector3(0.5f,0.5f,0))));
        //sensor.AddObservation(target);
        //sensor.AddObservation((target-this.transform.localPosition).normalized);
        //sensor.AddObservation(agentNormalizedValueX);
        //sensor.AddObservation(agentNormalizedValueY);
        //sensor.AddObservation(targetNormalizedValueX);
        //sensor.AddObservation(targetNormalizedValueY);
        //sensor.AddObservation(observationDistanceNormalized);
    }

    public override void WriteDiscreteActionMask(IDiscreteActionMask actionMask)
    {
        // Mask the necessary actions if selected by the user.
        if (maskActions)
        {
            // Prevents the agent from picking an action that would make it collide with a wall
            //var positionX = (int)transform.localPosition.x;
            //var positionZ = (int)transform.localPosition.z;
            //var maxPosition = (int)m_ResetParams.GetWithDefault("gridSize", 5f) - 1;
            Vector3Int gridPos = colTileMap.LocalToCell(this.transform.localPosition);

            if (colTileMap.HasTile(gridPos + new Vector3Int(-1,0,0)))
            {
                actionMask.SetActionEnabled(0, 1, false);
            }

            if (colTileMap.HasTile(gridPos + new Vector3Int(1, 0, 0)))
            {
                actionMask.SetActionEnabled(0, 2, false);
            }

            if (colTileMap.HasTile(gridPos + new Vector3Int(0, -1, 0)))
            {
                actionMask.SetActionEnabled(0, 3, false);
            }

            if (colTileMap.HasTile(gridPos + new Vector3Int(0, 1, 0)))
            {
                actionMask.SetActionEnabled(0, 4, false);
            }
        }
    }

    private void FixedUpdate()
    {
        WaitTimeInference();
    }
    void WaitTimeInference()
    {
        if (Academy.Instance.IsCommunicatorOn)
        {
            RequestDecision();
        }
        else
        {
            if (m_TimeSinceDecision >= timeBetweenDecisionsAtInference)
            {
                m_TimeSinceDecision = 0f;
                RequestDecision();
            }
            else
            {
                m_TimeSinceDecision += Time.fixedDeltaTime;
            }
        }
    }

    public override void OnActionReceived(ActionBuffers actions)
    {
        if (MaxStep > 0)
        {
            AddReward((-1f) / MaxStep);
        }

        //int dirX = 0, dirY = 0;
        int movement = actions.DiscreteActions[0];

        var targetPos = transform.localPosition;

        switch(movement)
        {
            case 0:
                // do nothing
                break;
            case 1:
                targetPos = transform.localPosition + new Vector3(-1f,0f,0f);
                //dirX = -1;
                break;
            case 2:
                targetPos = transform.localPosition + new Vector3(1f, 0f, 0f);
                //dirX = 1;
                break;
            case 3:
                targetPos = transform.localPosition + new Vector3(0f, -1f, 0f);
                //dirY = -1;
                break;
            case 4:
                targetPos = transform.localPosition + new Vector3(0f, 1f, 0f);
                //dirY = 1;
                break;
            default:
                throw new ArgumentException("No action value");
        }

        Collider2D[] hit = Physics2D.OverlapBoxAll(targetPos, new Vector3(0.3f, 0.3f, 0f), 0f);
        if (hit.Where(col => col.gameObject.CompareTag("Col")).ToArray().Length == 0)
        {
            transform.localPosition = targetPos;

            if (hit.Where(col => col.gameObject.CompareTag("End")).ToArray().Length == 1)
            {
                Debug.Log("hit end");
                AddReward(1f);
                EndEpisode();
            }
        }
        else Debug.Log("hit wall");

        /*if (movement == 1)
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
        }*/
        //var normVector = new Vector3(dirX, dirY,0f);
        /*float origValueX = groundTileMap.LocalToCell(this.transform.localPosition).x;
        float origValueY = groundTileMap.LocalToCell(this.transform.localPosition).y;

        float targetValueX = groundTileMap.LocalToCell(target).x;
        float targetValueY = groundTileMap.LocalToCell(target).y;*/

        /*float distanceX = Mathf.Abs(origValueX - targetValueX);
        float distanceY = Mathf.Abs(origValueY - targetValueY);
        float tempFactor = (distanceX + distanceY) * (-1f);
        float palkkio = (tempFactor / 10000f);*/
        //float palkkio = Vector2.Distance(target,this.transform.localPosition);
        //AddReward(palkkio);
        /*if (waypointQueue.Count > 10)
        {
            AddReward((10+palkkio-Vector2.Distance(waypointQueue.Peek(), this.transform.localPosition))/(-5000f));
            waypointQueue.Dequeue();
            waypointQueue.Enqueue(this.transform.localPosition);
        }
        else
        {
            waypointQueue.Enqueue(this.transform.localPosition);
        }*/

        //Debug.Log((10 + palkkio - Vector2.Distance(waypointQueue.Peek(), this.transform.localPosition)) / (-1000f));


        //float distanceNormalized = Vector3.Distance(this.transform.localPosition,target) / Mathf.Pow((Mathf.Pow((origValueX-targetValueX),2f) + Mathf.Pow((origValueY-targetValueY),2f)),0.5f);

        //Move(normVector);
        /*if (!Move(normVector))
        {
            AddReward(-1.0f);
            EndEpisode();
        }*/

        /*if (OnEndTile())
        {
            Debug.Log("tultiin maaliin");
            AddReward(10.0f);
            EndEpisode();
        }*/
    }

    /*private void OnCollisionEnter2D(Collision2D collision)
    {
        if (collision.gameObject.CompareTag("End"))
        {
            Debug.Log("tultiin maaliin");
            AddReward(1.0f);
            EndEpisode();
        }
    }*/

    private bool OnEndTile()
    {
        Vector3Int gridPos = endTileMap.LocalToCell(this.transform.localPosition);
        if (endTileMap.HasTile(gridPos))
        {
            return true;
        }
        else return false;
    }

    private bool Move(Vector3 direction)
    {
        // Use delay to slow down movement.
        //StartCoroutine(DelayedMovement(0.1f));
        if (CanMove(direction))
        {
            transform.localPosition += (Vector3)direction;
            return true;
        }
        else return false;
    }

    private IEnumerator DelayedMovement(float delay)
    {
        while(true)
        {
            yield return new WaitForSeconds(delay);
            break;
        }
        yield return null;
    }

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

    private void CreateTileText()
    {
        Canvas canv = GameObject.FindGameObjectWithTag("Canv").GetComponent<Canvas>();

        for (int i = minimX; i <= maximX; i++)
        {
            for (int j = minimY; j <= maximY; j++)
            {
                if (groundTileMap.GetTile(new Vector3Int(i, j)) != null)
                {
                    Canvas tempCanv = Instantiate(canv);
                    tempCanv.transform.SetParent(groundTileMap.gameObject.transform);
                    tempCanv.transform.localPosition = new Vector3(i + 0.5f, j + 0.5f);
                    TextMeshProUGUI neighbourText = tempCanv.GetComponentInChildren<TextMeshProUGUI>();
                    neighbourText.SetText(i + ", " + j);
                }
            }
        }
    }

}
