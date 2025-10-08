using System;

namespace URabbit.ErrorHandling
{
    public interface IMessageErrorHandler
    {
        /// <summary>
        /// Obsługuje wyjątek i zwraca true, jeśli wiadomość powinna zostać ponownie przetworzona (retry),
        /// lub false jeśli wiadomość powinna zostać odrzucona i przerzucona do DLQ.
        /// </summary>
        bool ShouldRetry(Exception ex);
    }
}
