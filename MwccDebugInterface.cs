using ClrDebug;
using ClrDebug.DbgEng;
using System.Runtime.InteropServices;

namespace mwcc_inspector
{
    internal class MwccDebugInterface
    {
        protected DebugClient Client { get; set; }
        internal DEBUG_STATUS ExecutionStatus { get; set; }
        private bool DebugOutputEnabled { get; set; } = false;
        private readonly List<(DebugBreakpoint, Action<DebugClient>)> breakpoints = [];

        public MwccDebugInterface()
        {
            Client = CreateDebugClient();

            Client.OutputCallbacks = new OutputCallbacks(this);
            Client.EventCallbacks = new EventCallbacks(this);

            Client.Control.EngineOptions = DEBUG_ENGOPT.INITIAL_BREAK | DEBUG_ENGOPT.FINAL_BREAK;
        }

        public void PrepareTarget(string args)
        {
            var flags = DEBUG_CREATE_PROCESS.DEBUG_ONLY_THIS_PROCESS;
            Client.CreateProcessAndAttach(0, args, flags, 0, DEBUG_ATTACH.DEFAULT);
            Client.Control.WaitForEvent(DEBUG_WAIT.DEFAULT, -1);
        }

        public void AddBreakpointHandler(long offset, Action<DebugClient> action)
        {
            var bp = Client.Control.AddBreakpoint(DEBUG_BREAKPOINT_TYPE.CODE, DbgEngExtensions.DEBUG_ANY_ID);
            bp.TrySetOffset(offset).ThrowOnNotOK();
            bp.TrySetFlags(DEBUG_BREAKPOINT_FLAG.ENABLED).ThrowOnNotOK();
            breakpoints.Add((bp, action));
        }

        public void Run()
        {
            while (ExecutionStatus != DEBUG_STATUS.NO_DEBUGGEE)
            {
                Client.Control.WaitForEvent(DEBUG_WAIT.DEFAULT, -1);

                if (ExecutionStatus == DEBUG_STATUS.BREAK)
                {
                    var ip = Client.Registers.InstructionOffset;
                    foreach (var (bp, action) in breakpoints)
                    {
                        if (ip == bp.Offset)
                        {
                            action(Client);
                        }
                    }
                    if (Client.Control.TrySetExecutionStatus(DEBUG_STATUS.GO_HANDLED) != HRESULT.S_OK)
                    {
                        break;
                    }
                }
            }
        }

        private static DebugClient CreateDebugClient()
        {
            // See https://github.com/lordmilko/ClrDebug/blob/master/Samples/DbgEngConsole/Debugger.cs#L101
            NativeMethods.SetDllDirectory("C:\\Program Files (x86)\\Windows Kits\\10\\Debuggers\\" + (IntPtr.Size == 8 ? "x64" : "x86"));

            var dbgEng = NativeMethods.LoadLibrary("dbgeng.dll");
            var debugCreatePtr = NativeMethods.GetProcAddress(dbgEng, "DebugCreate");
            var debugCreate = Marshal.GetDelegateForFunctionPointer<DebugCreateDelegate>(debugCreatePtr);

            debugCreate(DebugClient.IID_IDebugClient, out nint debugClientPtr).ThrowOnNotOK();
            var debugClient = new DebugClient(debugClientPtr);

            return debugClient;
        }

        private class OutputCallbacks(MwccDebugInterface debugger) : IDebugOutputCallbacks
        {
            private readonly MwccDebugInterface debugger = debugger;
            public HRESULT Output(DEBUG_OUTPUT mask, string text)
            {
                if (debugger.DebugOutputEnabled)
                {
                    Console.Write(text);
                }
                return HRESULT.S_OK;
            }
        }

        private class EventCallbacks(MwccDebugInterface debugger) : DebugBaseEventCallbacks
        {
            private readonly MwccDebugInterface debugger = debugger;

            public override HRESULT GetInterestMask(out DEBUG_EVENT_TYPE mask)
            {
                mask = DEBUG_EVENT_TYPE.CHANGE_ENGINE_STATE;
                return HRESULT.S_OK;
            }

            public override HRESULT ChangeEngineState(DEBUG_CES flags, long argument)
            {
                if ((flags & DEBUG_CES.EXECUTION_STATUS) != 0)
                {
                    debugger.ExecutionStatus = (DEBUG_STATUS)argument;
                }
                else
                {
                    Console.WriteLine("WARNING: Unhandled engine state change: {0} {1}", flags, argument);
                }

                return HRESULT.S_OK;
            }
        }
    }
}
