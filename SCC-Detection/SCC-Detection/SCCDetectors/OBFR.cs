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
        ResultSet result;
        int threadcount;
        Graph g;

        readonly object pulseLock = new object();

        bool[] status;
        ConcurrentQueue<Slice> taskList;

        public OBFR(int threadcount)
        {
            this.Name = "OBFR";
            this.threadcount = threadcount;
        }


        public override ResultSet Compute(Graph g)
        {
            this.g = g;

            // Divide the graph into rooted subgraphs
            HashSet<int> total = g.Vertices();

            while (total.Count > 0)
            {
                int pivot = g.PivotFromSet(total);
                HashSet<int> forward = g.Forward(pivot, total);

                Slice s = new Slice(forward, new HashSet<int>(pivot));
                taskList.Enqueue(s);

                total.ExceptWith(forward);
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

            return this.result;

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
            while(slice.subgraph.Count > 0)
            {
                Slice trimmed = Trim(slice);
                Slice recursor = Backward(trimmed);

                HashSet<int> nextSeeds = g.ImmediateSuccessors(slice.seeds, slice.subgraph);
                slice.subgraph.ExceptWith(recursor.subgraph);
                slice.seeds = nextSeeds;
            }
            

            //HashSet<int> nextSeeds = NextSeeds(trimmed);
        }

        public Slice Trim(Slice slice) => this.Trim(slice, this.g);

        /// <summary>
        /// Computes the slice without the trimmed vertices in the subgraph. 
        /// Assumes all of the seeds are in the subgraph.
        /// </summary>
        /// <param name="slice"> The slice to be trimmed </param>
        /// <returns>The trimmed slice</returns>
        public Slice Trim(Slice slice, Graph g)
        {
            Stack<int> trimStack = new Stack<int>(slice.seeds);

            while (trimStack.Count > 0)
            {
                int id = trimStack.Pop();

                if (g.InDegree(id) == 0)
                {
                    // Make a copy because you cannot directly alter the count of the list/array in a foreach loop
                    List<int> successors = g.ImmediateSuccessors(id);
                    int[] s = new int[successors.Count];
                    successors.CopyTo(s);

                    foreach (int v in s)
                    {
                        g.RemoveConnection(id, v);

                        if (slice.subgraph.Contains(v))
                        {
                            trimStack.Push(v);
                        }
                    }

                    slice.subgraph.Remove(id);
                }
            }

            return slice;
        }

        /// <summary>
        /// Calculate the backward closure of the seeds and make it into a new slice with a random pivot
        /// </summary>
        /// <param name="slice"></param>
        /// <returns></returns>
        public Slice Backward(Slice slice)
        {
            HashSet<int> nextSubgraph = g.Backward(slice.seeds, slice.subgraph);
            HashSet<int> nextSeeds = new HashSet<int>();
            nextSeeds.Add(g.PivotFromSet(nextSubgraph));

            Slice s = new Slice(nextSubgraph, nextSeeds);

            return s;
        }

        private bool Done()
        {
            return !this.status.Contains(false);
        }
    }
}
