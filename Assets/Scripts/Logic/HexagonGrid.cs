using System;
using System.Collections.Generic;
using System.Linq;

public class HexagonGrid
{
    private int size;
    private int habitatSize;
    private Dictionary<int, Hex> hexes = new Dictionary<int, Hex>();
    private readonly List<(int x, int y)> evenRowDirections = new()
    {
        (0, 1),
        (1, 1),
        (1, 0),
        (0, -1),
        (1, -1),
        (-1, 0)
    };

    private readonly List<(int x, int y)> oddRowDirections = new()
    {
        (0, 1),
        (-1, 1),
        (1, 0),
        (0, -1),
        (-1, -1),
        (-1, 0)
    };

    public HexagonGrid(int size, int habitatSize = 1)
    {
        this.size = size;
        this.habitatSize = habitatSize;
        // initialize hexes with IDs and coordinates
        for (int row = 0; row < size; row++)
        {
            for (int col = 0; col < size; col++)
            {
                int id = GetId(col, row);
                hexes[id] = new Hex
                {
                    Id = id,
                    Col = col,
                    Row = row
                };
            }
        }
    }

    public Hex GetHexById(int id) => hexes.ContainsKey(id) ? hexes[id] : null;
    public Hex GetHexByCoords(int col, int row) => hexes.ContainsKey(GetId(col, row)) ? hexes[GetId(col, row)] : null;

    public List<(int col, int row)> GetValidNeighbors(int col, int row)
    {
        // We use even-row offset coordinates starting with (0,0) at the bottom left
        var directions = (row + 1) % 2 == 0 ? evenRowDirections : oddRowDirections;
        var neighbors = new List<(int col, int row)>();
        foreach (var (dx, dy) in directions)
        {
            int newCol = col + dx;
            int newRow = row + dy;
            if (newCol >= 0 && newCol < size && newRow >= 0 && newRow < size)
            {
                neighbors.Add((newCol, newRow));
            }
        }
        return neighbors;
    }

    public List<(int col, int row)> GetSelectedNeighbors(int col, int row, List<(int col, int row)> selected)
    {
        var neighbors = GetValidNeighbors(col, row);
        var selectedNeighbors = new List<(int col, int row)>();
        foreach (var (nCol, nRow) in neighbors)
        {
            if (selected.Contains((nCol, nRow)))
            {
                selectedNeighbors.Add((nCol, nRow));
            }
        }
        return selectedNeighbors;
    }

    public void PrintGrid()
    {
        for (int row = size - 1; row >= 0; row--)
        {
            string line = "";
            if (row % 2 == 0) line += "  ";
            for (int col = 0; col < size; col++)
            {
                int id = GetId(col, row);
                var hex = hexes[id];
                string type = hex.Type != null ? hex.Type[0].ToString() : "?";
                int optimal = hex.Optimal;
                line += $"[{id}:{type}:{optimal}] ";
            }
            Console.WriteLine(line);
        }
    }

    public int GetId(int col, int row) => row * size + col + 1;

    public (int col, int row) GetCoords(int id)
    {
        if (id < 1 || id > size * size) throw new ArgumentOutOfRangeException();
        int zero = id - 1;
        return (zero % size, zero / size);
    }

    // Return a size*size array which contains an indicator binary value for if a hex is optimal
    public int[,] GetOptimalGrid()
    {
        int[,] optimalGrid = new int[size, size];
        foreach (var hex in hexes.Values)
        {
            optimalGrid[hex.Col, hex.Row] = hex.Optimal;
        }
        return optimalGrid;
    }

    // Return a size*size array which contains the utility value for each hex
    public int[,] GetUtilityGrid()
    {
        int[,] utilityGrid = new int[size, size];
        foreach (var hex in hexes.Values)
        {
            utilityGrid[hex.Col, hex.Row] = hex.Utility;
        }
        return utilityGrid;
    }

    // Return a size*size array which contains the type value for each hex
    public string[,] GetTypeGrid()
    {
        string[,] typeGrid = new string[size, size];
        foreach (var hex in hexes.Values)
        {
            typeGrid[hex.Col, hex.Row] = hex.Type ?? "?";
        }
        return typeGrid;
    }

    public List<(int col, int row)> GetHabitatCells()
    {
        var habitatCells = new List<(int col, int row)>();
        for (var i = 1; i <= (size * size); i++)
        {
            var hex = GetHexById(i);
            // Check for specific type or string
            if (hex.Type == "habitat")
            {
                habitatCells.Add((hex.Col, hex.Row));
            }
        }
        return habitatCells;
    }

    // Given a list of selected hexes, return true if the goal
    // hex can be reached from the start hex through the selected hexes.
    // Also returns set of visited hexes.
    public (bool, HashSet<(int col, int row)>) isValidCorridor(List<(int col, int row)> selected)
    {
        var status = false;

        // DYNAMICALLY FIND START AND END
        var habitats = GetHabitatCells();
        if (habitats.Count < 2) return (false, new HashSet<(int, int)>());

        // We assume the first found is Start and the last found is Goal.
        // This connects any two distinct habitat points found in the grid.
        var start = habitats[0];
        var goal = habitats[habitats.Count - 1];

        var selectedList = new List<(int col, int row)>(selected);

        // Ensure start and goal are considered "selected" so DFS can traverse them
        if (!selectedList.Contains(start)) selectedList.Add(start);
        if (!selectedList.Contains(goal)) selectedList.Add(goal);

        // Perform DFS from the start using GetSelectedNeighbors to check if we can reach the goal.
        var stack = new Stack<(int col, int row)>();
        stack.Push(start);
        var visited = new HashSet<(int col, int row)>();
        visited.Add(start);
        while (stack.Count > 0)
        {
            var current = stack.Pop();
            if (current == goal) { status = true; }
            var neighbors = GetSelectedNeighbors(current.col, current.row, selectedList);
            foreach ((int col, int row) neighbor in neighbors)
            {
                if (!visited.Contains(neighbor))
                {
                    stack.Push(neighbor);
                    visited.Add(neighbor);
                }
            }
        }
        return (status, visited);
    }

    // Given a list of selected hexes, return the total utility of the selected hexes.
    public int GetTotalCorridorUtility(List<(int col, int row)> selected)
    {
        int totalUtility = 0;
        foreach (var (col, row) in selected)
        {
            var hex = GetHexByCoords(col, row);
            totalUtility += hex.Utility;
        }
        return totalUtility;
    }

    // Given a list of selected hexes, return the position of a hint from the optimal grid.
    public (int col, int row) GetHint(List<(int col, int row)> selected, HashSet<(int col, int row)> alreadyRevealed)
    {
        var choices = new List<(int col, int row)>();
        var habitatCells = GetHabitatCells();
        var optimalGrid = GetOptimalGrid();
        for (int row = 0; row < size; row++)
        {
            for (int col = 0; col < size; col++)
            {
                if (optimalGrid[col, row] == 1 && (!selected.Contains((col, row)) && !habitatCells.Contains((col, row)) && !alreadyRevealed.Contains((col, row))))
                {
                    choices.Add((col, row));
                }
            }
        }
        if (choices.Count == 0) return (-1, -1);
        return choices[new Random().Next(choices.Count)];
    }
}