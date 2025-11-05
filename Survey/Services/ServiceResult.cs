namespace Survey.Services
{
    public class ServiceResult
    {
        public bool Success { get; set; }
        public string Message { get; set; } = string.Empty;
        public Dictionary<string, string> Errors { get; set; } = new();

        public static ServiceResult SuccessResult(string message = "Operation successful")
        {
            return new ServiceResult { Success = true, Message = message };
        }

        public static ServiceResult FailureResult(string message)
        {
            return new ServiceResult { Success = false, Message = message };
        }

        public static ServiceResult FailureResult(Dictionary<string, string> errors)
        {
            return new ServiceResult { Success = false, Errors = errors };
        }
    }

    public class ServiceResult<T> : ServiceResult
    {
        public T? Data { get; set; }

        public static ServiceResult<T> SuccessResult(T data, string message = "Operation successful")
        {
            return new ServiceResult<T> 
            { 
                Success = true, 
                Message = message,
                Data = data
            };
        }

        public new static ServiceResult<T> FailureResult(string message)
        {
            return new ServiceResult<T> { Success = false, Message = message };
        }
    }
}