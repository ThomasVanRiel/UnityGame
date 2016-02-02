using UnityEngine;
using System.Collections;
using System.Collections.Generic;

[RequireComponent(typeof(MeshFilter))]
public class MarchingSquares : MonoBehaviour {
    #region Classes
    public class Node {
        public Vector3 Position;
        public int VertexIndex = -1;
        public Node(Vector3 position) {
            Position = position;
        }
    }

    public class ControlNode : Node {
        public bool IsActive;
        public Node Above, Right;

        public ControlNode(Vector3 position, bool isActive, float size) : base(position) {
            IsActive = isActive;
            Above = new Node(position + Vector3.forward * size / 2f);
            Right = new Node(position + Vector3.right * size / 2f);
        }
    }

    public class Square {
        public ControlNode TopLeft, TopRight, BottomRight, BottomLeft;
        public Node CentreTop, CentreRight, CentreBottom, CentreLeft;
        public int Configuration;

        public Square(ControlNode topLeft, ControlNode topRight, ControlNode bottomRight, ControlNode bottomLeft) {
            TopLeft = topLeft;
            TopRight = topRight;
            BottomRight = bottomRight;
            BottomLeft = bottomLeft;

            CentreTop = TopLeft.Right;
            CentreRight = bottomRight.Above;
            CentreBottom = BottomLeft.Right;
            CentreLeft = BottomLeft.Above;

            if (TopLeft.IsActive)
                Configuration += 8;
            if (TopRight.IsActive)
                Configuration += 4;
            if (BottomRight.IsActive)
                Configuration += 2;
            if (BottomLeft.IsActive)
                Configuration += 1;

        }
    }

    public class Grid {
        public Square[,] squares;

        public Grid(int[,] map, float size) {
            int nodeWidth = map.GetLength(0);
            int nodeHeight = map.GetLength(1);
            float mapWidth = nodeWidth * size;
            float mapHeight = nodeHeight * size;

            ControlNode[,] controlNodes = new ControlNode[nodeWidth, nodeHeight];

            for (int x = 0; x < nodeWidth; x++) {
                for (int y = 0; y < nodeHeight; y++) {
                    Vector3 pos = new Vector3(-mapWidth / 2 + x * size + size / 2, 0, -mapHeight / 2 + y * size + size / 2);
                    controlNodes[x, y] = new ControlNode(pos, map[x, y] == 1, size);
                }
            }

            squares = new Square[nodeWidth - 1, nodeHeight - 1];
            for (int x = 0; x < nodeWidth - 1; x++) {
                for (int y = 0; y < nodeHeight - 1; y++) {
                    squares[x, y] = new Square(controlNodes[x, y + 1], controlNodes[x + 1, y + 1], controlNodes[x + 1, y], controlNodes[x, y]);
                }
            }

        }

    }

    struct Triangle {
        public int VertexIdA;
        public int VertexIdB;
        public int VertexIdC;
        int[] vertices;

        public Triangle(int a, int b, int c) {
            VertexIdA = a;
            VertexIdB = b;
            VertexIdC = c;
            vertices = new int[3];
            vertices[0] = a;
            vertices[1] = b;
            vertices[2] = c;
        }

        public int this[int i] {
            get {
                return vertices[i];
            }
        }

        public bool Containts(int vertexIndex) {
            return vertexIndex == VertexIdA || vertexIndex == VertexIdB || vertexIndex == VertexIdC;
        }
    }

    #endregion

    public Grid GridMap;
    List<Vector3> Vertices;
    List<int> Triangles;
    Dictionary<int, List<Triangle>> TriangleDictionary = new Dictionary<int, List<Triangle>>();
    List<List<int>> Outlines = new List<List<int>>();
    HashSet<int> CheckedVerts = new HashSet<int>();
    public MeshFilter WallMesh;

    public void GenerateMesh(int[,] map, float squareSize) {

        TriangleDictionary.Clear();
        Outlines.Clear();
        CheckedVerts.Clear();

        GridMap = new Grid(map, squareSize);

        Vertices = new List<Vector3>();
        Triangles = new List<int>();

        for (int x = 0; x < GridMap.squares.GetLength(0); x++) {
            for (int y = 0; y < GridMap.squares.GetLength(1); y++) {
                TriangulateSquare(GridMap.squares[x, y]);
            }
        }

        Mesh mesh = new Mesh();
        GetComponent<MeshFilter>().mesh = mesh;

        mesh.vertices = Vertices.ToArray();
        mesh.triangles = Triangles.ToArray();
        mesh.RecalculateNormals();

        CreateWallMesh();
    }

    void CreateWallMesh() {
        CalculateOutlines();
        List<Vector3> wallVertices = new List<Vector3>();
        List<int> wallTriangles = new List<int>();
        Mesh wallMesh = new Mesh();
        float WallHeight = 5;
        foreach (List<int> outline in Outlines) {
            for (int i = 0; i < outline.Count - 1; i++) {
                int startIndex = wallVertices.Count;

                //left
                wallVertices.Add(Vertices[outline[i]]);

                //right
                wallVertices.Add(Vertices[outline[i+1]]);

                //bottom left
                wallVertices.Add(Vertices[outline[i]] - Vector3.up * WallHeight);

                //bottom right
                wallVertices.Add(Vertices[outline[i+1]] - Vector3.up * WallHeight);

                wallTriangles.Add(startIndex + 0);
                wallTriangles.Add(startIndex + 2);
                wallTriangles.Add(startIndex + 3);
                wallTriangles.Add(startIndex + 3);
                wallTriangles.Add(startIndex + 1);
                wallTriangles.Add(startIndex + 0);
            }
        }

        wallMesh.vertices = wallVertices.ToArray();
        wallMesh.triangles = wallTriangles.ToArray();
        wallMesh.RecalculateNormals();
        WallMesh.mesh = wallMesh;
    }

    void TriangulateSquare(Square square) {
        switch (square.Configuration) {
            case 0:
                break;

            // 1 point
            case 1:
                MeshFromPoints(square.CentreLeft, square.CentreBottom, square.BottomLeft);
                break;
            case 2:
                MeshFromPoints(square.BottomRight, square.CentreBottom, square.CentreRight);
                break;
            case 4:
                MeshFromPoints(square.TopRight, square.CentreRight, square.CentreTop);
                break;
            case 8:
                MeshFromPoints(square.TopLeft, square.CentreTop, square.CentreLeft);
                break;

            // 2 points
            case 3:
                MeshFromPoints(square.CentreRight, square.BottomRight, square.BottomLeft, square.CentreLeft);
                break;
            case 6:
                MeshFromPoints(square.CentreTop, square.TopRight, square.BottomRight, square.CentreBottom);
                break;
            case 9:
                MeshFromPoints(square.TopLeft, square.CentreTop, square.CentreBottom, square.BottomLeft);
                break;
            case 12:
                MeshFromPoints(square.TopLeft, square.TopRight, square.CentreRight, square.CentreLeft);
                break;
            case 5:
                MeshFromPoints(square.CentreTop, square.TopRight, square.CentreRight, square.CentreBottom, square.BottomLeft, square.CentreLeft);
                break;
            case 10:
                MeshFromPoints(square.TopLeft, square.CentreTop, square.CentreRight, square.BottomRight, square.CentreBottom, square.CentreLeft);
                break;

            // 3 points
            case 7:
                MeshFromPoints(square.CentreTop, square.TopRight, square.BottomRight, square.BottomLeft, square.CentreLeft);
                break;
            case 11:
                MeshFromPoints(square.TopLeft, square.CentreTop, square.CentreRight, square.BottomRight, square.BottomLeft);
                break;
            case 13:
                MeshFromPoints(square.TopLeft, square.TopRight, square.CentreRight, square.CentreBottom, square.BottomLeft);
                break;
            case 14:
                MeshFromPoints(square.TopLeft, square.TopRight, square.BottomRight, square.CentreBottom, square.CentreLeft);
                break;

            // 4 points
            case 15:
                MeshFromPoints(square.TopLeft, square.TopRight, square.BottomRight, square.BottomLeft);
                CheckedVerts.Add(square.TopLeft.VertexIndex);
                CheckedVerts.Add(square.TopRight.VertexIndex);
                CheckedVerts.Add(square.BottomRight.VertexIndex);
                CheckedVerts.Add(square.BottomLeft.VertexIndex);
                break;
        }

    }

    void MeshFromPoints(params Node[] points) {
        AssignVertices(points);

        if (points.Length >= 3)
            CreateTriangle(points[0], points[1], points[2]);
        if (points.Length >= 4)
            CreateTriangle(points[0], points[2], points[3]);
        if (points.Length >= 5)
            CreateTriangle(points[0], points[3], points[4]);
        if (points.Length >= 6)
            CreateTriangle(points[0], points[4], points[5]);

    }

    void AssignVertices(Node[] points) {
        for (int i = 0; i < points.Length; i++) {
            if (points[i].VertexIndex == -1) {
                points[i].VertexIndex = Vertices.Count;
                Vertices.Add(points[i].Position);
            }
        }
    }

    void CreateTriangle(Node a, Node b, Node c) {
        Triangles.Add(a.VertexIndex);
        Triangles.Add(b.VertexIndex);
        Triangles.Add(c.VertexIndex);

        Triangle triangle = new Triangle(a.VertexIndex, b.VertexIndex, c.VertexIndex);
        AddTriangleToDict(triangle.VertexIdA, triangle);
        AddTriangleToDict(triangle.VertexIdB, triangle);
        AddTriangleToDict(triangle.VertexIdC, triangle);
    }

    void AddTriangleToDict(int vertexIdKey, Triangle triangle) {
        if (TriangleDictionary.ContainsKey(vertexIdKey)) {
            TriangleDictionary[vertexIdKey].Add(triangle);
        } else {
            List<Triangle> list = new List<Triangle>();
            list.Add(triangle);
            TriangleDictionary.Add(vertexIdKey, list);
        }
    }

    void CalculateOutlines() {
        for (int vertexIndex = 0; vertexIndex < Vertices.Count; vertexIndex++) {
            if (!CheckedVerts.Contains(vertexIndex)) {
                int newOutlineVert = GetConnectedOutlineVerts(vertexIndex);
                if (newOutlineVert != -1) {
                    CheckedVerts.Add(vertexIndex);
                    List<int> newOutline = new List<int>();
                    newOutline.Add(vertexIndex);
                    Outlines.Add(newOutline);
                    FollowOutline(newOutlineVert, Outlines.Count - 1);
                    Outlines[Outlines.Count - 1].Add(vertexIndex);
                }
            }
        }
    }

    void FollowOutline(int vertexIndex, int outlineIndex) {
        Outlines[outlineIndex].Add(vertexIndex);
        CheckedVerts.Add(vertexIndex);
        int nextVertexIndex = GetConnectedOutlineVerts(vertexIndex);
        if (nextVertexIndex != -1) {
            FollowOutline(nextVertexIndex, outlineIndex);
        }
    }

    int GetConnectedOutlineVerts(int vertexIndex) {
        List<Triangle> triangleList = TriangleDictionary[vertexIndex];

        for (int i = 0; i < triangleList.Count; i++) {
            Triangle triangle = triangleList[i];
            for (int j = 0; j < 3; j++) {
                int vertexB = triangle[j];
                if (vertexB != vertexIndex && !CheckedVerts.Contains(vertexB)) {
                    if (IsEdge(vertexIndex, vertexB)) {
                        return vertexB;
                    }
                }
            }
        }

        return -1;
    }

    bool IsEdge(int vertexA, int vertexB) {
        List<Triangle> aList = TriangleDictionary[vertexA];
        int sharedTriangleCount = 0;

        for (int i = 0; i < aList.Count; i++) {
            if (aList[i].Containts(vertexB)) {
                ++sharedTriangleCount;
                if (sharedTriangleCount >= 1) {
                    break;
                }
            }

        }
        return sharedTriangleCount == 1;
    }
}
