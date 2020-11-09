# Device Proxy Module

This Azure IoT Edge Solution contains the DeviceProxy module. The module listens for incoming device stream requests from the Azure IoT Hub and performs the connection forwarding between the Streaming Hub and the local endpoint within the Azure IoT Device.

Though it original use-case is to proxy the RTSP stream of an IP camera within the Azure IoT Device, this proxy can be used for arbitrary scenarios, where TCP connections between the Azure IoT Device network and external services needs to be performed.

## Usage

### Prerequisites

The underlying feature Azure IoT Hub Device Streams is in 'Public Preview' as of now. In order to use this module, make sure you this feature is available in the region (eg. North Europe). For further information on the feature Azure IoT Hub Device Streams feature [click here](https://docs.microsoft.com/en-us/azure/iot-hub/iot-hub-device-streams-overview).

Further you need to adjust the container registry to be used. You can simply find and replace the term `rtspdevicestreams.azurecr.io` with your container registry endpoint. Additionally you want to create the `.env` file in the module folder and configure it with your registry credentials.

```
CONTAINER_REGISTRY_USERNAME_rtspdevicestreams=<REGISTRY_USERNAME>
CONTAINER_REGISTRY_PASSWORD_rtspdevicestreams=<REGISTRY_PASSWORD>
```

### Deployment

In order to use the module, you will need to integrate it's module definition within your desired deployment template. Make sure to adjust the `REMOTEHOST` and `REMOTEPORT` according to the remote address you want the proxy to connect to. Configure the `CONNECTIONSTRING` with the device connection string.

```json
"DeviceProxy": {
    "version": "1.0",
    "type": "docker",
    "status": "running",
    "restartPolicy": "always",
    "settings": {
      "image": "${MODULES.DeviceProxy}",
      "createOptions": {}
    },
    "env": {
      "REMOTEHOST": {
        "value": "192.168.178.100"
      },
      "REMOTEPORT": {
        "value": 8554
      },
      "CONNECTIONSTRING": {
        "value": "$DEVICE_CONNSTR"
      }
    }
}
```

### Debug deployment

For debugging purposes, the `live555` RTSP simulator module was used. For building that image [this guide](https://github.com/Azure/live-video-analytics/tree/master/utilities/rtspsim-live555) was used.
