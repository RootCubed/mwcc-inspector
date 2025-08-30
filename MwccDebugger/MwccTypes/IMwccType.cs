using ClrDebug.DbgEng;
using System.Diagnostics;
using System.Runtime.CompilerServices;
[assembly: InternalsVisibleTo("MwccInspectorUI")]

namespace MwccInspector.MwccTypes {
    class MwccCachedType {
        protected static readonly Dictionary<uint, object> Cache = [];

        protected MwccCachedType(DebugClient _, uint address) {
            Cache[address] = this;
        }

        public static void ClearCache() {
            Cache.Clear();
        }

        public static T Read<T>(DebugClient client, uint address) where T : MwccCachedType {
            if (address == 0) {
                throw new ArgumentException("Address must not be null", "address");
            }
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

    abstract class MwccType<Raw>(DebugClient client, uint address) : MwccCachedType(client, address) where Raw : struct {
        protected Raw RawData = client.DataSpaces.ReadVirtual<Raw>(address);
    }
}
