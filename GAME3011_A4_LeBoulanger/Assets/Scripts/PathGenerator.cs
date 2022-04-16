using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PathGenerator : MonoBehaviour
{
    private void Start()
    {
        //Vector2Int endSpot;
        //List<Vector2Int> a = GeneratePath(Vector2Int.zero ,15,15, 35, out endSpot);
        //    if(a.Count >=  9 )
        //        print("a was completed");
        //    else print("a was only " + a.Count + " spaces");
        //List<Vector2Int> b = GeneratePath(Vector2Int.zero ,15,15, 20, out endSpot);
        //if (b.Count >= 9)
        //    print("b was completed");
        //else print("b was only " + b.Count + " spaces");
        //List<Vector2Int> c = GeneratePath(Vector2Int.zero ,15,15, 25, out endSpot);
        //if (c.Count >= 9)
        //    print("c was completed");
        //else print("c was only " + c.Count + " spaces");
        //List<Vector2Int> d = GeneratePath(Vector2Int.zero ,15,15, 30, out endSpot);
        //if (d.Count >= 9)
        //    print("d was completed");
        //else print("d was only " + d.Count + " spaces");
            
        //foreach(Vector2Int i in a)
        //        print(i);
    }

    //returns a list of directions for the path to go in. can overlap(which should cause a branching pipe)
    //might return early if it failed to find a path
    public static (List<Vector2Int>, List<Vector2Int>) GeneratePath(Vector2Int startPos, int width, int height, int pathLength, out Vector2Int lastAddedNode)
    {
        List<Vector2Int> path = new List<Vector2Int>(pathLength);
        List<Vector2Int> dir = new List<Vector2Int>(pathLength);

        Vector2Int currentPos = startPos;
        path.Add(currentPos);

        lastAddedNode = Vector2Int.zero;

        //get all path nodes
        for (int i = 1; i < pathLength; i++)
        {
            List<Vector2Int> possible = new List<Vector2Int>() { Vector2Int.up, Vector2Int.down, Vector2Int.left, Vector2Int.right };
            
            //step one: removing the directions that it cant go in
            if (currentPos.x <= 0 || currentPos + Vector2Int.left == lastAddedNode)
                possible.Remove(Vector2Int.left);
            if (currentPos.x >= width - 1 || currentPos + Vector2Int.right == lastAddedNode)
                possible.Remove(Vector2Int.right);
            if (currentPos.y <= 0 || currentPos + Vector2Int.down == lastAddedNode)
                possible.Remove(Vector2Int.down);
            if (currentPos.y >= height - 1 || currentPos + Vector2Int.up == lastAddedNode)
                possible.Remove(Vector2Int.up);

            //Step 2 get a random direction from the remaining directions, we'll use to update the grid position
            if (possible.Count <= 0)
                return (path, dir);
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
                    { 
                        overlappingTiles--;
                        path.RemoveAt(path.Count-1);
                        dir.RemoveAt(dir.Count-1);
                    }
                    possible.Remove(possible[random]);
                    if (possible.Count <= 0)
                        return (path,dir);

                    //pick a new direction and we'll see how things got this time
                    random = Random.Range(0, possible.Count);
                    newPos = currentPos + possible[random];
                    continue;
                }

                overlappingTiles++;
                path.Add(newPos);
                dir.Add(possible[random]);
                newPos += possible[random];
            }
            i += overlappingTiles;

            path.Add(newPos);
            dir.Add(possible[random]);
            currentPos = newPos;
            lastAddedNode = path[i-1];
        }
        //return dir;        
        return (path, dir);
    }

    private static bool OutOfBoundsCheck(Vector2Int pos, int w, int h)
    {
       return (pos.x < 0 || pos.y < 0 || pos.x >= w || pos.y >= h);
    }
}
