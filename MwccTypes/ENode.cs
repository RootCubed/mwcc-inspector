using ClrDebug.DbgEng;
using mwcc_inspector.MwccTypes;
using System.Runtime.InteropServices;

namespace mwcc_inspector {
    enum ENodeType : byte {
        EPOSTINC, EPOSTDEC, EPREINC, EPREDEC,
        EINDIRECT,
        EMONMIN,
        EBINNOT, ELOGNOT,
        EFORCELOAD,
        EMUL, EMULV, EDIV, EMODULO, EADDV, ESUBV,
        EADD, ESUB, ESHL, ESHR,
        ELESS, EGREATER, ELESSEQU, EGREATEREQU,
        EEQU, ENOTEQU,
        EAND, EXOR, EOR, ELAND, ELOR,
        EASS,
        EMULASS, EDIVASS, EMODASS, EADDASS, ESUBASS, ESHLASS, ESHRASS,
        EANDASS, EXORASS, EORASS,
        ECOMMA,
        EMIN, EMAX,
        EPMODULO,
        EROTL, EROTR,
        EBCLR, EBTST, EBSET,
        ETYPCON,
        EBITFIELD,
        EINTCONST, EFLOATCONST, E_UNK_54, ESTRINGCONST,
        ECOND,
        EFUNCCALL, EFUNCCALLP,
        EOBJREF,
        ENULLCHECK,
        EPRECOMP,
        ELABEL,
        EGCCASM,
        ESCOPEBEGIN, ESCOPEEND,
        EINFO,
        EMFPOINTER,
        ETEMP,
        ELOGOBJ, EARGOBJ,
        ESETCONST,
        ENEWEXCEPTION, ENEWEXCEPTIONARRAY,
        EINITTRYCATCH,
        EOBJACCESS,
        ETEMPLDEP,
        EPOINTERSTAR, EDOTSTAR,
        ECTORINIT,
        ESTMT,
        E_UNK_81, E_UNK_82,
        EINSTRUCTION,
        EDEFINE,
        EREUSE,
        EASSBLK,
        EVECTORCONST,
        ECONDASS
    }

    [StructLayout(LayoutKind.Explicit, Pack = 1)]
    struct ENodeBaseRaw {
        [FieldOffset(0x0)]
        public ENodeType Type;
    }

    [StructLayout(LayoutKind.Explicit, Pack = 1)]
    struct ENodeFuncCallRaw {
        [FieldOffset(0x0)]
        public uint FuncPtr;
        [FieldOffset(0x4)]
        public uint ArgsPtr;
        [FieldOffset(0x8)]
        public uint FuncTypePtr;
    }

    [StructLayout(LayoutKind.Explicit, Pack = 1)]
    struct ENodeInfoRaw {
        [FieldOffset(0x0)]
        public uint NodePtr;
        [FieldOffset(0x4)]
        public int Type;
    }

    class ENodeData {
        public ENodeData(DebugClient client, uint address) { }
    }

    class ENodeDataIntVal(DebugClient client, uint address) : ENodeData(client, address) {
        public readonly CInt64 Value = CInt64.Read(client, address);
    }

    class ENodeDataFloatVal(DebugClient client, uint address) : ENodeData(client, address) {
        public readonly double Value = client.DataSpaces.ReadVirtual<double>(address);
    }

    class ENodeDataStringVal : ENodeData {
        public readonly string Value;
        public ENodeDataStringVal(DebugClient client, uint address) : base(client, address) {
            var ptr = client.DataSpaces.ReadVirtual<uint>(address + 4);
            Value = client.DataSpaces.ReadMultiByteStringVirtual(ptr, 255);
        }
    }

    class ENodeDataMonadic(DebugClient client, uint address) : ENodeData(client, address) {
        public readonly ENode Operand = ENode.ReadPtr(client, address);
    }

    class ENodeDataDiadic(DebugClient client, uint address) : ENodeData(client, address) {
        public readonly ENode Lhs = ENode.ReadPtr(client, address);
        public readonly ENode Rhs = ENode.ReadPtr(client, address + 4);
    }

    class ENodeDataObject(DebugClient client, uint address) : ENodeData(client, address) {
        public readonly ObjObject Operand = ObjObject.ReadPtr(client, address);
    }

    class ENodeDataFuncCall : ENodeData {
        public readonly ENode Func;
        public readonly List<ENode> Args = [];
        public ENodeDataFuncCall(DebugClient client, uint address) : base(client, address) {
            var rawData = client.DataSpaces.ReadVirtual<ENodeFuncCallRaw>(address);
            Func = ENode.Read(client, rawData.FuncPtr);
            var currPtr = rawData.ArgsPtr;
            while (currPtr != 0) {
                var nodePtr = client.DataSpaces.ReadVirtual<uint>(currPtr + 4);
                Args.Add(ENode.Read(client, nodePtr));
                currPtr = client.DataSpaces.ReadVirtual<uint>(currPtr);
            }
        }
    }

    class ENodeDataInfo : ENodeData {
        public readonly int Type;
        public readonly ENode? Ref;
        public ENodeDataInfo(DebugClient client, uint address) : base(client, address) {
            var rawData = client.DataSpaces.ReadVirtual<ENodeInfoRaw>(address);
            Type = rawData.Type;
            if (Type == 5) {
                Ref = ENode.Read(client, rawData.NodePtr);
            }
        }
    }

    class ENode : IMwccType<ENode, ENodeBaseRaw> {
        public readonly ENodeType Type;
        public readonly ENodeData Data;

        private static readonly Dictionary<ENodeType, string> DiadicSyms = new()
        {
            { ENodeType.EASS, "=" },
            { ENodeType.EMUL, "*" },
            { ENodeType.EDIV, "/" },
            { ENodeType.EADD, "+" },
            { ENodeType.ESUB, "-" },
            { ENodeType.ESHL, "<<" },
            { ENodeType.ESHR, ">>" },
            { ENodeType.EAND, "&" },
            { ENodeType.EXOR, "^" },
            { ENodeType.EOR, "|" },
            { ENodeType.ELAND, "&&" },
            { ENodeType.ELOR, "||" },
            { ENodeType.ELESS, "<" },
            { ENodeType.EMULASS, "*=" },
            { ENodeType.EDIVASS, "/=" },
            { ENodeType.EADDASS, "+=" },
            { ENodeType.ESUBASS, "-=" },
            { ENodeType.EMODASS, "%=" },
            { ENodeType.ESHLASS, "<<=" },
            { ENodeType.ESHRASS, ">>=" },
            { ENodeType.EANDASS, "&=" },
            { ENodeType.EXORASS, "^=" },
            { ENodeType.EORASS, "|=" },
            { ENodeType.EMODULO, "%" },
            { ENodeType.EGREATER, ">" },
            { ENodeType.ELESSEQU, "<=" },
            { ENodeType.EGREATEREQU, ">=" },
            { ENodeType.EEQU, "==" },
            { ENodeType.ENOTEQU, "!=" }
        };

        private static readonly Dictionary<ENodeType, string> MonadicTypes = new()
        {
            { ENodeType.EPOSTINC, "$++" },
            { ENodeType.EPOSTDEC, "$--" },
            { ENodeType.EPREINC, "++$" },
            { ENodeType.EPREDEC, "--$" },
            { ENodeType.EINDIRECT, "[$]" },
            { ENodeType.EMONMIN, "-$" },
            { ENodeType.EBINNOT, "!$" },
            { ENodeType.ELOGNOT, "!$" },
            { ENodeType.EFORCELOAD, "FORCELOAD($)" },
            { ENodeType.ETYPCON, "TYPCON($)" },
            { ENodeType.EBITFIELD, "BITFIELD($)" }
        };

        public ENode(DebugClient client, uint address) : base(client, address) {
            Type = RawData.Type;
            var dataAddress = address + 0x10;
            if (DiadicSyms.ContainsKey(Type)) {
                Data = new ENodeDataDiadic(client, dataAddress);
            } else if (MonadicTypes.ContainsKey(Type)) {
                Data = new ENodeDataMonadic(client, dataAddress);
            } else {
                Data = Type switch {
                    ENodeType.EOBJREF => new ENodeDataObject(client, dataAddress),
                    ENodeType.EFUNCCALL => new ENodeDataFuncCall(client, dataAddress),
                    ENodeType.EINFO => new ENodeDataInfo(client, dataAddress),
                    ENodeType.EINTCONST => new ENodeDataIntVal(client, dataAddress),
                    ENodeType.EFLOATCONST => new ENodeDataFloatVal(client, dataAddress),
                    ENodeType.ESTRINGCONST => new ENodeDataStringVal(client, dataAddress),
                    _ => new ENodeData(client, dataAddress),
                };
            }
        }

        public override string ToString() {
            if (DiadicSyms.TryGetValue(Type, out string? value)) {
                var diadic = (ENodeDataDiadic)Data;
                return $"{diadic.Lhs} {value} {diadic.Rhs}";
            } else if (MonadicTypes.TryGetValue(Type, out value)) {
                var monadic = (ENodeDataMonadic)Data;
                return $"{value.Replace("$", monadic.Operand.ToString())}";
            } else {
                switch (Type) {
                    case ENodeType.EOBJREF:
                        var obj = (ENodeDataObject)Data;
                        return obj.Operand.ToString();
                    case ENodeType.EINTCONST:
                        return $"{((ENodeDataIntVal)Data).Value}";
                    case ENodeType.EFLOATCONST:
                        return $"{((ENodeDataFloatVal)Data).Value}";
                    case ENodeType.ESTRINGCONST:
                        return $"\"{((ENodeDataStringVal)Data).Value}\"";
                    case ENodeType.EFUNCCALL:
                        var funcall = (ENodeDataFuncCall)Data;
                        return $"{funcall.Func}({string.Join(", ", funcall.Args)})";
                    case ENodeType.EINFO:
                        var info = (ENodeDataInfo)Data;
                        return info.Type switch {
                            5 => $"NODE_INFO({info.Ref})",
                            _ => $"INFO_TYPE_{info.Type}(<???>)"
                        };
                    default:
                        return $"(unknown ENode type {Type})";
                }
            }
        }
    }
}
