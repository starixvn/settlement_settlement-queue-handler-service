namespace Settlement.Consumer.Models;
public class Response
{
    public bool IsSuccess { get; set; }

    public string Message { get; set; }

    public IEnumerable<object> Data { get; set; }
}
