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
            if (currentPos.x <= 0 || currentPos + Vector2Int.left == lastAddedNode || currentPos + Vector2Int.left == startPos)
                possible.Remove(Vector2Int.left);
            if (currentPos.x >= width - 1 || currentPos + Vector2Int.right == lastAddedNode || currentPos + Vector2Int.right == startPos)
                possible.Remove(Vector2Int.right);
            if (currentPos.y <= 0 || currentPos + Vector2Int.down == lastAddedNode || currentPos + Vector2Int.down == startPos)
                possible.Remove(Vector2Int.down);
            if (currentPos.y >= height - 1 || currentPos + Vector2Int.up == lastAddedNode || currentPos + Vector2Int.down == startPos)
                possible.Remove(Vector2Int.up);

            //Step 2 get a random direction from the remaining directions, we'll use to update the grid position
            if (possible.Count <= 0)
            {
                lastAddedNode = currentPos;
                return (path, dir);
            }
            int random = Random.Range(0, possible.Count);
            Vector2Int newPos = currentPos + possible[random];

            //step 3: This new Pos might aleadry be in the path. still add it, but skip over to the next available space if there is one
            //and later we'll make it a multidirection tile.
            int overlappingTiles = 0;
            while (path.Contains(newPos))
            {
                //escape if theres no space to continue in this direction
                if (i + overlappingTiles + 1 >= pathLength || OutOfBoundsCheck(newPos + possible[random], width, height) || newPos + possible[random] == startPos )
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
                    {
                        lastAddedNode = currentPos;
                        return (path,dir);
                    }

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

        lastAddedNode = currentPos;
        //return dir;        
        return (path, dir);
    }

    private static bool OutOfBoundsCheck(Vector2Int pos, int w, int h)
    {
       return (pos.x < 0 || pos.y < 0 || pos.x >= w || pos.y >= h);
    }
    public static List<Main.Pipe> GeneratePipesFromPath(List<Vector2Int> path, Main gameLogic, Vector2Int startPos)
    {
        List<Main.Pipe> pipes = new List<Main.Pipe>(path.Count);
        List<PipeSO> pipetypes = gameLogic.pipe_so_list_;

        Vector2Int lastPipe = startPos;
        
        Grid<Main.GridCell> grid = gameLogic.GetMainGrid();
        Main.Pipe startPipe = grid.GetGridObj(startPos.x, startPos.y).GetCellItem();

        //grid.GetGridObj(startPos.x, startPos.y).GetCellItem().SetRotType(GlobalEnums.RotType.Rot180);
        if (path[0] == Vector2Int.up)
            startPipe.SetRotType(GlobalEnums.RotType.Rot180);
        else if (path[0] == Vector2Int.right)
            startPipe.SetRotType(GlobalEnums.RotType.Rot270);
        else if (path[0] == Vector2Int.down)
            startPipe.SetRotType(GlobalEnums.RotType.Rot0);
        else if (path[0] == Vector2Int.left)
            startPipe.SetRotType(GlobalEnums.RotType.Rot90);


        for (int i = 0; i < path.Count; i++)
        {
            int newX = path[i].x + lastPipe.x;
            int newY = path[i].y + lastPipe.y;
            
            Main.Pipe nextPipe = grid.GetGridObj(newX, newY)?.GetCellItem();


            if(i == path.Count-1)
            {
                if(path[i] == Vector2Int.up)
                    nextPipe.SetRotType(GlobalEnums.RotType.Rot0);
                else if(path[i] == Vector2Int.right)
                    nextPipe.SetRotType(GlobalEnums.RotType.Rot90);
                else if (path[i] == Vector2Int.down)
                    nextPipe.SetRotType(GlobalEnums.RotType.Rot180);
                else if (path[i] == Vector2Int.left)
                    nextPipe.SetRotType(GlobalEnums.RotType.Rot270);
                break;
            }

            if(nextPipe == null)
                continue;

            //check for other pipes with the same coordinates 
            if(pipes.Find(pipe => pipe.Xcoord == newX && pipe.Ycoord == newY) != null )
            {
                if(path[i] == path[i+1]) //straight line through an already existing pipe
                    nextPipe.SetPipeSo(pipetypes[3]);
                else
                    nextPipe.SetPipeSo(pipetypes[2]); //turns while passing into another pipe
                
            }
            else
            {
                if (path[i] == path[i + 1]) //straight line 
                    nextPipe.SetPipeSo(pipetypes[0]);
                else
                    nextPipe.SetPipeSo(pipetypes[1]); //turning pipe

                pipes.Add(nextPipe);
            }
            
            gameLogic.Invoke_ChangedPipeSO(nextPipe);
            lastPipe = nextPipe.GetGridCoord();
        }

        return pipes;
    }
}

