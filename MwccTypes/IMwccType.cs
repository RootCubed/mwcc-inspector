using ClrDebug.DbgEng;
using System.Diagnostics;

namespace mwcc_inspector.MwccTypes {
    internal class MwccCachedType {
        protected static readonly Dictionary<uint, object> Cache = [];

        protected MwccCachedType(DebugClient _, uint address) {
            Cache[address] = this;
        }

        public static void ClearCache() {
            Cache.Clear();
        }

        public static T Read<T>(DebugClient client, uint address) where T : MwccCachedType {
            if (Cache.TryGetValue(address, out object? value)) {
                return (T)value;
            }
            T? instance = (T?)Activator.CreateInstance(typeof(T), client, address);
            Debug.Assert(instance != null);
            return instance;
        }

        public static T ReadPtr<T>(DebugClient client, uint address) where T : MwccCachedType {
            uint ptr = client.DataSpaces.ReadVirtual<uint>(address);
            Debug.Assert(ptr != 0);
            return Read<T>(client, ptr);
        }
    }

    internal abstract class MwccType<Raw>(DebugClient client, uint address) : MwccCachedType(client, address) where Raw : struct {
        protected Raw RawData = client.DataSpaces.ReadVirtual<Raw>(address);
    }
}
