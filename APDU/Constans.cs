using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace APDU
{
    public static class Constants
    {
        public const int MaxResponseSize = UInt16.MaxValue + 1;

        public const int U2F_CHAL_SIZE = 32;
        public const int U2F_APPID_SIZE = 32;

        public const int U2F_EC_KEY_SIZE = 32;                         // EC key size in bytes
        public const int U2F_EC_POINT_SIZE = ((U2F_EC_KEY_SIZE * 2) + 1); // Size of EC point
    }
    public enum CommandCode : byte
    {
        Register = 0x01,
        Authenticate,
        Version,
        CheckRegister,
        AuthenticateBatch,
    }

    public enum Control : byte
    {
        EnforceUserPresenceAndSign = 0x03,
        CheckOnly = 0x07,

        Invalid = 0xFF,
    }

    public enum CommandClass : byte
    {
        Reserved = 0x00,
    }

    public enum ResponseErrorCode : uint
    {
        BadSize,
        BadStatus,
        BadCertificate,
        BadData
    }

    public enum ProtocolErrorCode : UInt16
    {
        NoError = 0x9000,
        WrongData = 0x6A80,
        ConditionNoSatisfied = 0x6985,
        CommandNotAllowed = 0x6986,
        InsNotSupported = 0x6D00,
        WrongLength = 0x6700,
        ClassNotSupported = 0x6E00,
        OtherError = 0x6F00
    }
}
