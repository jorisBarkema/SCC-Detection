using SCC_Detection.Datastructures;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace SCC_Detection.SCCDetectors
{
    class DCSC : SCCDetector
    {
        ResultSet result;
        int threads;
        Queue<HashSet<int>> taskList;

        uint status;
        uint done;

        Graph g;

        public DCSC(int threads)
        {
            this.threads = threads;

            // Now we can compare status and done to see if all threads are done
            // without using an array. Limits the program to max. 31 threads
            this.done = (uint) (1 << threads) - 1;
            this.status = 0;

            this.taskList = new Queue<HashSet<int>>();
        }

        public override ResultSet Compute(Graph g)
        {
            this.g = g;

            Task[] tasks = new Task[threads];

            for(uint i = 0; i < threads; i++)
            {
                // Make a copy to capture the variable
                // https://stackoverflow.com/questions/271440/captured-variable-in-a-loop-in-c-sharp
                uint copy = i;
                tasks[i] = Task.Factory.StartNew(() => ThreadTask(copy));
            }

            Task.WaitAll(tasks);

            return this.result;
        }

        private void ThreadTask(uint id)
        {
            while (status != done)
            {
                // Set thread status to 0 if it is 1.
                // Cannot do XOR because that goes wrong in the beginning.
                status &= ~id;

                while (taskList.Count > 0)
                {
                    HashSet<int> subgraph = new HashSet<int>();

                    lock (taskList)
                    {
                        subgraph = taskList.Dequeue();
                    }

                    ProcessSubgraph(subgraph);
                }

                status |= id;
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

            lock(this.taskList)
            {
                this.taskList.Enqueue(subgraph);
                this.taskList.Enqueue(forward);
                this.taskList.Enqueue(backward);
            }

            return;
        }
    }
}
