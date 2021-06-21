using System;
using System.Text;
using System.Threading.Tasks;
using TestUSBXpress.Helpers;

namespace TestUSBXpress.Model
{
    class SiliconDevice : SIUSBXP, IDevice
    {
        private IntPtr _handle;                          
        private uint _bytesWritten;
        private uint _bytesRead;

        public bool IsConnected { get; set; }
       
        public uint GetDevices 
        { 
            get
            {
                uint n = 0;

                int errCode = SI_GetNumDevices(ref n);
                if (errCode != SI_SUCCESS && errCode != SI_DEVICE_NOT_FOUND)
                {
                    throw new Exception($"Cannot get number of devices. Error code: {errCode}");
                }

                return n;
            }
        }
        
        public string VID { get; private set; }
        
        public string PID { get; private set; }

        public SiliconDevice()
        {
            _handle = (IntPtr)0;
            IsConnected = false;
        }

        public void Open()
        {
            uint numDevices = 0;

            int errCode = SI_GetNumDevices(ref numDevices);
            if (numDevices == 0)
            {
                throw new Exception($"Unable to find a device. Error code: {errCode}");
            }

            errCode = SI_Open(0, ref _handle);
            if (errCode != SI_SUCCESS)
            {
                throw new Exception($"Unable to open device. Error code: {errCode}");
            }

            VID = GetVID();
            PID = GetPID();

            if (!VID.Equals(Constants.VID) || !PID.Equals(Constants.PID))
            {
                throw new Exception("Specified device not found");
            }

            IsConnected = true;
        }

        public void Close()
        {
            if (!IsConnected)
            {
                return;
            }

            int errCode = SI_FlushBuffers(_handle, 1, 1);
            if (errCode != SI_SUCCESS)
            {
                throw new Exception($"Unable to set timeout. Error code: {errCode}");
            }

            errCode = SI_Close(_handle);
            if (errCode != SI_SUCCESS)
            {
                throw new Exception($"Unable to close the device. Error code: {errCode}");
            }

            IsConnected = false;
            _handle = (IntPtr)0;
        }

        public async Task WriteToDeviceAsync(byte[] data)
        {
            await Task.Run(() => WriteToDevice(data));
        }

        public void WriteToDevice(byte[] data)
        {
            IntPtr o = (IntPtr)0;

            int errCode = SI_Write(_handle, data, (uint)data.Length, ref _bytesWritten, o);
            if (errCode != SI_SUCCESS)
            {
                throw new Exception($"Error writing to device. Error code: {errCode}");
            }
        }

        public async Task<byte[]> ReadFromDeviceAsync()
        {
            return await Task.Run(() => ReadFromDevice());
        }

        public byte[] ReadFromDevice()
        {
            byte[] data = new byte[8];
            IntPtr o = (IntPtr)0;
           
            int errCode = SI_Read(_handle, data, (uint)data.Length, ref _bytesRead, o);
            if (errCode != SI_SUCCESS)
            {
                throw new Exception($"Error reading from device. Error code: {errCode}");
            }
            
            return data;
        }

        private string GetVID()
        {
            StringBuilder s = new StringBuilder();

            int errCode = SI_GetProductString(0, s, SI_RETURN_VID);
            if (errCode != SI_SUCCESS || s == null)
            {
                throw new Exception($"Cannot get VID. Error code: {errCode}");
            }

            return s.ToString();
        }

        private string GetPID()
        {
            StringBuilder s = new StringBuilder();

            int errCode = SI_GetProductString(0, s, SI_RETURN_PID);
            if (errCode != SI_SUCCESS || s == null)
            {
                throw new Exception($"Cannot get PID. Error code: {errCode}");
            }

            return s.ToString();
        }
    }
}
