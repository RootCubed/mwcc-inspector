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
            Dictionary<string, string> BuildDateToVersion = new() {
                { "Jul 22 2004", "GC/2.7" },
                { "Jan 26 2006", "GC/3.0a3" },
                { "Aug 31 2006", "GC/3.0" },
                { "Aug 26 2008", "Wii/1.0" },
                { "Apr  2 2009", "Wii/1.1" },
            };
            Dictionary<string, uint> BreakpointsForVersion = new() {
                { "GC/2.7", 0x0042de25 },
                { "GC/3.0a3", 0x005759f3 },
                { "GC/3.0", 0x00577703 },
                { "Wii/1.0", 0x0058d743 },
                { "Wii/1.1", 0x00592123 },
            };
            var buildDate = DebugInterface.GetBuildDate();
            if (!BuildDateToVersion.TryGetValue(buildDate, out string? versionName)) {
                throw new Exception($"Unknown mwcc version (Build date {buildDate})");
            }

            Console.WriteLine($"Detected mwcc version {versionName}");
#if MWCC_WII_1_1
            if (versionName != "Wii/1.1") {
#elif MWCC_WII_1_0
            if (versionName != "Wii/1.0") {
#elif MWCC_GC_3_0
            if (versionName != "GC/3.0" &&
                versionName != "GC/3.0a3") {
#else
            if (versionName != "GC/2.7") {
#endif
                throw new Exception($"Wrong version of mwccinspector");
            }
            var bpAddress = BreakpointsForVersion[versionName];
            DebugInterface.AddBreakpointHandler(bpAddress, OnBreakpointHit);
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
