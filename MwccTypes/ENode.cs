using ClrDebug.DbgEng;
using mwcc_inspector.MwccTypes;
using System.Diagnostics;
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

    interface IENodeData { }

    class ENodeNotImplemented : IENodeData {
        public ENodeNotImplemented(ENodeType type) {
            Debug.WriteLine($"Unhandled ENode type {type}");
        }
    }

    class ENodeMwccData<T>(DebugClient client, uint address) : IENodeData {
        public readonly T Value = client.DataSpaces.ReadVirtual<T>(address);
    }
    class ENodeMwccPtrData<T>(DebugClient client, uint address) : IENodeData where T : MwccCachedType {
        protected readonly T Data = MwccCachedType.ReadPtr<T>(client, address);
    }

    class ENodeDataIntVal(DebugClient client, uint address) : CInt64(client, address), IENodeData;
    class ENodeDataFloatVal(DebugClient client, uint address) : ENodeMwccData<double>(client, address);

    class ENodeDataStringVal : IENodeData {
        public readonly string Value;
        public ENodeDataStringVal(DebugClient client, uint address) {
            var ptr = client.DataSpaces.ReadVirtual<uint>(address + 4);
            Value = client.DataSpaces.ReadMultiByteStringVirtual(ptr, 255);
        }
    }

    class ENodeDataMonadic(DebugClient client, uint address) : IENodeData {
        public readonly ENode Operand = MwccCachedType.ReadPtr<ENode>(client, address);
    }

    class ENodeDataDiadic(DebugClient client, uint address) : IENodeData {
        public readonly ENode Lhs = MwccCachedType.ReadPtr<ENode>(client, address);
        public readonly ENode Rhs = MwccCachedType.ReadPtr<ENode>(client, address + 4);
    }

    class ENodeDataObject(DebugClient client, uint address) : IENodeData {
        public readonly ObjObject Operand = MwccCachedType.ReadPtr<ObjObject>(client, address);
    }

    class ENodeDataFuncCall : IENodeData {
        public readonly ENode Func;
        public readonly List<ENode> Args = [];
        public ENodeDataFuncCall(DebugClient client, uint address) {
            var rawData = client.DataSpaces.ReadVirtual<ENodeFuncCallRaw>(address);
            Func = MwccCachedType.Read<ENode>(client, rawData.FuncPtr);
            var currNodeListPtr = rawData.ArgsPtr;
            while (currNodeListPtr != 0) {
                Args.Add(MwccCachedType.ReadPtr<ENode>(client, currNodeListPtr + 4));
                currNodeListPtr = client.DataSpaces.ReadVirtual<uint>(currNodeListPtr);
            }
        }
    }

    class ENodeDataInfo : IENodeData {
        public readonly int Type;
        public readonly ENode? Ref;
        public ENodeDataInfo(DebugClient client, uint address) {
            var rawData = client.DataSpaces.ReadVirtual<ENodeInfoRaw>(address);
            Type = rawData.Type;
            if (Type == 5) {
                Ref = MwccCachedType.Read<ENode>(client, rawData.NodePtr);
            }
        }
    }

    class ENode : MwccType<ENodeBaseRaw> {
        public readonly ENodeType Type;
        public readonly IENodeData Data;

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
                    _ => new ENodeNotImplemented(Type)
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
