using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Security.Cryptography;
using System.Threading.Tasks;
using static System.Windows.Forms.VisualStyles.VisualStyleElement.TreeView;

namespace ImageTemplate
{

    /*
     * TASK 1: Documentation for analysis and code review
     * TASK 2: intersect the 3 graphs correctly and find each node does it belong to the same component in all 3 graphs or not
     * TASK 3: Output file containing the number of components and size of each.
     * What is this ?
     *   every region is colored with a distinct color and combined with the original picture so the detected regions are easy to see.
     *   
     * Test size images
     * Small: 	   image size O(100’s KBs)
     * Medium: image size O(10’s MBs)
     * Large: 	   image size O(100’s MBs)

     */

    /* 
     * Component = Sub-Graph
     * 
     * Int = Smallest Cost in a Component
     * 
     * Smallest difference between any node from a component and another node in the second component
     * 
     * MInt(C1, C2) = min(Int(C1) + ThreshC1, Int(C2) + ThreshC2)
     * 
     * Thresh = K / (Number of Vertices in a component)
     * 
     * D = true: Sufficient evidence of a real boundary ->  keep regions separate.
     * D = false: Boundary not strong enough ->  regions may be merged.
     */

    public class Node
    {
        public int id;
        public int x, y;
        public List<KeyValuePair<Node, int>> children;

        public Node(int id, int x, int y)
        {
            this.id = id;
            this.x = x;
            this.y = y;

            children = new List<KeyValuePair<Node, int>>();
        }
    }
    public class Edge
    {
        public (int x, int y) From;
        public (int x, int y) To;
        public int Weight;

        public Edge((int x, int y) from, (int x, int y) to, int weight)
        {
            From = from;
            To = to;
            Weight = weight;
        }
    }

    internal class Segmentation
    {


        // Exact(N^2)
        private static (Dictionary<(int, int), List<Edge>> adjacencyList, List<Edge> sortedEdges) GraphConstruct(RGBPixel[,] ImageMatrix, string color)
        {
            int[] dx = { -1, -1, -1, 0, 0, 1, 1, 1 };
            int[] dy = { -1, 0, 1, -1, 1, -1, 0, 1 };

            int height = ImageMatrix.GetLength(0);
            int width = ImageMatrix.GetLength(1);

            var graph = new Dictionary<(int, int), List<Edge>>();
            var edgeSet = new HashSet<(int, int, int, int)>();  // To prevent duplicates
            var allEdges = new List<Edge>();

            for (int i = 0; i < height; i++)
            {
                for (int j = 0; j < width; j++)
                {
                    var currentKey = (i, j);
                    if (!graph.ContainsKey(currentKey))
                        graph[currentKey] = new List<Edge>();

                    for (int k = 0; k < 8; k++)
                    {
                        int x = i + dx[k];
                        int y = j + dy[k];
                        var neighborKey = (x, y);

                        if (x >= 0 && x < height && y >= 0 && y < width)
                        {
                            // Avoid duplicate edges by enforcing (min, max) order
                            var edgeId = (Math.Min(i, x), Math.Min(j, y), Math.Max(i, x), Math.Max(j, y));
                            if (edgeSet.Contains(edgeId))
                                continue;

                            edgeSet.Add(edgeId);

                            int weight;
                            if (color == "red")
                                weight = Math.Abs((int)ImageMatrix[i, j].red - (int)ImageMatrix[x, y].red);
                            else if (color == "green")
                                weight = Math.Abs((int)ImageMatrix[i, j].green - (int)ImageMatrix[x, y].green);
                            else
                                weight = Math.Abs((int)ImageMatrix[i, j].blue - (int)ImageMatrix[x, y].blue);

                            // Create undirected edge (stored once)
                            var edge = new Edge(currentKey, neighborKey, weight);
                            allEdges.Add(edge);

                            // Add to adjacency list both ways
                            graph[currentKey].Add(new Edge(currentKey, neighborKey, weight));
                            if (!graph.ContainsKey(neighborKey))
                                graph[neighborKey] = new List<Edge>();
                            graph[neighborKey].Add(new Edge(neighborKey, currentKey, weight));
                        }
                    }
                }
            }

            // Sort global edge list by weight
            allEdges.Sort((a, b) => a.Weight.CompareTo(b.Weight));

            return (graph, allEdges);
        }


        // Exact(M)
        private static DisjointSet ImageSegmentation(
            Dictionary<(int, int), List<Edge>> adjacencyList, List<Edge> sortedEdges, int height, int width)
        {
            int totalPixels = height * width;
            DisjointSet dsu = new DisjointSet(totalPixels);

            foreach (var edge in sortedEdges)
            {
                int fromId = edge.From.x * width + edge.From.y;
                int toId = edge.To.x * width + edge.To.y;

                dsu.Union(fromId, toId, edge.Weight);
            }

            return dsu;
        }
        private static RGBPixel GetColorForSegment(int id)
        {
            Random rand = new Random(id);
            return new RGBPixel
            {
                red = (byte)rand.Next(256),
                green = (byte)rand.Next(256),
                blue = (byte)rand.Next(256)
            };
        }
        public static void ImageProcess(ref RGBPixel[,] imageMatrix)
        {
            // Get the height and width of the image matrix
            int height = imageMatrix.GetLength(0);
            int width = imageMatrix.GetLength(1);

            // Graphs for each color
            var (graphRed, sortedRedEdges) = GraphConstruct(imageMatrix, "red");
            var (graphGreen, sortedGreenEdges) = GraphConstruct(imageMatrix, "green");
            var (graphBlue, sortedBlueEdges) = GraphConstruct(imageMatrix, "blue");

            // Segment the image for each color using the adjacency list and sorted edges
            DisjointSet redSegments = ImageSegmentation(graphRed, sortedRedEdges, height, width);
            DisjointSet greenSegments = ImageSegmentation(graphGreen, sortedGreenEdges, height, width);
            DisjointSet blueSegments = ImageSegmentation(graphBlue, sortedBlueEdges, height, width);

            // Log the segmentation info (optional)
            LogSegmentInfo("Red", redSegments);
            LogSegmentInfo("Green", greenSegments);
            LogSegmentInfo("Blue", blueSegments);

            // Intersect the 3 label maps for the final segmentation
            var segmentColorsMap = new Dictionary<(int, int, int), RGBPixel>();

            // Traverse the image to assign new colors based on the segment IDs
            for (int i = 0; i < height; i++)
            {
                for (int j = 0; j < width; j++)
                {
                    // Access the ID of the pixel in each color channel (flattened ID)
                    int redId = redSegments.Find(i * width + j);  // Use flattened ID for red
                    int greenId = greenSegments.Find(i * width + j);  // Use flattened ID for green
                    int blueId = blueSegments.Find(i * width + j);  // Use flattened ID for blue

                    var combinedId = (redId, greenId, blueId);

                    // If the combined segment does not exist, create a new color for it
                    if (!segmentColorsMap.ContainsKey(combinedId))
                        segmentColorsMap[combinedId] = GetColorForSegment(segmentColorsMap.Count);

                    // Update the pixel with the color corresponding to its segment
                    imageMatrix[i, j] = segmentColorsMap[combinedId];
                }
            }
        }



        // Worst Case: Exact(N^2)
        private static void LogSegmentInfo(string color, DisjointSet segments)
        {
            Console.WriteLine($"{color} Segments: {segments.uniqueComponents.Count}");
            foreach (var component in segments.uniqueComponents)
            {
                Console.WriteLine($"  Size: {segments.size[component]}");
            }
        }

    }
    // bool isRedEqualGreen = (idRed == idGreen);
    // bool isRedEqualBlue = (idRed == idBlue);
    //if (idRed != idGreen || idRed != idBlue) 
    //{
    //    ImageMatrix[i, j] = new RGBPixel { red = 0, green = 0, blue = 0 };
    //}

}
