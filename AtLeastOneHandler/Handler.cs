namespace AtLeastOneHandler;

public class Handler: IHandler
{
    private readonly IClient _client1;
    private readonly IClient _client2;
    private readonly ILogger<Handler> _logger;
    private TimeSpan timeout;
   
    public Handler(
        IClient service1,
        IClient service2,
        ILogger<Handler> logger, 
        TimeSpan timeout)
    {
        _client1 = service1;
        _client2 = service2;
        _logger = logger;
        this.timeout = timeout;
    }
   
    public async Task<IApplicationStatus> GetApplicationStatus(string id)
    {
        var tokenSource = new CancellationTokenSource(timeout);
        var retriesCount = 0;
        DateTime? lastRequestTime = null;
        
        while (!tokenSource.IsCancellationRequested)
        {
            var client1Task = _client1.GetApplicationStatus(id, tokenSource.Token);
            var client2Task = _client2.GetApplicationStatus(id, tokenSource.Token);
            lastRequestTime = DateTime.UtcNow;
            var cancellationTask = Task.Factory.StartNew(() =>
            {
                while (!tokenSource.Token.IsCancellationRequested) { }
                return (IResponse)null;
            }, tokenSource.Token);
            
            var responseTask = await Task.WhenAny(client1Task, client2Task, cancellationTask);
            var response = await responseTask;

            if (response is null)
            {
                throw new TimeoutException("Timeout exceed");
            }

            switch (response)
            {
                case RetryResponse retryResponse:
                    retriesCount++;
                    var delayTask = Task.Delay(retryResponse.Delay, tokenSource.Token);
                    await Task.WhenAny(delayTask, cancellationTask);
                    break;
                case SuccessResponse successResponse:
                    return new SuccessStatus(successResponse.Id, successResponse.Status);
                case FailureResponse:
                    return new FailureStatus(lastRequestTime, retriesCount);
            }
        }

        throw new TimeoutException("Timeout exceed");
    }
    
    
}