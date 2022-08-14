using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

public class MovingBlocksArea2 : MonoBehaviour
{

    [SerializeField] private Tilemap groundTileMap;
    [SerializeField] private Tilemap colTileMap;
    [SerializeField] private Tilemap startTileMap;
    [SerializeField] private Tilemap endTileMap;

    [SerializeField] private TileBase colTile;
    [SerializeField] private TileBase freeTile;

    private Vector3Int[] tilePos = new Vector3Int[3];

    // Start is called before the first frame update
    void Start()
    {
        tilePos[0] = new Vector3Int(27, -2, 0);

        StartCoroutine(MoveBlock(tilePos[0], 0, 2f));
    }

    IEnumerator MoveBlock(Vector3Int tilePos, int index, float seconds)
    {
        while (true)
        {
            yield return new WaitForSeconds(seconds);
            groundTileMap.SetTile(tilePos, null);
            colTileMap.SetTile(tilePos, colTile);
            yield return new WaitForSeconds(seconds);
            groundTileMap.SetTile(tilePos, freeTile);
            colTileMap.SetTile(tilePos, null);
        }
    }
}
