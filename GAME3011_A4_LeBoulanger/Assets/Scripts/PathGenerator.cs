using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PathGenerator : MonoBehaviour
{
    //returns a list of directions for the path to go in. can overlap(which should cause a branching pipe)
    //might return early if it failed to find a path
    public List<Vector2Int> GeneratePath(Vector2Int startPos, int width, int height, int pathLength)
    {

        List<Vector2Int> path = new List<Vector2Int>(pathLength);

        Vector2Int currentPos = startPos;
        path.Add(currentPos);

        List<Vector2Int> dirs = new List<Vector2Int>() { Vector2Int.up, Vector2Int.down, Vector2Int.left, Vector2Int.right };
        Vector2Int lastAddedNode = Vector2Int.zero;


        //get all path nodes

        for (int i = 1; i < pathLength; i++)
        {
            List<Vector2Int> possible = dirs;

            //step one: removing the directions that it cant go in (there will always be at least one direction to go in)

            if (currentPos.x <= 0 || currentPos + Vector2Int.left == lastAddedNode)
                possible.Remove(Vector2Int.left);
            if (currentPos.x >= width - 1 || currentPos + Vector2Int.right == lastAddedNode)
                possible.Remove(Vector2Int.right);
            if (currentPos.y <= 0 || currentPos + Vector2Int.down == lastAddedNode)
                possible.Remove(Vector2Int.down);
            if (currentPos.y >= height - 1 || currentPos + Vector2Int.up == lastAddedNode)
                possible.Remove(Vector2Int.up);

            //Step 2 get a random direction from the remaining directions, we'll use to update the grid position

            int random = Random.Range(0, possible.Count);
            Vector2Int newPos = currentPos + possible[random];

            //step 3: This new Pos might aleadry be in the path. still add it, but skip over to the next available space if there is one
            //and later we'll make it a multidirection tile.
            int overlappingTiles = 0;
            while (path.Contains(newPos))
            {
                //escape if theres no space to continue in this direction
                if (i + overlappingTiles + 1 >= pathLength || OutOfBoundsCheck(newPos + possible[random], width, height))
                {
                    //start over, or return early if no other paths are available
                    while (overlappingTiles > 0)
                        path.RemoveAt(--i);
                    possible.Remove(possible[random]);
                    if (possible.Count <= 0)
                        return path;

                    //pick a new direction and we'll see how things got this time
                    random = Random.Range(0, possible.Count);
                    newPos = currentPos + possible[random];
                    continue;
                }

                overlappingTiles++;
                path.Add(newPos);
                newPos += possible[random];
            }
            i += overlappingTiles;

            path.Add(newPos);
            currentPos = newPos;
            lastAddedNode = path[i];
        }

        return path;
    }

    bool OutOfBoundsCheck(Vector2Int pos, int w, int h)
    {
       return (pos.x < 0 || pos.y < 0 || pos.x >= w || pos.y >= h);

    }

}
