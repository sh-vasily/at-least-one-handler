namespace AtLeastOneHandler;

public interface IClient
{
    Task<IResponse> GetApplicationStatus(string id, CancellationToken cancellationToken);
}