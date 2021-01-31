using System;
using System.Collections.Generic;
using System.Text;

namespace SCC_Detection.Datastructures
{
    class Graph
    {
        private Dictionary<int, List<int>> map;
        private Dictionary<int, List<int>> transposeMap;

        private int nodeCount = 0;
        private Dictionary<int, int> idMap = new Dictionary<int, int>();
        private List<int> reverseIdMap = new List<int>();

        public Graph(Dictionary<int, List<int>> map)
        {
            Graph.CheckMap(map);
            this.map = this.InitializeMap(map);
            transposeMap = Graph.Transpose(this.map);
        }

        public Graph(Graph g, bool transposed = false)
        {
            if (transposed)
            {
                this.map = g.GetTransposedMap();
                this.transposeMap = g.GetMap();
            }
            else
            {
                this.map = g.GetMap();
                this.transposeMap = g.GetTransposedMap();
            }
        }

        private static HashSet<int> Reachable(HashSet<int> fromSet, HashSet<int> totalSet, Dictionary<int, List<int>> map)
        {
            throw new NotImplementedException();
        }

        private static Dictionary<int, List<int>> Transpose(Dictionary<int, List<int>> map)
        {
            throw new NotImplementedException();
        }

        private static void CheckMap(Dictionary<int, List<int>> map)
        {
            throw new NotImplementedException();
        }

        private Dictionary<int, List<int>> InitializeMap(Dictionary<int, List<int>> map)
        {
            throw new NotImplementedException();
        }

        private int GetIdMap(int id)
        {
            throw new NotImplementedException();
        }

        private List<int> GetIdMap(List<int> ids)
        {
            throw new NotImplementedException();
        }

        private int MPlusN(HashSet<int> set)
        {
            throw new NotImplementedException();
        }

        public Dictionary<int, List<int>> GetMap()
        {
            throw new NotImplementedException();
        }
        
        public Dictionary<int, List<int>> GetTransposedMap()
        {
            throw new NotImplementedException();
        }

        public HashSet<int> Vertices()
        {
            throw new NotImplementedException();
        }

        public HashSet<int> Forward(HashSet<int> fromSet, HashSet<int> totalSet)
        {
            throw new NotImplementedException();
        }

        public HashSet<int> Backward(HashSet<int> fromSet, HashSet<int> totalSet)
        {
            throw new NotImplementedException();
        }

        public HashSet<int> PivotSetSchudy(HashSet<int> totalSet)
        {
            throw new NotImplementedException();
        }

        public HashSet<int> PivotSetSchudyOnlyVertices(HashSet<int> totalSet)
        {
            throw new NotImplementedException();
        }

        public HashSet<int> SubSetSchudy(HashSet<int> totalSet)
        {
            throw new NotImplementedException();
        }

        public HashSet<int> HalfSetForward(HashSet<int> totalSet)
        {
            throw new NotImplementedException();
        }

        public int InDegree(int id)
        {
            throw new NotImplementedException();
        }

        public int OutDegree(int id)
        {
            throw new NotImplementedException();
        }

        public List<int> ImmediateSuccessors(int id)
        {
            throw new NotImplementedException();
        }

        public List<int> ImmediatePredecessors(int id)
        {
            throw new NotImplementedException();
        }

        public bool IsTSCC(HashSet<int> set)
        {
            throw new NotImplementedException();
        }

        public HashSet<int> ForwardSeeds(HashSet<int> fromSet, HashSet<int> range)
        {
            throw new NotImplementedException();
        }

        public bool IsLeaf(int key)
        {
            throw new NotImplementedException();
        }

        public override string ToString()
        {
            throw new NotImplementedException();
        }
    }
}
