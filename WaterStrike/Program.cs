using System;
using System.Threading;

namespace WaterStrike
{
    class Program
    {
        static void Main(string[] args)
        {
            var controller = new VkController(149727984,
                "02b205cd29045662d9d227929582caab9a2f1e74b9a0c59f97edd2225ee8fb973fd84c32bc4df06582f11");
            controller.StartLongPoll().Wait();
            while (true)
            {
                Thread.Sleep(10000);
            }
        }
    }
}
