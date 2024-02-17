namespace AtLeastOneHandler;

interface IHandler
{
    Task<IApplicationStatus> GetApplicationStatus(string id);
}
