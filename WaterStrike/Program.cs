using System;
using System.Text;
using System.Threading;

namespace WaterStrike
{
    class Program
    {
        static void Main(string[] args)
        {
            Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
            var group = ulong.Parse(Environment.GetEnvironmentVariable("group"));
            var key = Environment.GetEnvironmentVariable("key");
            var controller = new VkController(group, key);
            controller.StartLongPoll().Wait();
            while (true)
            {
                Thread.Sleep(10000);
            }
        }
    }
}
