using ClrDebug.DbgEng;
using System.Diagnostics;
using System.Runtime.InteropServices;

namespace mwcc_inspector.MwccTypes
{
    enum StatementType : byte
    {
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
    struct StatementRaw
    {
        [FieldOffset(0x0)]
        public uint NextPtr;
        [FieldOffset(0x4)]
        public byte Type;
        [FieldOffset(0xa)]
        public uint ENodePtr;
        [FieldOffset(0xe)]
        public uint LabelPtr;
    }

    class Statement : IMwccType<Statement, StatementRaw>
    {
        public readonly StatementType Type;
        public ENode? Expression;
        public CLabel? Label;

        public Statement(DebugClient client, uint address) : base(client, address)
        {
            Type = (StatementType)RawData.Type;
            if (Type == StatementType.ST_LABEL)
            {
                Debug.Assert(RawData.LabelPtr != 0);
                Label = CLabel.Read(client, RawData.LabelPtr);
            }
            if (Type == StatementType.ST_EXPRESSION)
            {
                Debug.Assert(RawData.ENodePtr != 0);
                Expression = ENode.Read(client, RawData.ENodePtr);
            }
        }

        public static List<Statement> ReadStatements(DebugClient client, uint address)
        {
            List<Statement> statements = [];
            while (address != 0)
            {
                var stmt = new Statement(client, address);
                statements.Add(stmt);
                address = stmt.RawData.NextPtr;
            }
            return statements;
        }
    }
}
