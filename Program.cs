using mwcc_inspector;
using mwcc_inspector.MwccTypes;
using System.Diagnostics;

Console.WriteLine("- MWCC Inspector -");

var dbgInterface = new MwccDebugInterface();
dbgInterface.PrepareTarget();

dbgInterface.AddBreakpointHandler(0x0046c10e, (client) =>
{
    Console.WriteLine("Hit breakpoint. Dumping IR...");
    uint stmtPtr = (uint)client.Registers.GetValue(client.Registers.GetIndexByName("ebp")).I64;

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
                Console.WriteLine($"  goto {statement.Label.Name.Name}");
                break;
            case StatementType.ST_IFGOTO:
            case StatementType.ST_IFNGOTO:
                Debug.Assert(statement.Expression != null);
                Debug.Assert(statement.Label != null);
                var n = (statement.Type == StatementType.ST_IFGOTO) ? "" : " not";
                Console.WriteLine($"  if{n} {statement.Expression} goto {statement.Label.Name.Name}");
                break;
            case StatementType.ST_RETURN:
                if (statement.Expression != null)
                {
                    Console.WriteLine($"  return {statement.Expression}");
                }
                else
                {
                    Console.WriteLine($"  return");
                }
                break;
            case StatementType.ST_LABEL:
                Debug.Assert(statement.Label != null);
                Console.WriteLine($"{statement.Label.Name.Name}:");
                break;
            default:
                Console.WriteLine($"{statement.Type} {{ ... }}");
                continue;
        }
    }
});

dbgInterface.Run();
