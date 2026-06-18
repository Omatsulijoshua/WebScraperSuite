using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Scraper.Bypass.Helpers
{
    public static class RetryHelper
    {
        public static async Task<T> RetryAsync<T>(Func<Task<T>> action, int retries = 3)
        {
            for (int i = 0; i < retries; i++)
            {
                try
                {
                    return await action();
                }
                catch
                {
                    await Task.Delay(2000);
                }
            }

            throw new Exception("Max retries reached");
        }
    }
}
