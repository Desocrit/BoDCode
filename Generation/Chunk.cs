using UnityEngine;
using System.Collections.Generic;

public class Chunk: MonoBehaviour
{
    public enum Zone
    {
        Grassland,
        Graveyard,
        Dungeon,
        Floating,
        Hell
    }

    public enum Direction
    {
        FORWARD = 0,
        RIGHT = 1,
        BACK = 2,
        LEFT = 3,
        UP = 4,
        DOWN = 5
    }

    public int difficulty = 1;
    public ChunkType type;
    public Zone zone;

    public List<EdgeType> topEdges;
    public List<EdgeType> leftEdges;
    public List<EdgeType> rightEdges;
    public List<EdgeType> bottomEdges;

    // TODO: Full support for this.

    public List<EdgeType> upEdges;
    public List<EdgeType> downEdges;

    public List<EdgeDescription> Edges
    {
        get
        {
            List<EdgeDescription> edges = new List<EdgeDescription>();
            for(int i = 0; i < 4; i++)
                for(int j = 0; j < GetEdges(i).Count; j++)
                    edges.Add(GetEdgeDescription(j, (Direction)i));
            return edges;
        }
    }

    public class EdgeDescription
    {
        public Vector3 position;
        public Direction dir;
        public EdgeType type;

        public Vector3 ExitVector
        {
            get
            {
                return GetDirectionVector(dir); 
            }
        }

        public int RotationsNeeded(EdgeDescription exit)
        {
            return RotationsNeeded(exit.dir);
        }

        public int RotationsNeeded(Direction targetDirection)
        {
            return (targetDirection - dir + 6) % 4;
        }

        public EdgeDescription(Vector3 pos, Direction direction,
                                EdgeType type)
        {
            position = pos;
            dir = direction;
            this.type = type;
        }
    }

    public List<EdgeType> GetEdges(int dir)
    {
        return GetEdges((Direction)dir);
    }

    public List<EdgeType> GetEdges(Direction dir)
    {
        switch(dir)
        {
            case Direction.FORWARD:
                return topEdges;
            case Direction.LEFT:
                return leftEdges;
            case Direction.RIGHT:
                return rightEdges;
            case Direction.BACK:
            default:
                return bottomEdges;
        }
    }

    public EdgeDescription GetEdgeDescription(int pos, Direction dir)
    {
        return GetEdgeDescription(pos, dir, GetEdges(dir)[pos]);
    }

    public EdgeDescription GetEdgeDescription(int pos, Direction dir, 
                                              EdgeType type)
    {
        switch(dir)
        {
            case Direction.FORWARD:
                return new EdgeDescription(new Vector3(pos, 0, leftEdges.Count
                    - 1), dir, type);
            case Direction.RIGHT:
                return new EdgeDescription(new Vector3(topEdges.Count - 1,
                                                   0, pos), dir, type);
            case Direction.LEFT:
                return new EdgeDescription(new Vector3(0, 0, pos), dir, type);
            case Direction.BACK:
                return new EdgeDescription(new Vector3(pos, 0, 0), dir, type);
            case Direction.UP:
                return new EdgeDescription(new Vector3(0, 1, 0), dir, type);
            case Direction.DOWN:
                return new EdgeDescription(new Vector3(0, -1, 0), dir, type);
            default:
                return null;
        }
    }

    public Chunk Rotated(int rotations)
    {
        Chunk rotated = clone();
        for(int i = 0; i < rotations; i++)
        {
            var temp = rotated.topEdges;
            rotated.topEdges = rotated.leftEdges;
            rotated.leftEdges = rotated.bottomEdges;
            rotated.leftEdges.Reverse();
            rotated.bottomEdges = rotated.rightEdges;
            rotated.rightEdges = temp;
            rotated.rightEdges.Reverse();
        }
        return rotated;
    }

    public Chunk clone()
    {
        Chunk clone = (Chunk)this.MemberwiseClone();
        clone.topEdges = new List<EdgeType>(topEdges);
        clone.leftEdges = new List<EdgeType>(leftEdges);
        clone.rightEdges = new List<EdgeType>(rightEdges);
        clone.bottomEdges = new List<EdgeType>(bottomEdges);
        clone.upEdges = new List<EdgeType>(upEdges);
        clone.downEdges = new List<EdgeType>(downEdges);
        return clone;
    }

    public EdgeDescription RotateEdge(EdgeDescription edge, int rotations)
    {
        // Update position;
        Vector3 newPos = edge.position;
        int w = topEdges.Count - 1;
        int h = leftEdges.Count - 1;
        for(int i = 0; i < rotations; i++)
            newPos = new Vector3(newPos.z, 0, (i % 2 == 0 ? w : h) - newPos.x);

        // Update direction.
        Direction newDir = (Chunk.Direction)(((int)edge.dir + rotations) % 4);
        var r = new EdgeDescription(newPos, newDir, edge.type);

        return r;
    }

    public static Vector3 GetDirectionVector(Direction dir)
    {
        switch(dir)
        {
            case Direction.FORWARD:
                return Vector3.forward;
            case Direction.LEFT:
                return Vector3.left;
            case Direction.BACK:
                return Vector3.back;
            case Direction.RIGHT:
                return Vector3.right;
            case Direction.UP:
                return Vector3.up;
            case Direction.DOWN:
            default:
                return Vector3.down;
        }
    }
}
