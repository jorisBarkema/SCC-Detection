using SCC_Detection.Datastructures;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace SCC_Detection.SCCDetectors
{
    /// <summary>
    /// Almost exactly the same as DCSC, so quite a lot of repetitive code
    /// </summary>
    class MultiPivot : SCCDetector
    {
        readonly object pulseLock = new object();

        ResultSet result;
        int threadcount;
        ConcurrentQueue<HashSet<int>> taskList;

        bool[] status;
        Graph g;

        public MultiPivot(int threadcount)
        {
            this.Name = "MultiPivot";

            this.threadcount = threadcount;
            this.status = new bool[threadcount];

            this.result = new ResultSet();

            this.taskList = new ConcurrentQueue<HashSet<int>>();
        }

        public override ResultSet Compute(Graph g)
        {
            this.g = g;

            taskList.Enqueue(g.Vertices());

            Task[] tasks = new Task[threadcount];

            Thread[] threads = new Thread[threadcount];

            for (int i = 0; i < threadcount; i++)
            {
                // Make a copy to capture the variable
                // https://stackoverflow.com/questions/271440/captured-variable-in-a-loop-in-c-sharp
                int copy = i;

                threads[copy] = new Thread(new ThreadStart(() => DCSCTask(copy)));
                threads[copy].Start();
            }

            for (int i = 0; i < threadcount; i++)
            {
                threads[i].Join();
            }

            return this.result;
        }

        private void DCSCTask(int id)
        {
            HashSet<int> subgraph;

            while (true)
            {
                this.status[id] = false;

                while (taskList.TryDequeue(out subgraph))
                {
                    ProcessSubgraph(subgraph);
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

        private void ProcessSubgraph(HashSet<int> subgraph)
        {
            if (subgraph.Count == 0) return;

            //int pivot = g.PivotFromSet(subgraph);
            HashSet<int> pivots = g.pivotSetMultiPivot(subgraph);

            HashSet<int> forward = g.Forward(pivots, subgraph);
            HashSet<int> backward = g.Backward(pivots, subgraph);

            // Need to clone because IntersectWith modifies the existing set 
            // and we need the original forward for th next step
            HashSet<int> SCC = new HashSet<int>(forward, forward.Comparer);
            SCC.IntersectWith(backward);

            // ResultSet has the locks so no need here
            this.result.Add(SCC);

            // Calculate the remainder set
            subgraph.ExceptWith(forward);
            subgraph.ExceptWith(backward);

            forward.ExceptWith(SCC);
            backward.ExceptWith(SCC);

            this.taskList.Enqueue(subgraph);
            this.taskList.Enqueue(forward);
            this.taskList.Enqueue(backward);

            lock (pulseLock)
            {
                Monitor.PulseAll(pulseLock);
            }

            return;
        }

        private bool Done()
        {
            return !this.status.Contains(false);
        }
    }
}
