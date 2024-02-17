namespace AtLeastOneHandler;

public interface IResponse;
public interface IApplicationStatus;

public record SuccessResponse(string Id, string Status): IResponse;
public record FailureResponse: IResponse;
public record RetryResponse(TimeSpan Delay): IResponse;
public record SuccessStatus(string ApplicationId, string Status): IApplicationStatus;
public record FailureStatus(DateTime? LastRequestTime, int RetriesCount): IApplicationStatus;