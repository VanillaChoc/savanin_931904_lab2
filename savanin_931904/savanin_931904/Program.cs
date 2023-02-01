using System.Threading.Channels;

ThreadManager.TokenRing(7);
await Task.Delay(2000);

public static class ThreadManager
{
    private static readonly List<Channel<Token>> TokenChannel = new();
    private static readonly List<Thread> Threads = new();

    public static void TokenRing(int threadCount)
    {
        TokenChannel.Add(Channel.CreateBounded<Token>(new BoundedChannelOptions(1)));
        WriteMessage(TokenChannel[0], "Main thread ", threadCount - 1, 1);

        try
        {
            for (var i = 1; i < threadCount; i++)
            {
                var threadNumb = i;
                TokenChannel.Add(Channel.CreateBounded<Token>(new BoundedChannelOptions(1)));
                var worker = new WorkCollector();

                async void Start()
                {
                    await WorkCollector.TokenWork(threadNumb, TokenChannel[threadNumb - 1], TokenChannel[threadNumb]);
                }

                Threads.Add(new Thread(Start));
                Threads[i - 1].Start();
            }

            foreach (var thread in Threads) thread.Join();
        }
        catch (Exception exception)
        {
            Console.WriteLine(exception.Message);
            throw;
        }


    }

    private static void WriteMessage(Channel<Token, Token> channel, string tokenMessage, int recipient, int timeOfLife)
    {
        channel.Writer.WriteAsync(new Token
        {
            Message = tokenMessage,
            Recipient = recipient,
            TimeOfLife = timeOfLife
        });
        Console.WriteLine($"Main message: {tokenMessage}");
    }
}

public class Token
{
    public string Message { get; set; } = "Token message";
    public int Recipient { get; set; }
    public int TimeOfLife { get; set; }
}

public class WorkCollector
{
    public static async Task TokenWork(int thisThreadNumb, Channel<Token, Token> readFrom, Channel<Token> writeTo)
    {
        await readFrom.Reader.WaitToReadAsync();
        var startTime = DateTime.Now;
        var token = await readFrom.Reader.ReadAsync();
        Console.WriteLine(token.Recipient == thisThreadNumb
            ? $"Thread number - {thisThreadNumb}; Result - {token.Message}"
            : $"Thread number - {thisThreadNumb}; Result - Nothing, mehhhh whatever");
        await writeTo.Writer.WriteAsync(token);
        var endTime = DateTime.Now;
        if ((endTime - startTime).TotalSeconds > token.TimeOfLife)
            throw new Exception("Token life is over...");
    }

}