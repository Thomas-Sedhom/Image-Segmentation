using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
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

    //public class Node
    //{
    //    public int id;
    //    public int x, y;

    //    public Node(int id, int x, int y)
    //    {
    //        this.id = id;
    //        this.x = x;
    //        this.y = y;
    //    }
    //}

    public readonly struct Edge
    {
        public int V1 { get; }
        public int V2 { get; }
        public byte Weight { get; }

        public Edge(int V1, int V2, byte weight)
        {
            this.V1 = V1;
            this.V2 = V2;
            Weight = weight;
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
        public SegmentInfo(RGBPixel color, int size)
        {
            Color = color;
            Size = size;
        }
    }

    class Segmentation
    {
        private static ushort width, height;
        private static readonly Random rand = new Random();

        private static DisjointSet intersectionSegments;
        private static Dictionary<int, SegmentInfo> segmentColorsMap;

        // Θ(N²), assuming width and height are the same value, equivalent to O(N*M), where N and M are the dimensions of the image
        private static List<Edge> GraphConstruct(RGBPixel[,] ImageMatrix, string color)
        {
            width = (ushort)ImageMatrix.GetLength(0);
            height = (ushort)ImageMatrix.GetLength(1);

            sbyte[] dx = { -1, -1, -1, 0, 0, 1, 1, 1 };
            sbyte[] dy = { -1, 0, 1, -1, 1, -1, 0, 1 };

            List<Edge> edges = new List<Edge>();
            for (ushort i = 0; i < width; i++)
            {
                for (ushort j = 0; j < height; j++)
                {
                    int currentId = i * height + j;

                    for (byte k = 0; k < 8; k++)
                    {
                        ushort x = (ushort)(i + dx[k]);
                        ushort y = (ushort)(j + dy[k]);

                        if (x >= 0 && x < width && y >= 0 && y < height)
                        {
                            int neighborId = x * height + y;

                            byte weight;
                            if (color == "red")
                                weight = (byte)Math.Abs(ImageMatrix[i, j].red - ImageMatrix[x, y].red);
                            else if (color == "green")
                                weight = (byte)Math.Abs(ImageMatrix[i, j].green - ImageMatrix[x, y].green);
                            else
                                weight = (byte)Math.Abs(ImageMatrix[i, j].blue - ImageMatrix[x, y].blue);
                            edges.Add(new Edge(currentId, neighborId, weight));
                        }

                    }
                }
            }
            return edges;
        }

        private static void CountingSortEdges(List<Edge> edges, byte maxWeight)
        {
            int[] count = new int[maxWeight + 1];

            // Step 1: Count frequencies
            foreach (var edge in edges)
            {
                count[edge.Weight]++;
            }

            // Step 2: Compute prefix sums to know starting index for each weight
            for (int i = 1; i <= maxWeight; i++)
            {
                count[i] += count[i - 1];
            }

            // Step 3: Place items into correct positions (stable sort)
            Edge[] output = new Edge[edges.Count];
            for (int i = edges.Count - 1; i >= 0; i--)
            {
                var edge = edges[i];
                int pos = --count[edge.Weight];
                output[pos] = edge;
            }

            // Step 4: Copy sorted data back to original list
            for (int i = 0; i < edges.Count; i++)
            {
                edges[i] = output[i];
            }
        }




        // O(M log M), where M is the number of edges that is = width * height * 8
        private static DisjointSet ImageSegmentation(List<Edge> edges)
        {
            byte maxWeight = 255;
            CountingSortEdges(edges, maxWeight);

            DisjointSet dsu = new DisjointSet(width * height);

            foreach (var edge in edges)
            {
                dsu.Union(edge.V1, edge.V2, edge.Weight);
            }

            return dsu;
        }

        // O(1)
        private static RGBPixel GetColorForSegment()
        {
            return new RGBPixel
            {
                red = (byte)rand.Next(256),
                green = (byte)rand.Next(256),
                blue = (byte)rand.Next(256)
            };
        }

        private static async Task<List<Edge>[]> ConstructGraphs(RGBPixel[,] imageMatrix)
        {
            try
            {
                var t1 = Task.Run(() => GraphConstruct(imageMatrix, "red"));
                var t2 = Task.Run(() => GraphConstruct(imageMatrix, "green"));
                var t3 = Task.Run(() => GraphConstruct(imageMatrix, "blue"));

                return await Task.WhenAll(t1, t2, t3);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error in ConstructGraphs: {ex.Message}");
                throw;
            }
        }


        private static async Task<DisjointSet[]> SegmentGraphs(List<Edge>[] graphs)
        {
            try
            {
                var tRed = Task.Run(() => ImageSegmentation(graphs[0]));
                var tGreen = Task.Run(() => ImageSegmentation(graphs[1]));
                var tBlue = Task.Run(() => ImageSegmentation(graphs[2]));

                DisjointSet[] results = await Task.WhenAll(tRed, tGreen, tBlue);
                return results;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error in SegmentGraphs: {ex.Message}");
                throw;
            }

        }



        public static async Task<RGBPixel[,]> ImageProcess(RGBPixel[,] imageMatrix)
        {
            Stopwatch stopwatch = new Stopwatch();
            stopwatch.Start();
            Stopwatch graphStopwatch = new Stopwatch();
            graphStopwatch.Start();

            // Construct graphs
            List<Edge>[] graphs = await ConstructGraphs(imageMatrix);

            graphStopwatch.Stop();
            Debug.WriteLine($"Time of graph construction: {graphStopwatch.ElapsedMilliseconds} ms");

            Stopwatch segmentGraphStopwatch = new Stopwatch();
            segmentGraphStopwatch.Start();

            // Segment the image for each channel
            DisjointSet[] segmentResults = await SegmentGraphs(graphs);

            segmentGraphStopwatch.Stop();
            Debug.WriteLine($"Time of graph segmentation: {segmentGraphStopwatch.ElapsedMilliseconds} ms");

            DisjointSet redSegments = segmentResults[0];
            DisjointSet greenSegments = segmentResults[1];
            DisjointSet blueSegments = segmentResults[2];

            segmentResults = null;
            GC.Collect();



            // intersecting the 3 label maps by checking if the segments are equal
            intersectionSegments = new DisjointSet(width * height);
            sbyte[] dx = { -1, -1, -1, 0, 0, 1, 1, 1 };
            sbyte[] dy = { -1, 0, 1, -1, 1, -1, 0, 1 };


            Parallel.For(0, width * height, currentIndex =>
            {

                ushort i = (ushort)(currentIndex / height);
                ushort j = (ushort)(currentIndex % height);

                for (byte k = 0; k < 8; k++)
                {
                    int x = i + dx[k];
                    int y = j + dy[k];

                    if (x >= 0 && x < width && y >= 0 && y < height)
                    {
                        int neighborIndex = x * height + y;

                        int redId1 = redSegments.Find(currentIndex);
                        int redId2 = redSegments.Find(neighborIndex);

                        int greenId1 = greenSegments.Find(currentIndex);
                        int greenId2 = greenSegments.Find(neighborIndex);

                        int blueId1 = blueSegments.Find(currentIndex);
                        int blueId2 = blueSegments.Find(neighborIndex);

                        if (redId1 == redId2 && greenId1 == greenId2 && blueId1 == blueId2)
                        {
                            intersectionSegments.Union(currentIndex, neighborIndex, 0);
                        }
                    }

                }
            });
            redSegments = greenSegments = blueSegments = null;
            GC.Collect();
            Stopwatch dictStopwatch = new Stopwatch();
            dictStopwatch.Start();

            // Assign a color per segment
            segmentColorsMap = new Dictionary<int, SegmentInfo>();

            for (int index = 0; index < width * height; index++)
            {
                int i = index / height;
                int j = index % height;

                int segmentId = intersectionSegments.Find(index);

                if (!segmentColorsMap.ContainsKey(segmentId))
                    segmentColorsMap[segmentId] = new SegmentInfo(GetColorForSegment());

                imageMatrix[i, j] = segmentColorsMap[segmentId].Color;
                segmentColorsMap[segmentId].Size++;
            }


            segmentColorsMap = segmentColorsMap.OrderByDescending(x => x.Value.Size).ToDictionary(x => x.Key, x => x.Value);

            dictStopwatch.Stop();
            Debug.WriteLine($"Time of dict: {dictStopwatch.ElapsedMilliseconds} ms");
            stopwatch.Stop();
            Debug.WriteLine($"Total time: {stopwatch.ElapsedMilliseconds} ms");
            //Debug.WriteLine($"Segments Total: {segmentColorsMap.Count}");
            //foreach (var segment in segmentColorsMap)
            //    Debug.WriteLine($"Segment Size: {segment.Value.Size}");

            return imageMatrix;
        }

        public static void LogRegionInfoToFile(string filePath = "logs/output.txt")
        {
            try
            {

                string directory = Path.GetDirectoryName(filePath);
                if (!Directory.Exists(directory))
                {
                    Directory.CreateDirectory(directory);
                }
                using (StreamWriter writer = new StreamWriter(filePath))
                {
                    writer.WriteLine($"{segmentColorsMap.Count}");

                    foreach (var segment in segmentColorsMap)
                        writer.WriteLine($"{segment.Value.Size}");
                    //Segment ID: { segment.Key}, Size:
                }

                Debug.WriteLine("Segment info successfully written to file.");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error writing segment info to file: {ex.Message}");
            }
        }

        public static void MergeTwoSegments(ref RGBPixel[,] imageMatrix, int seg1, int seg2)
        {
            if (seg1 == seg2 || seg1 == -1 || seg2 == -1)
            {
                MessageBox.Show("Please select two different regions to merge.", "Malfunction", MessageBoxButtons.OK, MessageBoxIcon.Stop);
                return;
            }
            if (imageMatrix == null || segmentColorsMap == null)
            {
                MessageBox.Show("Open an Image and Segment it first.", "Malfunction", MessageBoxButtons.OK, MessageBoxIcon.Stop);
                return;
            }

            int r1 = intersectionSegments.Find(seg1);
            int r2 = intersectionSegments.Find(seg2);
            if (r1 == r2)
            {
                MessageBox.Show("Select two difference regions", "Malfunction", MessageBoxButtons.OK, MessageBoxIcon.Stop);
                return;
            }
            SegmentInfo segInfo1 = segmentColorsMap[r1];
            SegmentInfo segInfo2 = segmentColorsMap[r2];

            if (segmentColorsMap.ContainsKey(r2))
                segmentColorsMap.Remove(r2);

            intersectionSegments.Union(r1, r2, 0);

            int totalSize = segInfo1.Size + segInfo2.Size;
            int newRoot = intersectionSegments.Find(seg1);

            segmentColorsMap[newRoot] = new SegmentInfo(segInfo1.Color, totalSize);


            for (int i = 0; i < width; i++)
            {
                for (int j = 0; j < height; j++)
                {
                    int pixelId = i * height + j;
                    if (intersectionSegments.Find(pixelId) == newRoot)
                    {
                        imageMatrix[i, j] = segmentColorsMap[newRoot].Color;
                    }
                }
            }
            Console.WriteLine($"Merged segments {seg1} and {seg2} into segment {r1}");
        }

        //private static void LogSegmentInfo(string color, DisjointSet segments)
        //{
        //    Console.WriteLine($"{color} Segments: {segments.uniqueComponents.Count}");
        //    foreach (var component in segments.uniqueComponents)
        //    {
        //        Console.WriteLine($"  Size: {segments.size[component]}");
        //    }
        //}

    }

}
