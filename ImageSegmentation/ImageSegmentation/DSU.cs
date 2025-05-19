using System;
using System.Collections.Generic;
using System.Threading.Tasks;


namespace ImageTemplate
{


    public class DisjointSet
    {

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
        private readonly int K_CONSTANT = int.Parse(MainForm.Instance.K_Constant_Value);
        // DEBUGGING: Find how many components are found in the current state
        //public HashSet<int> uniqueComponents;

        // rank is incremented by 1 when Merging to state which component is dominant in upcoming merging.
        private int[] parent, rank;

        // Size of each component
        public int[] size;
        // Storing the max internal difference of each component
        private int[] InternalDifference;


        public DisjointSet(int n)
        {
            parent = new int[n];
            //uniqueComponents = new HashSet<int>(n);

            rank = new int[n];

            size = new int[n];
            InternalDifference = new int[n];

            Parallel.For(0, n, i =>
            {
                parent[i] = i;
                //uniqueComponents.Add(i);

                rank[i] = 1;

                size[i] = 1;
                InternalDifference[i] = 0;
            });
        }


        // O(log(M))
        //public int Find(int i)
        //{
        //    if (parent[i] != i)
        //    {
        //        parent[i] = Find(parent[i]);
        //    }
        //    return parent[i];
        //}
        public int Find(int i)
        {
            if (parent[i] == i) return i;  // Early exit for roots

            int root = i;
            while (parent[root] != root)
                root = parent[root];

            while (i != root)
            {  // Full compression
                int next = parent[i];
                parent[i] = root;
                i = next;
            }
            return root;
        }

        // O(log(M))
        public void Union(int x, int y, int weight)
        {
            int c1 = Find(x);
            int c2 = Find(y);
            if (c1 != c2 && CanUnion(c1, c2, weight))
            {
                int newParent;
                if (rank[c1] < rank[c2])
                {
                    parent[c1] = c2;
                    //uniqueComponents.Remove(c1);
                    newParent = c2;
                    size[c2] += size[c1];
                }
                else if (rank[c1] > rank[c2])
                {
                    parent[c2] = c1;
                    //uniqueComponents.Remove(c2);
                    newParent = c1;

                    size[c1] += size[c2];
                }
                else
                {
                    parent[c2] = c1;
                    //uniqueComponents.Remove(c2);
                    newParent = c1;

                    rank[c1]++;
                    size[c1] += size[c2];
                }

                InternalDifference[newParent] = Math.Max(
                    weight,
                    Math.Max(InternalDifference[c1], InternalDifference[c2])
                );

                // Console.WriteLine($"Merged: {c1} & {c2} → Parent: {newParent} | Size: {size[newParent]} | InternalDiff: {InternalDifference[newParent]}");
            }
        }

        private bool CanUnion(int c1, int c2, int weight)
        {
            if (weight > GetMinDifference(c1, c2, K_CONSTANT))
                return false;
            return true;
        }

        private float GetMinDifference(int c1, int c2, int K)
        {
            return Math.Min(InternalDifference[c1] + Convert.ToSingle(K / size[c1]), InternalDifference[c2] + Convert.ToSingle(K / size[c2]));
        }


    }
}



