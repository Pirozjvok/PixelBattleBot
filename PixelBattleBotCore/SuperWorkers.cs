using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Channels;
using System.Threading.Tasks;

namespace PixelBattleBotCore
{
    public class SuperWorkers<T>
    {
        private Func<T, CancellationToken, Task> _worker;

        private Channel<T>? _channel;
        public SuperWorkers(Func<T, CancellationToken, Task> worker) 
        { 
            _worker = worker;
        }
        public async Task Start(int workersCount, CancellationToken cancellationToken)
        {
            _channel = Channel.CreateBounded<T>(workersCount);
            Task[] workers = new Task[workersCount];
            for (int i = 0; i < workersCount; i++)
                workers[i] = Task.Run(() => Worker(cancellationToken));
            await Task.WhenAll(workers);
        }
        public async Task AddTask(T task, CancellationToken token)
        {
            if (_channel == null)
                throw new NullReferenceException();
            await _channel.Writer.WriteAsync(task, token);
        }

        public async Task Worker(CancellationToken cancellationToken)
        {
            if (_channel == null)
                throw new NullReferenceException();
            while (!cancellationToken.IsCancellationRequested)
            {
                T task = await _channel.Reader.ReadAsync(cancellationToken);
                await _worker.Invoke(task, cancellationToken);
            }
        }
    }
}
