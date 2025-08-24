using ClrDebug.DbgEng;
using System.Runtime.InteropServices;

namespace mwcc_inspector.MwccTypes
{

    [StructLayout(LayoutKind.Explicit, Pack = 1)]
    struct CInt64Raw
    {
        [FieldOffset(0x0)]
        public int Hi;
        [FieldOffset(0x4)]
        public uint Lo;
    }

    internal class CInt64(DebugClient client, uint address) : IMwccType<CInt64, CInt64Raw>(client, address)
    {
        public long Value => ((long)RawData.Hi << 32) | RawData.Lo;

        public override string ToString() => Value.ToString();
    }
}
