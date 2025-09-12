using System;
using System.Diagnostics;
using System.Net;
using System.Net.Http;
using System.Net.Sockets;
using System.Threading.Tasks;

class Program
{
    static async Task Main(string[] args)
    {
        if (args.Length == 0)
        {
            Console.WriteLine("Usage: LatencyChecker.dll <url> [dns_attempts]");
            return;
        }

        var url = args[0];
        var attempts = args.Length > 1 ? int.Parse(args[1]) : 1;

        Uri uri = new Uri(url);
        string host = uri.Host;
        int port = uri.Port == -1 ? 443 : uri.Port;

        Console.WriteLine($"Checking latency for {url} with {attempts} DNS attempts...\n");

        // Measure DNS lookup time (average over attempts)
        double totalDnsTime = 0;
        IPAddress[] addresses = null;
        long latestDnsTime = 0;
        
        for (int i = 0; i < attempts; i++)
        {
            var dnsStart = Stopwatch.GetTimestamp();
            addresses = await Dns.GetHostAddressesAsync(host);
            var dnsEnd = Stopwatch.GetTimestamp();
            var dnsMs = (dnsEnd - dnsStart) * 1000.0 / Stopwatch.Frequency;
            totalDnsTime += dnsMs;
            
            // Capture the latest DNS lookup time
            if (i == attempts - 1)
            {
                latestDnsTime = dnsEnd - dnsStart;
            }
        }

        double avgDnsMs = totalDnsTime / attempts;
        double latestDnsMs = latestDnsTime * 1000.0 / Stopwatch.Frequency;
        var remoteIP = addresses[0];

        // Start connection timing AFTER DNS (based on latest DNS lookup)
        var connectionStart = Stopwatch.GetTimestamp();

        // Measure TCP connection time
        var connectStart = Stopwatch.GetTimestamp();
        var socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
        await socket.ConnectAsync(remoteIP, port);
        var connectEnd = Stopwatch.GetTimestamp();

        // Get local endpoint info
        string localIP = ((IPEndPoint)socket.LocalEndPoint).Address.ToString();
        int localPort = ((IPEndPoint)socket.LocalEndPoint).Port;

        // Measure TLS handshake time
        var sslStart = Stopwatch.GetTimestamp();
        var stream = new NetworkStream(socket, ownsSocket: true);
        var sslStream = new System.Net.Security.SslStream(stream, false);
        await sslStream.AuthenticateAsClientAsync(host);
        var sslEnd = Stopwatch.GetTimestamp();

        // Close the manual connection
        sslStream.Close();

        // Measure HTTP request with a fresh HttpClient
        var httpStart = Stopwatch.GetTimestamp();
        using var client = new HttpClient();
        var request = new HttpRequestMessage(HttpMethod.Get, url);
        request.Headers.UserAgent.ParseAdd("Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 Chrome/115.0.0.0 Safari/537.36");

        var response = await client.SendAsync(request, HttpCompletionOption.ResponseHeadersRead);
        var ttfbEnd = Stopwatch.GetTimestamp();

        // Download content
        var contentBytes = await response.Content.ReadAsByteArrayAsync();
        var totalEnd = Stopwatch.GetTimestamp();

        // Calculate timings (excluding DNS from connection total)
        double dnsAvgTime = avgDnsMs;
        double dnsLatestTime = latestDnsMs;
        double connectionTime = (connectEnd - connectStart) * 1000.0 / Stopwatch.Frequency;
        double sslTime = (sslEnd - sslStart) * 1000.0 / Stopwatch.Frequency;
        double httpRequestTime = (ttfbEnd - httpStart) * 1000.0 / Stopwatch.Frequency;
        double transferTime = (totalEnd - ttfbEnd) * 1000.0 / Stopwatch.Frequency;
        
        // Total connection time (excluding DNS)
        double totalConnectionTime = (totalEnd - connectionStart) * 1000.0 / Stopwatch.Frequency;
        
        // Total time including latest DNS lookup
        double totalTimeWithDns = dnsLatestTime + totalConnectionTime;
        
        double preTransferTime = connectionTime + sslTime;
        double speedDownload = contentBytes.Length / ((totalEnd - ttfbEnd) * 1.0 / Stopwatch.Frequency);

        // Print results
        Console.WriteLine("+--------------------------------------------------+----------------------+");
        Console.WriteLine("| Metric                                           | Value                |");
        Console.WriteLine("+--------------------------------------------------+----------------------+");
        Console.WriteLine($"| DNS Lookup (avg) for {attempts} attempts        | {dnsAvgTime:F2} ms   |");
        Console.WriteLine($"| DNS Lookup (latest)                             | {dnsLatestTime:F2} ms |");
        Console.WriteLine($"| TCP Connection                                   | {connectionTime:F0} ms |");
        Console.WriteLine($"| TLS Handshake                                    | {sslTime:F0} ms      |");
        Console.WriteLine($"| Pre-transfer                                     | {preTransferTime:F0} ms |");
        Console.WriteLine("| Redirect Time                                    | 0 ms                 |");
        Console.WriteLine($"| Time to First Byte (TTFB)                       | {httpRequestTime:F0} ms |");
        Console.WriteLine($"| Content Transfer                                 | {transferTime:F0} ms |");
        Console.WriteLine("+--------------------------------------------------+----------------------+");
        Console.WriteLine($"| Total Connection Time (no DNS)                  | {totalConnectionTime:F0} ms |");
        Console.WriteLine($"| Total Time (with latest DNS)                    | {totalTimeWithDns:F0} ms |");
        Console.WriteLine("+--------------------------------------------------+----------------------+");
        Console.WriteLine($"| Download Speed                                   | {speedDownload:F0} B/s |");
        Console.WriteLine($"| Content Size                                     | {contentBytes.Length} bytes |");
        Console.WriteLine("| Upload Speed                                     | 0 B/s                |");
        Console.WriteLine($"| Remote Address                                   | {remoteIP}:{port}    |");
        Console.WriteLine($"| Local Address                                    | {localIP}:{localPort} |");
        Console.WriteLine("+--------------------------------------------------+----------------------+");

        Console.WriteLine($"\nStatus Code: {(int)response.StatusCode} {response.ReasonPhrase}");
        Console.WriteLine("Latency check completed.");
    }
}
