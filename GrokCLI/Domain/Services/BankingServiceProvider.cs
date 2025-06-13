using GrokCLI.Domain.Services;

namespace GrokCLI.Domain.Services
{
    /// <summary>
    /// Provides a singleton instance of the banking data service
    /// </summary>
    public static class BankingServiceProvider
    {
        private static IBankingDataService? _instance;
        private static readonly object _lock = new object();

        public static IBankingDataService Instance
        {
            get
            {
                if (_instance == null)
                {
                    lock (_lock)
                    {
                        if (_instance == null)
                        {
                            _instance = new BankingDataService();
                        }
                    }
                }
                return _instance;
            }
        }

        /// <summary>
        /// Reset the instance (useful for testing or reinitialization)
        /// </summary>
        public static void Reset()
        {
            lock (_lock)
            {
                _instance = null;
            }
        }
    }
}
