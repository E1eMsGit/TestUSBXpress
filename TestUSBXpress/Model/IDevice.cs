using System.Threading.Tasks;

namespace TestUSBXpress.Model
{
    interface IDevice
    {
        bool IsConnected { get; set; }
        
        uint GetDevices { get; }

        void Open();

        void Close();

        Task WriteToDeviceAsync(byte[] data);

        void WriteToDevice(byte[] data);

        Task<byte[]> ReadFromDeviceAsync();

        byte[] ReadFromDevice(); 
    }
}
