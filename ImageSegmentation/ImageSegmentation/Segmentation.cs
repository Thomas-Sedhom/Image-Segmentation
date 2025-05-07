using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using static System.Windows.Forms.VisualStyles.VisualStyleElement.TreeView;

namespace ImageTemplate
{

    public class Node
    {
        public int id;
        public int x, y;
        public List<KeyValuePair<Node, int>> children;
        private int component;

        public Node(int id, int x, int y)
        {
            this.id = id;
            this.x = x;
            this.y = y;

            children = new List<KeyValuePair<Node, int>>();
            component = -1;
        }

    }

    internal class Segmentation
    {
        public static Node[,] GraphConstruct(RGBPixel[,] ImageMatrix, string color)
        {
            Node[,] graph = new Node[ImageMatrix.GetLength(0), ImageMatrix.GetLength(1)];

            int uuid = 0;
            for (int i = 0; i < ImageMatrix.GetLength(0); i++)
            {
                for (int j = 0; j < ImageMatrix.GetLength(1); j++)
                {
                    graph[i, j] = new Node(uuid++, i, j);
                }
            }

            int[] dx = { -1, -1, -1, 0, 0, 1, 1, 1 };
            int[] dy = { -1, 0, 1, -1, 1, -1, 0, 1 };

            for (int i = 0; i < ImageMatrix.GetLength(0); i++)
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
                            if(weight > 0)
                                Console.WriteLine( " x: " + x + " y: "+ y + "  i: " + i + " j: " + j + " weight " + weight);
                            graph[i, j].children.Add(new KeyValuePair<Node, int>(graph[x, y], weight));
                        }
                    }
                }
            }
            return graph;
        }
        public static DisjointSet ImageSegmentation(Node[,] graph)
        {
            List<KeyValuePair<int, KeyValuePair<Node, Node>>> nodes = new List<KeyValuePair<int, KeyValuePair<Node, Node>>>();
            for (int i = 0; i < graph.GetLength(0); i++)
            {
                for (int j = 0; j < graph.GetLength(1); j++)
                {
                    foreach (KeyValuePair<Node, int> child in graph[i, j].children)
                    {
                        nodes.Add(
                            new KeyValuePair<int, KeyValuePair<Node, Node>>(
                                child.Value,
                                new KeyValuePair<Node, Node>(child.Key, graph[i, j])
                            )
                        );
                    }
                }
            }
            nodes.Sort((a, b) => a.Key.CompareTo(b.Key));

            DisjointSet dsu = new DisjointSet(graph.GetLength(0) * graph.GetLength(1));
            foreach (KeyValuePair<int, KeyValuePair<Node, Node>> child in nodes)
            {
                dsu.Union(child.Value.Key.id, child.Value.Value.id, child.Key);
            }
            return dsu;
        }
        public static RGBPixel GetColorForSegment(int id)
        {
            Random rand = new Random(id);
            return new RGBPixel
            {
                red = (byte)rand.Next(256),
                green = (byte)rand.Next(256),
                blue = (byte)rand.Next(256)
            };
        }
        public static void ImageProcess(ref RGBPixel[,] ImageMatrix)
        {
            Node[,] graphRed = GraphConstruct(ImageMatrix, "red");
            Node[,] graphGreen = GraphConstruct(ImageMatrix, "green");
            Node[,] graphBlue = GraphConstruct(ImageMatrix, "blue");

            DisjointSet resRed = ImageSegmentation(graphRed);
            DisjointSet resBlue = ImageSegmentation(graphBlue);
            DisjointSet resGreen = ImageSegmentation(graphGreen);

            // Console.WriteLine("Number of Components: " + resRed.uniqueComponents.Count);
            Console.WriteLine("Red Segments: " + resRed.uniqueComponents.Count);
            Console.WriteLine("Green Segments: " + resGreen.uniqueComponents.Count);
            Console.WriteLine("Blue Segments: " + resBlue.uniqueComponents.Count);
            //Console.WriteLine("Size: " + size[c1]);
            //Console.WriteLine("Size: " + size[c2]);

            for (int i = 0; i < graphRed.GetLength(0); i++)
            {
                for (int j = 0; j < graphRed.GetLength(1); j++)
                {
                    int idRed = resRed.Find(graphRed[i, j].id);
                    int idGreen = resGreen.Find(graphGreen[i, j].id);
                    int idBlue = resBlue.Find(graphBlue[i, j].id);

                    // bool isRedEqualGreen = (idRed == idGreen);
                    // bool isRedEqualBlue = (idRed == idBlue);
                    if (idRed != idGreen || idRed != idBlue) 
                    {
                        ImageMatrix[i, j] = new RGBPixel { red = 0, green = 0, blue = 0 }; // Not in same component across all channels
                    }
                    else
                    {
                        ImageMatrix[i, j] = GetColorForSegment(idRed); // All labels match — valid region

                    }
                }
            }

            return;
        }

    }
}



/*
 * 
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
