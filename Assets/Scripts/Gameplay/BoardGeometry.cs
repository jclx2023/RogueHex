using System.Collections.Generic;
using UnityEngine;
using HexGame.Core;

namespace HexGame.Gameplay
{
    /// <summary>
    /// 棋盘几何工具类 - 提供六边形棋盘的几何计算功能
    /// 包含坐标转换、距离计算、路径查找等实用方法
    /// </summary>
    public static class BoardGeometry
    {
        // 六边形的6个相邻方向（轴坐标系）
        public static readonly Vector2Int[] HexDirections = new Vector2Int[]
        {
            new Vector2Int(-1, 0),  // 上
            new Vector2Int(-1, 1),  // 右上
            new Vector2Int(0, -1),  // 左
            new Vector2Int(0, 1),   // 右
            new Vector2Int(1, -1),  // 左下
            new Vector2Int(1, 0)    // 下
        };
        
        // 六边形方向名称（调试用）
        public static readonly string[] DirectionNames = new string[]
        {
            "上", "右上", "左", "右", "左下", "下"
        };
        
        #region 坐标转换
        
        /// <summary>
        /// 六边形坐标转世界坐标
        /// </summary>
        public static Vector3 HexToWorldPosition(Vector2Int hexCoord, GameConfig config)
        {
            if (config == null) return Vector3.zero;
            
            return config.GetHexWorldPosition(hexCoord);
        }
        
        /// <summary>
        /// 世界坐标转六边形坐标
        /// </summary>
        public static Vector2Int WorldToHexPosition(Vector3 worldPos, GameConfig config)
        {
            if (config == null) return Vector2Int.zero;
            
            // 使用六边形网格的逆向计算
            float hexWidth = config.hexWidth;
            float hexHeight = config.hexHeight;
            float xOffset = hexWidth * 0.75f;
            float spacing = config.hexSpacing;
            
            // 反向计算
            worldPos /= spacing;
            
            // 简化的坐标转换（基于偏移六边形网格）
            float centerOffsetX = Mathf.Floor(config.boardCols * xOffset) / 2f;
            
            // 估算行和列
            int estimatedRow = Mathf.RoundToInt(worldPos.y / hexHeight);
            int estimatedCol = Mathf.RoundToInt((worldPos.x + centerOffsetX - estimatedRow * xOffset * 0.5f) / xOffset);
            
            // 边界检查和精确化
            return FindClosestValidHex(new Vector2Int(estimatedRow, estimatedCol), worldPos, config);
        }
        
        /// <summary>
        /// 查找距离世界坐标最近的有效六边形坐标
        /// </summary>
        private static Vector2Int FindClosestValidHex(Vector2Int estimated, Vector3 worldPos, GameConfig config)
        {
            Vector2Int bestHex = estimated;
            float bestDistance = float.MaxValue;
            
            // 检查估算位置及其周围的格子
            for (int dr = -1; dr <= 1; dr++)
            {
                for (int dc = -1; dc <= 1; dc++)
                {
                    Vector2Int candidate = new Vector2Int(estimated.x + dr, estimated.y + dc);
                    
                    if (IsValidCoordinate(candidate, config))
                    {
                        Vector3 candidateWorld = HexToWorldPosition(candidate, config);
                        float distance = Vector3.Distance(worldPos, candidateWorld);
                        
                        if (distance < bestDistance)
                        {
                            bestDistance = distance;
                            bestHex = candidate;
                        }
                    }
                }
            }
            
            return bestHex;
        }
        
        #endregion
        
        #region 邻居和方向
        
        /// <summary>
        /// 获取六边形的所有相邻坐标
        /// </summary>
        public static List<Vector2Int> GetNeighbors(Vector2Int center)
        {
            var neighbors = new List<Vector2Int>();
            
            foreach (var direction in HexDirections)
            {
                neighbors.Add(center + direction);
            }
            
            return neighbors;
        }
        
        /// <summary>
        /// 获取在棋盘范围内的相邻坐标
        /// </summary>
        public static List<Vector2Int> GetValidNeighbors(Vector2Int center, GameConfig config)
        {
            var neighbors = new List<Vector2Int>();
            
            foreach (var direction in HexDirections)
            {
                Vector2Int neighbor = center + direction;
                if (IsValidCoordinate(neighbor, config))
                {
                    neighbors.Add(neighbor);
                }
            }
            
            return neighbors;
        }
        
        /// <summary>
        /// 获取指定方向的邻居坐标
        /// </summary>
        public static Vector2Int GetNeighborInDirection(Vector2Int center, int directionIndex)
        {
            if (directionIndex < 0 || directionIndex >= HexDirections.Length)
                return center;
            
            return center + HexDirections[directionIndex];
        }
        
        /// <summary>
        /// 获取从center到target的方向索引
        /// </summary>
        public static int GetDirectionIndex(Vector2Int center, Vector2Int target)
        {
            Vector2Int direction = target - center;
            
            for (int i = 0; i < HexDirections.Length; i++)
            {
                if (HexDirections[i] == direction)
                {
                    return i;
                }
            }
            
            return -1; // 不是相邻格子
        }
        
        #endregion
        
        #region 距离计算
        
        /// <summary>
        /// 计算两个六边形之间的曼哈顿距离
        /// </summary>
        public static int GetManhattanDistance(Vector2Int a, Vector2Int b)
        {
            return Mathf.Abs(a.x - b.x) + Mathf.Abs(a.y - b.y);
        }
        
        /// <summary>
        /// 计算两个六边形之间的六边形距离（真实距离）
        /// </summary>
        public static int GetHexDistance(Vector2Int a, Vector2Int b)
        {
            // 转换为立方体坐标系进行精确计算
            var cubeA = OffsetToCube(a);
            var cubeB = OffsetToCube(b);
            
            return (Mathf.Abs(cubeA.x - cubeB.x) + 
                    Mathf.Abs(cubeA.y - cubeB.y) + 
                    Mathf.Abs(cubeA.z - cubeB.z)) / 2;
        }
        
        /// <summary>
        /// 计算世界空间中两点的距离
        /// </summary>
        public static float GetWorldDistance(Vector2Int a, Vector2Int b, GameConfig config)
        {
            Vector3 worldA = HexToWorldPosition(a, config);
            Vector3 worldB = HexToWorldPosition(b, config);
            return Vector3.Distance(worldA, worldB);
        }
        
        #endregion
        
        #region 坐标系转换
        
        /// <summary>
        /// 偏移坐标转立方体坐标
        /// </summary>
        public static Vector3Int OffsetToCube(Vector2Int offset)
        {
            int x = offset.y - (offset.x - (offset.x & 1)) / 2;
            int z = offset.x;
            int y = -x - z;
            return new Vector3Int(x, y, z);
        }
        
        /// <summary>
        /// 立方体坐标转偏移坐标
        /// </summary>
        public static Vector2Int CubeToOffset(Vector3Int cube)
        {
            int row = cube.z;
            int col = cube.x + (cube.z - (cube.z & 1)) / 2;
            return new Vector2Int(row, col);
        }
        
        #endregion
        
        #region 路径查找
        
        /// <summary>
        /// 获取两点之间的直线路径
        /// </summary>
        public static List<Vector2Int> GetLinePath(Vector2Int start, Vector2Int end)
        {
            var path = new List<Vector2Int>();
            
            var cubeStart = OffsetToCube(start);
            var cubeEnd = OffsetToCube(end);
            
            int distance = GetHexDistance(start, end);
            
            for (int i = 0; i <= distance; i++)
            {
                float t = distance == 0 ? 0f : (float)i / distance;
                
                var cubeLerp = CubeLerp(cubeStart, cubeEnd, t);
                var offsetPos = CubeToOffset(CubeRound(cubeLerp));
                
                path.Add(offsetPos);
            }
            
            return path;
        }
        
        /// <summary>
        /// 立方体坐标插值
        /// </summary>
        private static Vector3 CubeLerp(Vector3Int a, Vector3Int b, float t)
        {
            return new Vector3(
                Mathf.Lerp(a.x, b.x, t),
                Mathf.Lerp(a.y, b.y, t),
                Mathf.Lerp(a.z, b.z, t)
            );
        }
        
        /// <summary>
        /// 立方体坐标四舍五入
        /// </summary>
        private static Vector3Int CubeRound(Vector3 cube)
        {
            int rx = Mathf.RoundToInt(cube.x);
            int ry = Mathf.RoundToInt(cube.y);
            int rz = Mathf.RoundToInt(cube.z);
            
            float xDiff = Mathf.Abs(rx - cube.x);
            float yDiff = Mathf.Abs(ry - cube.y);
            float zDiff = Mathf.Abs(rz - cube.z);
            
            if (xDiff > yDiff && xDiff > zDiff)
            {
                rx = -ry - rz;
            }
            else if (yDiff > zDiff)
            {
                ry = -rx - rz;
            }
            else
            {
                rz = -rx - ry;
            }
            
            return new Vector3Int(rx, ry, rz);
        }
        
        #endregion
        
        #region 区域和范围
        
        /// <summary>
        /// 获取指定范围内的所有六边形坐标
        /// </summary>
        public static List<Vector2Int> GetHexesInRange(Vector2Int center, int range, GameConfig config)
        {
            var hexes = new List<Vector2Int>();
            
            for (int dx = -range; dx <= range; dx++)
            {
                int minDy = Mathf.Max(-range, -dx - range);
                int maxDy = Mathf.Min(range, -dx + range);
                
                for (int dy = minDy; dy <= maxDy; dy++)
                {
                    int dz = -dx - dy;
                    var cubeCoord = new Vector3Int(dx, dy, dz);
                    var offsetCoord = CubeToOffset(cubeCoord + OffsetToCube(center));
                    
                    if (IsValidCoordinate(offsetCoord, config))
                    {
                        hexes.Add(offsetCoord);
                    }
                }
            }
            
            return hexes;
        }
        
        /// <summary>
        /// 获取环形区域的六边形坐标
        /// </summary>
        public static List<Vector2Int> GetHexRing(Vector2Int center, int radius, GameConfig config)
        {
            var ring = new List<Vector2Int>();
            
            if (radius == 0)
            {
                ring.Add(center);
                return ring;
            }
            
            var cube = OffsetToCube(center);
            var current = new Vector3Int(cube.x - radius, cube.y + radius, cube.z);
            
            for (int i = 0; i < 6; i++)
            {
                for (int j = 0; j < radius; j++)
                {
                    var offsetPos = CubeToOffset(current);
                    if (IsValidCoordinate(offsetPos, config))
                    {
                        ring.Add(offsetPos);
                    }
                    
                    current += OffsetToCube(HexDirections[i]);
                }
            }
            
            return ring;
        }
        
        #endregion
        
        #region 边界和验证
        
        /// <summary>
        /// 检查坐标是否在棋盘范围内
        /// </summary>
        public static bool IsValidCoordinate(Vector2Int coord, GameConfig config)
        {
            if (config == null) return false;
            
            return coord.x >= 0 && coord.x < config.boardRows &&
                   coord.y >= 0 && coord.y < config.boardCols;
        }
        
        /// <summary>
        /// 检查坐标是否在边界上
        /// </summary>
        public static bool IsOnBoundary(Vector2Int coord, GameConfig config)
        {
            if (!IsValidCoordinate(coord, config)) return false;
            
            return coord.x == 0 || coord.x == config.boardRows - 1 ||
                   coord.y == 0 || coord.y == config.boardCols - 1;
        }
        
        /// <summary>
        /// 检查坐标是否在指定边界上
        /// </summary>
        public static bool IsOnSpecificBoundary(Vector2Int coord, GameConfig config, int boundary)
        {
            if (!IsValidCoordinate(coord, config)) return false;
            
            switch (boundary)
            {
                case 0: return coord.x == 0; // 上边界
                case 1: return coord.x == config.boardRows - 1; // 下边界
                case 2: return coord.y == 0; // 左边界
                case 3: return coord.y == config.boardCols - 1; // 右边界
                default: return false;
            }
        }
        
        /// <summary>
        /// 获取指定边界上的所有坐标
        /// </summary>
        public static List<Vector2Int> GetBoundaryCoordinates(GameConfig config, int boundary)
        {
            var coordinates = new List<Vector2Int>();
            if (config == null) return coordinates;
            
            switch (boundary)
            {
                case 0: // 上边界
                    for (int y = 0; y < config.boardCols; y++)
                        coordinates.Add(new Vector2Int(0, y));
                    break;
                case 1: // 下边界
                    for (int y = 0; y < config.boardCols; y++)
                        coordinates.Add(new Vector2Int(config.boardRows - 1, y));
                    break;
                case 2: // 左边界
                    for (int x = 0; x < config.boardRows; x++)
                        coordinates.Add(new Vector2Int(x, 0));
                    break;
                case 3: // 右边界
                    for (int x = 0; x < config.boardRows; x++)
                        coordinates.Add(new Vector2Int(x, config.boardCols - 1));
                    break;
            }
            
            return coordinates;
        }
        
        #endregion
        
        #region 实用工具方法
        
        /// <summary>
        /// 获取棋盘中心坐标
        /// </summary>
        public static Vector2Int GetBoardCenter(GameConfig config)
        {
            if (config == null) return Vector2Int.zero;
            
            return new Vector2Int(config.boardRows / 2, config.boardCols / 2);
        }
        
        /// <summary>
        /// 获取棋盘四个角的坐标
        /// </summary>
        public static List<Vector2Int> GetBoardCorners(GameConfig config)
        {
            var corners = new List<Vector2Int>();
            if (config == null) return corners;
            
            corners.Add(new Vector2Int(0, 0)); // 左上角
            corners.Add(new Vector2Int(0, config.boardCols - 1)); // 右上角
            corners.Add(new Vector2Int(config.boardRows - 1, 0)); // 左下角
            corners.Add(new Vector2Int(config.boardRows - 1, config.boardCols - 1)); // 右下角
            
            return corners;
        }
        
        /// <summary>
        /// 检查两个坐标是否相邻
        /// </summary>
        public static bool AreNeighbors(Vector2Int a, Vector2Int b)
        {
            Vector2Int diff = b - a;
            
            foreach (var direction in HexDirections)
            {
                if (direction == diff)
                {
                    return true;
                }
            }
            
            return false;
        }
        
        /// <summary>
        /// 获取从起点到终点的最短路径方向
        /// </summary>
        public static Vector2Int GetShortestDirection(Vector2Int from, Vector2Int to)
        {
            Vector2Int diff = to - from;
            
            // 找到最接近的六边形方向
            Vector2Int bestDirection = Vector2Int.zero;
            float bestDot = float.MinValue;
            
            foreach (var direction in HexDirections)
            {
                float dot = Vector2.Dot(diff, direction);
                if (dot > bestDot)
                {
                    bestDot = dot;
                    bestDirection = direction;
                }
            }
            
            return bestDirection;
        }
        
        /// <summary>
        /// 计算位置到边界的最短距离
        /// </summary>
        public static int GetDistanceToBoundary(Vector2Int coord, GameConfig config, int targetBoundary = -1)
        {
            if (!IsValidCoordinate(coord, config)) return -1;
            
            if (targetBoundary >= 0 && targetBoundary <= 3)
            {
                // 计算到指定边界的距离
                switch (targetBoundary)
                {
                    case 0: return coord.x; // 到上边界
                    case 1: return config.boardRows - 1 - coord.x; // 到下边界
                    case 2: return coord.y; // 到左边界
                    case 3: return config.boardCols - 1 - coord.y; // 到右边界
                }
            }
            
            // 计算到最近边界的距离
            return Mathf.Min(
                coord.x, // 到上边界
                config.boardRows - 1 - coord.x, // 到下边界
                coord.y, // 到左边界
                config.boardCols - 1 - coord.y // 到右边界
            );
        }
        
        /// <summary>
        /// 获取两点之间的所有可能路径（BFS）
        /// </summary>
        public static List<List<Vector2Int>> FindAllPaths(Vector2Int start, Vector2Int end, GameConfig config, 
                                                         System.Func<Vector2Int, bool> isBlocked = null)
        {
            var paths = new List<List<Vector2Int>>();
            var queue = new Queue<List<Vector2Int>>();
            var visited = new HashSet<Vector2Int>();
            
            queue.Enqueue(new List<Vector2Int> { start });
            
            while (queue.Count > 0)
            {
                var currentPath = queue.Dequeue();
                var currentPos = currentPath[currentPath.Count - 1];
                
                if (currentPos == end)
                {
                    paths.Add(new List<Vector2Int>(currentPath));
                    continue;
                }
                
                if (visited.Contains(currentPos))
                    continue;
                
                visited.Add(currentPos);
                
                foreach (var neighbor in GetValidNeighbors(currentPos, config))
                {
                    if (!visited.Contains(neighbor) && (isBlocked == null || !isBlocked(neighbor)))
                    {
                        var newPath = new List<Vector2Int>(currentPath) { neighbor };
                        queue.Enqueue(newPath);
                    }
                }
            }
            
            return paths;
        }
        
        /// <summary>
        /// 计算一组坐标的重心
        /// </summary>
        public static Vector2 GetCentroid(List<Vector2Int> coordinates)
        {
            if (coordinates == null || coordinates.Count == 0)
                return Vector2.zero;
            
            Vector2 sum = Vector2.zero;
            foreach (var coord in coordinates)
            {
                sum += new Vector2(coord.x, coord.y);
            }
            
            return sum / coordinates.Count;
        }
        
        #endregion
        
        #region 调试和可视化
        
        /// <summary>
        /// 获取坐标的调试字符串
        /// </summary>
        public static string CoordToString(Vector2Int coord)
        {
            return $"({coord.x}, {coord.y})";
        }
        
        /// <summary>
        /// 获取路径的调试字符串
        /// </summary>
        public static string PathToString(List<Vector2Int> path)
        {
            if (path == null || path.Count == 0)
                return "Empty path";
            
            var pathStrings = new string[path.Count];
            for (int i = 0; i < path.Count; i++)
            {
                pathStrings[i] = CoordToString(path[i]);
            }
            
            return string.Join(" -> ", pathStrings);
        }
        
        /// <summary>
        /// 在Unity编辑器中绘制六边形格子（调试用）
        /// </summary>
        public static void DrawHexGizmo(Vector2Int coord, GameConfig config, Color color)
        {
            #if UNITY_EDITOR
            if (config == null) return;
            
            Vector3 center = HexToWorldPosition(coord, config);
            float size = config.hexWidth * 0.5f;
            
            Gizmos.color = color;
            
            // 绘制六边形轮廓
            Vector3[] vertices = new Vector3[6];
            for (int i = 0; i < 6; i++)
            {
                float angle = i * 60f * Mathf.Deg2Rad;
                vertices[i] = center + new Vector3(
                    Mathf.Cos(angle) * size,
                    Mathf.Sin(angle) * size,
                    0
                );
            }
            
            for (int i = 0; i < 6; i++)
            {
                int next = (i + 1) % 6;
                Gizmos.DrawLine(vertices[i], vertices[next]);
            }
            
            // 绘制中心点
            Gizmos.DrawSphere(center, size * 0.1f);
            #endif
        }
        
        #endregion
    }
}