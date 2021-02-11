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
    class Slice
    {
        public HashSet<int> subgraph;
        public HashSet<int> seeds;

        public Slice(HashSet<int> subgraph, HashSet<int> seeds)
        {
            this.subgraph = subgraph;
            this.seeds = seeds;
        }
    }

    class OBFR : SCCDetector
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
                    ProcessSubgraph(slice);
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

        private void ProcessSubgraph(Slice slice)
        {
            Trim(slice);
        }

        private HashSet<int> Trim(Slice slice)
        {
            throw new NotImplementedException();
        }

        private HashSet<int> Backward(Slice slice)
        {
            throw new NotImplementedException();
        }

        private HashSet<int> NextSeeds(Slice slice)
        {
            throw new NotImplementedException();
        }

        private bool Done()
        {
            return !this.status.Contains(false);
        }
    }
}
