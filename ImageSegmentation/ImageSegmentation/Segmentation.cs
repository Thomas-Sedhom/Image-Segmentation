using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ImageTemplate
{


    public struct Node
    {
        public int x, y;
        public List<KeyValuePair<Node, int>> children;
        private int component;

        public Node(int x, int y)
        {
            this.x = x;
            this.y = y;
            children = new List<KeyValuePair<Node, int>>();
            component = -1;
        }

    }
    public struct Component
    {
        // Nodes in a single Component
        public List<Node> nodes;
        public int maxInternalDifference;

    }

    internal class Segmentation
    {


        public static Node[,] GraphConstruct(RGBPixel[,] ImageMatrix, string color)
        {
            RGBPixel[,] filteredImageMatrix = ImageOperations.GaussianFilter1D(ImageMatrix, 4, 0.8);
            Node[,] graph = new Node[ImageMatrix.GetLength(0), ImageMatrix.GetLength(1)];

            for (int i = 0; i < filteredImageMatrix.GetLength(0); i++)
            {
                for (int j = 0; j < filteredImageMatrix.GetLength(1); j++)
                {
                    graph[i, j] = new Node(i, j);
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
                                weight = Math.Abs(filteredImageMatrix[i, j].red - filteredImageMatrix[x, y].red);
                            else if (color == "green")
                                weight = Math.Abs(filteredImageMatrix[i, j].green - filteredImageMatrix[x, y].green);
                            else
                                weight = Math.Abs(filteredImageMatrix[i, j].blue - filteredImageMatrix[x, y].blue);
                            graph[i, j].children.Add(new KeyValuePair<Node, int>(graph[x, y], weight));
                        }
                    }
                }
            }
            return graph;
        }
        public static Node[,] ImageSegmentation(Node[,] graph, string color)
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

            return graph;
        }

        public static int MaxInternalDifference(Node[,] component)
        {

            return -1;
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
