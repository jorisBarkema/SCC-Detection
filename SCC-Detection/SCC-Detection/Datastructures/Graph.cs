using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SCC_Detection.Datastructures
{
    public class Graph
    {
        private Dictionary<int, List<int>> map;
        private Dictionary<int, List<int>> transposedMap;

        //private int nodeCount = 0;
        //private Dictionary<int, int> idMap = new Dictionary<int, int>();
        //private List<int> reverseIdMap = new List<int>();

        public Graph(Dictionary<int, List<int>> map)
        {
            Graph.CheckMap(map);
            this.map = this.InitializeMap(map);
            this.transposedMap = Graph.Transpose(this.map);
        }

        public Graph(Graph g, bool transposed = false)
        {
            if (transposed)
            {
                this.map = g.GetTransposedMap();
                this.transposedMap = g.GetMap();
            }
            else
            {
                this.map = g.GetMap();
                this.transposedMap = g.GetTransposedMap();
            }
        }

        /// <summary>
        /// Use standard BFS to find the reachable vertices.
        /// </summary>
        /// <param name="fromSet">Set from which we start</param>
        /// <param name="totalSet">(Sub)set of the graph we want to include in the reachability search</param>
        /// <param name="map">Mapping from vertex to its neighbours</param>
        /// <returns>HashSet of the reachable vertices</returns>
        public static HashSet<int> Reachable(HashSet<int> fromSet, HashSet<int> totalSet, Dictionary<int, List<int>> map)
        {
            Queue<int> edge = new Queue<int>(fromSet);

            HashSet<int> reachable = new HashSet<int>(fromSet);

            // Use the convention that a vertex can reach itself always,
            // Because that makes sense when defining a single vertex as a trivial SCC.
            
            while (edge.Count > 0)
            {
                int current = edge.Dequeue();

                if (totalSet.Contains(current) && !reachable.Contains(current))
                {
                    reachable.Add(current);
                    edge.Enqueue(current);
                }

                List<int> neighbours = map[current];

                foreach(int neighbour in neighbours)
                {
                    // Look at the totalSet because we also use this for subgraphs
                    if (totalSet.Contains(neighbour) && !reachable.Contains(neighbour))
                    {
                        edge.Enqueue(neighbour);
                    }
                }
            }

            return reachable;
        }

        /// <summary>
        /// Transpose the graph: reverse each connection.
        /// </summary>
        /// <param name="map">Gaph to be transposed</param>
        /// <returns>The transposed graph.</returns>
        private static Dictionary<int, List<int>> Transpose(Dictionary<int, List<int>> map)
        {
            Dictionary<int, List<int>> result = new Dictionary<int, List<int>>();
            HashSet<int> keys = new HashSet<int>(map.Keys);

            foreach(int key in keys)
            {
                result[key] = new List<int>();
            }

            // For each id in the graph
            foreach(int id in keys)
            {
                List<int> neighbours = map[id];

                if (neighbours != null)
                {
                    // Look at its neighbours
                    foreach(int neighbour in neighbours)
                    {
                        // In the transposed graph, the neighbours can go to it,
                        // instead of it going to the neighbours
                        result[neighbour].Add(id);
                    }
                }
            }

            return result;
        }

        /// <summary>
        /// Check if the map contains non-existing IDs.
        /// </summary>
        /// <param name="map">Graph map</param>
        private static void CheckMap(Dictionary<int, List<int>> map)
        {
            HashSet<int> vertices = new HashSet<int>(map.Keys);

            foreach(int vertex in vertices)
            {
                List<int> neighbours = map[vertex];

                if (neighbours != null)
                {
                    foreach(int neighbour in neighbours)
                    {
                        if (!map.ContainsKey(neighbour))
                        {
                            throw new Exception($"Graph map includes non-existing ID {neighbour}.");
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Fisher-Yates shuffle
        /// https://en.wikipedia.org/wiki/Fisher%E2%80%93Yates_shuffle
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="list"></param>
        /// <returns>Shuffled list</returns>
        private List<T> Shuffled<T>(List<T> list)
        {
            Random rng = new Random();

            int n = list.Count;
            while (n > 1)
            {
                n--;
                int k = rng.Next(n + 1);
                T value = list[k];
                list[k] = list[n];
                list[n] = value;
            }

            return list;
        }

        /// <summary>
        /// I don't know what this function is supposed to accomplish actually
        /// </summary>
        /// <param name="map">The graph to be initialized</param>
        /// <returns>The initialized graph</returns>
        private Dictionary<int, List<int>> InitializeMap(Dictionary<int, List<int>> map)
        {
            Dictionary<int, List<int>> result = new Dictionary<int, List<int>>();

            foreach (KeyValuePair<int, List<int>> entry in map)
            {
                result[entry.Key] = entry.Value;
            }

            return result;
        }

        /*
        /// <summary>
        /// Apparently there is a mapping from id to id? from something to id? From id to something?
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        private int GetIdMap(int id)
        {
            if (idMap.ContainsKey(id))
            {
                return idMap[id];
            }

            int v = nodeCount++;
            idMap[id] = v;
            reverseIdMap.Add(id);
            return v;
        }

        /// <summary>
        /// Convenience function to call GetIdMap for multiple IDs at a time.
        /// </summary>
        /// <param name="ids"></param>
        /// <returns>The result of the GetIdMap calls in a list</returns>
        private List<int> GetIdMap(List<int> ids)
        {
            if (ids == null)
            {
                return null;
            }

            List<int> result = new List<int>();

            foreach (int id in ids)
            {
                result.Add(GetIdMap(id));
            }

            return result;
        }
        */

        /// <summary>
        /// Returns the number of vertices + edges of the graph within the subset
        /// </summary>
        /// <param name="set"></param>
        /// <returns></returns>
        private int MPlusN(HashSet<int> set)
        {
            int result = 0;

            foreach (int key in set)
            {
                List<int> neighbours = map[key];
                if (neighbours != null)
                {
                    List<int> filtered = neighbours.FindAll(x => set.Contains(x));
                    result += filtered.Count;
                    /*
                    foreach (int neighbour in neighbours)
                    {
                        if (set.Contains(neighbour))
                        {
                            result++;
                        }
                    }
                    */
                }

                //result++;
            }

            result += set.Count;

            return result;
        }

        public int PivotFromSet(HashSet<int> totalSet)
        {
            List<int> ids = totalSet.ToList();
            Random r = new Random();

            return ids[r.Next(ids.Count)];
        }

        public int RandomId()
        {
            List<int> keys = map.Keys.ToList();
            Random r = new Random();

            return keys[r.Next(keys.Count)];
        }
        /// <summary>
        /// Makes a deep copy of the graph, or transposed graph
        /// </summary>
        /// /// <param name="transposed">Optional boolean variable to indicate if the transposed map is required</param>
        /// <returns>Deep copy of the (transposed) graph</returns>
        public Dictionary<int, List<int>> GetMap(bool transposed = false)
        {
            Dictionary<int, List<int>> mapToCopy = transposed ? transposedMap : map;
            Dictionary<int, List<int>> result = new Dictionary<int, List<int>>();

            foreach (KeyValuePair<int, List<int>> entry in mapToCopy)
            {
                result[entry.Key] = new List<int>();

                foreach(int v in entry.Value)
                {
                    result[entry.Key].Add(v);
                }
            }

            return result;
        }
        
        /// <summary>
        /// Convenience function to get the transposed graph.
        /// </summary>
        /// <returns>Deep copy of the transposed graph.</returns>
        public Dictionary<int, List<int>> GetTransposedMap()
        {
            return GetMap(true);
        }

        /// <summary>
        /// Get all the vertices from the graph
        /// </summary>
        /// <returns></returns>
        public HashSet<int> Vertices()
        {
            return new HashSet<int>(map.Keys);
        }

        public HashSet<int> Forward(int from, HashSet<int> totalSet)
        {
            return Forward(new HashSet<int> { from }, totalSet);
        }

        public HashSet<int> Backward(int from, HashSet<int> totalSet)
        {
            return Backward(new HashSet<int> { from }, totalSet);
        }

        public HashSet<int> Forward(HashSet<int> fromSet, HashSet<int> totalSet)
        {
            return Reachable(fromSet, totalSet, map);
        }

        public HashSet<int> Backward(HashSet<int> fromSet, HashSet<int> totalSet)
        {
            return Reachable(fromSet, totalSet, transposedMap);
        }

        /// <summary>
        /// Find the pivot set for the next step of Schudy's algorithm
        /// See "Finding strongly connectedcomponents in parallel using O (log2 n) reachability queries"
        /// </summary>
        /// <param name="totalSet"></param>
        /// <returns></returns>
        public HashSet<int> PivotSetSchudy(HashSet<int> totalSet)
        {
            if (totalSet.Count == 0)
            {
                return new HashSet<int>();
            }

            int goal = MPlusN(totalSet) / 2;

            int start = 0;
            int end = totalSet.Count;

            List<int> list = totalSet.ToList();

            // Just to be sure, do a quick Fisher-Yates shuffle.
            // Might not even be necessary because a HashSet is already a bit random.
            list = Shuffled(list);

            while ((end - start) > 1)
            {
                int position = (end - start) / 2 + start;

                HashSet<int> fromSet = new HashSet<int>();

                for (int i = 0; i < position; i++)
                {
                    fromSet.Add(list[i]);
                }

                HashSet<int> setF = Reachable(fromSet, totalSet, map);
                if (MPlusN(setF) >= goal)
                {
                    end = position;
                }
                else
                {
                    start = position;
                }
            }

            HashSet<int> result = new HashSet<int>();

            for (int i = 0; i < end; i++)
            {
                result.Add(list[i]);
            }

            return result;
        }

        /// <summary>
        /// Implement this if 'improvement 2' is necessary
        /// </summary>
        /// <param name="totalSet"></param>
        /// <returns></returns>
        public HashSet<int> PivotSetSchudyOnlyVertices(HashSet<int> totalSet)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Implement this if 'improvement 3' is necessary
        /// </summary>
        /// <param name="totalSet"></param>
        /// <returns></returns>
        public HashSet<int> SubSetSchudy(HashSet<int> totalSet)
        {
            throw new NotImplementedException();
        }

        public HashSet<int> HalfSetForward(HashSet<int> totalSet)
        {
            if (totalSet.Count == 0)
            {
                return new HashSet<int>();
            }
            int position = totalSet.Count;
            int goal = position / 2;

            List<int> list = totalSet.ToList();
            list = Shuffled(list);

            HashSet<int> resultSet = totalSet;
            while (position > 1 && resultSet.Count > goal)
            {
                position = position / 2;

                HashSet<int> fromSet = new HashSet<int>();

                for (int i = 0; i < position; i++)
                {
                    fromSet.Add(list[i]);
                }

                resultSet = Reachable(fromSet, resultSet, map);
            }
            return resultSet;
        }

        public int InDegree(int id)
        {
            if (!transposedMap.ContainsKey(id)) return 0;

            return transposedMap[id].Count;
        }

        public int OutDegree(int id)
        {
            if (!map.ContainsKey(id)) return 0;

            return map[id].Count;
        }

        public List<int> ImmediateSuccessors(int id)
        {
            if (!map.ContainsKey(id)) return null;

            return map[id];
        }

        public HashSet<int> ImmediateSuccessors(HashSet<int> ids)
        {
            return this.ImmediateSuccessors(ids, this.Vertices());
        }

        public HashSet<int> ImmediateSuccessors(HashSet<int> ids, HashSet<int> subgraph)
        {
            HashSet<int> result = new HashSet<int>();

            foreach(int id in ids)
            {
                result.UnionWith(map[id]);
            }

            result.IntersectWith(subgraph);
            result.ExceptWith(ids);
            return result;
        }

        public List<int> ImmediatePredecessors(int id)
        {
            if (!transposedMap.ContainsKey(id)) return null;

            return transposedMap[id];
        }

        public void RemoveConnection(int from, int to)
        {
            this.map[from].Remove(to);
            this.transposedMap[to].Remove(from);
        }
        
        public void RemoveNode(int id)
        {
            // Remove this node and the connections from this node with it
            this.map.Remove(id);

            // Remove the connections to this node
            foreach(int v in this.transposedMap[id])
            {
                this.map[v].Remove(id);
            }

            // Now we can delete the reference to the removed connections to this node
            this.transposedMap.Remove(id);
        }

        /// <summary>
        /// Utiliy function to test whether a subset is an SCC.
        /// Does not test if it is a maximum SCC.
        /// Really inefficient but doesn't really matter 
        /// since it is not to be used in the actual algorithms, 
        /// only for testing if the output is correct,
        /// </summary>
        /// <param name="set">The set to be tested</param>
        /// <returns></returns>
        public bool IsSCC(HashSet<int> set)
        {
            foreach(int start in set)
            {
                HashSet<int> reachable = Forward(start, set);

                foreach(int id in set)
                {
                    if (id == start) continue;

                    if (!reachable.Contains(id)) return false;
                }
            }

            return true;
        }
        /// <summary>
        /// Checks whether a found SCC is a Terminal SCC.
        /// This assumes that the set is an SCC (that all vertices can reach each other),
        /// and only checks that they cannot reach anything outside the set.
        /// </summary>
        /// <param name="set">The SCC</param>
        /// <returns>If the SCC is terminal</returns>
        public bool IsTSCC(HashSet<int> set)
        {
            foreach (int id in set)
            {
                List<int> neighbours = map[id];

                if (neighbours != null)
                {
                    foreach (int neighbour in neighbours)
                    {
                        if (!set.Contains(neighbour))
                        {
                            return false;
                        }
                    }
                }
            }
            return true;
        }

        public HashSet<int> ForwardSeeds(HashSet<int> fromSet, HashSet<int> range)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Checks if the given vertex has any neighbours
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        public bool IsLeaf(int id)
        {
            List<int> neighbours = map[id];
            return neighbours == null || neighbours.Count == 0;
        }

        public override string ToString()
        {
            String result = "";
            HashSet<int> vertices = new HashSet<int>(map.Keys);

            foreach(int vertex in vertices)
            {
                result += vertex + " --> ";

                List<int> neighbours = map[vertex];

                if (neighbours != null)
                {
                    foreach (int neighbour in neighbours)
                    {
                        result += " " + neighbour;
                    }
                }

                result += "\n";
            }

            return result;
        }
    }
}
