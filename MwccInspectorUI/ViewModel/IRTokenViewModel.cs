using MwccInspector.MwccTypes;
using MwccInspectorUI.MVVM;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Windows.Input;

namespace MwccInspectorUI.ViewModel {
    class IRTokenViewModel : ViewModelBase {
        public class IRToken(string name, MwccCachedType? data) {
            public string Name { get; } = name;
            public MwccCachedType? Data { get; } = data;
            public string TokenType => Data?.GetType().Name ?? "";
            public Cursor HoverCursor => Data != null ? Cursors.Hand : Cursors.Arrow;

            public static IRToken Space = new(" ", null);
        }

        public ObservableCollection<IRToken> Tokens { get; } = [];
        public ICommand TokenClickedInternal { get; }

        public event Action<IRToken>? TokenClicked;

        public IRTokenViewModel(Statement stmt) {
            TokenClickedInternal = new RelayCommand(tokenObj => {
                TokenClicked?.Invoke((IRToken)tokenObj);
            });
            foreach (var token in MakeTokens(stmt)) {
                Tokens.Add(token);
            }
        }

        private static List<IRToken> MakeTokens(Statement stmt) {
            List<IRToken> res = [];
            if (stmt.Type != StatementType.ST_LABEL) {
                res.Add(new("    ", null));
            }
            switch (stmt.Type) {
                case StatementType.ST_EXPRESSION:
                    Debug.Assert(stmt.Expression != null);
                    res.AddRange(MakeTokens(stmt.Expression));
                    break;
                case StatementType.ST_GOTO:
                    Debug.Assert(stmt.Label != null);
                    res.Add(new("Goto", stmt));
                    res.Add(IRToken.Space);
                    res.Add(new(stmt.Label.Name.Name, stmt.Label));
                    break;
                case StatementType.ST_IFGOTO:
                case StatementType.ST_IFNGOTO:
                    Debug.Assert(stmt.Expression != null);
                    Debug.Assert(stmt.Label != null);
                    var ifStr = (stmt.Type == StatementType.ST_IFGOTO) ? "If" : "IfNot";
                    res.Add(new(ifStr, stmt));
                    res.Add(new("(", stmt.Expression));
                    res.AddRange(MakeTokens(stmt.Expression));
                    res.Add(new(")", stmt.Expression));
                    res.Add(IRToken.Space);
                    res.Add(new(stmt.Label.Name.Name, stmt.Label));
                    break;
                case StatementType.ST_RETURN:
                    res.Add(new("Return", stmt));
                    if (stmt.Expression != null) {
                        res.Add(IRToken.Space);
                        res.AddRange(MakeTokens(stmt.Expression));
                    }
                    break;
                case StatementType.ST_LABEL:
                    Debug.Assert(stmt.Label != null);
                    res.Add(new("Label", stmt));
                    res.Add(IRToken.Space);
                    res.Add(new(stmt.Label.Name.Name, stmt.Label));
                    res.Add(new(":", null));
                    break;
                case StatementType.ST_ASM:
                    Debug.Assert(stmt.Asm != null);
                    res.Add(new("Asm", stmt));
                    res.Add(IRToken.Space);
                    res.Add(new("???", stmt.Asm));
                    break;
                case StatementType.ST_NOP:
                    res.Add(new("Nop", stmt));
                    break;
            }
            return res;
        }
        private static List<IRToken> MakeTokens(ENode expr) {
            List<IRToken> res = [];
            if (ENode.DiadicSyms.TryGetValue(expr.Type, out var monadicFormat)) {
                var diadic = (ENodeDataDiadic)expr.Data;
                res.AddRange(MakeTokens(diadic.Lhs));
                res.Add(IRToken.Space);
                res.Add(new($"{monadicFormat}", expr));
                res.Add(IRToken.Space);
                res.AddRange(MakeTokens(diadic.Rhs));
            } else if (ENode.MonadicTypes.TryGetValue(expr.Type, out var diadicFormat)) {
                var (ls, rs) = diadicFormat;
                var monadic = ((ENodeDataMonadic)expr.Data).Value;
                if (ls != "") {
                    res.Add(new(ls, expr));
                }
                res.AddRange(MakeTokens(monadic));
                if (rs != "") {
                    res.Add(new(rs, expr));
                }
            } else {
                switch (expr.Type) {
                    case ENodeType.EOBJREF:
                        res.AddRange(MakeTokens(((ENodeDataObject)expr.Data).Value));
                        break;
                    case ENodeType.EINTCONST:
                        res.Add(new(((ENodeDataIntVal)expr.Data).Value.ToString(), expr));
                        break;
                    case ENodeType.EFLOATCONST:
                        res.Add(new(((ENodeDataFloatVal)expr.Data).Value.ToString(), expr));
                        break;
                    case ENodeType.ESTRINGCONST:
                        res.Add(new($"\"{((ENodeDataStringVal)expr.Data).Value}\"", expr));
                        break;
                    case ENodeType.EFUNCCALL:
                        var funccall = (ENodeDataFuncCall)expr.Data;
                        res.AddRange(MakeTokens(funccall.Func));
                        res.Add(new("(", expr));
                        foreach (var arg in funccall.Args) {
                            res.AddRange(MakeTokens(arg));
                            if (arg != funccall.Args[^1]) {
                                res.Add(new(", ", null));
                            }
                        }
                        res.Add(new(")", expr));
                        break;
                    case ENodeType.EINFO:
                        var info = (ENodeDataInfo)expr.Data;
                        res.Add(new("INFO", expr));
                        break;
                    case ENodeType.ECOND:
                        var cond = (ENodeDataCond)expr.Data;
                        res.AddRange(MakeTokens(cond.Cond));
                        res.Add(IRToken.Space);
                        res.Add(new("?", expr));
                        res.Add(IRToken.Space);
                        res.AddRange(MakeTokens(cond.Lhs));
                        res.Add(IRToken.Space);
                        res.Add(new(":", expr));
                        res.Add(IRToken.Space);
                        res.AddRange(MakeTokens(cond.Rhs));
                        break;
                    default:
                        res.Add(new($"[{expr.Type}]", expr));
                        break;
                }
            }
            return res;
        }
        private static List<IRToken> MakeTokens(NameSpace ns) {
            List<IRToken> res = [];
            for (int i = 0; i < ns.Hierarchy.Count; i++) {
                var curr = ns.Hierarchy[i];
                res.Add(new(curr.Name?.Name ?? "", curr));
                if (i != ns.Hierarchy.Count - 1) {
                    res.Add(new("::", null));
                }
            }
            return res;
        }
        private static List<IRToken> MakeTokens(ObjObject obj) {
            List<IRToken> res = [];
            if (obj.Namespace != null) {
                res = MakeTokens(obj.Namespace);
                if (res.Count > 0) {
                    res.Add(new("::", null));
                }
            }
            res.Add(new(obj.Name.Name, obj));
            return res;
        }
    }
}
