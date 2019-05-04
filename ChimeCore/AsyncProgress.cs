using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ChimeCore
{
    public class AsyncProgress
    {
        public Task AsyncTask { get; }
        public double Progress => progressFunc();
        Func<double> progressFunc;
        public int Number { get; } = 0;

        public AsyncProgress(Task asyncTask, Func<double> progressFunc)
        {
            AsyncTask = asyncTask;
            this.progressFunc = progressFunc;
        }

        public AsyncProgress(Task asyncTask, Func<double> progressFunc, int number)
        {
            AsyncTask = asyncTask;
            this.progressFunc = progressFunc;
            Number = number;
        }
    }
}
