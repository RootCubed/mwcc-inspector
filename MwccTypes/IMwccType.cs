using ClrDebug.DbgEng;
using System.Diagnostics;

namespace mwcc_inspector.MwccTypes
{
    internal class IMwccType<T, Raw> where T : IMwccType<T, Raw> where Raw : struct
    {
        private static readonly Dictionary<uint, object> Cache = [];

        protected Raw RawData;

        protected IMwccType(DebugClient client, uint address)
        {
            RawData = client.DataSpaces.ReadVirtual<Raw>(address);
            Cache[address] = this;
        }

        public static T Read(DebugClient client, uint address)
        {
            Debug.WriteLine($"Reading {typeof(T).Name} at 0x{address:X8}");
            if (Cache.TryGetValue(address, out object? value))
            {
                return (T)value;
            }
            T? instance = (T?)Activator.CreateInstance(typeof(T), client, address);
            Debug.Assert(instance != null);
            return instance;
        }

        public static T ReadPtr(DebugClient client, uint address)
        {
            uint ptr = client.DataSpaces.ReadVirtual<uint>(address);
            Debug.Assert(ptr != 0);
            return Read(client, ptr);
        }
    }
}
