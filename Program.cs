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
                Console.WriteLine(string.Join(Environment.NewLine,
                    "Expression {",
                    $"  {statement.Expression}",
                    "}"
                ));
                break;
            case StatementType.ST_LABEL:
                Debug.Assert(statement.Label != null);
                Console.WriteLine(string.Join(Environment.NewLine,
                    "Label {",
                    $" name='{statement.Label.Name.Name}'",
                    $" uniqueName='{statement.Label.Name.Name}'",
                    "}"
                ));
                break;
            default:
                Console.WriteLine($"{statement.Type} {{ ... }}");
                continue;
        }
    }
});

dbgInterface.Run();
