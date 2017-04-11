using System.Collections.Generic;

namespace IotLib
{
    public interface IConfig
    {
        string GetConnectionForObjectId(string objectid);

        List<DeviceInfo> GetDevices();
    }
}