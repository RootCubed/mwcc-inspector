using ClrDebug.DbgEng;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
[assembly: InternalsVisibleTo("MwccInspectorUI")]

namespace MwccInspector.MwccTypes {
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
        public ObjectType ObjectType;
        [FieldOffset(0x1)]
        public AccessType Access;
    }

    abstract class ObjBase : MwccType<ObjBaseRaw> {
        public ObjectType ObjectType { get; }
        public AccessType Access { get; }

        public ObjBase(DebugClient client, uint address) : base(client, address) {
            ObjectType = RawData.ObjectType;
            Access = RawData.Access;
        }
        public override string ToString() {
            return $"<unknown object type {ObjectType}>";
        }
    }

    class ObjUnknown(DebugClient client, uint address) : ObjBase(client, address) { }

    [StructLayout(LayoutKind.Explicit, Pack = 1)]
    struct ObjEnumConstRaw {
        [FieldOffset(0x0)]
        public ObjBaseRaw Base;
        [FieldOffset(0x2)]
        public uint NextPtr;
        [FieldOffset(0x6)]
        public uint NamePtr;
        [FieldOffset(0xa)]
        public uint TypePtr;
        [FieldOffset(0xe)]
        public ulong Value;
    }
    class ObjEnumConst : ObjBase {
        private ObjEnumConstRaw RawSelfData;
        public HashNameNode Name { get; }
        public TypeBase Type { get; }
        public CInt64 Value { get; }

        public ObjEnumConst(DebugClient client, uint address) : base(client, address) {
            RawSelfData = client.DataSpaces.ReadVirtual<ObjEnumConstRaw>(address);
            Name = Read<HashNameNode>(client, RawSelfData.NamePtr);
            Type = MwccType.ReadType(client, RawSelfData.TypePtr);
            Value = Read<CInt64>(client, (uint)(address + Marshal.OffsetOf<ObjEnumConstRaw>("Value")));
        }
        public override string ToString() {
            return $"{Type} {Name}";
        }

        public static List<ObjEnumConst> ReadList(DebugClient client, uint address) {
            var currPtr = address;
            List<ObjEnumConst> res = [];
            while (currPtr != 0) {
                var data = Read<ObjEnumConst>(client, currPtr);
                res.Add(data);
                currPtr = data.RawSelfData.NextPtr;
            }
            return res;
        }
    }

    [StructLayout(LayoutKind.Explicit, Pack = 1)]
    struct ObjMemberVarRaw {
        [FieldOffset(0x0)]
        public ObjBaseRaw Base;
        [FieldOffset(0x2)]
        public bool IsAnonymous;
        [FieldOffset(0x3)]
        public bool HasPath;
        [FieldOffset(0x4)]
        public uint NextPtr;
        [FieldOffset(0x8)]
        public uint NamePtr;
        [FieldOffset(0x10)]
        public uint TypePtr;
        [FieldOffset(0x14)]
        public uint Qual;
        [FieldOffset(0x18)]
        public uint Offset;
    }
    class ObjMemberVar : ObjBase {
        private ObjMemberVarRaw RawSelfData;
        public bool IsAnonymous { get; }
        public HashNameNode Name { get; }
        public TypeBase? Type { get; }
        public uint Qual { get; }
        public uint Offset { get; }

        public ObjMemberVar(DebugClient client, uint address) : base(client, address) {
            RawSelfData = client.DataSpaces.ReadVirtual<ObjMemberVarRaw>(address);
            IsAnonymous = RawSelfData.IsAnonymous;
            Name = Read<HashNameNode>(client, RawSelfData.NamePtr);
            if (RawSelfData.TypePtr != 0) {
                Type = MwccType.ReadType(client, RawSelfData.TypePtr);
            }
            Qual = RawSelfData.Qual;
            Offset = RawSelfData.Offset;
        }
        public override string ToString() {
            return $"{Type} {Name}";
        }

        public static List<ObjMemberVar> ReadList(DebugClient client, uint address) {
            var currPtr = address;
            List<ObjMemberVar> res = [];
            while (currPtr != 0) {
                var data = Read<ObjMemberVar>(client, currPtr);
                res.Add(data);
                currPtr = data.RawSelfData.NextPtr;
            }
            return res;
        }
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
    class ObjObject : ObjBase {
        public DataType MetaType { get; }
        public NameSpace? Namespace { get; }
        public HashNameNode Name { get; }
        public TypeBase Type { get; }

        public ObjObject(DebugClient client, uint address) : base(client, address) {
            var data = client.DataSpaces.ReadVirtual<ObjObjectRaw>(address);
            MetaType = data.Datatype;
            if (data.NamespacePtr != 0) {
                Namespace = Read<NameSpace>(client, data.NamespacePtr);
            }
            Name = Read<HashNameNode>(client, data.NamePtr);
            Type = MwccType.ReadType(client, data.TypePtr);
        }
        public override string ToString() {
            string ns = Namespace?.ToString() ?? "";
            if (ns != "") {
                ns += "::";
            }
            return $"{ns}{Name.Name}";
        }
    }

    static class MwccObject {
        public static ObjBase ReadObject(DebugClient client, uint address) {
            var baseRaw = client.DataSpaces.ReadVirtual<ObjBaseRaw>(address);
            return baseRaw.ObjectType switch {
                ObjectType.OT_OBJECT => MwccCachedType.Read<ObjObject>(client, address),
                ObjectType.OT_MEMBERVAR => MwccCachedType.Read<ObjMemberVar>(client, address),
                _ => MwccCachedType.Read<ObjUnknown>(client, address)
            };
        }
    }
}
