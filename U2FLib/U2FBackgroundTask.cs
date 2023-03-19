#region

using System;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using APDU;

#endregion

#pragma warning disable CS0414

namespace U2FLib
{
    #region Constants

    using LPSECURITY_ATTRIBUTES = IntPtr;
    using LPOVERLAPPED = IntPtr;
    using LPVOID = IntPtr;
    using HANDLE = IntPtr;
    using LARGE_INTEGER = Int64;
    using DWORD = UInt32;
    using LPCTSTR = String;

    #endregion

    public interface IU2FBackgroundTask
    {
        void StartIoLoop(CancellationToken token);
        bool OpenDevice();
    }

    public sealed partial class BackgroundTask : IU2FBackgroundTask
    {

        private static IntPtr _device;

        public BackgroundTask()
        {
        }

        public void StartIoLoop(CancellationToken token)
        {

            IRawConvertible response = null;
            IO_CTL_XFER_MESSAGE replyTo = default;
            while (!token.IsCancellationRequested)
            {
                // enter inverted call
                replyTo = SendInitRequest(out var nTransferred, out var data, response, replyTo);
                response = HandleRequest(data, replyTo);
            }
        }

        private IRawConvertible HandleRequest(byte[] data, IO_CTL_XFER_MESSAGE request)
        {
            try
            {
                var ins = Command.CommandType(data);

                IRawConvertible response;
                switch (ins)
                {
                    case CommandCode.Register:
                        response = HandleRegisterRequest(data, request);
                        break;
                        
                    case CommandCode.Version:
                        response = HandleVersionRequest(data, request);
                        break;

                    case CommandCode.Authenticate:
                        response = HandleAuthenticationRequest(data, request);
                        break;

                    default:
                        response = CreateError(ProtocolErrorCode.InsNotSupported);
                        break;
                }

                return response;
            }
            catch (ProtocolError e)
            {
                return CreateError(e.ErrorCode);
            }
            catch
            {
                return CreateError(ProtocolErrorCode.OtherError);
            }
           
        }

        public bool OpenDevice()
        {
            var ptr = GetInterfaceDevicePath();
            if (ptr == IntPtr.Zero) return false;
            var devicePath = Marshal.PtrToStringUni(ptr);
            _device = CreateFile(devicePath, FILE_READ_DATA | FILE_WRITE_DATA, FILE_SHARE_READ | FILE_SHARE_WRITE,
                IntPtr.Zero,
                OPEN_EXISTING, 0, 0);
            return _device != IntPtr.Zero;
        }
    }
}