using ClrDebug.DbgEng;
using System.Runtime.InteropServices;

namespace mwcc_inspector.MwccTypes {
    [StructLayout(LayoutKind.Explicit, Pack = 1)]
    struct NameSpaceRaw {
        [FieldOffset(0x0)]
        public uint ParentPtr;
        [FieldOffset(0x4)]
        public uint NamePtr;
    }

    class NameSpace : MwccType<NameSpaceRaw> {
        public readonly NameSpace? Parent;
        public readonly HashNameNode? Name;

        public NameSpace(DebugClient client, uint address) : base(client, address) {
            if (RawData.ParentPtr != 0) {
                Parent = Read<NameSpace>(client, RawData.ParentPtr);
            }
            if (RawData.NamePtr != 0) {
                Name = Read<HashNameNode>(client, RawData.NamePtr);
            }
        }
    }
}
