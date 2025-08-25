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

    enum DataType : byte {
        DDATA,
        DLOCAL,
        DABSOLUTE,
        DFUNC, DVFUNC, DINLINEFUNC,
        DALIAS,
        DNONLAZYPTR,
        DLABEL
    }

    [StructLayout(LayoutKind.Explicit, Pack = 1)]
    struct ObjBaseRaw {
        [FieldOffset(0x0)]
        public ObjectType Type;
        [FieldOffset(0x1)]
        public AccessType Access;
    }

    [StructLayout(LayoutKind.Explicit, Pack = 1)]
    struct ObjObjectRaw {
        [FieldOffset(0x0)]
        public ObjBaseRaw Base;
        [FieldOffset(0x2)]
        public DataType Datatype;
        [FieldOffset(0x8)]
        public uint NamespacePtr;
        [FieldOffset(0xc)]
        public uint NamePtr;
        [FieldOffset(0x10)]
        public uint TypePtr;
    }

    interface IObj {
        ObjectType ObjectType { get; }
    }

    internal class ObjObject : MwccType<ObjObjectRaw>, IObj {
        public ObjectType ObjectType { get; }
        public readonly NameSpace? Namespace;
        public readonly HashNameNode Name;
        public readonly IType Type;

        public ObjObject(DebugClient client, uint address) : base(client, address) {
            ObjectType = RawData.Base.Type;
            if (RawData.NamespacePtr != 0) {
                Namespace = Read<NameSpace>(client, RawData.NamespacePtr);
            }
            Name = Read<HashNameNode>(client, RawData.NamePtr);
            Type = MwccType.ReadType(client, RawData.TypePtr);
        }

        public override string ToString() {
            string ns = Namespace?.ToString() ?? "";
            if (ns != "") {
                ns += "::";
            }
            return $"{ns}{Name.Name}[type={Type}]";
        }
    }
}
