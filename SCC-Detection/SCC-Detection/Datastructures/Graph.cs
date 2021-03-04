using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SCC_Detection.Datastructures
{
    public class Graph
    {
        private ConcurrentDictionary<int, List<int>> map;
        private ConcurrentDictionary<int, List<int>> transposedMap;
        private ParallelOptions parallelOptions;
        private bool shortcutsAdded = false;

        Random rng;

        private readonly object graphLock = new object();

        public Graph(ConcurrentDictionary<int, List<int>> map, int threads = 1)
        {
            this.rng = new Random();

            Graph.CheckMap(map);
            this.map = this.InitializeMap(map);
            this.transposedMap = Graph.Transpose(this.map);

            this.parallelOptions = new ParallelOptions { MaxDegreeOfParallelism = threads };
        }

        public Graph(Graph g, bool transposed = false, int threads = 1)
        {
            this.rng = new Random();

            this.parallelOptions = new ParallelOptions { MaxDegreeOfParallelism = threads };

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
        public HashSet<int> Reachable(HashSet<int> fromSet, HashSet<int> totalSet, ConcurrentDictionary<int, List<int>> map)
        {
            //return ParallelBFS(fromSet, totalSet, map);
            return ParallelDigraphReachability(fromSet, totalSet, map);
        }

        private HashSet<int> ParallelDigraphReachability(HashSet<int> fromSet, HashSet<int> totalSet, ConcurrentDictionary<int, List<int>> map)
        {
            bool shouldAddShortcuts = false;

            // Make sure to only add the shortcuts the first time
            // The extra if is to prevent locking delays for after the first time
            if (!this.shortcutsAdded)
            {
                lock (graphLock)
                {
                    if (!this.shortcutsAdded)
                    {
                        shouldAddShortcuts = true;
                        this.shortcutsAdded = true;
                    }
                }
            }
                
            if (shouldAddShortcuts)
            {
                // Add the shortcuts
                int h = 1; // Maximum recursion
                ConcurrentDictionary<int, HashSet<int>> shortcuts = ParSC(totalSet, h);

                foreach(KeyValuePair<int, HashSet<int>> shortcut in shortcuts)
                {
                    foreach (int to in shortcut.Value)
                    {
                        AddConnection(shortcut.Key, to);
                    }
                }

                // Should be working, but sometimes this method hangs for ~20s, debugging where this happens now.
                // Without the Parallel here, it only seems ot be aroudn 10 seconds

                /*
                Parallel.ForEach(shortcuts, parallelOptions, (shortcut) =>
                {
                    // Also parallelise this? Don't think it's worth it because this is already going on in parallel
                    foreach (int to in shortcut.Value)
                    {
                        AddConnection(shortcut.Key, to);
                    }
                });
                */
            }

            // Then perform parallel BFS
            return ParallelBFS(fromSet, totalSet, map);
        }

        private ConcurrentDictionary<int, HashSet<int>> ParSC(HashSet<int> totalSet, int h)
        {
            if (h == 0) return new ConcurrentDictionary<int, HashSet<int>>();

            ConcurrentDictionary<int, HashSet<int>> S = new ConcurrentDictionary<int, HashSet<int>>();

            int count = totalSet.Count();
            int current = 0;
            int size = 1;

            //TODO: bedenken wat deze waarden moeten zijn, misschien als eigenschappen van Graph class opslaan
            int Nk = 1;
            int Nl = 4;
            int D = 1;

            // Initialise the values
            List<int> pivots = Shuffled(totalSet.ToList());
            Dictionary<int, bool> alive = new Dictionary<int, bool>();

            foreach (int k in totalSet)
            {
                alive[k] = true;
            }

            // Instead of calculating an appropriate k, we will check when we have done half the work and start decreasing then.
            // Rework if I want to use an epsilon_pi other than 1 later.
            while(current < pivots.Count - 1)
            {
                List<int> currentPivots = pivots.GetRange(current, Math.Min(size, pivots.Count - current));

                current += size;

                // Random value for d in [1, ..., Nl)
                int d = rng.Next(Nl - 1) + 1;

                // Ensure that d will always go down as we recurse deeper
                // This is not yet exactly the same as the paper describes,
                // but I don't understand the reasoning behind the complexity of the paper's version
                d += h * Nl * Nk;

                ConcurrentDictionary<int, HashSet<int>> backwardCores = new ConcurrentDictionary<int, HashSet<int>>();
                ConcurrentDictionary<int, HashSet<int>> forwardCores = new ConcurrentDictionary<int, HashSet<int>>();

                ConcurrentDictionary<int, HashSet<int>> backwardFringes = new ConcurrentDictionary<int, HashSet<int>>();
                ConcurrentDictionary<int, HashSet<int>> forwardFringes = new ConcurrentDictionary<int, HashSet<int>>();

                ConcurrentDictionary<int, List<int>> tagDictionary = new ConcurrentDictionary<int, List<int>>();

                // TODO: dit kan efficienter met de tags zoals uitgelegd in de paper
                Parallel.ForEach(currentPivots, parallelOptions, (pivot) =>
                {
                    if (!alive[pivot]) return;

                    
                    backwardCores[pivot] = this.DepthLimitedBFS(pivot, d * D, transposedMap);
                    forwardCores[pivot] = this.DepthLimitedBFS(pivot, d * D);

                    backwardFringes[pivot] = new HashSet<int>(this.DepthLimitedBFS(pivot, (d + 1) * D, transposedMap).Except(backwardCores[pivot]));
                    forwardFringes[pivot] = new HashSet<int>(this.DepthLimitedBFS(pivot, (d + 1) * D).Except(forwardCores[pivot]));
                    

                    /*
                    backwardCores[pivot] = this.DepthLimitedBFSWithTags(pivot, d * D, transposedMap, tagDictionary);
                    forwardCores[pivot] = this.DepthLimitedBFSWithTags(pivot, d * D, tagDictionary);

                    backwardFringes[pivot] = new HashSet<int>(this.DepthLimitedBFSWithTags(pivot, (d + 1) * D, transposedMap, tagDictionary).Except(backwardCores[pivot]));
                    forwardFringes[pivot] = new HashSet<int>(this.DepthLimitedBFSWithTags(pivot, (d + 1) * D, tagDictionary).Except(forwardCores[pivot]));
                    */
                    // Hoped to be able to just add connections straight away and not keep track of them,
                    // but then there are issues where the graph is changed during other threads' BFS, causing errors.
                    // These can be fixed but require locks, throwing away the parallelism./
                    // Instead add the edges in parallel after the ParSC is completed.
                    foreach (int id in backwardCores[pivot].Union(backwardFringes[pivot]))
                    {
                        /*
                        if (!S.ContainsKey(id))
                        {
                            S[id] = new HashSet<int>();
                        }
                        S[id].Add(pivot);
                        */

                        S.AddOrUpdate(id, new HashSet<int> { pivot }, (key, value) => {
                            value.Add(pivot);
                            return value;
                        });
                    }

                    foreach (int id in forwardCores[pivot].Union(forwardFringes[pivot]))
                    {
                        S.AddOrUpdate(pivot, new HashSet<int> { id }, (key, value) => {
                            value.Add(id);
                            return value;
                        });
                        /*
                        if (!S.ContainsKey(pivot))
                        {
                            S[pivot] = new HashSet<int>();
                        }
                        S[pivot].Add(id);
                        */
                    }
                });

                Parallel.ForEach(currentPivots, parallelOptions, (pivot) =>
                {
                    if (alive[pivot])
                    {
                        //TODO: something with the tags

                        HashSet<int> VB = new HashSet<int>(forwardCores[pivot].Intersect(backwardCores[pivot]));
                        HashSet<int> VS = new HashSet<int>(VB.Except(forwardCores[pivot]));
                        HashSet<int> VP = new HashSet<int>(VB.Except(backwardCores[pivot]));

                        // This also in parallel? I think I have too much or at least enough parallelism already
                        ConcurrentDictionary<int, HashSet<int>> forwardS = ParSC(new HashSet<int>(VS.Union(forwardFringes[pivot])), h - 1);
                        ConcurrentDictionary<int, HashSet<int>> backwardS = ParSC(new HashSet<int>(VP.Union(backwardFringes[pivot])), h - 1);

                        foreach(KeyValuePair<int, HashSet<int>> pair in forwardS)
                        {
                            S[pair.Key].UnionWith(pair.Value);
                        }

                        foreach (KeyValuePair<int, HashSet<int>> pair in backwardS)
                        {
                            S[pair.Key].UnionWith(pair.Value);
                        }
                    }
                });

                foreach (int pivot in currentPivots)
                {
                    if (!alive[pivot]) continue;

                    foreach (int id in forwardCores[pivot].Union(backwardCores[pivot]))
                    {
                        alive[id] = false;
                    }
                }

                // Check if we've passed the halfway point and adapt the number of the next pivots accordingly
                if (((current - size) * 2) <= count)
                {
                    size++;
                } else
                {
                    size--;
                }
            }

            return S;
        }


        private HashSet<int> ParallelBFS(HashSet<int> fromSet, HashSet<int> totalSet, ConcurrentDictionary<int, List<int>> map)
        {
            ConcurrentDictionary<int, bool> edge = new ConcurrentDictionary<int, bool>();

            foreach(int id in fromSet)
            {
                edge[id] = true;
            }

            ConcurrentDictionary<int, bool> reachable = new ConcurrentDictionary<int, bool>();
            
            // Maximum number of threads
            //https://docs.microsoft.com/en-us/dotnet/api/system.threading.tasks.paralleloptions.maxdegreeofparallelism?redirectedfrom=MSDN&view=net-5.0#System_Threading_Tasks_ParallelOptions_MaxDegreeOfParallelism
            
            while (edge.Except(reachable).Count() > 0)
            {
                Parallel.ForEach(edge, parallelOptions, (current) =>
                {
                    if (!reachable.Keys.Contains(current.Key))
                    {
                        reachable[current.Key] = true;
                    }

                    // Only look at neighbours in set
                    // Because OBFR changes the graph
                    // which changes the neighbours, causing an error in the ForEach
                    // but OBFR only changes the subgraph it is working on,
                    // so if we only look at the neighbours in the subgraph then this is no problem.

                    List<int> allNeighbours = new List<int>(map[current.Key]);
                    List<int> neighboursInSet = totalSet.Intersect(allNeighbours).ToList();

                    foreach (int neighbour in neighboursInSet)
                    {
                        edge[neighbour] = true;
                    }
                });
            }

            return new HashSet<int>(reachable.Keys);
        }

        private HashSet<int> BFS(HashSet<int> fromSet, HashSet<int> totalSet, ConcurrentDictionary<int, List<int>> map)
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
                    if (!reachable.Contains(neighbour))
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
        private static ConcurrentDictionary<int, List<int>> Transpose(ConcurrentDictionary<int, List<int>> map)
        {
            ConcurrentDictionary<int, List<int>> result = new ConcurrentDictionary<int, List<int>>();
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
        private static void CheckMap(ConcurrentDictionary<int, List<int>> map)
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
        private ConcurrentDictionary<int, List<int>> InitializeMap(ConcurrentDictionary<int, List<int>> map)
        {
            ConcurrentDictionary<int, List<int>> result = new ConcurrentDictionary<int, List<int>>();

            foreach (KeyValuePair<int, List<int>> entry in map)
            {
                result[entry.Key] = entry.Value;
            }

            return result;
        }
        
        public HashSet<int> DepthLimitedBFSWithTags(int pivot, int depth, ConcurrentDictionary<int, List<int>> tagDictionary)
        {
            return DepthLimitedBFSWithTags(pivot, depth, this.map, tagDictionary);
        }

        //TODO: DepthLimitedParallelBFS implementeren
        /// <summary>
        /// BFS with a depth limit.
        /// </summary>
        /// <param name="pivot"></param>
        /// <param name="depth"></param>
        /// <returns></returns>
        public HashSet<int> DepthLimitedBFSWithTags(int pivot, int depth, ConcurrentDictionary<int, List<int>> map, ConcurrentDictionary<int, List<int>> tagDictionary)
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

                int currentID = current.Item1;
                int currentDistance = current.Item2;

                // Check if it has not already been visited
                // And check if a smaller tag has not already visited it.
                if (!reachable.Keys.Contains(currentID))//  && !HasLowerTag(tagDictionary, currentID, pivot)
                {
                    reachable[currentID] = currentDistance;
                    /*
                    if (!tagDictionary.ContainsKey(currentID))
                    {
                        tagDictionary[currentID] = new List<int>();
                    }

                    tagDictionary[currentID].Add(pivot);
                    */
                }
                
                // Make it a new list to prevent it being changed during the foreach
                List<int> neighbours = new List<int>(map[current.Item1]);

                foreach (int neighbour in neighbours)
                {
                    // Look at the totalSet because we also use this for subgraphs
                    if (!reachable.Keys.Contains(neighbour)) // && !HasLowerTag(tagDictionary, neighbour, pivot)
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

        private bool HasLowerTag(ConcurrentDictionary<int, List<int>> dict, int id, int tag)
        {
            if (!dict.ContainsKey(id) || dict[id].Count < 1) return false;

            // To prevent the list from being changed while callculating Min()
            //int[] a = new int[dict[id].Count];
            //dict[id].CopyTo(a);

            return dict[id].Min() < tag;
        }

        public HashSet<int> DepthLimitedBFS(int pivot, int depth)
        {
            return DepthLimitedBFS(pivot, depth, this.map);
        }

        //TODO: DepthLimitedParallelBFS implementeren
        /// <summary>
        /// BFS with a depth limit.
        /// </summary>
        /// <param name="pivot"></param>
        /// <param name="depth"></param>
        /// <returns></returns>
        public HashSet<int> DepthLimitedBFS(int pivot, int depth, ConcurrentDictionary<int, List<int>> map)
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

                int currentID = current.Item1;
                int currentDistance = current.Item2;

                // Check if it has not already been visited
                // And check if a smaller tag has not already visited it.
                if (!reachable.Keys.Contains(currentID))
                {
                    reachable[currentID] = currentDistance;
                }
                
                // Make it a new list to prevent it being changed during the foreach
                List<int> neighbours = new List<int>(map[current.Item1]);

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

        public HashSet<int> DepthLimitedParallelBFS(int pivot, int depth)
        {
            return DepthLimitedParallelBFS(pivot, depth, this.map);
        }

        public HashSet<int> DepthLimitedParallelBFS(int pivot, int depth, ConcurrentDictionary<int, List<int>> map)
        {
            ConcurrentDictionary<int, int> edge = new ConcurrentDictionary<int, int>();
            ConcurrentDictionary<int, int> reachable = new ConcurrentDictionary<int, int>();

            edge[pivot] = 0;
            reachable[pivot] = 0;
            
            //TODO: maximum number of threads
            //https://docs.microsoft.com/en-us/dotnet/api/system.threading.tasks.paralleloptions.maxdegreeofparallelism?redirectedfrom=MSDN&view=net-5.0#System_Threading_Tasks_ParallelOptions_MaxDegreeOfParallelism
            
            while (edge.Except(reachable).Count() > 0)
            {
                Parallel.ForEach(edge, parallelOptions, (current) =>
                {
                    if (!reachable.Keys.Contains(current.Key))
                    {
                        reachable[current.Key] = current.Value;
                    }

                    if (current.Value < depth)
                    {
                        List<int> neighboursInSet = map[current.Key];

                        foreach (int neighbour in neighboursInSet)
                        {
                            edge[neighbour] = current.Value + 1;
                        }
                    }
                });
            }

            return new HashSet<int>(reachable.Keys);
        }

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
        public ConcurrentDictionary<int, List<int>> GetMap(bool transposed = false)
        {
            ConcurrentDictionary<int, List<int>> mapToCopy = transposed ? transposedMap : map;
            ConcurrentDictionary<int, List<int>> result = new ConcurrentDictionary<int, List<int>>();

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
        public ConcurrentDictionary<int, List<int>> GetTransposedMap()
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
            if (from == to || !this.map.ContainsKey(from) || !this.transposedMap.ContainsKey(to)) return;

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
            this.map.TryRemove(id, out _);

            // And the transposed map
            this.transposedMap.TryRemove(id, out _);
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
