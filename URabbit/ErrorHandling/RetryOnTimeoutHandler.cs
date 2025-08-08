using System;
using System.Net.Http;

namespace URabbit.ErrorHandling
{
    public class RetryOnTimeoutHandler : IMessageErrorHandler
    {
        public bool ShouldRetry(Exception ex)
        {
            return ex is TimeoutException || ex is HttpRequestException;
        }
    }
}
