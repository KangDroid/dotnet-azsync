namespace KangDroid.Azsync;

public class AzSyncResponse<T>
{
    public T Result { get; set; }
    public bool IsError { get; set; }
    public string Message { get; set; }

    public AzSyncResponse(string message)
    {
        Message = message;
    }

    public AzSyncResponse()
    {
    }
}