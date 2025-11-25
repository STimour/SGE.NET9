namespace SGE.Core.Exception;

public class SgeException : System.Exception
{
	public string ErrorCode { get; }
	public int StatusCode { get; }

	public SgeException()
	{
		ErrorCode = "SGE_ERROR";
		StatusCode = 500;
	}

	public SgeException(string message) : base(message)
	{
		ErrorCode = "SGE_ERROR";
		StatusCode = 500;
	}

	public SgeException(string message, string errorCode, int statusCode) : base(message)
	{
		ErrorCode = errorCode ?? "SGE_ERROR";
		StatusCode = statusCode;
	}

	public SgeException(string message, Exception innerException, string errorCode = "SGE_ERROR", int statusCode = 500)
		: base(message, innerException)
	{
		ErrorCode = errorCode;
		StatusCode = statusCode;
	}
}