using Hangfire;
using System.Linq.Expressions;

namespace ParrotAgent.Services
{

    public interface IJobQueue
    {
        void Enqueue<T>(Expression<Func<T, Task>> methodCall);
    }
    public class HangfireJobQueue :IJobQueue
    {
        public void Enqueue<T>(Expression<Func<T, Task>> methodCall)
        {
            BackgroundJob.Enqueue<T>(methodCall);
        }
    }
}
