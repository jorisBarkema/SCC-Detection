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
    public class MultiPivot : SCCDetector
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

                threads[copy] = new Thread(new ThreadStart(() => MultiPivotTask(copy)));
                threads[copy].Start();
            }

            for (int i = 0; i < threadcount; i++)
            {
                threads[i].Join();
            }

            return this.result;
        }

        private void MultiPivotTask(int id)
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
            
            List<int> pivots = g.PivotSetMultiPivot(subgraph);

            if (pivots.Count == 0) return;

            int s = pivots[pivots.Count - 1];

            pivots.RemoveAt(pivots.Count - 1);

            HashSet<int> remainingPivotsSet = new HashSet<int>(pivots);

            HashSet<int> A = g.Forward(remainingPivotsSet, subgraph);
            HashSet<int> B = g.Forward(s, subgraph);
            HashSet<int> backward = g.Backward(s, subgraph);

            HashSet<int> C = new HashSet<int>(B, B.Comparer);
            C.IntersectWith(backward);

            // ResultSet has the locks so no need here
            this.result.Add(C);

            this.taskList.Enqueue(new HashSet<int>(subgraph.Except(A.Union(B))));
            this.taskList.Enqueue(new HashSet<int>(A.Except(B)));
            this.taskList.Enqueue(new HashSet<int>(B.Except(A.Union(C))));
            this.taskList.Enqueue(new HashSet<int>(A.Intersect(B).Except(C)));

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
