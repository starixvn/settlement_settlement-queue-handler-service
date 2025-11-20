namespace Settlement.Shared.Models;

public class Result
{
    public int ErrCode { get; set; }

    public string ErrMsg { get; set; }

    public Result(int errCode, string errMsg)
    {
        ErrMsg = errMsg;
        ErrCode = errCode;
    }

    public bool IsSuccessful => string.IsNullOrEmpty(ErrMsg) && ErrCode == 0;
}