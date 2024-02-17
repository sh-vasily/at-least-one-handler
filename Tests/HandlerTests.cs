using AtLeastOneHandler;
using Moq;

namespace Tests;

public class HandlerTests
{
    private class FakeClient : IClient
    {
        private TimeSpan timeout;
        private string status;
        private string id;

        public FakeClient(TimeSpan timeout, string status)
        {
            this.timeout = timeout;
            this.status = status;
        }

        public async Task<IResponse> GetApplicationStatus(string id, CancellationToken cancellationToken)
        {
            await Task.Delay(timeout, cancellationToken);
            return new SuccessResponse(id, status);
        }
    }

    [Fact]
    public async Task ThrowsExceptionWhenTimeoutExceed()
    {
        var clientExecutionDuration = TimeSpan.FromSeconds(20);
        var handler = new Handler(
            new FakeClient(clientExecutionDuration, string.Empty),
            new FakeClient(clientExecutionDuration, string.Empty),
            new Logger<Handler>(),
            TimeSpan.FromSeconds(10));

        await Assert.ThrowsAsync<TimeoutException>(() => handler.GetApplicationStatus(""));
    }

    [Theory]
    [InlineData("client1", 1, "client2", 10, "client1")]
    [InlineData("client1", 10, "client2", 1, "client2")]
    public async Task ReturnsFirstResult(
        string client1Result, int client1DurationSeconds,
        string client2Result, int client2DurationSeconds,
        string expectedResult)
    {
        var handler = new Handler(
            new FakeClient(TimeSpan.FromSeconds(client1DurationSeconds), client1Result),
            new FakeClient(TimeSpan.FromSeconds(client2DurationSeconds), client2Result),
            new Logger<Handler>(),
            TimeSpan.FromSeconds(10));

        var result = await handler.GetApplicationStatus("");

        var response = Assert.IsType<SuccessStatus>(result);
        Assert.Equal(expectedResult, response.Status);
    }
    
    [Fact]
    public async Task ReturnsRetryCount()
    {
        var retryCount = 3;
        var counter = 0;

        var mock = new Mock<IClient>();

        mock
            .Setup(x => x.GetApplicationStatus(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((string id, CancellationToken token) =>
            {
                IResponse response = counter == retryCount
                    ? new FailureResponse()
                    : new RetryResponse(TimeSpan.Zero);
                counter++;
                return response;
            });
        
        var handler = new Handler(
            mock.Object,
            new FakeClient(TimeSpan.FromSeconds(100), string.Empty),
            new Logger<Handler>(),
            TimeSpan.FromSeconds(10));

        var result = await handler.GetApplicationStatus("");

        var response = Assert.IsType<FailureStatus>(result);
        Assert.Equal(retryCount, response.RetriesCount);
    }
    
    [Fact]
    public async Task ThrowsTimeoutExceptionWhenTimeoutExceedBeforeRetryTime()
    {
        var retryTime = TimeSpan.FromSeconds(10);
        var mock = new Mock<IClient>();

        mock
            .Setup(x => x.GetApplicationStatus(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((string id, CancellationToken token) => new RetryResponse(retryTime));
        
        var handler = new Handler(
            mock.Object,
            new FakeClient(TimeSpan.FromSeconds(100), string.Empty),
            new Logger<Handler>(),
            TimeSpan.FromSeconds(15));

        await Assert.ThrowsAsync<TimeoutException>(() => handler.GetApplicationStatus(""));
        mock.Verify(
            x => x.GetApplicationStatus(It.IsAny<string>(), It.IsAny<CancellationToken>()),
            Times.Exactly(2));
    }
}