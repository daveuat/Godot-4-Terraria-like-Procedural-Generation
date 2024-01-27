using Godot;
using System;
using System.Collections.Generic;
using System.Text;

public partial class LevelGenerator : Node2D
{
#region VARIABLES

private int _width = 96;
private int _height = 64;
private int _borderWidth = 3;
private int _density = 50;
[Export(PropertyHint.Range, "1, 10")]
private int _smoothing = 3;
private int _minRegionSize = 32;

private Dictionary<int, Vector2I> _tiles;
private int[,] _tileFlags;

private int[,] _map;
private TileMap _tileMap;
private Random _pseudoRandom;

private const int WALLTHRESHOLD = 4;

#endregion

    #region MAIN
    
    public override void _Ready()
    {
        Initialize();
        GenerateMap();
        GenerateFlags();
        UpdateTileMap();
    }

    #endregion

    #region INIT

    private void Initialize()
    {
        GatherRequirements();
        Setup();
    }
    
    private void Setup()
    {
        _map = new int[_width, _height];
        _pseudoRandom = new Random();
        _tiles = TilemapHelper.LoadTextureCoords("coords.json");
        _tileFlags = new int[_width, _height];
    }

    private void GatherRequirements()
    {
        _tileMap = GetNode<TileMap>("Map");
    }

    #endregion

    #region GENERATION

    private void GenerateMap()
    {
        for (int x = 0; x < _width; x++)
        {
            for (int y = 0; y < _height; y++)
            {
                if (IsBorderTile(x, y))
                {
                    _map[x, y] = 1;
                }
                else
                {
                    _map[x, y] = _pseudoRandom.Next(0, 100) < _density ? 1 : 0;
                }
            }
        }

        for (int i = 0; i < _smoothing; i++)
        {
            SmoothMap();
        }
    }

    private void UpdateTileMap()
    {
        for (int x = 0; x < _width; x++)
        {
            for (int y = 0; y < _height; y++)
            {
                int flag = _tileFlags[x, y];
                if (flag == 0) continue;
                if(!_tiles.ContainsKey(flag)) continue;
                Vector2I flagCoord = _tiles[flag];
                _tileMap.SetCell(0, new Vector2I(x, y), 0, new Vector2I(flagCoord.X, flagCoord.Y));
            }
        }
    }

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
                    > WALLTHRESHOLD => 1,
                    < WALLTHRESHOLD => 0,
                    _ => _map[x, y]
                };
            }
        }

        _map = temp;
    }

    private void GenerateFlags()
    {
        for (int x = 0; x < _width; x++)
        {
            for (int y = 0; y < _height; y++)
            {
                if(_map[x, y] == 0)
                {
                    _tileFlags[x, y] = 0;
                } 
                else
                {
                    _tileFlags[x, y] = GetNeighbourMask(x, y);
                }
            }
        }
    }

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
                    if (!IsInBounds(tileX, tileY)) continue;
                    if (current.X != tileX && current.Y != tileY) continue;
                    if (flags[tileX, tileY] != 0 || _map[tileX, tileY] != tileType) continue;
                    flags[tileX, tileY] = 1;
                    queue.Enqueue(new Vector2I(tileX, tileY));
                }
            }
        }
        return temp;
    }

    private List<List<Vector2I>> GetRegions(int tileType)
    {
        List<List<Vector2I>> temp = new List<List<Vector2I>>();
        int[,] flags = new int[_width, _height];
        for (int x = 0; x < _width; x++)
        {
            for (int y = 0; y < _height; y++)
            {
                if (flags[x, y] != 0 || _map[x, y] != tileType) continue;
                List<Vector2I> region = GetRegion(x, y);
                temp.Add(region);
                foreach (Vector2I coord in region)
                {
                    flags[coord.X, coord.Y] = 1;
                }
            }
        }
        return temp;
    }

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
                _map[tile.X, tile.Y] = 0; // Set to empty space
            }
        }
    }

    foreach (List<Vector2I> roomRegion in roomRegions)
    {
        if (roomRegion.Count < _minRegionSize)
        {
            foreach (Vector2I tile in roomRegion)
            {
                _map[tile.X, tile.Y] = 1; // Set to wall
            }
        }
    }
}

    private bool IsBorderTile(int x, int y)
    {
        return
            x < _borderWidth ||
            x > _width - _borderWidth - 1 ||
            y < _borderWidth ||
            y > _height - _borderWidth - 1;
    }

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
                        continue;
                    }
                    count += _map[nX, nY];
                }
                else
                {
                    count++;
                }
            }
        }
        return count;
    }

    private bool IsInBounds(int x, int y)
    {
        return x >= 0 && x < _width && y >= 0 && y < _height;
    }

    private int GetNeighbourMask(int x, int y)
    {
        StringBuilder builder = new StringBuilder();
        for(int nY = y-1; nY <= y + 1; nY++)
        {
            for(int nX = x-1; nX <= x + 1; nX++)
            {
                if (nY == y && nX == x) continue;
                if (!IsInBounds(nX, nY))
                {
                    builder.Append(1);
                    continue;
                }
                builder.Append(_map[nX, nY]);
            }
        }
        string bitMask = builder.ToString();
        return Convert.ToInt32(bitMask, 2);
    }

    #endregion
}