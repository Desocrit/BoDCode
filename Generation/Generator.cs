using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;

public class Generator : MonoBehaviour
{
    [Serializable]
    public struct ChunkCount
    {
        public ChunkType type;
        public int min;
        public int max;
    }

    public Chunk.Zone zone;
    public ChunkCount[] chunkTypes;
    public int exitDifficulty;

    private Vector3? exitPosition; // Is it a Vector3? IS IT?!?


    private List<InstantiateCall> instantiateCalls;
    private Dictionary<Vector3, Cell> placedCells;

    private List<ChunkType> remainingChunks;
    private List<ChunkType> failedChunks;

    private Dictionary<ChunkType, GameObject[]> chunkCache;

    private List<Cell> openCells;
    private List<Cell> deadEnds;

    private Vector3 position;
    private System.Random rng;

    private GameObject terrain;
    private static int MAXIMUM_ATTEMPTS = 5;

    private const EdgeType CLOSED = EdgeType.Closed;

    void Awake()
    {
        for(int i = 0; !generateTerrain(); i++)
        {
            if(i > MAXIMUM_ATTEMPTS)
                throw new InvalidOperationException("Generation failed.");
            clearTerrain();
        }

    }

    void LateUpdate()
    {   // After the grid has generated. I hate this addon.
        if(terrain != null)
        {
            terrain.BroadcastMessage("AttachGrid");
            Destroy(this);
        }
    }

    public void clearTerrain()
    {
        foreach(ChunkType tag in
                 Enum.GetValues(typeof(ChunkType)))
            foreach(GameObject chunk in GameObject.
                    FindGameObjectsWithTag(tag.ToString()))
                GameObject.Destroy(chunk);
    }

    public bool generateTerrain()
    {
        // Initialise the dictionaries.
        initialise();

        // Place start
        GameObject[] startChunks = GetChunks(ChunkType.Entrance);
        PlaceChunk(startChunks[rng.Next(startChunks.Length)], 0);

        placedCells[Vector3.zero].distance = 0;
    
        // Place blocks directly outward until the exit is placed.
        OuterWhile:
        while(openCells.Count != 0)
        {
            // Select next cell to fill.
            if(exitPosition == null)
                openCells.Sort((Cell a, Cell b) => b.distance - a.distance);
            else
                openCells.Sort((Cell a, Cell b) => a.distance - b.distance);
            Cell nextCell = openCells[0];
            position = nextCell.position;
        
            // Get exits and check to see if map is complete.
            List<Chunk.Direction> validExits = GetValidExits();

            // Try to place an exit.
            if(exitPosition == null)
            {
                ChunkType exit = ChunkType.Exit;
                if(nextCell.distance >= exitDifficulty)
                {
                    if(remainingChunks[0] != exit)
                        remainingChunks.Insert(0, exit);
                } else if(remainingChunks[0] == exit)
                    remainingChunks.RemoveAt(0);
            }
        
            // Prepare variables.
            Chunk.Direction dir = Chunk.Direction.FORWARD;
            Chunk.EdgeDescription edge = null;
            GameObject nextChunk = null;
        
            while(nextChunk == null)
            {
                // If no exits can be built on, rebase.
                if(validExits.Count == 0)
                {
                    deadEnds.Add(nextCell);
                    openCells.Remove(nextCell);
                    goto OuterWhile;
                }
            
                // Select a random exit.
                int selectedExit = rng.Next(validExits.Count);
                dir = validExits[selectedExit];
                var exit = placedCells[position].edges[(int)dir];
            
                // Select a chunk that fits there.
                var nextChunkType = remainingChunks.Count == 0 ? 
                    ChunkType.Edge : remainingChunks[0];

                GameObject[] chunks = GetChunks(nextChunkType);
                nextChunk = SelectChunk(exit, dir, chunks, position, out edge);
                if(nextChunk == null)
                    validExits.RemoveAt(selectedExit);
            }
        
            // Rotate it if needed, draw, and update x, y.
            Chunk details = nextChunk.GetComponent<Chunk>();
            int rotations = edge.RotationsNeeded(dir);
            edge = details.RotateEdge(edge, rotations);
            details = details.Rotated(rotations);
        
            // Update position, place tile.
            position = position - edge.position - edge.ExitVector;
            PlaceChunk(nextChunk, rotations);

            // Empty the closed set.
            openCells.AddRange(deadEnds);
            deadEnds.Clear();

            if(details.type == ChunkType.Exit)
                exitPosition = position;
            if(remainingChunks.Count > 0)
                remainingChunks.RemoveAt(0);
        }

        // Actually generate the terrain.

        if(remainingChunks.Count == 0 && exitPosition != null)
        {
            foreach(Cell cell in deadEnds)
            {
                // Place dead ends.
                GameObject[] blocks = GetChunks(ChunkType.Block);
                for(int dir = 0; dir < 4; dir++)
                {
                    if(cell.edges[dir] != CLOSED)
                    {
                        position = cell.position + DirVector(dir);
                        PlaceChunk(blocks[rng.Next(blocks.Length)], 0);
                    }
                }
            }

            // Now instantiate the terrain, and create the grid.
            terrain = new GameObject();
            terrain.name = "Chunks"; // To make the editor neater.
            foreach(InstantiateCall obj in instantiateCalls)
                obj.Instantiate(terrain);
            CreateGrid();

            return true;
        } else
            return false;
    }

    private void initialise()
    {
        chunkCache = new Dictionary<ChunkType, GameObject[]>();
        placedCells = new Dictionary<Vector3, Cell>();
        instantiateCalls = new List<InstantiateCall>();
        openCells = new List<Cell>();
        deadEnds = new List<Cell>();
        rng = new System.Random();
        position = Vector3.zero;

        remainingChunks = new List<ChunkType>();
        foreach(ChunkCount cc in chunkTypes)
            for(int i = 0; i < rng.Next(cc.min, cc.max); i++)
                remainingChunks.Add(cc.type);
        Shuffle(remainingChunks);
    }
    
    private List<Chunk.Direction> GetValidExits()
    {
        // Count the number of valid exits.
        Cell finalCell = placedCells[position];
        var validEdges = new List<Chunk.Direction>();
        for(int d = 0; d < 4; d++)
            if(finalCell.edges[d] != CLOSED)
                validEdges.Add((Chunk.Direction)d);
        // Pick one at random, if possible.
        return validEdges;
    }

    private GameObject SelectChunk(EdgeType exitType,
                                   Chunk.Direction exitDir,
                                   GameObject[] chunks, Vector3 position,
                                   out Chunk.EdgeDescription nextExit)
    {
        // Produce a list of all possible edges.
        var suitableEdges = new 
            Dictionary<GameObject, List<Chunk.EdgeDescription>>();
        foreach(GameObject chunk in chunks)
        {
            // Find details.
            Chunk details = chunk.GetComponent<Chunk>();
            var edges = new List<Chunk.EdgeDescription>();
            // Test edges using testChunk
            foreach(Chunk.EdgeDescription edge in details.Edges)
            {
                if(edge.type == exitType)
                {
                    int rotations = edge.RotationsNeeded(exitDir);
                    var rotatedEdge = details.RotateEdge(edge, rotations);
                    if(TestChunk(details.Rotated(rotations), 
                     position - rotatedEdge.position - rotatedEdge.ExitVector))
                        edges.Add(edge);
                }
            }
            // If edges are found, add them to the dict.
            if(edges.Count != 0)
                suitableEdges.Add(chunk, edges);
        }
        if(suitableEdges.Count == 0)
        {
            nextExit = null;
            return null;
        }

        // Select a chunk, and grab details.
        GameObject[] keys = new GameObject[suitableEdges.Keys.Count];
        suitableEdges.Keys.CopyTo(keys, 0);
        GameObject selectedChunk = keys[rng.Next(keys.Length)];

        // Select an edge.
        var possibleEdges = suitableEdges[selectedChunk];
        nextExit = possibleEdges[rng.Next(possibleEdges.Count)];

        return selectedChunk;
    }

    private bool TestChunk(Chunk cell, Vector3 position)
    {
        // Highest and lowest difficulties around the cell.
        int minDifficulty = int.MaxValue;
        int maxDifficulty = int.MinValue;
        // For each cell.
        for(int i = 0; i < cell.topEdges.Count; i++)
        {
            for(int j = 0; j < cell.leftEdges.Count; j++)
            {
                Vector3 cellPos = position + new Vector3(i, 0, j);
                // Check for collision.
                if(placedCells.ContainsKey(cellPos))
                    return false;
                // Check if edges match.
                Cell adj; // Adjacent cell.

                // Top edge
                for(int d = 0; d < 4; d++)
                {
                    var conEdge = cell.GetEdges(d)[d % 2 == 0 ? i : j];
                    if(placedCells.TryGetValue(cellPos + DirVector(d), out adj))
                    {
                        var adjEdge = adj.edges[(d + 2) % 4];
                        if(conEdge != adjEdge)
                            return false;
                        if(adjEdge != CLOSED)
                        {
                            if(adj.distance > maxDifficulty)
                                maxDifficulty = adj.distance;
                            if(adj.distance < minDifficulty)
                                minDifficulty = adj.distance;
                        }
                    } else if(cell.type == ChunkType.Edge
                        && conEdge != EdgeType.Closed)
                        return false;
                }

            }
        }
        return (minDifficulty <= 0 ||
            maxDifficulty - minDifficulty <= cell.difficulty + 1);
    }

    private Chunk PlaceChunk(GameObject chunk, int rotations)
    {
        // Get the parameters
        Chunk details = chunk.GetComponent<Chunk>().Rotated(rotations);
        int dist = int.MaxValue; // Distance from the start.

        // For each cell in the chunk:
        for(int x = 0; x < details.topEdges.Count; x++) // Relative x.
        {
            for(int z = 0; z < details.leftEdges.Count; z++) // Relative z.
            {
                Vector3 globalPos = position + new Vector3(x, 0, z);

                // Select the four edges.
                var edges = new EdgeType[4];
                for(int val = 0; val < 4; val++)
                    edges[val] = CLOSED;

                // Connect cells if necessary.
                Cell adjCell;
                for(int d = 0; d < 4; d++) // Clockwise direction from N.
                {
                    var cellPos = globalPos + DirVector(d);
                    // If connected cell detected, set its value to connected.
                    if(placedCells.TryGetValue(cellPos, out adjCell))
                    {
                        if(adjCell.distance + details.difficulty < dist && 
                            adjCell.edges[(d + 2) % 4] != CLOSED)
                        {
                            dist = adjCell.distance + details.difficulty;
                        }
                        adjCell.edges[(d + 2) % 4] = CLOSED;
                        if(adjCell.isComplete() && openCells.Contains(adjCell))
                            openCells.Remove(adjCell);

                    } else // If not, cell edge is an edge (for now).
                        edges[d] = details.GetEdges(d)[d % 2 == 0 ? x : z];
                }
                // Create the Cell.
                Cell cell = new Cell(globalPos, edges);
                if(!cell.isComplete() && details.type != 
                    ChunkType.Edge)
                    openCells.Add(cell);
                placedCells.Add(globalPos, cell);
            }
        }

        // Update difficulty across the cells.
        for(int x = 0; x < details.topEdges.Count; x++) // Relative x.
            for(int z = 0; z < details.leftEdges.Count; z++) // Relative z.
                placedCells[position + new Vector3(x, 0, z)].distance = dist;

        // Instantiate the object itself.
        if((rotations + 1) % 4 > 1)
            position.z += details.leftEdges.Count;
        if(rotations > 1)
            position.x += details.topEdges.Count;
        var rot = Quaternion.AngleAxis(rotations * 90, Vector3.up);
        // Adds the instantiate call to the stack, to be called later.
        instantiateCalls.Add(new InstantiateCall(chunk, position * 10, rot));
        return details;
    }

    private GameObject[] GetChunks(ChunkType type)
    {
        // Check the cache first.
        GameObject[] chunks;
        if(chunkCache.TryGetValue(type, out chunks))
            return chunks;

        // Find chunks with correct tag and zone.
        GameObject[] chunksInFolder = Resources.LoadAll<GameObject>("Chunks/" + 
            zone.ToString() + "/" + ChunkLocation(type));
        List<GameObject> validChunks = new List<GameObject>();
        foreach(GameObject c in chunksInFolder)
            if(c.GetComponent<Chunk>().zone == zone && c.tag == type.ToString())
                validChunks.Add(c);

        // Return the collected chunks.
        chunks = validChunks.ToArray();
        chunkCache.Add(type, chunks);
        return chunks;
    }

    private void CreateGrid()
    {
        // Update the grid. First, calculate min and max cell values.
        Vector3 min = Vector3.zero;
        Vector3 max = Vector3.zero;
        Vector3[] gridPositions = new Vector3[placedCells.Count];
        placedCells.Keys.CopyTo(gridPositions, 0);
        foreach(Vector3 cellPosition in gridPositions)
        {
            for(int i = 0; i < 3; i++)
            {
                if(min[i] > cellPosition[i])
                    min[i] = cellPosition[i];
                if(max[i] < cellPosition[i] + 1f)
                    max[i] = cellPosition[i] + 1f;
            }
        }
        // Stretch to actual size.
        min *= 10f;
        max *= 10f;
        Vector3 mid = (max - min) / 2 + min;
        // Get the grid
        
        
        // Move and resize the grid.
        Grid grid = gameObject.GetComponent<Grid>();
        grid.CubeSize = 0.5f;
        grid.CapsuleSize = 0.4f;

        // Set up collision layers.
        grid.DynamicObstacleCollisionLayer = 11;
        grid.CharacterCollisionLayer = 8;
        grid.NotWalkableCollisionLayer = 10;
        grid.NotWalkableCollisionLayer2 = 10;
        grid.NotWalkableCollisionLayer3 = 10;
        grid.NotWalkableCollisionLayer4 = 10;
        grid.IgnoreCollisionLayer = 12;
        grid.IgnoreCollisionLayer2 = 13;
        grid.IgnoreCollisionLayer3 = 13;
        grid.IgnoreCollisionLayer4 = 13;

        // Adjust position.
        grid.transform.position = new Vector3(mid.x, max.y + 10f, mid.z);
        grid.GridSize = (int)Mathf.Max(max.x - min.x, max.z - min.z) * 2;

        //grid.
        
        // Create the grid.


    }

    private Vector3 DirVector(int dir)
    {
        return Chunk.GetDirectionVector((Chunk.Direction)dir);
    }

    private class InstantiateCall
    {
        private GameObject obj;
        private Vector3 position;
        private Quaternion rotation;
        
        public InstantiateCall(GameObject obj, Vector3 pos, Quaternion rot)
        {
            this.obj = obj;
            position = pos;
            rotation = rot;
        }

        public void Instantiate(GameObject parent)
        {
            GameObject newObject = (GameObject)GameObject.
                Instantiate(obj, position, rotation);
            newObject.transform.parent = parent.transform;
        }
    }

    private class Cell
    {
        public Vector3 position;
        public int distance = 500000;
        public EdgeType[] edges;

        public EdgeType Front { get { return edges[0]; } }
        public EdgeType Right { get { return edges[1]; } }
        public EdgeType Back { get { return edges[2]; } }
        public EdgeType Left { get { return edges[3]; } }

        public Cell(Vector3 pos, params EdgeType[] edges)
        {
            position = pos;
            this.edges = edges;
        }

        public bool isComplete()
        {
            return Array.TrueForAll(edges, x => x == CLOSED);
        }
    }

    public void Shuffle<T>(List<T> list)
    {  
        int n = list.Count;  
        while(n > 1)
        {  
            n--;  
            int k = rng.Next(n + 1);  
            T value = list[k];  
            list[k] = list[n];  
            list[n] = value;  
        }  
    }

    public static string ChunkLocation(ChunkType chunk)
    {
        switch(chunk)
        {
            case ChunkType.Entrance:
                return "Core";
            case ChunkType.Exit:
                return "Core";
            case ChunkType.Block:
                return "Core";
            case ChunkType.SmallChest:
                return "Chest";
            case ChunkType.LargeChest:
                return "Chest";
            case ChunkType.Gem:
                return "Gem";
            case ChunkType.Path:
                return "Path";
            case ChunkType.Trap:
                return "Trap";
            case ChunkType.Miniboss:
                return "Challenge";
            case ChunkType.Challenge:
                return "Challenge";
            case ChunkType.Dungeon:
                return "Special";
            case ChunkType.Well:
                return "Special";
            case ChunkType.Edge:
                return "Edge";
            default:
                throw new UnityException("Unknown chunk type.");
        }
    }
}