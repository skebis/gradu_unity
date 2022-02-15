using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

public class MovingBlocksScript : MonoBehaviour
{

    [SerializeField] private Tilemap groundTileMap;
    [SerializeField] private Tilemap colTileMap;
    [SerializeField] private Tilemap startTileMap;
    [SerializeField] private Tilemap endTileMap;

    [SerializeField] private TileBase colTile;
    [SerializeField] private TileBase freeTile;

    private int[] movingFlag = new int[3];

    private Vector3Int[] tilePos = new Vector3Int[3];

    // Start is called before the first frame update
    void Start()
    {
        tilePos[0] = new Vector3Int(-7, 3);
        tilePos[1] = new Vector3Int(6, 0);
        tilePos[2] = new Vector3Int(1, -3);

        movingFlag[0] = 1;
        movingFlag[1] = 1;
        movingFlag[2] = 1;

        StartCoroutine(MoveBlock(tilePos[0], 0));
        StartCoroutine(MoveBlock(tilePos[1], 1));
        StartCoroutine(MoveBlock(tilePos[2], 2));
    }

    IEnumerator MoveBlock(Vector3Int tilePos, int index)
    {
        while (true)
        {
            yield return new WaitForSeconds(2f);
            groundTileMap.SetTile(tilePos, null);
            colTileMap.SetTile(tilePos, null);
            groundTileMap.SetTile(tilePos, freeTile);
            movingFlag[index] *= -1;
            tilePos.y += movingFlag[index];
            colTileMap.SetTile(tilePos, colTile);
        }
    }
}
