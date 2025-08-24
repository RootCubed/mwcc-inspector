using ClrDebug.DbgEng;
using System.Runtime.InteropServices;

namespace mwcc_inspector.MwccTypes {
    enum ObjectType : byte {
        OT_ENUMCONST,
        OT_TYPE,
        OT_TYPETAG,
        OT_NAMESPACE,
        OT_MEMBERVAR,
        OT_OBJECT,
        OT_ILLEGAL
    }

    [StructLayout(LayoutKind.Explicit, Pack = 1)]
    struct ObjBaseRaw {
        [FieldOffset(0x0)]
        public ObjectType Type;
        [FieldOffset(0x1)]
        public byte Access;
    }

    [StructLayout(LayoutKind.Explicit, Pack = 1)]
    struct ObjObjectRaw {
        [FieldOffset(0x0)]
        public ObjBaseRaw Base;
        [FieldOffset(0x2)]
        public byte Datatype;
        [FieldOffset(0xc)]
        public uint NamePtr;
        [FieldOffset(0x10)]
        public uint TypePtr;
    }

    interface IObj {
        ObjectType Type { get; }
    }

    internal class ObjObject : IMwccType<ObjObject, ObjObjectRaw>, IObj {
        public ObjectType Type { get; }
        public readonly HashNameNode Name;

        public ObjObject(DebugClient client, uint address) : base(client, address) {
            Type = RawData.Base.Type;
            Name = HashNameNode.Read(client, RawData.NamePtr);
        }
    }
}
