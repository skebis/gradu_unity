using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

public class MovingBlocksAreaDiff : MonoBehaviour
{
    [SerializeField] private Tilemap groundTileMap;
    [SerializeField] private Tilemap colTileMap;

    [SerializeField] private TileBase colTile;
    [SerializeField] private TileBase freeTile;

    public readonly static Vector3Int[] ignoredSpawnPositions = new Vector3Int[]
    { new Vector3Int(-19,3), new Vector3Int(-19,2), new Vector3Int(-19,1),
      new Vector3Int(-2,11), new Vector3Int(-1,11), new Vector3Int(0,11),
      new Vector3Int(1,11), new Vector3Int(2,11), new Vector3Int(3,11),
      new Vector3Int(4,11), new Vector3Int(5,11), new Vector3Int(6,11), new Vector3Int(7,11)
    };

    private Vector3Int[] gatePosition = new Vector3Int[]
    {
        new Vector3Int(-19,3), new Vector3Int(-19,2), new Vector3Int(-19,1)
    };

    private Vector3Int[] movingTargetPosition = new Vector3Int[]
    {
        new Vector3Int(-2,11), new Vector3Int(-1,11), new Vector3Int(0,11),
        new Vector3Int(1,11), new Vector3Int(2,11), new Vector3Int(3,11),
        new Vector3Int(4,11), new Vector3Int(5,11), new Vector3Int(6,11), new Vector3Int(7,11)
    };

    // Start is called before the first frame update
    void Start()
    {
        StartCoroutine(MoveBlock(gatePosition, 2f));
        StartCoroutine(MoveTarget(movingTargetPosition, 1f));
    }

    /// <summary>
    /// Replaces a collision block with a walkable one and vice versa.
    /// </summary>
    /// <param name="tilePositions">Positions of the replaced blocks.</param>
    /// <param name="seconds">Replacement delay.</param>
    /// <returns></returns>
    IEnumerator MoveBlock(Vector3Int[] tilePositions, float seconds)
    {
        while (true)
        {
            yield return new WaitForSeconds(seconds);
            foreach (Vector3Int tilePos in tilePositions)
            {
                groundTileMap.SetTile(tilePos, null);
                colTileMap.SetTile(tilePos, colTile);
            }
            
            yield return new WaitForSeconds(seconds);
            foreach (Vector3Int tilePos in tilePositions)
            {
                groundTileMap.SetTile(tilePos, freeTile);
                colTileMap.SetTile(tilePos, null);
            }
            
        }
    }

    IEnumerator MoveTarget(Vector3Int[] tilePositions, float seconds)
    {
        while (true)
        {
            for (int i = 0; i < tilePositions.Length; i++)
            {
                yield return new WaitForSeconds(seconds);
                if (i != 0)
                {
                    Vector3Int tempOffset = new Vector3Int(-1, 0);
                    groundTileMap.SetTile(tilePositions[i] + tempOffset, freeTile);
                    colTileMap.SetTile(tilePositions[i] + tempOffset, null);
                }
                groundTileMap.SetTile(tilePositions[i], null);
                colTileMap.SetTile(tilePositions[i], colTile);
            }
            for (int i = tilePositions.Length-1; i >= 0; i--)
            {
                yield return new WaitForSeconds(seconds);
                if (i != tilePositions.Length-1)
                {
                    Vector3Int tempOffset = new Vector3Int(1, 0);
                    groundTileMap.SetTile(tilePositions[i] + tempOffset, freeTile);
                    colTileMap.SetTile(tilePositions[i] + tempOffset, null);

                }
                groundTileMap.SetTile(tilePositions[i], null);
                colTileMap.SetTile(tilePositions[i], colTile);
            }
        }
    }
}
