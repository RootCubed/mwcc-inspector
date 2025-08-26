using ClrDebug.DbgEng;
using MwccInspector;
using MwccInspector.MwccTypes;

namespace MwccInspectorUI.Model {
    internal class MwccDebugger {
        public class Snapshot(string functionName, List<Statement> statements) : EventArgs {
            public string FunctionName { get; set; } = functionName;
            public List<Statement> Statements { get; set; } = statements;
        }

        public event EventHandler<Snapshot>? SnapshotBuilt;

        private readonly MwccDebugInterface DebugInterface;

        public MwccDebugger(string commandLine) {
            DebugInterface = new MwccDebugInterface(commandLine);
            DebugInterface.AddBreakpointHandler(0x00592123, OnBreakpointHit);
        }

        public void Run() {
            DebugInterface.Run();
        }

        private void OnBreakpointHit(DebugClient client) {
            Console.WriteLine($"---------------------------------");
            MwccCachedType.ClearCache();

            uint stmtPtr = (uint)client.Registers.GetValue(client.Registers.GetIndexByName("ebx")).I64;
            uint edi = (uint)client.Registers.GetValue(client.Registers.GetIndexByName("edi")).I64;
            string funcName = "Init-code";
            if (edi != 0) {
                ObjObject func = MwccCachedType.Read<ObjObject>(client, edi);
                Console.WriteLine($"Function: {func}");
                funcName = func.Name.Name;
            }
            var snapshot = BuildSnapshot(client, stmtPtr);
            SnapshotBuilt?.Invoke(this, new Snapshot(funcName, snapshot));
        }

        private static List<Statement> BuildSnapshot(DebugClient client, uint statementPtr) {
            var statements = Statement.ReadStatements(client, statementPtr);
            foreach (var statement in statements) {
                string source = $":{statement.SourceOffset}";
                Console.WriteLine($"{source,-10} {statement}");
            }
            return statements;
        }
    }
}
