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
            var controller = new VkController(149727984,
                "40b9fe6b0e9c42fe25da42cf4c92b697b2c2917d03e7ba6dabf6a0dac4e827856938e4a477fb32a3b83fe");
            controller.StartLongPoll().Wait();
            while (true)
            {
                Thread.Sleep(10000);
            }
        }
    }
}
