using ClrDebug.DbgEng;
using mwcc_inspector.MwccTypes;
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
        EFUNCCALL, EFUNCCALLP,
        EOBJREF,
        EINSTRUCTION = 83,
    }

    [StructLayout(LayoutKind.Explicit, Pack = 1)]
    struct ENodeBaseRaw
    {
        [FieldOffset(0x0)]
        public byte Type;
    }

    class ENodeData
    {
        public ENodeData(DebugClient client, uint address) { }
    }

    class ENodeDataIntVal(DebugClient client, uint address) : ENodeData(client, address)
    {
        public readonly CInt64 Value = CInt64.Read(client, address);
    }

    class ENodeDataMonadic(DebugClient client, uint address) : ENodeData(client, address)
    {
        public readonly ENode Operand = ENode.ReadPtr(client, address);
    }

    class ENodeDataDiadic(DebugClient client, uint address) : ENodeData(client, address)
    {
        public readonly ENode Lhs = ENode.ReadPtr(client, address);
        public readonly ENode Rhs = ENode.ReadPtr(client, address + 4);
    }

    class ENodeDataObject(DebugClient client, uint address) : ENodeData(client, address)
    {
        public readonly ObjObject Operand = ObjObject.ReadPtr(client, address);
    }

    class ENode : IMwccType<ENode, ENodeBaseRaw>
    {
        public readonly ENodeType Type;
        public readonly ENodeData Data;

        public ENode(DebugClient client, uint address) : base(client, address)
        {
            Type = (ENodeType)RawData.Type;
            var dataAddress = address + 0x10;
            Data = Type switch
            {
                ENodeType.EASS or
                ENodeType.EADD or
                ENodeType.EMUL => new ENodeDataDiadic(client, dataAddress),
                ENodeType.EOBJREF => new ENodeDataObject(client, dataAddress),
                ENodeType.EINTCONST => new ENodeDataIntVal(client, dataAddress),
                _ => new ENodeData(client, dataAddress),
            };
        }

        private static readonly Dictionary<ENodeType, string> DiadicSyms = new()
        {
            { ENodeType.EASS, "=" },
            { ENodeType.EADD, "+" },
            { ENodeType.EMUL, "*" }
        };

        public override string ToString()
        {
            switch (Type)
            {
                case ENodeType.EASS:
                case ENodeType.EADD:
                case ENodeType.EMUL:
                    var diadic = (ENodeDataDiadic)Data;
                    return $"{diadic.Lhs} {DiadicSyms[Type]} {diadic.Rhs}";
                case ENodeType.EOBJREF:
                    var obj = (ENodeDataObject)Data;
                    return obj.Operand.Name.Name;
                case ENodeType.EINTCONST:
                    var intval = (ENodeDataIntVal)Data;
                    return $"{intval.Value:x08}";
                default:
                    return $"(unknown ENode type {Type})";
            }
        }
    }
}
