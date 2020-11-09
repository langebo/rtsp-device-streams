# Device Proxy Client

## Projects

The device proxy client solution contains two projects

### DeviceProxyClient

This project is a .NET Standard 2.0 class library that you can use in any project to establish a client sided proxy connection to a device.

### DeviceProxyClient.CLI

This project is a cross-platform CLI that creates a proxy connection that is made accessible via a TCP socket.

#### Usage

Required arguments are the service connection string of the Azure IoT Hub and the device id.

```bash
DeviceProxyClient.CLI -c "<CONNECTION_STRING>" -d "<DEVICE_ID>"
```

The CLI can be further configured to use a static port and a custom buffer size. For debugging purposes you can adjust the verbosity of the application logs.

```bash
DeviceProxyClient.CLI -c "<CONNECTION_STRING>" -d "<DEVICE_ID>" -p <PORT> -b <BUFFER_SIZE> -v <LOG_LEVEL>
```

After successfully launching the CLI, you should see following log message:

```bash
Info: DeviceProxyClient.ProxyClient[0]
Started TCP listener on tcp://localhost:50105
```

Further information about the usage of the CLI can be retrieved by typing:

```bash
DeviceProxyClient.CLI --help
```

## Builds

### DeviceProxyClient

The library can be used in every project that can reference .NET Standard 2.0 libraries. For further information on usage, see the code documentation of the library itself and its usage within the CLI.

### DeviceProxyClient.CLI

This project is based on .NET Core 3.1 (`netcoreapp3.1` being the target framework), so you will need to have the .NET Core 3.1 SDK installed when running the build. In order to publish the CLI as a platform specific self-contained, single-file executable, execute the following instruction within the CLI project folder:

```bash
dotnet publish -c Release -r "<RID>" /p:PublishSingleFile=true /p:PublishTrimmed=true
```

Make sure to replace `"<RID>"` with your desired runtime identifier (eg. `win-x64`, `osx-x64`, `linux-x64`, or others within the [.NET Core Runtime Identifier Catalog](https://docs.microsoft.com/en-us/dotnet/core/rid-catalog))

The resulting executable can be renamed (since DeviceProxyClient.CLI is not very handy to type imho) and made available to your desired terminal or command prompt via the PATH variable of your system or shell configuration.

In case of issues at runtime, try omitting the `/p:PublishTrimmed=true` part.

## Next

- Expose the proxy stream directly, instead of forcing TCP socket usage
- Support for Microsoft.Extensions.DependencyInjection
