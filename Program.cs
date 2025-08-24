using mwcc_inspector;
using mwcc_inspector.MwccTypes;
using System.Diagnostics;

Console.WriteLine("- MWCC Inspector -");

var dbgInterface = new MwccDebugInterface();
dbgInterface.PrepareTarget();

dbgInterface.AddBreakpointHandler(0x0046c10e, (client) =>
{
    Console.WriteLine("Hit breakpoint. Dumping IR...");
    var stmtPtr = client.Registers.GetValue(client.Registers.GetIndexByName("ebp")).I32;

    var statements = Statement.ReadStatements(client, stmtPtr);
    foreach (var statement in statements)
    {
        switch (statement.Type)
        {
            case StatementType.ST_NOP:
            case StatementType.ST_LABEL:
            case StatementType.ST_GOTO:
            case StatementType.ST_SWITCH:
            case StatementType.ST_IFGOTO:
            case StatementType.ST_IFNGOTO:
            case StatementType.ST_RETURN:
            case StatementType.ST_OVF:
            case StatementType.ST_EXIT:
            case StatementType.ST_ENTRY:
            case StatementType.ST_BEGINCATCH:
            case StatementType.ST_ENDCATCH:
            case StatementType.ST_ENDCATCHDTOR:
            case StatementType.ST_GOTOEXPR:
            case StatementType.ST_ASM:
            case StatementType.ST_BEGINLOOP:
            case StatementType.ST_ENDLOOP:
            case StatementType.ST_ILLEGAL:
                Console.WriteLine("{0}", statement.Type);
                continue;
            case StatementType.ST_EXPRESSION:
                Debug.Assert(statement.Expression != null);
                Console.WriteLine("{0}[{1}]", statement.Type, statement.Expression.Type);
                break;
        }
    }
});

dbgInterface.Run();
