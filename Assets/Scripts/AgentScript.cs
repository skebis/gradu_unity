using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.MLAgents;
using Unity.MLAgents.Sensors;
using Unity.MLAgents.Actuators;
using UnityEngine.Tilemaps;
using TMPro;

public class AgentScript : Agent
{
    [SerializeField] private bool debuggable;

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
            Camera.main.transform.position = new Vector3(30f, -1.5f, -10f);
            Camera.main.orthographicSize = 8.5f;
        }
        else if (areaObject.name == "Area3")
        {
            Camera.main.transform.position = new Vector3(0f, 9f, -10f);
            Camera.main.orthographicSize = 28f;
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
        this.transform.localPosition = startPos;
        //timer = 0;
    }

    public override void CollectObservations(VectorSensor sensor)
    {
        float origValueX = groundTileMap.WorldToCell(this.transform.position).x;
        float origValueY = groundTileMap.WorldToCell(this.transform.position).y;

        float targetValueX = groundTileMap.WorldToCell(target).x;
        float targetValueY = groundTileMap.WorldToCell(target).y;

        // normalize all vectors
        float agentNormalizedValueX = (origValueX - minimX) / (maximX - minimX);
        float agentNormalizedValueY = (origValueY - minimY) / (maximY - minimY);

        float targetNormalizedValueX = (targetValueX - minimX) / (maximX - minimX);
        float targetNormalizedValueY = (targetValueY - minimY) / (maximY - minimY);

        float distanceX = Mathf.Abs(origValueX - targetValueX);
        float distanceY = Mathf.Abs(origValueY - targetValueY);

        /*float distanceNormalizedX = (distanceX - minDistanceX) / (maxDistanceX - minDistanceX);
        float distanceNormalizedY = (distanceY - minDistanceY) / (maxDistanceY - minDistanceY);
        float observationAgentX = (this.transform.localPosition.x - minimX) / (maximX - minimX);
        float observationAgentY = (this.transform.localPosition.y - minimY) / (maximY - minimY);
        
        float observationEndTileX = (target.x - minimX) / (maximX - minimX);
        float observationEndTileY = (target.y - minimY) / (maximY - minimY);
        
        float observationDistanceX = Mathf.Abs(target.x - this.transform.localPosition.x) / (maximX - minimX);
        float observationDistanceY = Mathf.Abs(target.y - this.transform.localPosition.y) / (maximY - minimY);*/

        float observationDistance = (distanceX + distanceY);

        sensor.AddObservation(agentNormalizedValueX);
        sensor.AddObservation(agentNormalizedValueY);
        sensor.AddObservation(targetNormalizedValueX);
        sensor.AddObservation(targetNormalizedValueY);
        sensor.AddObservation(observationDistance);
    }

    public override void OnActionReceived(ActionBuffers actions)
    {
        int dirX = 0, dirY = 0;
        int movement = actions.DiscreteActions[0];

        /*switch (movement)
        {
            case 0:
                dirX = 0;
                dirY = 0;
                break;
            case 1:
                dirX = -1;
                dirY = 0;
                break;
            case 2:
                dirX = 1;
                dirY = 0;
                break;
            case 3:
                dirY = -1;
                dirX = 0;
                break;
            case 4:
                dirY = 1;
                dirX = 0;
                break;
        }*/

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

        var normVector = new Vector2Int(dirX, dirY);

        float origValueX = groundTileMap.WorldToCell(this.transform.position).x;
        float origValueY = groundTileMap.WorldToCell(this.transform.position).y;

        float targetValueX = groundTileMap.WorldToCell(target).x;
        float targetValueY = groundTileMap.WorldToCell(target).y;

        float distanceX = Mathf.Abs(origValueX - targetValueX);
        float distanceY = Mathf.Abs(origValueY - targetValueY);

        if (MaxStep > 0)
        {
            float tempFactor = (distanceX + distanceY) * (-1);
            AddReward(tempFactor/MaxStep);
            Debug.Log((distanceX + distanceY));
        }
        if (!Move(normVector))
        {
            //AddReward(-1f);
        }

        /*if (OnEndTile())
        {
            Debug.Log("tultiin maaliin");
            AddReward(10.0f);
            EndEpisode();
        }*/
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (collision.gameObject.CompareTag("End"))
        {
            Debug.Log("tultiin maaliin");
            AddReward(10.0f);
            EndEpisode();
        }
    }

    private bool OnEndTile()
    {
        Vector3Int gridPos = endTileMap.WorldToCell(this.transform.localPosition);
        if (endTileMap.HasTile(gridPos))
        {
            return true;
        }
        else return false;
    }

    private bool Move(Vector2Int direction)
    {
        // Use delay to slow down movement.
        //StartCoroutine(DelayedMovement(0.1f));
        if (CanMove(direction))
        {
            transform.localPosition += (Vector3Int)direction;
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

    private bool CanMove(Vector2 direction)
    {
        Vector3Int gridPosition = groundTileMap.WorldToCell(this.transform.localPosition + (Vector3)direction);

        if (!groundTileMap.HasTile(gridPosition) || colTileMap.HasTile(gridPosition))
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
