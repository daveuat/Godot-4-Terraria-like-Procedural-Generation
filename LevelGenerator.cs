using Godot;
using System;
using System.Collections.Generic;
using System.Text;

public partial class LevelGenerator : Node2D
{
    #region VARIABLES

    // Define the dimensions of the level and various parameters
    private int _width = 96;
    private int _height = 64;
    private int _borderWidth = 3;
    private int _density = 50;
    [Export(PropertyHint.Range, "1, 10")] private int _smoothing = 3;
    private int _minRegionSize = 32;

    // Dictionaries and arrays for storing tile information
    private Dictionary<int, Vector2I> _tiles;
    private int[,] _tileFlags;

    // The map array and the TileMap node
    private int[,] _map;
    private TileMap _tileMap;
    private Random _pseudoRandom;

    // Threshold for determining if a tile should be a wall
    private const int WALLTHRESHOLD = 4;

    #endregion

    #region MAIN

    // Called when the node is ready
    public override void _Ready()
    {
        Initialize();
        GenerateMap();
        GenerateFlags();
        UpdateTileMap();
        Vector2 spawnPosition = FindClosestOpenSpot();
        SpawnPlayer(spawnPosition);
    }

    #endregion

    #region INIT

    // Initialize the level generator
    private void Initialize()
    {
        GatherRequirements();
        Setup();
    }

    // Setup the map and random number generator
    private void Setup()
    {
        _map = new int[_width, _height];
        _pseudoRandom = new Random();
        _tiles = TilemapHelper.LoadTextureCoords("coords.json");
        _tileFlags = new int[_width, _height];
    }

    // Gather required nodes from the scene
    private void GatherRequirements()
    {
        _tileMap = GetNode<TileMap>("Map");
    }

    #endregion

    #region GENERATION

    // Generate the initial map
    private void GenerateMap()
    {
        for (int x = 0; x < _width; x++)
        {
            for (int y = 0; y < _height; y++)
            {
                if (IsBorderTile(x, y))
                {
                    _map[x, y] = 1; // Set border tiles as walls
                }
                else
                {
                    _map[x, y] = _pseudoRandom.Next(0, 100) < _density ? 1 : 0; // Randomly set tiles based on density
                }
            }
        }

        // Smooth the map to create more natural formations
        for (int i = 0; i < _smoothing; i++)
        {
            SmoothMap();
        }
    }

    // Update the TileMap with the generated map
    private void UpdateTileMap()
    {
        for (int x = 0; x < _width; x++)
        {
            for (int y = 0; y < _height; y++)
            {
                int flag = _tileFlags[x, y];
                if (flag == 0) continue; // Skip empty tiles
                if (!_tiles.ContainsKey(flag)) continue; // Skip if flag not found in dictionary
                Vector2I flagCoord = _tiles[flag];
                _tileMap.SetCell(0, new Vector2I(x, y), 0, new Vector2I(flagCoord.X, flagCoord.Y));
            }
        }
    }

    // Smooth the map by checking neighboring tiles
    private void SmoothMap()
    {
        int[,] temp = new int[_width, _height];
        for (int x = 0; x < _width; x++)
        {
            for (int y = 0; y < _height; y++)
            {
                int nWalls = NeighbouringWallCount(x, y);

                temp[x, y] = nWalls switch
                {
                    > WALLTHRESHOLD => 1, // More walls than threshold, set as wall
                    < WALLTHRESHOLD => 0, // Less walls than threshold, set as open space
                    _ => _map[x, y] // Keep current state
                };
            }
        }

        _map = temp; // Update map with smoothed version
    }

    // Generate flags for each tile based on their neighbors
    private void GenerateFlags()
    {
        for (int x = 0; x < _width; x++)
        {
            for (int y = 0; y < _height; y++)
            {
                if (_map[x, y] == 0)
                {
                    _tileFlags[x, y] = 0; // Set flag as 0 for open spaces
                }
                else
                {
                    _tileFlags[x, y] = GetNeighbourMask(x, y); // Set flag based on neighboring tiles
                }
            }
        }
    }

    // Get a specific region of tiles
    private List<Vector2I> GetRegion(int x, int y)
    {
        List<Vector2I> temp = new List<Vector2I>();
        int[,] flags = new int[_width, _height];
        int tileType = _map[x, y];
        Queue<Vector2I> queue = new Queue<Vector2I>();
        queue.Enqueue(new Vector2I(x, y));
        flags[x, y] = 1;
        while (queue.Count > 0)
        {
            Vector2I current = queue.Dequeue();
            temp.Add(current);
            for (int tileX = current.X - 1; tileX <= current.X + 1; tileX++)
            {
                for (int tileY = current.Y - 1; tileY <= current.Y + 1; tileY++)
                {
                    if (!IsInBounds(tileX, tileY)) continue; // Skip if out of bounds
                    if (current.X != tileX && current.Y != tileY) continue; // Skip diagonal tiles
                    if (flags[tileX, tileY] != 0 || _map[tileX, tileY] != tileType)
                        continue; // Skip if already visited or different tile type
                    flags[tileX, tileY] = 1;
                    queue.Enqueue(new Vector2I(tileX, tileY));
                }
            }
        }

        return temp;
    }

    // Get all regions of a specific tile type
    private List<List<Vector2I>> GetRegions(int tileType)
    {
        List<List<Vector2I>> temp = new List<List<Vector2I>>();
        int[,] flags = new int[_width, _height];
        for (int x = 0; x < _width; x++)
        {
            for (int y = 0; y < _height; y++)
            {
                if (flags[x, y] != 0 || _map[x, y] != tileType)
                    continue; // Skip if already visited or different tile type
                List<Vector2I> region = GetRegion(x, y);
                temp.Add(region);
                foreach (Vector2I coord in region)
                {
                    flags[coord.X, coord.Y] = 1; // Mark as visited
                }
            }
        }

        return temp;
    }

    // Cleanup small regions to ensure map integrity
    private void CleanupRegions()
    {
        List<List<Vector2I>> wallRegions = GetRegions(1);
        List<List<Vector2I>> roomRegions = GetRegions(0);

        foreach (List<Vector2I> wallRegion in wallRegions)
        {
            if (wallRegion.Count < _minRegionSize)
            {
                foreach (Vector2I tile in wallRegion)
                {
                    _map[tile.X, tile.Y] = 0; // Convert small wall regions to open space
                }
            }
        }

        foreach (List<Vector2I> roomRegion in roomRegions)
        {
            if (roomRegion.Count < _minRegionSize)
            {
                foreach (Vector2I tile in roomRegion)
                {
                    _map[tile.X, tile.Y] = 1; // Convert small open regions to walls
                }
            }
        }
    }

    // Check if a tile is a border tile
    private bool IsBorderTile(int x, int y)
    {
        return
            x < _borderWidth ||
            x > _width - _borderWidth - 1 ||
            y < _borderWidth ||
            y > _height - _borderWidth - 1;
    }

    // Count the number of walls surrounding a tile
    private int NeighbouringWallCount(int x, int y)
    {
        int count = 0;
        for (int nY = y - 1; nY <= y + 1; nY++)
        {
            for (int nX = x - 1; nX <= x + 1; nX++)
            {
                if (IsInBounds(nX, nY))
                {
                    if (nY == y && nX == x)
                    {
                        continue; // Skip the current tile
                    }

                    count += _map[nX, nY]; // Count if neighboring tile is a wall
                }
                else
                {
                    count++; // Count edges as walls
                }
            }
        }

        return count;
    }

    // Check if coordinates are within the map bounds
    private bool IsInBounds(int x, int y)
    {
        return x >= 0 && x < _width && y >= 0 && y < _height;
    }

    // Generate a bitmask based on neighboring tiles
    private int GetNeighbourMask(int x, int y)
    {
        StringBuilder builder = new StringBuilder();
        for (int nY = y - 1; nY <= y + 1; nY++)
        {
            for (int nX = x - 1; nX <= x + 1; nX++)
            {
                if (nY == y && nX == x) continue; // Skip the current tile
                if (!IsInBounds(nX, nY))
                {
                    builder.Append(1); // Treat out-of-bounds as walls
                    continue;
                }

                builder.Append(_map[nX, nY]); // Append the state of the neighboring tile
            }
        }

        string bitMask = builder.ToString();
        return Convert.ToInt32(bitMask, 2); // Convert bitmask to integer
    }

    // Find the closest open spot to a given position
    private Vector2 FindClosestOpenSpot()
    {
        Vector2 startSearchPosition = new Vector2(10, 10); // Starting position for search
        Vector2 closestSpot = new Vector2(-1, -1);
        float closestDistance = float.MaxValue;

        for (int y = 0; y < _height; y++)
        {
            for (int x = 0; x < _width; x++)
            {
                if (_map[x, y] == 0 && IsClearArea(x, y)) // Check for open spot and clear area
                {
                    Vector2 currentSpot = new Vector2(x, y);
                    float distance = (currentSpot - startSearchPosition).Length();
                    if (distance < closestDistance)
                    {
                        closestDistance = distance;
                        closestSpot = currentSpot; // Update closest spot
                    }
                }
            }
        }

        GD.Print("Closest open spot found at: " + closestSpot);
        return closestSpot;
    }

    // Check if the area around a tile is clear
    private bool IsClearArea(int x, int y)
    {
        // Check surrounding tiles to ensure they are open
        for (int i = -1; i <= 1; i++)
        {
            for (int j = -1; j <= 1; j++)
            {
                int checkX = x + i;
                int checkY = y + j;
                if (!IsInBounds(checkX, checkY) || _map[checkX, checkY] != 0)
                {
                    return false; // Not a clear area if out of bounds or not open
                }
            }
        }

        return true; // Clear area
    }

    // Spawn the player at a specific tile position
    private void SpawnPlayer(Vector2 tilePosition)
    {
        PackedScene playerScene = (PackedScene)ResourceLoader.Load("res://Player.tscn");
        Node2D playerInstance = (Node2D)playerScene.Instantiate();

        // Manually calculate the world position
        Vector2 tileSize = new Vector2(32, 32); // Your tile size is 16x16 pixels
        Vector2 worldPosition = tilePosition * tileSize;

        playerInstance.Position = worldPosition;
        AddChild(playerInstance);
    }

    #endregion
}