using ClrDebug.DbgEng;
using System.Runtime.InteropServices;

namespace mwcc_inspector.MwccTypes {
    enum Opcode : uint { };

    [StructLayout(LayoutKind.Explicit, Pack = 1)]
    struct InlineAsmRaw {
        [FieldOffset(0x0)]
        public Opcode Opcode;
    }

    class InlineAsm : MwccType<InlineAsmRaw> {
        public readonly Opcode Opcode;

        public InlineAsm(DebugClient client, uint address) : base(client, address) {
            Opcode = RawData.Opcode;
        }
    }
}
