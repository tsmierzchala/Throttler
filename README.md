# Throttler

Console application in .NET for simple port listener and data forwarding to destination with built-in data throttling function.
In addition, the application simulates random packet loss.

> Usage: listenPort destinationHost destinationPort kbps



> Example: Throttler.exe 5999 "10.58.255.252" 5999 12



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
