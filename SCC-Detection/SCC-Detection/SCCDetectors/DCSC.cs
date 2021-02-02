using SCC_Detection.Datastructures;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace SCC_Detection.SCCDetectors
{
    public class DCSC : SCCDetector
    {
        ResultSet result;
        int threads;
        Queue<HashSet<int>> taskList;

        bool[] status;
        Graph g;

        public DCSC(int threads)
        {
            this.threads = threads;
            this.status = new bool[threads];

            this.result = new ResultSet();

            this.taskList = new Queue<HashSet<int>>();
        }

        public override ResultSet Compute(Graph g)
        {
            this.g = g;

            taskList.Enqueue(g.Vertices());

            Task[] tasks = new Task[threads];

            for(int i = 0; i < threads; i++)
            {
                // Make a copy to capture the variable
                // https://stackoverflow.com/questions/271440/captured-variable-in-a-loop-in-c-sharp
                int copy = i;
                tasks[i] = Task.Factory.StartNew(() => ThreadTask(copy));
            }

            Task.WaitAll(tasks);

            return this.result;
        }

        private void ThreadTask(int id)
        {
            while (!this.Done())
            {
                while (true)
                {
                    HashSet<int> subgraph;

                    lock (taskList)
                    {
                        if (taskList.Count == 0) break;
                        subgraph = taskList.Dequeue();
                    }

                    this.status[id] = false;
                    
                    ProcessSubgraph(subgraph);
                }

                this.status[id] = true;
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
