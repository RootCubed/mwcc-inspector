using ClrDebug.DbgEng;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
[assembly: InternalsVisibleTo("MwccInspectorUI")]

namespace MwccInspector.MwccTypes {
    enum StatementType : byte {
        ST_NOP = 1,
        ST_LABEL,
        ST_GOTO,
        ST_EXPRESSION,
        ST_SWITCH,
        ST_IFGOTO, ST_IFNGOTO,
        ST_RETURN,
        ST_OVF,
        ST_EXIT, ST_ENTRY,
        ST_BEGINCATCH, ST_ENDCATCH, ST_ENDCATCHDTOR,
        ST_GOTOEXPR,
        ST_ASM,
        ST_BEGINLOOP, ST_ENDLOOP,
        ST_ILLEGAL
    }

    [StructLayout(LayoutKind.Explicit, Pack = 1)]
    struct StatementRaw {
        [FieldOffset(0x0)]
        public uint NextPtr;
        [FieldOffset(0x4)]
        public StatementType Type;
        [FieldOffset(0xa)]
        public uint ENodePtr; // Or InlineAsmPtr, for ST_ASM
        [FieldOffset(0xe)]
        public uint LabelPtr;
        [FieldOffset(0x1a)]
        public int SourceOffset;
    }

    class Statement : MwccType<StatementRaw> {
        public readonly StatementType Type;
        public readonly ENode? Expression;
        public readonly InlineAsm? Asm;
        public readonly CLabel? Label;
        public readonly int SourceOffset;

        public Statement(DebugClient client, uint address) : base(client, address) {
            Type = RawData.Type;
            switch (Type) {
                case StatementType.ST_EXPRESSION:
                case StatementType.ST_IFGOTO:
                case StatementType.ST_IFNGOTO:
                    Expression = Read<ENode>(client, RawData.ENodePtr);
                    break;
                case StatementType.ST_RETURN:
                    if (RawData.ENodePtr != 0) {
                        Expression = Read<ENode>(client, RawData.ENodePtr);
                    }
                    break;
                case StatementType.ST_ASM:
                    Asm = Read<InlineAsm>(client, RawData.ENodePtr);
                    break;
            }
            switch (Type) {
                case StatementType.ST_LABEL:
                case StatementType.ST_GOTO:
                case StatementType.ST_IFGOTO:
                case StatementType.ST_IFNGOTO:
                    Label = Read<CLabel>(client, RawData.LabelPtr);
                    break;
            }
            SourceOffset = RawData.SourceOffset;
        }

        public static List<Statement> ReadStatements(DebugClient client, uint address) {
            List<Statement> statements = [];
            while (address != 0) {
                var stmt = new Statement(client, address);
                statements.Add(stmt);
                address = stmt.RawData.NextPtr;
            }
            return statements;
        }

        public override string ToString() {
            switch (Type) {
                case StatementType.ST_EXPRESSION:
                    Debug.Assert(Expression != null);
                    return Expression.ToString();
                case StatementType.ST_GOTO:
                    Debug.Assert(Label != null);
                    return $"Goto {Label.Name.Name}";
                case StatementType.ST_IFGOTO:
                case StatementType.ST_IFNGOTO:
                    Debug.Assert(Expression != null);
                    Debug.Assert(Label != null);
                    var ifStr = (Type == StatementType.ST_IFGOTO) ? "If" : "IfNot";
                    return $"{ifStr} ({Expression}) {Label.Name.Name}";
                case StatementType.ST_RETURN:
                    if (Expression != null) {
                        return $"Return {Expression}";
                    } else {
                        return "Return";
                    }
                case StatementType.ST_LABEL:
                    Debug.Assert(Label != null);
                    return $"Label {Label.Name.Name}:";
                case StatementType.ST_ASM:
                    Debug.Assert(Asm != null);
                    return $"[asm] {Asm.Opcode}";
                case StatementType.ST_NOP:
                    return $"Nop {Expression?.ToString()}";
                default:
                    return $"{Type} {{ ... }}";
            }
        }
    }
}
