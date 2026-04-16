using System.Collections.Generic;
using UnityEngine;
using Puzzle2048.Core;

namespace Puzzle2048.Managers
{
    public class GridManager : MonoBehaviour
    {
        [SerializeField] private GameObject tilePrefab;
        [SerializeField] private Transform gridContainer;

        private Tile[,] grid = new Tile[GameConfig.GRID_SIZE, GameConfig.GRID_SIZE];
        private List<Vector2Int> emptyCells = new List<Vector2Int>();

        public bool HasMoved { get; private set; }

        void Awake()
        {
            InitializeGrid();
        }

        private void InitializeGrid()
        {
            for (int x = 0; x < GameConfig.GRID_SIZE; x++)
                for (int y = 0; y < GameConfig.GRID_SIZE; y++)
                    grid[x, y] = null;
        }

        public void SpawnRandomTile()
        {
            UpdateEmptyCells();
            if (emptyCells.Count == 0) return;

            var pos = emptyCells[Random.Range(0, emptyCells.Count)];
            SpawnTile(pos, Random.value < 0.9f ? 2 : 4);
        }

        private void SpawnTile(Vector2Int pos, int value)
        {
            var go = Instantiate(tilePrefab, gridContainer);
            var tile = go.GetComponent<Tile>();
            tile.GridPosition = pos;
            tile.SetValue(value);
            tile.AnimateSpawn();
            grid[pos.x, pos.y] = tile;
            PositionTile(tile, pos);
        }

        private void PositionTile(Tile tile, Vector2Int pos)
        {
            float cellSize = GameConfig.TILE_SIZE + GameConfig.TILE_SPACING;
            float offset = (GameConfig.GRID_SIZE - 1) * cellSize * 0.5f;
            var rt = tile.GetComponent<RectTransform>();
            rt.anchoredPosition = new Vector2(pos.x * cellSize - offset, pos.y * cellSize - offset);
        }

        public int Move(Vector2Int direction)
        {
            HasMoved = false;
            int score = 0;
            ResetMergeFlags();

            var order = GetTraversalOrder(direction);
            foreach (var pos in order)
            {
                if (grid[pos.x, pos.y] == null) continue;
                score += MoveTile(pos, direction);
            }

            return score;
        }

        private int MoveTile(Vector2Int from, Vector2Int direction)
        {
            int score = 0;
            var tile = grid[from.x, from.y];
            var current = from;
            var next = current + direction;

            while (IsInBounds(next))
            {
                if (grid[next.x, next.y] != null)
                {
                    if (grid[next.x, next.y].Value == tile.Value && !grid[next.x, next.y].Merged && !tile.Merged)
                    {
                        // Merge
                        int newValue = tile.Value * 2;
                        score += newValue;
                        grid[next.x, next.y].SetValue(newValue);
                        grid[next.x, next.y].Merged = true;
                        grid[next.x, next.y].AnimateMerge();
                        grid[current.x, current.y] = null;
                        Destroy(tile.gameObject);
                        HasMoved = true;
                        return score;
                    }
                    break;
                }
                current = next;
                next = current + direction;
            }

            if (current != from)
            {
                grid[current.x, current.y] = tile;
                grid[from.x, from.y] = null;
                tile.GridPosition = current;
                PositionTile(tile, current);
                HasMoved = true;
            }

            return score;
        }

        private List<Vector2Int> GetTraversalOrder(Vector2Int direction)
        {
            var list = new List<Vector2Int>();
            int xStart = direction.x > 0 ? GameConfig.GRID_SIZE - 1 : 0;
            int xEnd = direction.x > 0 ? -1 : GameConfig.GRID_SIZE;
            int xStep = direction.x > 0 ? -1 : 1;
            int yStart = direction.y > 0 ? GameConfig.GRID_SIZE - 1 : 0;
            int yEnd = direction.y > 0 ? -1 : GameConfig.GRID_SIZE;
            int yStep = direction.y > 0 ? -1 : 1;

            for (int x = xStart; x != xEnd; x += xStep)
                for (int y = yStart; y != yEnd; y += yStep)
                    list.Add(new Vector2Int(x, y));
            return list;
        }

        private void ResetMergeFlags()
        {
            for (int x = 0; x < GameConfig.GRID_SIZE; x++)
                for (int y = 0; y < GameConfig.GRID_SIZE; y++)
                    if (grid[x, y] != null) grid[x, y].Merged = false;
        }

        private void UpdateEmptyCells()
        {
            emptyCells.Clear();
            for (int x = 0; x < GameConfig.GRID_SIZE; x++)
                for (int y = 0; y < GameConfig.GRID_SIZE; y++)
                    if (grid[x, y] == null) emptyCells.Add(new Vector2Int(x, y));
        }

        private bool IsInBounds(Vector2Int pos) =>
            pos.x >= 0 && pos.x < GameConfig.GRID_SIZE &&
            pos.y >= 0 && pos.y < GameConfig.GRID_SIZE;

        public bool HasAvailableMoves()
        {
            UpdateEmptyCells();
            if (emptyCells.Count > 0) return true;

            Vector2Int[] directions = { Vector2Int.right, Vector2Int.up };
            for (int x = 0; x < GameConfig.GRID_SIZE; x++)
                for (int y = 0; y < GameConfig.GRID_SIZE; y++)
                    foreach (var d in directions)
                    {
                        var neighbor = new Vector2Int(x, y) + d;
                        if (IsInBounds(neighbor) && grid[neighbor.x, neighbor.y] != null &&
                            grid[x, y] != null && grid[x, y].Value == grid[neighbor.x, neighbor.y].Value)
                            return true;
                    }
            return false;
        }

        public bool HasTileWithValue(int value)
        {
            for (int x = 0; x < GameConfig.GRID_SIZE; x++)
                for (int y = 0; y < GameConfig.GRID_SIZE; y++)
                    if (grid[x, y] != null && grid[x, y].Value == value) return true;
            return false;
        }

        public void ClearGrid()
        {
            for (int x = 0; x < GameConfig.GRID_SIZE; x++)
                for (int y = 0; y < GameConfig.GRID_SIZE; y++)
                {
                    if (grid[x, y] != null) Destroy(grid[x, y].gameObject);
                    grid[x, y] = null;
                }
        }
    }
}
