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
        [FieldOffset(0x8)]
        public uint NamespacePtr;
        [FieldOffset(0xc)]
        public uint NamePtr;
        [FieldOffset(0x10)]
        public uint TypePtr;
    }

    interface IObj {
        ObjectType Type { get; }
    }

    internal class ObjObject : MwccType<ObjObjectRaw>, IObj {
        public ObjectType Type { get; }
        public readonly NameSpace? Namespace;
        public readonly HashNameNode Name;

        public ObjObject(DebugClient client, uint address) : base(client, address) {
            Type = RawData.Base.Type;
            if (RawData.NamespacePtr != 0) {
                Namespace = Read<NameSpace>(client, RawData.NamespacePtr);
            }
            Name = Read<HashNameNode>(client, RawData.NamePtr);
        }

        public override string ToString() {
            NameSpace? currNS = Namespace;
            List<string> parts = [];
            while (currNS != null && currNS.Name != null) {
                parts.Add(currNS.Name.Name);
                currNS = currNS.Parent;
            }
            parts.Reverse();
            parts.Add(Name.Name);
            return string.Join("::", parts);
        }
    }
}
