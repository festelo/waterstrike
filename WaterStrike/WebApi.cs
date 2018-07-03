using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace WaterStrike
{
    public class WebApi : IDisposable
    {
        private readonly HttpClient client = new HttpClient();
        public async Task<byte[]> GetFieldImage(FieldPoint[,] points)
        {
            var shipstrb = new StringBuilder();
            var shootedstrb = new StringBuilder();
            for (int i = 0; i < 10; i++)
                for (int j = 0; j < 10; j++)
                {
                    if (points[i, j].HasFlag(FieldPoint.Ship))
                    {
                        shipstrb.Append(j.ToString() + i);
                    }
                    if (points[i, j].HasFlag(FieldPoint.Shooted))
                    {
                        shootedstrb.Append(j.ToString() + i);
                    }
                }
            return await client.GetByteArrayAsync(
                $"http://waterstrikephp-waterstrike.a3c1.starter-us-west-1.openshiftapps.com/get.php?ships={shipstrb}&crosses={shootedstrb}");
        }
        public void Dispose()
        {
            client?.Dispose();
        }
    }
}
