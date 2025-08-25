using mwcc_inspector;
using mwcc_inspector.MwccTypes;
using System.CommandLine;
using System.Diagnostics;

class Program {
    static int Main(string[] args) {

        Argument<string> mwccPath = new("mwcc_path") {
            Description = "Path to MWCC executable",
        };

        Argument<string[]> mwccArgs = new("args") {
            Description = "Arguments passed to MWCC",
            Arity = ArgumentArity.ZeroOrMore
        };

        Option<string> cwd = new("--cwd") {
            Description = "Set working directory for MWCC process"
        };

        RootCommand rootCommand = new("MWCC Inspector");
        rootCommand.Arguments.Add(mwccPath);
        rootCommand.Arguments.Add(mwccArgs);
        rootCommand.Options.Add(cwd);

        rootCommand.SetAction(parseResult => {
            List<string> argsList = [];
            string mwccPathStr = parseResult.GetRequiredValue(mwccPath);
            argsList.Add(parseResult.GetRequiredValue(mwccPath));
            argsList.AddRange(parseResult.GetRequiredValue(mwccArgs));

            if (parseResult.GetValue(cwd) is string userCwd) {
                if (!Directory.Exists(userCwd)) {
                    Console.WriteLine($"Error: Specified working directory does not exist: {userCwd}");
                    return;
                }
                Directory.SetCurrentDirectory(userCwd);
            }

            string mwccFullPath = Path.Join(Directory.GetCurrentDirectory(), mwccPathStr);
            if (!File.Exists(mwccFullPath)) {
                Console.WriteLine($"Could not find MWCC execute at {mwccFullPath}");
                return;
            }

            RunInspector(string.Join(" ", argsList));
        });

        ParseResult parseResult = rootCommand.Parse(args);
        return parseResult.Invoke();
    }

    private static void RunInspector(string commandLine) {

        var dbgInterface = new MwccDebugInterface();
        dbgInterface.PrepareTarget(commandLine);

        dbgInterface.AddBreakpointHandler(0x00592123, (client) => {
            Console.WriteLine($"---------------------------------");
            MwccCachedType.ClearCache();

            uint stmtPtr = (uint)client.Registers.GetValue(client.Registers.GetIndexByName("ebx")).I64;
            uint edi = (uint)client.Registers.GetValue(client.Registers.GetIndexByName("edi")).I64;
            if (edi == 0) {
                Console.WriteLine($"Function: Init-code");
            } else {
                ObjObject func = MwccCachedType.Read<ObjObject>(client, edi);
                Console.WriteLine($"Function: {func}");
            }

            var statements = Statement.ReadStatements(client, stmtPtr);
            foreach (var statement in statements) {
                string source = $":{statement.SourceOffset}";
                Console.Write($"{source,-10}");
                switch (statement.Type) {
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
                        if (statement.Expression != null) {
                            Console.WriteLine($"  Return {statement.Expression}");
                        } else {
                            Console.WriteLine($"  Return");
                        }
                        break;
                    case StatementType.ST_LABEL:
                        Debug.Assert(statement.Label != null);
                        Console.WriteLine($"Label {statement.Label.Name.Name}:");
                        break;
                    case StatementType.ST_ASM:
                        Debug.Assert(statement.Asm != null);
                        Console.WriteLine($"  [asm] {statement.Asm.Opcode}");
                        break;
                    case StatementType.ST_NOP:
                        Console.WriteLine($"  Nop {statement.Expression?.ToString()}");
                        break;
                    default:
                        Console.WriteLine($"{statement.Type} {{ ... }}");
                        continue;
                }
            }
        });

        dbgInterface.Run();
    }
}
