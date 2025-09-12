# Latency Checker

A containerized .NET application that measures network latency and performance metrics for any given URL.

## Overview

This tool provides detailed network performance analysis including:
- DNS lookup times (with averaging capability)
- TCP connection establishment
- TLS/SSL handshake timing
- HTTP Time to First Byte (TTFB)
- Content transfer speed
- Download throughput

## Building the Image

### Using Docker
```bash
# Navigate to the project directory
cd latency-check/LatencyChecker

# Build with Docker
export DOCKER_BUILDKIT=1
docker build -t latency-checker .
```

### Using Podman
```bash
# Navigate to the project directory
cd latency-check/LatencyChecker

# Build with Podman
podman build -t latency-checker .
```


## Usage

### Basic Usage

```bash
# Check latency for a website (single DNS lookup)
podman run --rm latency-checker https://google.com

# Using Docker
docker run --rm latency-checker https://google.com
```

### Advanced Usage

```bash
# Check latency with multiple DNS attempts for better accuracy
podman run --rm latency-checker https://google.com 100

# Check different websites
podman run --rm latency-checker https://github.com 50
podman run --rm latency-checker https://stackoverflow.com 1000

# Check specific ports
podman run --rm latency-checker https://example.com:8080 10
```

## Parameters

- **URL** (required): The target URL to test (must include protocol: http:// or https://)
- **DNS_ATTEMPTS** (optional): Number of DNS lookups to perform for averaging (default: 1)

## Example Output

```
Checking latency for https://google.com with 1000 DNS attempts...

+--------------------------------------------------+----------------------+
| Metric                                           | Value                |
+--------------------------------------------------+----------------------+
| DNS Lookup (avg) for 1000 attempts             | 0.41 ms              |
| DNS Lookup (latest)                             | 0.41 ms              |
| TCP Connection                                   | 37 ms                |
| TLS Handshake                                    | 155 ms               |
| Pre-transfer                                     | 192 ms               |
| Redirect Time                                    | 0 ms                 |
| Time to First Byte (TTFB)                       | 382 ms               |
| Content Transfer                                 | 120 ms               |
+--------------------------------------------------+----------------------+
| Total Connection Time (no DNS)                  | 696 ms               |
| Total Time (with latest DNS)                    | 697 ms               |
+--------------------------------------------------+----------------------+
| Download Speed                                   | 1836377 B/s          |
| Content Size                                     | 220548 bytes         |
| Upload Speed                                     | 0 B/s                |
| Remote Address                                   | 142.251.209.14:443   |
| Local Address                                    | 10.88.0.8:34382      |
+--------------------------------------------------+----------------------+

Status Code: 200 OK
Latency check completed.
```

## Metrics Explained

### DNS Metrics
- **DNS Lookup (avg)**: Average DNS resolution time across all attempts
- **DNS Lookup (latest)**: DNS resolution time for the final lookup used for connection

### Connection Metrics
- **TCP Connection**: Time to establish TCP connection
- **TLS Handshake**: Time to complete SSL/TLS handshake (HTTPS only)
- **Pre-transfer**: Combined TCP + TLS time

### HTTP Metrics
- **Time to First Byte (TTFB)**: Time from HTTP request to first response byte
- **Content Transfer**: Time to download the full response body
- **Download Speed**: Average download throughput in bytes per second

### Total Metrics
- **Total Connection Time (no DNS)**: Pure network latency excluding DNS overhead
- **Total Time (with latest DNS)**: End-to-end time including realistic DNS lookup

### Connection Details
- **Remote Address**: Server IP and port
- **Local Address**: Your local IP and port
- **Content Size**: Size of downloaded content in bytes
- **Status Code**: HTTP response status

## Use Cases

### Performance Monitoring
```bash
# Monitor website performance over time
podman run --rm latency-checker https://yourapp.com 100

# Compare different CDN endpoints
podman run --rm latency-checker https://cdn1.example.com 50
podman run --rm latency-checker https://cdn2.example.com 50
```

### DNS Performance Testing
```bash
# Test DNS performance with high accuracy
podman run --rm latency-checker https://example.com 10000

# Quick DNS check
podman run --rm latency-checker https://example.com 1
```

### Network Troubleshooting
```bash
# Diagnose slow website loading
podman run --rm latency-checker https://slow-site.com 10

# Check if issues are DNS, TCP, TLS, or server-related
podman run --rm latency-checker https://problematic-site.com 100
```

## Requirements

- Docker, Podman for building/running
- Network connectivity to target URLs
- Supports both HTTP and HTTPS URLs

## Notes

- The tool performs two separate connections: one for measuring TCP/TLS timing, and another for HTTP request timing
- DNS attempts parameter helps average out DNS caching effects for more accurate measurements
- Total times exclude DNS measurement overhead but include realistic DNS lookup time
- All times are measured using high-precision system timers

## Troubleshooting

### Build Issues
```bash
# Ensure BuildKit is enabled for Docker
export DOCKER_BUILDKIT=1


### Runtime Issues
```bash
# For network connectivity issues
podman run --rm latency-checker https://google.com 1

# Check if URL is reachable
curl -I https://your-target-url.com
```
