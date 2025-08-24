using ClrDebug.DbgEng;
using System.Runtime.InteropServices;

namespace mwcc_inspector.MwccTypes {
    [StructLayout(LayoutKind.Explicit, Pack = 1)]
    struct HashNameNodeRaw {
        [FieldOffset(0x0)]
        public uint NextPtr;
        [FieldOffset(0x4)]
        public int ID;
        [FieldOffset(0x8)]
        public short HashVal;
        // Name not included, read separately
    }

    class HashNameNode : IMwccType<HashNameNode, HashNameNodeRaw> {
        public readonly int ID;
        public readonly short HashVal;
        public readonly string Name;

        public HashNameNode(DebugClient client, uint address) : base(client, address) {
            ID = RawData.ID;
            HashVal = RawData.HashVal;

            var nameAddr = address + Marshal.SizeOf<HashNameNodeRaw>();
            Name = client.DataSpaces.ReadMultiByteStringVirtual(nameAddr, 255);
        }
    }
}
