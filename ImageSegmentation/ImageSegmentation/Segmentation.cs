using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Security.Cryptography;
using System.Threading;
using System.Threading.Tasks;
using static System.Windows.Forms.VisualStyles.VisualStyleElement.Rebar;

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
        public List<KeyValuePair<int, int>> children;

        public Node(int id, int x, int y)
        {
            this.id = id;
            this.x = x;
            this.y = y;

            children = new List<KeyValuePair<int, int>>();
        }
    }

    class SegmentInfo
    {
        public RGBPixel Color { get; set; }
        public int Size { get; set; }

        public SegmentInfo(RGBPixel color)
        {
            Color = color;
            Size = 0;
        }
    }

    internal class Segmentation
    {

        // Exact(N^2)
        private static Node[,] GraphConstruct(RGBPixel[,] ImageMatrix, string color)
        {
            Node[,] graph = new Node[ImageMatrix.GetLength(0), ImageMatrix.GetLength(1)];
            int uuid = 0;

            //Parallel.For(0, ImageMatrix.GetLength(0), i =>
            //{
            for (int i = 0; i < graph.GetLength(0); i++)
            {
                for (int j = 0; j < ImageMatrix.GetLength(1); j++)
                {
                    graph[i, j] = new Node(uuid++, i, j);
                }
            }

            int[] dx = { -1, -1, -1, 0, 0, 1, 1, 1 };
            int[] dy = { -1, 0, 1, -1, 1, -1, 0, 1 };

            Parallel.For(0, ImageMatrix.GetLength(0), i =>
            {
                for (int j = 0; j < ImageMatrix.GetLength(1); j++)
                {
                    for (int k = 0; k < 8; k++)
                    {
                        int x = i + dx[k];
                        int y = j + dy[k];

                        if (x >= 0 && x < ImageMatrix.GetLength(0) && y >= 0 && y < ImageMatrix.GetLength(1))
                        {
                            int weight;
                            if (color == "red")
                                weight = Math.Abs(ImageMatrix[i, j].red - ImageMatrix[x, y].red);
                            else if (color == "green")
                                weight = Math.Abs(ImageMatrix[i, j].green - ImageMatrix[x, y].green);
                            else
                                weight = Math.Abs(ImageMatrix[i, j].blue - ImageMatrix[x, y].blue);
                            //if (weight > 0)
                            //    Console.WriteLine(" x: " + x + " y: " + y + "  i: " + i + " j: " + j + " weight " + weight);
                            graph[i, j].children.Add(new KeyValuePair<int, int>(graph[x, y].id, weight));
                        }
                    }
                }
            });
            return graph;
        }

        // Exact(M)
        private static DisjointSet ImageSegmentation(Node[,] graph)
        {
            List<KeyValuePair<int, KeyValuePair<int, int>>> nodes = new List<KeyValuePair<int, KeyValuePair<int, int>>>();
            for (int i = 0; i < graph.GetLength(0); i++)
            {
                for (int j = 0; j < graph.GetLength(1); j++)
                {
                    foreach (KeyValuePair<int, int> child in graph[i, j].children)
                    {
                        //if (child.Key > graph[i, j].id)
                        {
                            nodes.Add(
                            new KeyValuePair<int, KeyValuePair<int, int>>(
                                child.Value,
                                new KeyValuePair<int, int>(child.Key, graph[i, j].id)
                                )
                            );
                        }
                    }
                }
            }
            nodes.Sort((a, b) => a.Key.CompareTo(b.Key));

            DisjointSet dsu = new DisjointSet(graph.GetLength(0) * graph.GetLength(1));
            // Exact(M * log(M))
            foreach (KeyValuePair<int, KeyValuePair<int, int>> child in nodes)
            {
                dsu.Union(child.Value.Key, child.Value.Value, child.Key);
            }
            return dsu;
        }

        private static RGBPixel GetColorForSegment(Random rand)
        {
            return new RGBPixel
            {
                red = (byte)rand.Next(256),
                green = (byte)rand.Next(256),
                blue = (byte)rand.Next(256)
            };
        }

        //private static RGBPixel GetColorForSegment(int id)
        //{
        //    double hue = (id * 137.508) % 360; // Golden angle approximation
        //    return HSVtoRGB(hue, 1, 1);
        //}

        //private static RGBPixel HSVtoRGB(double h, double s, double v)
        //{
        //    double c = v * s;
        //    double x = c * (1 - Math.Abs((h / 60 % 2) - 1));
        //    double m = v - c;
        //    double r = 0, g = 0, b = 0;

        //    if (h < 60) { r = c; g = x; b = 0; }
        //    else if (h < 120) { r = x; g = c; b = 0; }
        //    else if (h < 180) { r = 0; g = c; b = x; }
        //    else if (h < 240) { r = 0; g = x; b = c; }
        //    else if (h < 300) { r = x; g = 0; b = c; }
        //    else { r = c; g = 0; b = x; }

        //    return new RGBPixel
        //    {
        //        red = (byte)((r + m) * 255),
        //        green = (byte)((g + m) * 255),
        //        blue = (byte)((b + m) * 255)
        //    };
        //}

        private static async Task<List<Node[,]>> ConstructGraphs(RGBPixel[,] imageMatrix)
        {
            try
            {
                var t1 = Task.Run(() => GraphConstruct(imageMatrix, "red"));
                var t2 = Task.Run(() => GraphConstruct(imageMatrix, "green"));
                var t3 = Task.Run(() => GraphConstruct(imageMatrix, "blue"));

                Node[][,] results = await Task.WhenAll(t1, t2, t3);
                return results.ToList();
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error in ConstructGraphs: {ex.Message}");
                throw;
            }
        }

        private static async Task<List<DisjointSet>> SegmentGraphs(Node[,] graphRed, Node[,] graphGreen, Node[,] graphBlue)
        {
            var tRed = Task.Run(() => ImageSegmentation(graphRed));
            var tGreen = Task.Run(() => ImageSegmentation(graphGreen));
            var tBlue = Task.Run(() => ImageSegmentation(graphBlue));

            DisjointSet[] results = await Task.WhenAll(tRed, tGreen, tBlue);
            return results.ToList();
        }


        public static async Task<RGBPixel[,]> ImageProcess(RGBPixel[,] imageMatrix)
        {
            Stopwatch stopwatch = new Stopwatch();
            stopwatch.Start();

            // Construct graphs for each of R, G, B channels
            List<Node[,]> graphs = await ConstructGraphs(imageMatrix);
            Node[,] graphRed = graphs[0];
            Node[,] graphGreen = graphs[1];
            Node[,] graphBlue = graphs[2];

            // Segment the image for each channel
            List<DisjointSet> segmentResults = await SegmentGraphs(graphRed, graphGreen, graphBlue);
            DisjointSet redSegments = segmentResults[0];
            DisjointSet greenSegments = segmentResults[1];
            DisjointSet blueSegments = segmentResults[2];

            int height = imageMatrix.GetLength(0);
            int width = imageMatrix.GetLength(1);

            // Build final intersection disjoint set based on 8-neighbor agreement
            DisjointSet intersectionSegments = new DisjointSet(height * width);
            int[] dx = { -1, -1, -1, 0, 0, 1, 1, 1 };
            int[] dy = { -1, 0, 1, -1, 1, -1, 0, 1 };

            Parallel.For(0, height, i =>
            {
                for (int j = 0; j < width; j++)
                {
                    for (int k = 0; k < 8; k++)
                    {
                        int x = i + dx[k];
                        int y = j + dy[k];

                        if (x >= 0 && x < height && y >= 0 && y < width)
                        {
                            int currentIndex = i * width + j;
                            int redId = redSegments.Find(graphRed[i, j].id);
                            int greenId = greenSegments.Find(graphGreen[i, j].id);
                            int blueId = blueSegments.Find(graphBlue[i, j].id);

                            int neighborRedId = redSegments.Find(graphRed[x, y].id);
                            int neighborGreenId = greenSegments.Find(graphGreen[x, y].id);
                            int neighborBlueId = blueSegments.Find(graphBlue[x, y].id);

                            if (redId == neighborRedId && greenId == neighborGreenId && blueId == neighborBlueId)
                            {
                                int neighborIndex = x * width + y;
                                // Join them
                                intersectionSegments.Union(currentIndex, neighborIndex, 0);
                            }

                        }
                    }
                }
            });


            Dictionary<int, SegmentInfo> segmentColorsMap = new Dictionary<int, SegmentInfo>();
            Random rand = new Random();
            for (int i = 0; i < height; i++)
            {
                for (int j = 0; j < width; j++)
                {
                    int index = i * width + j;
                    int segmentId = intersectionSegments.Find(index);

                    if (!segmentColorsMap.ContainsKey(segmentId))
                        segmentColorsMap[segmentId] = new SegmentInfo(GetColorForSegment(rand));

                    imageMatrix[i, j] = segmentColorsMap[segmentId].Color;
                    segmentColorsMap[segmentId].Size++;
                }
            }

            // Sort segments (optional)
            segmentColorsMap = segmentColorsMap.OrderByDescending(x => x.Value.Size)
                                               .ToDictionary(x => x.Key, x => x.Value);

            stopwatch.Stop();
            Debug.WriteLine($"Total Time: {stopwatch.ElapsedMilliseconds} ms");
            Debug.WriteLine($"Segments Total: {segmentColorsMap.Count}");
            foreach (var segment in segmentColorsMap)
                Debug.WriteLine($"Segment Size: {segment.Value.Size}");

            return imageMatrix;
        }



        //public static async Task<RGBPixel[,]> ImageProcess(RGBPixel[,] imageMatrix)
        //{
        //    Stopwatch stopwatch = new Stopwatch();
        //    stopwatch.Start();

        //    // CALL ConstructGraphs here and wait for it to excute to get all graph values and proceed with the rest of the imageProcess function
        //    List<Node[,]> graphs = await ConstructGraphs(imageMatrix);
        //    Node[,] graphRed = graphs[0];
        //    Node[,] graphGreen = graphs[1];
        //    Node[,] graphBlue = graphs[2];

        //    //Node[,] graphRed = GraphConstruct(imageMatrix, "red");
        //    //Node[,] graphGreen = GraphConstruct(imageMatrix, "green");
        //    //Node[,] graphBlue = GraphConstruct(imageMatrix, "blue");


        //    // Segment the image for each color
        //    List<DisjointSet> segmentResults = await SegmentGraphs(graphRed, graphGreen, graphBlue);
        //    //DisjointSet redSegments = ImageSegmentation(graphRed);
        //    //DisjointSet greenSegments = ImageSegmentation(graphGreen);
        //    //DisjointSet blueSegments = ImageSegmentation(graphBlue);

        //    DisjointSet redSegments = segmentResults[0];
        //    DisjointSet greenSegments = segmentResults[1];
        //    DisjointSet blueSegments = segmentResults[2];

        //    //LogSegmentInfo("Red", redSegments);
        //    //LogSegmentInfo("Green", greenSegments);
        //    //LogSegmentInfo("Blue", blueSegments);

        //    // Intersect the 3 label maps
        //    Random random = new Random();
        //    var segmentColorsMap = new Dictionary<RGBPixelD, SegmentInfo>();

        //    int height = imageMatrix.GetLength(0);
        //    int width = imageMatrix.GetLength(1);

        //    // Exact(N^2)
        //    for (int i = 0; i < height; i++)
        //    {
        //        for (int j = 0; j < width; j++)
        //        {
        //            int redId = redSegments.Find(graphRed[i, j].id);
        //            int greenId = greenSegments.Find(graphGreen[i, j].id);
        //            int blueId = blueSegments.Find(graphBlue[i, j].id);

        //            var combinedId = new RGBPixelD
        //            {
        //                red = redId,
        //                green = greenId,
        //                blue = blueId
        //            };

        //            if (!segmentColorsMap.ContainsKey(combinedId))
        //                segmentColorsMap[combinedId] = new SegmentInfo(GetColorForSegment(segmentColorsMap.Count));

        //            if (redId == greenId && greenId == blueId)
        //            {
        //                imageMatrix[i, j] = segmentColorsMap[combinedId].Color;
        //            }
        //            else
        //            {
        //                imageMatrix[i, j] = new RGBPixel
        //                {
        //                    red = (byte)random.Next(256),
        //                    green = (byte)random.Next(256),
        //                    blue = (byte)random.Next(256)
        //                };
        //            }
        //            segmentColorsMap[combinedId].Size++;
        //        }
        //    }
        //    segmentColorsMap = segmentColorsMap.OrderByDescending(x => x.Value.Size).ToDictionary(x => x.Key, x => x.Value);
        //    stopwatch.Stop();
        //    Debug.WriteLine($"Total Time: {stopwatch.ElapsedMilliseconds} ms");

        //    Debug.WriteLine($"Segments Total: {segmentColorsMap.Count}");
        //    foreach (var segment in segmentColorsMap)
        //        Debug.WriteLine($"Segment Size: {segment.Value.Size} | Color: {segment.Key.red}, {segment.Key.green}, {segment.Key.blue}");

        //    return imageMatrix;
        //}


        // Worst Case: Exact(N^2)
        //private static void LogSegmentInfo(string color, DisjointSet segments)
        //{
        //    Console.WriteLine($"{color} Segments: {segments.uniqueComponents.Count}");
        //    foreach (var component in segments.uniqueComponents)
        //    {
        //        Console.WriteLine($"  Size: {segments.size[component]}");
        //    }
        //}

    }
    // bool isRedEqualGreen = (idRed == idGreen);
    // bool isRedEqualBlue = (idRed == idBlue);
    //if (idRed != idGreen || idRed != idBlue) 
    //{
    //    ImageMatrix[i, j] = new RGBPixel { red = 0, green = 0, blue = 0 };
    //}

}
