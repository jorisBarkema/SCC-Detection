using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using SCC_Detection.Datastructures;

namespace SCC_Detection.SCCDetectors
{
    public class Slice
    {
        public HashSet<int> subgraph;
        public HashSet<int> seeds;

        public Slice(HashSet<int> subgraph, HashSet<int> seeds)
        {
            this.subgraph = subgraph;
            this.seeds = seeds;
        }
    }

    public class OBFR : SCCDetector
    {
        public ResultSet Result { get; private set; }
        int threadcount;
        Graph g;

        readonly object pulseLock = new object();

        bool[] status;
        ConcurrentQueue<Slice> taskList;

        public OBFR(int threadcount)
        {
            this.Name = "OBFR";
            this.threadcount = threadcount;
            this.status = new bool[threadcount];
            this.Result = new ResultSet();
            this.taskList = new ConcurrentQueue<Slice>();
        }


        public override ResultSet Compute(Graph g)
        {
            this.g = g;

            // Divide the graph into rooted subgraphs
            HashSet<int> total = g.Vertices();

            List<Slice> slices = this.ToRootedSlices(total);

            foreach(Slice slice in slices)
            {
                taskList.Enqueue(slice);
            }
            
            Task[] tasks = new Task[threadcount];

            Thread[] threads = new Thread[threadcount];

            //CancellationToken token = source.Token;

            for (int i = 0; i < threadcount; i++)
            {
                // Make a copy to capture the variable
                // https://stackoverflow.com/questions/271440/captured-variable-in-a-loop-in-c-sharp
                int copy = i;

                threads[copy] = new Thread(new ThreadStart(() => OBFRTask(copy)));
                threads[copy].Start();
            }
            
            for (int i = 0; i < threadcount; i++)
            {
                threads[i].Join();
            }

            return this.Result;

        }

        private void OBFRTask(int id)
        {
            Slice slice;

            while (true)
            {
                this.status[id] = false;

                while (taskList.TryDequeue(out slice))
                {
                    ProcessSLice(slice);
                }

                this.status[id] = true;

                if (this.Done())
                {
                    lock (pulseLock)
                    {
                        Monitor.PulseAll(pulseLock);
                    }
                    return;
                }

                lock (pulseLock)
                {
                    Monitor.Wait(pulseLock);
                }
            }
        }

        private void ProcessSLice(Slice slice)
        {
            int totalCount = slice.subgraph.Count;

            while (slice.subgraph.Count > 0)
            {
                
                Slice trimmed = Trim(slice);

                // If it is fully trimmed we are done
                if (trimmed.subgraph.Count == 0) return;

                HashSet<int> recursiveSubgraph = Backward(trimmed);

                /*
                 * The recursive Subgraph count can only be equal to the total count
                 * in the first iteration of this while loop, because otherwise there is already a resursive subgraph eliminated.
                 * 
                 * So we know that there is only one seed, and its forward closure is the whole slice.
                 * If the backward closure is also the while slice, then the slice is an SCC.
                */
                
                if (totalCount == recursiveSubgraph.Count)
                {
                    Result.Add(recursiveSubgraph);
                    return;
                }
                else
                {
                    HashSet<int> nextSeeds = g.ImmediateSuccessors(recursiveSubgraph, slice.subgraph);
                    slice.subgraph.ExceptWith(recursiveSubgraph);
                    slice.seeds = nextSeeds;

                    List<Slice> rootedSlices = this.ToRootedSlices(recursiveSubgraph);

                    foreach (Slice rootedSlice in rootedSlices)
                    {
                        taskList.Enqueue(rootedSlice);
                    }

                    lock (pulseLock)
                    {
                        Monitor.PulseAll(pulseLock);
                    }
                }
            }
            

            //HashSet<int> nextSeeds = NextSeeds(trimmed);
        }

        public Slice Trim(Slice slice) => this.Trim(slice, this.g);

        /// <summary>
        /// Computes the slice without the trimmed vertices in the subgraph. 
        /// Assumes all of the seeds are part of the subgraph.
        /// </summary>
        /// <param name="slice"> The slice to be trimmed </param>
        /// <returns>The trimmed slice</returns>
        public Slice Trim(Slice slice, Graph g)
        {
            Stack<int> trimStack = new Stack<int>(slice.seeds);
            HashSet<int> seeds = new HashSet<int>();

            while (trimStack.Count > 0)
            {
                int id = trimStack.Pop();

                if (g.InDegree(id) == 0)
                {
                    // The node is a trivial SCC
                    Result.Add(new HashSet<int> { id });

                    // Make a copy because you cannot directly alter the count of the list/array in a foreach loop
                    List<int> successors = g.ImmediateSuccessors(id);

                    foreach (int v in successors)
                    {
                        if (slice.subgraph.Contains(v))
                        {
                            trimStack.Push(v);
                        }
                    }

                    g.RemoveNode(id);
                    slice.subgraph.Remove(id);
                    if (seeds.Contains(id)) seeds.Remove(id);
                } else
                {
                    seeds.Add(id);
                }
            }
            slice.seeds = seeds;
            return slice;
        }

        /// <summary>
        /// Calculate the backward closure of the seeds and make it into a new slice with a random pivot
        /// </summary>
        /// <param name="slice"></param>
        /// <returns></returns>
        public HashSet<int> Backward(Slice slice)
        {
            return g.Backward(slice.seeds, slice.subgraph);
        }

        private List<Slice> ToRootedSlices(HashSet<int> subgraph)
        {
            // Divide the graph into rooted subgraphs

            List<Slice> slices = new List<Slice>();

            while (subgraph.Count > 0)
            {
                int pivot = g.PivotFromSet(subgraph);
                HashSet<int> forward = g.Forward(pivot, subgraph);

                Slice s = new Slice(forward, new HashSet<int> { pivot });
                slices.Add(s);

                subgraph.ExceptWith(forward);
            }

            return slices;
        }

        private bool Done()
        {
            return !this.status.Contains(false);
        }
    }
}
