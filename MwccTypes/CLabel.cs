using ClrDebug.DbgEng;
using System.Runtime.InteropServices;

namespace mwcc_inspector.MwccTypes {

    [StructLayout(LayoutKind.Explicit, Pack = 1)]
    struct CLabelRaw {
        [FieldOffset(0x0)]
        public uint NextPtr;
        [FieldOffset(0x4)]
        public uint StmtPtr;
        [FieldOffset(0x8)]
        public uint UniqueNamePtr;
        [FieldOffset(0xc)]
        public uint NamePtr;
    }

    class CLabel : MwccType<CLabelRaw> {
        public readonly Statement? Stmt;
        public readonly HashNameNode UniqueName;
        public readonly HashNameNode Name;

        public CLabel(DebugClient client, uint address) : base(client, address) {
            if (RawData.StmtPtr != 0) {
                Stmt = Read<Statement>(client, RawData.StmtPtr);
            }
            UniqueName = Read<HashNameNode>(client, RawData.UniqueNamePtr);
            Name = Read<HashNameNode>(client, RawData.NamePtr);
        }
    }
}
