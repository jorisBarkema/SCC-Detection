﻿using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SCC_Detection.Datastructures
{
    public class Graph
    {
        private Dictionary<int, List<int>> map;
        private Dictionary<int, List<int>> transposedMap;

        readonly object graphLock = new object();

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
        /// Use parallel BFS to find the reachable vertices.
        /// </summary>
        /// <param name="fromSet">Set from which we start</param>
        /// <param name="totalSet">(Sub)set of the graph we want to include in the reachability search</param>
        /// <param name="map">Mapping from vertex to its neighbours</param>
        /// <returns>HashSet of the reachable vertices</returns>
        public HashSet<int> Reachable(HashSet<int> fromSet, HashSet<int> totalSet, Dictionary<int, List<int>> map)
        {
            return ParallelBFS(fromSet, totalSet, map);
        }

        private HashSet<int> ParallelDigraphReachability(HashSet<int> fromSet, HashSet<int> totalSet, Dictionary<int, List<int>> map)
        {
            // Add the shortcuts
            int h = 5; // Maximum recursion
            ParSC(totalSet, h);

            // Then perform parallel BFS
            return ParallelBFS(fromSet, totalSet, map);
        }

        private void ParSC(HashSet<int> totalSet, int h)
        {
            if (h == 0) return;

            Random rng = new Random();

            int count = totalSet.Count();
            int current = 0;
            int size = 1;

            //TODO: bedenken wat deze waarden moeten zijn, misschien als eigenschappen van Graph class opslaan
            //int Nk = 1;
            int Nl = 1;
            //int D = 1;

            // Initialise the values
            List<int> pivots = Shuffled(totalSet.ToList());
            Dictionary<int, bool> alive = new Dictionary<int, bool>();

            foreach (int k in totalSet)
            {
                alive[k] = true;
            }

            // Instead of calculating an appropriate k, we will check when we have done half the work and start decreasing then.
            // Rework if I want to use another epsilon_pi than 1 later.
            while(current < count)
            {
                List<int> currentPivots = pivots.GetRange(current, size);
                current += size;

                // Random value for d in [1, ..., Nl)
                int d = rng.Next(Nl - 1) + 1;

                // Ensure that d will always go down as we recurse deeper
                d += h * Nl;

                foreach(int pivot in currentPivots)
                {
                    if (!alive[pivot]) return;

                    //TODO: the actual logic of the algorithm
                }

                foreach (int pivot in currentPivots)
                {
                    if (!alive[pivot]) return;

                    //TODO: the actual logic of the algorithm
                }

                // Check if we've passed the halfway point and adapt the number of the next pivots accordingly
                if ((current << 1) <= count)
                {
                    size++;
                } else
                {
                    size--;
                }
            }
        }


        private HashSet<int> ParallelBFS(HashSet<int> fromSet, HashSet<int> totalSet, Dictionary<int, List<int>> map)
        {
            ConcurrentBag<int> edge = new ConcurrentBag<int>(fromSet);
            ConcurrentBag<int> reachable = new ConcurrentBag<int>();

            //TODO: maximum number of threads
            //https://docs.microsoft.com/en-us/dotnet/api/system.threading.tasks.paralleloptions.maxdegreeofparallelism?redirectedfrom=MSDN&view=net-5.0#System_Threading_Tasks_ParallelOptions_MaxDegreeOfParallelism

            while (edge.Except(reachable).Count() > 0)
            {
                Parallel.ForEach(edge, (current) =>
                {
                    if (!reachable.Contains(current))
                    {
                        reachable.Add(current);
                    }

                    List<int> neighbours = map[current];

                    // Only look at neighbours in set
                    // Because OBFR changes the graph
                    // which changes the neighbours, causing an error in the ForEach
                    // but OBFR only changes the subgraph it is working on,
                    // so if we only look at the neighbours in the subgraph then this is no problem.
                    List<int> neighboursInSet = totalSet.Intersect(neighbours).ToList();

                    Parallel.ForEach(neighboursInSet, (neighbour) =>
                    {
                        if (!edge.Contains(neighbour))
                        {
                            edge.Add(neighbour);
                        }
                    });
                });
            }

            return new HashSet<int>(reachable);
        }

        private HashSet<int> BFS(HashSet<int> fromSet, HashSet<int> totalSet, Dictionary<int, List<int>> map)
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
                    //edge.Enqueue(current);
                }

                List<int> neighbours = map[current];

                // Only look at neighbours in set
                // Because OBFR changes the graph
                // which changes the neighbours, causing an error
                // but OBFR only changes the subgraph it is working on,
                // so if we only look at the neighbours in the subgraph then this is no problem.
                List<int> neighboursInSet = totalSet.Intersect(neighbours).ToList();

                foreach (int neighbour in neighboursInSet)
                {
                    // Look at the totalSet because we also use this for subgraphs
                    if (!reachable.Contains(neighbour))
                    {
                        //reachable.Add(neighbour);
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

        /// <summary>
        /// First non-parallel version of shortcutting, not following the paper per se,
        /// just to see if it does something for the performance.
        /// </summary>
        /// <param name="depth">maximum depth of the BFS search</param>
        public void AddShortcuts(int depth)
        {
            List<int> vertices = Vertices().ToList();
            vertices = Shuffled(vertices);

            Dictionary<int, bool> alive = new Dictionary<int, bool>();

            foreach(int id in vertices)
            {
                alive[id] = true;
            }

            foreach(int id in vertices)
            {
                if (!alive[id]) continue;

                alive[id] = false;

                HashSet<int> cluster = DepthLimitedBFS(id, depth);

                foreach(int target in cluster)
                {
                    AddConnection(id, target);
                    alive[target] = false;
                }
            }
        }

        //TODO: DepthLimitedParallelBFS implementeren
        /// <summary>
        /// BFS with a depth limit.
        /// </summary>
        /// <param name="pivot"></param>
        /// <param name="depth"></param>
        /// <returns></returns>
        public HashSet<int> DepthLimitedBFS(int pivot, int depth)
        {
            // Item1 is the id, Item2 is the distance
            Queue<Tuple<int, int>> edge = new Queue<Tuple<int, int>>();
            edge.Enqueue(new Tuple<int, int>(pivot, 0));
            Dictionary<int, int> reachable = new Dictionary<int, int>();
            reachable[pivot] = 0;

            // Use the convention that a vertex can reach itself always,
            // Because that makes sense when defining a single vertex as a trivial SCC.

            while (edge.Count > 0)
            {
                Tuple<int, int> current = edge.Dequeue();

                if (!reachable.Keys.Contains(current.Item1))
                {
                    reachable[current.Item1] = current.Item2;
                    //edge.Enqueue(current);
                }

                List<int> neighbours = map[current.Item1];
                
                foreach (int neighbour in neighbours)
                {
                    // Look at the totalSet because we also use this for subgraphs
                    if (!reachable.Keys.Contains(neighbour))
                    {
                        //reachable.Add(neighbour);
                        if (current.Item2 + 1 <= depth)
                        {
                            edge.Enqueue(new Tuple<int, int>(neighbour, current.Item2 + 1));
                        }
                    }
                }
            }

            return new HashSet<int>(reachable.Keys);
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
                }
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
        /// See "Finding strongly connected components in parallel using O (log2 n) reachability queries"
        /// </summary>
        /// <param name="totalSet"></param>
        /// <returns></returns>
        public List<int> PivotSetMultiPivot(HashSet<int> totalSet)
        {
            if (totalSet.Count == 0)
            {
                return new List<int>();
            }

            //int goal = MPlusN(totalSet) / 2;
            int goal = totalSet.Count / 2;

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
                //if (MPlusN(setF) >= goal)
                if (setF.Count >= goal)
                {
                    end = position;
                }
                else
                {
                    start = position;
                }
            }

            List<int> result = new List<int>();

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

        public void AddConnection(int from, int to)
        {
            if (!this.map[from].Contains(to))
            {
                this.map[from].Add(to);
            }

            if (!this.transposedMap[to].Contains(from))
            {
                this.transposedMap[to].Add(from);
            }
        }
        public void RemoveConnection(int from, int to)
        {
            this.map[from].Remove(to);
            this.transposedMap[to].Remove(from);
        }
        
        public void RemoveNode(int id)
        {
            // Cannot remove two nodes at the same time which are connected to each other
            lock(this.graphLock)
            {
                // Remove the connections with the node
                // Make a copy because you cannot alter the iterator
                int[] copy_successors = new int[this.map[id].Count];
                this.map[id].CopyTo(copy_successors);

                foreach (int v in copy_successors)
                {
                    this.RemoveConnection(id, v);
                }

                int[] copy_predecessors = new int[this.transposedMap[id].Count];
                this.transposedMap[id].CopyTo(copy_predecessors);

                foreach (int v in copy_predecessors)
                {
                    this.RemoveConnection(v, id);
                }

                // Remove the node from the map
                this.map.Remove(id);

                // And the transposed map
                this.transposedMap.Remove(id);
            }
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
