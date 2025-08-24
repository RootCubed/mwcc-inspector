using mwcc_inspector;
using mwcc_inspector.MwccTypes;
using System.Diagnostics;

Console.WriteLine("- MWCC Inspector -");

var dbgInterface = new MwccDebugInterface();
dbgInterface.PrepareTarget();

dbgInterface.AddBreakpointHandler(0x005b506a, (client) =>
{
    Console.WriteLine("Hit breakpoint. Dumping IR...");
    uint stmtPtr = (uint)client.Registers.GetValue(client.Registers.GetIndexByName("esi")).I64;

    var statements = Statement.ReadStatements(client, stmtPtr);
    foreach (var statement in statements)
    {
        switch (statement.Type)
        {
            case StatementType.ST_EXPRESSION:
                Debug.Assert(statement.Expression != null);
                Console.WriteLine($"  {statement.Expression}");
                break;
            case StatementType.ST_GOTO:
                Debug.Assert(statement.Label != null);
                Console.WriteLine($"  Goto {statement.Label.Name.Name}");
                break;
            case StatementType.ST_IFGOTO:
            case StatementType.ST_IFNGOTO:
                Debug.Assert(statement.Expression != null);
                Debug.Assert(statement.Label != null);
                var ifStr = (statement.Type == StatementType.ST_IFGOTO) ? "If" : "IfNot";
                Console.WriteLine($"  {ifStr} ({statement.Expression}) {statement.Label.Name.Name}");
                break;
            case StatementType.ST_RETURN:
                if (statement.Expression != null)
                {
                    Console.WriteLine($"  Return {statement.Expression}");
                }
                else
                {
                    Console.WriteLine($"  Return");
                }
                break;
            case StatementType.ST_LABEL:
                Debug.Assert(statement.Label != null);
                Console.WriteLine($"Label {statement.Label.Name.Name}:");
                break;
            case StatementType.ST_NOP:
                Console.WriteLine($"  Nop");
                break;
            default:
                Console.WriteLine($"{statement.Type} {{ ... }}");
                continue;
        }
    }
});

dbgInterface.Run();
