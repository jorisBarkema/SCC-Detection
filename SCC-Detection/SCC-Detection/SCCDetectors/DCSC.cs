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
    public class DCSC : SCCDetector
    {
        readonly object pulseLock = new object();
        readonly object finishedLock = new object();

        ResultSet result;
        int threadcount;
        ConcurrentQueue<HashSet<int>> taskList;

        private bool busy;
        bool[] status;
        Graph g;

        public DCSC(int threadcount)
        {
            this.Name = "DCSC";

            this.threadcount = threadcount;
            this.status = new bool[threadcount];

            this.result = new ResultSet();

            this.taskList = new ConcurrentQueue<HashSet<int>>();
            this.busy = true;
        }

        public override ResultSet Compute(Graph g)
        {
            this.g = g;
            this.busy = true;

            //g.AddShortcuts(7);

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

            /*
            for (int i = 0; i < threadcount; i++)
            {
                threads[i].Join();
            }
            */

            lock(finishedLock)
            {
                Monitor.Wait(finishedLock);
            }
            
            this.busy = false;

            lock (pulseLock)
            {
                Monitor.PulseAll(pulseLock);
            }

            return this.result;
        }

        private void DCSCTask(int id)
        {
            HashSet<int> subgraph;
            
            while(this.busy)
            {
                while (taskList.TryDequeue(out subgraph))
                {
                    this.status[id] = false;
                    ProcessSubgraph(subgraph);
                }

                this.status[id] = true;
                
                if (this.Done())
                {
                    lock(finishedLock)
                    {
                        Monitor.Pulse(finishedLock);
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

            int pivot = g.PivotFromSet(subgraph);

            HashSet<int> forward = g.Forward(pivot, subgraph);
            HashSet<int> backward = g.Backward(pivot, subgraph);

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

            // Does not appear to speed it up, at least not significantly.
            /*
            int threshold = 20;

            if (subgraph.Count < threshold)
            {
                ProcessSubgraph(subgraph);
            } else
            {
                this.taskList.Enqueue(subgraph);
            }

            if (forward.Count < threshold)
            {
                ProcessSubgraph(forward);
            }
            else
            {
                this.taskList.Enqueue(forward);
            }

            if (backward.Count < threshold)
            {
                ProcessSubgraph(backward);
            }
            else
            {
                this.taskList.Enqueue(backward);
            }
            */

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

        private void PrintHashSet(HashSet<int> set)
        {
            string s = "";

            foreach (int i in set)
            {
                s += i + " ";
            }

            Console.WriteLine(s);
        }
    }
}
