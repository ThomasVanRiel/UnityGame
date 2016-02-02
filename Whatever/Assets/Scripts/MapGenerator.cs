using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System;

using Random = System.Random;

public class MapGenerator : MonoBehaviour {

    private int[,] _map;

    public int Width;
    public int Height;
    public int BorderSize = 5;

    [Range(0, 1)]
    public float FillPercent;

    public int Seed;
    public bool UseRandomSeed;

    public int SmoothIterations = 5;

    public int WallThresholdSize = 20;
    public int RoomThresholdSize = 20;

    struct Coord {

        public int TileX;
        public int TileY;

        public Coord(int x, int y) {
            TileX = x;
            TileY = y;
        }
    }

    // Use this for initialization
    void Start() {
        GenerateMap();
    }

    void Update() {
        if (Input.GetKeyDown(KeyCode.R)) {
            GenerateMap();
        }
    }


    void GenerateMap() {
        FillMap();
        CellularAutomata(SmoothIterations);

        ProcessMap();

        int[,] borderedMap = new int[Width + BorderSize * 2, Height + BorderSize * 2];

        for (int x = 0; x < borderedMap.GetLength(0); x++) {
            for (int y = 0; y < borderedMap.GetLength(1); y++) {
                if (x >= BorderSize && x < Width + BorderSize && y >= BorderSize && y < Height + BorderSize) {
                    borderedMap[x, y] = _map[x - BorderSize, y - BorderSize];
                } else {
                    borderedMap[x, y] = 1;
                }
            }
        }

        MarchingSquares meshGenerator = GetComponent<MarchingSquares>();
        meshGenerator.GenerateMesh(borderedMap, 1);
    }

    void FillMap() {
        _map = new int[Width, Height];

        Random rng = UseRandomSeed ? new Random() : new Random(Seed);

        for (int x = 0; x < Width; x++) {
            for (int y = 0; y < Height; y++) {
                if (x == 0 || x == Width - 1 || y == 0 || y == Height - 1)
                    _map[x, y] = 1;
                else
                    _map[x, y] = rng.Next(0, 100) < FillPercent * 100 ? 1 : 0;
            }
        }
    }

    void ProcessMap() {
        List<List<Coord>> wallRegions = GetRegions(1);
        foreach (List<Coord> wallRegion in wallRegions) {
            if (wallRegion.Count < WallThresholdSize) {
                foreach (Coord tile in wallRegion) {
                    _map[tile.TileX, tile.TileY] = 0;
                }
            }
        }

        List<List<Coord>> roomRegions = GetRegions(0);
        foreach (List<Coord> roomRegion in roomRegions) {
            if (roomRegion.Count < RoomThresholdSize) {
                foreach (Coord tile in roomRegion) {
                    _map[tile.TileX, tile.TileY] = 1;
                }
            }
        }
    }

    List<List<Coord>> GetRegions(int tileType) {
        List<List<Coord>> regions = new List<List<Coord>>();
        int[,] mapFlags = new int[Width, Height];

        for (int x = 0; x < Width; x++) {
            for (int y = 0; y < Height; y++) {
                if (mapFlags[x, y] == 0 && _map[x, y] == tileType) {
                    List<Coord> newRegion = GetRegionTiles(x, y);
                    regions.Add(newRegion);

                    foreach (Coord tile in newRegion) {
                        mapFlags[tile.TileX, tile.TileY] = 1;
                    }
                }
            }
        }
        return regions;
    }

    List<Coord> GetRegionTiles(int startX, int startY) {
        List<Coord> tiles = new List<Coord>();
        int[,] mapFlags = new int[Width, Height];
        int tileType = _map[startX, startY];

        Queue<Coord> queue = new Queue<Coord>();
        queue.Enqueue(new Coord(startX, startY));
        mapFlags[startX, startY] = 1;

        while (queue.Count > 0) {
            Coord tile = queue.Dequeue();
            tiles.Add(tile);

            for (int x = tile.TileX - 1; x <= tile.TileX + 1; x++) {
                for (int y = tile.TileY - 1; y <= tile.TileY + 1; y++) {
                    if (IsInMapRange(x, y) && (x == tile.TileX || y == tile.TileY)) {
                        if (mapFlags[x, y] == 0 && _map[x, y] == tileType) {
                            mapFlags[x, y] = 1;
                            queue.Enqueue(new Coord(x, y));
                        }
                    }
                }
            }
        }
        return tiles;
    }

    bool IsInMapRange(int x, int y) {
        return x >= 0 && x < Width && y >= 0 && y < Height;
    }

    void CellularAutomata(int iterations) {
        for (int i = 0; i < iterations; i++) {
            for (int x = 0; x < Width; x++) {
                for (int y = 0; y < Height; y++) {
                    int neighbours = GetSurroundingWallCount(x, y);
                    if (neighbours > 4) {
                        _map[x, y] = 1;
                    } else if (neighbours < 4) {
                        _map[x, y] = 0;
                    }
                }
            }
        }
    }

    int GetSurroundingWallCount(int xIndex, int yIndex) {
        int wallCount = 0;
        int kernelSize = 1;
        for (int x = xIndex - kernelSize; x <= xIndex + kernelSize; x++) {
            for (int y = yIndex - kernelSize; y <= yIndex + kernelSize; y++) {
                if (IsInMapRange(x, y)) {
                    if (x != xIndex || y != yIndex) {
                        wallCount += _map[x, y];
                    }
                } else {
                    wallCount++;
                }
            }
        }

        return wallCount;
    }
}
