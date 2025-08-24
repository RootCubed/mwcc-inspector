using ClrDebug.DbgEng;
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
        public long ENodePtr;
    }

    class Statement
    {
        private uint NextPtr { get; }
        public StatementType Type { get; }
        public ENode? Expression { get; }
        public Statement(DebugClient client, byte[] data)
        {
            var raw = MemoryMarshal.Read<StatementRaw>(data);
            NextPtr = raw.NextPtr;
            Type = (StatementType)raw.Type;
            if (Type == StatementType.ST_EXPRESSION)
            {
                Expression = ENode.ReadENode(client, raw.ENodePtr);
            }
        }

        public static Statement ReadStatement(DebugClient client, long address)
        {
            byte[] buffer = client.DataSpaces.ReadVirtual(address, Marshal.SizeOf<StatementRaw>());
            return new Statement(client, buffer);
        }

        public static List<Statement> ReadStatements(DebugClient client, long address)
        {
            List<Statement> statements = [];
            while (address != 0)
            {
                var stmt = ReadStatement(client, address);
                statements.Add(stmt);
                address = stmt.NextPtr;
            }
            return statements;
        }
    }
}
