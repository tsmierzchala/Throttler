# Throttler

Console application in .NET for simple port listener and data forwarding to destination with built-in data throttling function.
In addition, the application simulates random packet loss.

> Usage: listenPort destinationHost destinationPort kbps packetLossProbability 

packetLossProbability - between 0,0 and 1

> Example: Throttler.exe 5999 "10.58.255.252" 5999 12 "0,1"

**EDGE**: approximately 236 kbps

**2G (GPRS)**: approximately 114 kbps

**3G (UMTS)**: From 384 kbps to 2 Mbps (often averaging about 1 Mbps = 1024 kbps)

**4G LTE**: From 5 Mbps to 100 Mbps (often averaging around 20 Mbps = 20480 kbps)

**Ethernet (standardowy 100BASE-T)**: 100 Mbps = 102400 kbps



> [INFO] 19.09.2024 15:08:07: Listening on port 5999
>
> [INFO] 19.09.2024 15:08:23: Client connected
>
> [INFO] 19.09.2024 15:08:23: Connected to 10.58.255.252:5999
>
> [INFO] 19.09.2024 15:08:23: Transferred 512 bytes
>
> [INFO] 19.09.2024 15:08:23: Transferred 512 bytes
>
> [INFO] 19.09.2024 15:08:24: Dropped 512 bytes
>
> [INFO] 19.09.2024 15:08:34: Transferred 512 bytes
>
> [INFO] 19.09.2024 15:08:35: Dropped 355 bytes
>
> [INFO] 19.09.2024 15:08:35: Transferred 355 bytes
>
> [INFO] 19.09.2024 15:08:53: Data transfer completed
