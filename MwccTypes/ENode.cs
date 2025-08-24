using ClrDebug.DbgEng;
using System.Runtime.InteropServices;

namespace mwcc_inspector
{
    enum ENodeType : byte
    {
        EPOSTINC, EPOSTDEC, EPREINC, EPREDEC,
        EINDIRECT,
        EMONMIN,
        EBINNOT, ELOGNOT,
        EFORCELOAD,
        EMUL, EMULV, EDIV, EMODULO, EADDV, ESUBV,
        EADD, ESUB, ESHL, ESHR,
        ELESS, EGREATER, ELESSEQU, EGREATEREQU,
        EEQU, ENOTEQU,
        EAND, EXOR, EOR, ELAND, ELOR, EASS,
        EMULASS, EDIVASS, EMODASS, EADDASS, ESUBASS, ESHLASS, ESHRASS,
        EANDASS, EXORASS, EORASS,
        ECOMMA,
        EPMODULO,
        EROTL, EROTR,
        EBCLR, EBTST, EBSET,
        ETYPCON = 50,
        EBITFIELD = 51,
        EINTCONST, EFLOATCONST, ESTRINGCONST,
        ECOND = 56,
        EINSTRUCTION = 83,
    }

    [StructLayout(LayoutKind.Explicit, Pack = 1)]
    struct ENodeBaseRaw
    {
        [FieldOffset(0x0)]
        public byte Type;
    }

    class ENode
    {
        public ENodeType Type { get; set; }
        public ENode(byte[] data)
        {
            var raw = MemoryMarshal.Read<ENodeBaseRaw>(data);
            Type = (ENodeType)raw.Type;
        }

        public static ENode ReadENode(DebugClient client, long address)
        {
            byte[] buffer = client.DataSpaces.ReadVirtual(address, Marshal.SizeOf<ENodeBaseRaw>());
            return new ENode(buffer);
        }
    }
}
