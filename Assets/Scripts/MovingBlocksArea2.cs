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

    private Vector3Int[] tilePos = new Vector3Int[1];

    // Start is called before the first frame update
    void Start()
    {
        tilePos[0] = new Vector3Int(-7, 2, 0);

        StartCoroutine(MoveBlock(tilePos[0], 2f));
    }

    /// <summary>
    /// Replaces a collision block with a walkable one and vice versa.
    /// </summary>
    /// <param name="tilePos">Position of the replaced block.</param>
    /// <param name="seconds">Replacement delay.</param>
    /// <returns></returns>
    IEnumerator MoveBlock(Vector3Int tilePos, float seconds)
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
