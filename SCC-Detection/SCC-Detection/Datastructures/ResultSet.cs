using System;
using System.Collections.Generic;
using System.Text;

namespace SCC_Detection.Datastructures
{
    public class ResultSet
    {
        public List<HashSet<int>> List { get; private set; }

        //private ReentrantLock lock;

        public ResultSet()
        {
            List = new List<HashSet<int>>();
            //lock = new ReentrantLock();
        }

        public void Add(HashSet<int> set)
        {
            // Maybe this should be used differently, give class a member
            // private readonly object balanceLock = new object();
            // and lock that instead of this
            lock (this)
            {
                List.Add(set);
            }
        }

        /*used in CH*/
        public void AddAll(List<HashSet<int>> results)
        {
            throw new NotImplementedException();
            //this.list.AddAll(results);
        }

        public int Count()
        {
            lock(this) {
                return List.Count;
            }
        }

        public bool Contains(int id)
        {
            lock(this)
            {
                foreach(HashSet<int> set in List)
                {
                    if (set.Contains(id))
                    {
                        return true;
                    }
                }
            }

            return false;
        }

        public HashSet<int> SCCById(int id)
        {
            lock (this)
            {
                foreach (HashSet<int> set in List)
                {
                    if (set.Contains(id))
                    {
                        return set;
                    }
                }
            }

            return new HashSet<int>();
        }

        public override string ToString()
        {

            String result = "";
            Dictionary<int, int> components = new Dictionary<int, int>(); //size of component - number of such components
            foreach (HashSet<int> set in List)
            {
                int size = set.Count;

                if (components.ContainsKey(size))
                {
                    int amount = components[size];
                    amount++;
                    components[size] = amount;
                }
                else
                {
                    components[size] = 1;
                }
            }

            foreach (KeyValuePair<int, int> entry in components)
            {
                result += "size " + entry.Key + ": " + entry.Value;
                result += "\n";
            }

            return result;
        }
    }
}
