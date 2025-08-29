using ClrDebug.DbgEng;
using System.Runtime.InteropServices;

namespace MwccInspector.MwccTypes {
    [StructLayout(LayoutKind.Explicit, Pack = 1)]
    struct NameSpaceRaw {
        [FieldOffset(0x0)]
        public uint ParentPtr;
        [FieldOffset(0x4)]
        public uint NamePtr;
    }

    class NameSpace : MwccType<NameSpaceRaw> {
        public List<NameSpace> Hierarchy { get; } = [];
        public HashNameNode? Name { get; }

        public NameSpace(DebugClient client, uint address) : base(client, address) {
            if (RawData.ParentPtr != 0) {
                var parent = Read<NameSpace>(client, RawData.ParentPtr);
                Hierarchy.AddRange(parent.Hierarchy);
                Hierarchy.Add(this);
            }
            if (RawData.NamePtr != 0) {
                Name = Read<HashNameNode>(client, RawData.NamePtr);
            }
        }
        public override string ToString() {
            if (Name == null) {
                return "";
            }
            return string.Join("::", Hierarchy.Select(h => h.Name?.Name));
        }
    }
}
