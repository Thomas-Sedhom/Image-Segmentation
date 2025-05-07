using System;
using System.Collections.Generic;

namespace ImageTemplate
{


    public class DisjointSet
    {
        private readonly int K_CONSTANT = 1;

        // We first assume that each component is a single node

        /* 
         * After that we start merging the components based on 
         * the comparison of the minimum internal difference
         * and the difference between two components.
         * 
         * Dif(c1,c2) <= MinInt(c1, c2) then MERGE
         * else do nothing.
         * 
         */
        public HashSet<int> uniqueComponents;
        private int[] parent, rank;

        // Storing the max internal difference of each component
        private int[] size;
        private int[] InternalDifference;


        public DisjointSet(int n)
        {
            parent = new int[n];
            uniqueComponents = new HashSet<int>(n);

            rank = new int[n];

            // Size of each component, difference between size and rank is that rank is incremented by 1 when Merging
            size = new int[n];
            InternalDifference = new int[n];

            for (int i = 0; i < n; i++)
            {
                parent[i] = i;
                uniqueComponents.Add(i);

                rank[i] = 1;

                size[i] = 1;
                InternalDifference[i] = 0;
            }
        }

        public int Find(int i)
        {
            if (parent[i] != i)
            {
                parent[i] = Find(parent[i]);
            }
            return parent[i];
        }

        public void Union(int x, int y, int weight)
        {
            int c1 = Find(x);
            int c2 = Find(y);
            if (c1 != c2 && canUnion(c1, c2, weight))
            {
                int newParent;
                if (rank[c1] < rank[c2])
                {
                    parent[c1] = c2;
                    uniqueComponents.Remove(c1);
                    newParent = c2;
                    size[c2] += size[c1];
                }
                else if (rank[c1] > rank[c2])
                {
                    parent[c2] = c1;
                    uniqueComponents.Remove(c2);
                    newParent = c1;

                    size[c1] += size[c2];
                }
                else
                {
                    parent[c2] = c1;
                    uniqueComponents.Remove(c2);
                    newParent = c1;

                    rank[c1]++;
                    size[c1] += size[c2];
                }

                InternalDifference[newParent] = Math.Max(
                    weight,
                    Math.Max(InternalDifference[c1], InternalDifference[c2])
                );

                Console.WriteLine($"Merged: {c1} & {c2} → Parent: {newParent} | Size: {size[newParent]} | InternalDiff: {InternalDifference[newParent]}");
            }
        }

        private bool canUnion(int c1, int c2, int weight)
        {
            if (weight > getMinDifference(c1, c2, K_CONSTANT))
                return false;
            return true;
        }

        private double getMinDifference(int c1, int c2, int K)
        {
            double T_c1 = K / size[c1];
            double T_c2 = K / size[c2];
            double minInt = Math.Min(InternalDifference[c1] + T_c1, InternalDifference[c2] + T_c2);
            return minInt;
        }


    }
}



