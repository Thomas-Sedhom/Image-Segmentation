namespace ImageTemplate
{
    public class DSU {
        private int[] parent, rank;

        public DSU(int n) {
            parent = new int[n];
            rank = new int[n];
            for (int i = 0; i < n; i++) {
                parent[i] = i;
                rank[i] = 1;
            }
        }

        public int Find(int i) {
            if (parent[i] != i) {
                parent[i] = Find(parent[i]);
            }
            return parent[i];
        }

        public void Union(int x, int y) {
            int s1 = Find(x);
            int s2 = Find(y);
            if (s1 != s2) {
                if (rank[s1] < rank[s2]) {
                    parent[s1] = s2;
                } else if (rank[s1] > rank[s2]) {
                    parent[s2] = s1;
                } else {
                    parent[s2] = s1;
                    rank[s1]++;
                }
            }
        }
    }
}