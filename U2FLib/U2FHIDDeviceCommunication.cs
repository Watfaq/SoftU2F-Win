using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using APDU;

#pragma warning disable CS0414

namespace U2FLib
{
    partial class BackgroundTask
    {
        private static readonly uint FILE_DEVICE_KEYBOARD = 0x0000000b;
        private static readonly uint METHOD_BUFFERED = 0;
        private static uint METHOD_IN_DIRECT = 1;
        private static uint METHOD_OUT_DIRECT = 2;
        private static uint METHOD_NEITHER = 3;

        private static uint FILE_ANY_ACCESS = 0;
        private static readonly uint FILE_READ_DATA = 1;
        private static readonly uint FILE_WRITE_DATA = 2;

        private static uint GENERIC_READ = 0x80000000;
        private static uint GENERIC_WRITE = 0x40000000;

        private static readonly int FILE_SHARE_READ = 0x00000001;
        private static readonly int FILE_SHARE_WRITE = 0x00000002;

        private static readonly int OPEN_EXISTING = 3;

        private static uint FILE_ATTRIBUTE_NORMAL = 0x80;

        private static readonly int MAX_BCNT = 7609;

        private static readonly uint IOCTL_INDEX = 0x800;
        private static readonly uint IOCTL_SOFTU2F_FILTER_INIT = CTL_CODE(FILE_DEVICE_KEYBOARD, IOCTL_INDEX,
            METHOD_BUFFERED,
            FILE_READ_DATA | FILE_WRITE_DATA);

        private readonly int
            IO_CTL_XFER_MESSAGE_LEN =
                Marshal.SizeOf<IO_CTL_XFER_MESSAGE>(); // the length HID header info in the IO_CTL_XFER_MESSAGE;

        private static uint CTL_CODE(uint deviceType, uint function, uint method, uint access)
        {
            return (deviceType << 16) | (access << 14) | (function << 2) | method;
        }

        private IO_CTL_XFER_MESSAGE SendInitRequest(out uint nTransferred, out byte[] data,
         IRawConvertible response = null, IO_CTL_XFER_MESSAGE replyTo = default)
        {
            nTransferred = 0;
            data = default;
            var outputBuffer = new byte[MAX_BCNT + IO_CTL_XFER_MESSAGE_LEN];
            var outputBufferHandle = GCHandle.Alloc(outputBuffer, GCHandleType.Pinned);
            var outputBufferPtr = outputBufferHandle.AddrOfPinnedObject();
            var outputBufferLen = (uint)outputBuffer.Length;
            GCHandle inputBufferHandle = default;

            var inputBufferPtr = IntPtr.Zero;
            uint inputBufferLen = 0;

            if (response != null)
            {
                var reply = new IO_CTL_XFER_MESSAGE();
                reply.cid = replyTo.cid;
                reply.cmd = replyTo.cmd;
                reply.bcnt = (short)response.Raw.Length;

                var messageHeader = StructToBytes(reply);

                var inputBuffer = messageHeader.Concat(response.Raw).ToArray();
                inputBufferHandle = GCHandle.Alloc(inputBuffer, GCHandleType.Pinned);
                inputBufferPtr = inputBufferHandle.AddrOfPinnedObject();
                inputBufferLen = (uint)inputBuffer.Length;
            }

            // block on inverted call
            var result = DeviceIoControl(
                _device,
                IOCTL_SOFTU2F_FILTER_INIT,
                inputBufferPtr, inputBufferLen,
                outputBufferPtr, outputBufferLen,
                ref nTransferred, IntPtr.Zero);

            if (result == 0) return default;

            var xferMessage =
                ByteArrayToStructure<IO_CTL_XFER_MESSAGE>(outputBuffer.Take(IO_CTL_XFER_MESSAGE_LEN).ToArray());
            data = outputBuffer.Skip(IO_CTL_XFER_MESSAGE_LEN).Take((int)(nTransferred - IO_CTL_XFER_MESSAGE_LEN)).ToArray();

            outputBufferHandle.Free();
            if (inputBufferHandle != default) inputBufferHandle.Free();

            return xferMessage;
        }

        private const string BridgeDllPath = "NativeBridge.dll";
        [DllImport(BridgeDllPath, CharSet = CharSet.Unicode, CallingConvention = CallingConvention.StdCall)]
        public static extern IntPtr GetInterfaceDevicePath();

        [DllImport("kernel32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        internal static extern IntPtr CreateFile(
            string lpFileName,
            uint dwDesiredAccess,
            int dwShareMode,
            IntPtr lpSecurityAttributes,
            int dwCreationDisposition,
            int dwFlagsAndAttributes,
            int hTemplateFile);

        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern uint DeviceIoControl(
            IntPtr hDevice,
            uint dwIoControlCode,
            IntPtr lpInBuffer,
            uint nInBufferSize,
            IntPtr lpOutBuffer,
            uint nOutBufferSize,
            ref uint lpBytesReturned,
            IntPtr lpOverlapped
        );

        private static T ByteArrayToStructure<T>(byte[] bytes) where T : struct

        {
            var handle = GCHandle.Alloc(bytes, GCHandleType.Pinned);
            try
            {
                return (T)Marshal.PtrToStructure(handle.AddrOfPinnedObject(), typeof(T));
            }
            finally
            {
                handle.Free();
            }
        }

        private static byte[] StructToBytes<T>(T s) where T : struct
        {
            var size = Marshal.SizeOf(s);
            var rv = new byte[size];

            var ptr = Marshal.AllocHGlobal(size);
            Marshal.StructureToPtr(s, ptr, true);
            Marshal.Copy(ptr, rv, 0, size);
            Marshal.FreeHGlobal(ptr);
            return rv;
        }

        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        public struct IO_CTL_XFER_MESSAGE
        {
            public byte cmd;
            public int cid;
            public short bcnt;
        }
    }
}
