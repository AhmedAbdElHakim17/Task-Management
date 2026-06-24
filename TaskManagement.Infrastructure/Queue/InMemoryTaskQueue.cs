using System.Threading.Channels;
using TaskManagement.Application.Interfaces;

namespace TaskManagement.Infrastructure.Queue;

public class InMemoryTaskQueue(Channel<Guid> channel) : ITaskQueue
{
    public ChannelReader<Guid> Reader => channel.Reader;

    public void Enqueue(Guid taskId)
    {
        channel.Writer.TryWrite(taskId);
    }
}
