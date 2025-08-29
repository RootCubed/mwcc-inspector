using ClrDebug.DbgEng;
using System.Runtime.InteropServices;

namespace MwccInspector.MwccTypes {
    enum TypeType : byte {
        TYPEVOID,
        TYPEINT,
        TYPEFLOAT,
        TYPE_3,
        TYPE_4,
        TYPEENUM,
        TYPESTRUCT,
        TYPECLASS,
        TYPEFUNC,
        TYPEBITFIELD,
        TYPELABEL,
        TYPETEMPLATE,
        TYPEMEMBERPOINTER,
        TYPEPOINTER,
        TYPEARRAY,
        TYPEOBJCID,
        TYPETEMPLDEPEXPR
    }

    enum BasicType : byte {
        IT_BOOL,
        IT_CHAR, IT_SCHAR, IT_UCHAR, IT_WCHAR_T,
        IT_SHORT, IT_USHORT,
        IT_INT, IT_UINT,
        IT_LONG, IT_ULONG,
        IT_LONGLONG, IT_ULONGLONG,
        IT_FLOAT, IT_SHORTDOUBLE,
        IT_DOUBLE, IT_LONGDOUBLE
    }

    [StructLayout(LayoutKind.Explicit, Pack = 1)]
    struct TypeBaseRaw {
        [FieldOffset(0x0)]
        public TypeType Type;
        [FieldOffset(0x1)]
        public int Size;
    }

    [StructLayout(LayoutKind.Explicit, Pack = 1)]
    struct TypeBasicTypeRaw {
        [FieldOffset(0x0)]
        public TypeBaseRaw Base;
        [FieldOffset(0x6)]
        public BasicType BasicType;
    }

    [StructLayout(LayoutKind.Explicit, Pack = 1)]
    struct TypeClassRaw {
        [FieldOffset(0x0)]
        public TypeBaseRaw Base;
        [FieldOffset(0x6)]
        public uint NameSpacePtr;
        [FieldOffset(0xa)]
        public uint ClassNamePtr;
    }

    [StructLayout(LayoutKind.Explicit, Pack = 1)]
    struct FuncArgRaw {
        [FieldOffset(0x0)]
        public uint NextPtr;
        [FieldOffset(0x4)]
        public uint NamePtr;
        [FieldOffset(0x8)]
        public uint ExprPtr;
        [FieldOffset(0xc)]
        public uint TypePtr;
    }

    [StructLayout(LayoutKind.Explicit, Pack = 1)]
    struct TypeFuncRaw {
        [FieldOffset(0x0)]
        public TypeBaseRaw Base;
        [FieldOffset(0x6)]
        public uint ArgsPtr;
        [FieldOffset(0xe)]
        public uint FuncTypePtr;
    }

    [StructLayout(LayoutKind.Explicit, Pack = 1)]
    struct TypePointerRaw {
        [FieldOffset(0x0)]
        public TypeBaseRaw Base;
        [FieldOffset(0x6)]
        public uint TargetTypePtr;
        [FieldOffset(0xa)]
        public uint Quals;
    }

    class TypeBase : MwccType<TypeBaseRaw> {
        public TypeType Type { get; }
        public int Size { get; }
        public TypeBase(DebugClient client, uint address) : base(client, address) {
            Type = RawData.Type;
            Size = RawData.Size;
        }

        public override string ToString() {
            return $"<unhandled type {Type}>";
        }
    }

    class TypeVoid(DebugClient client, uint address) : TypeBase(client, address) {
        public override string ToString() {
            return "void";
        }
    }

    class TypeBasicType : TypeBase {

        private readonly Dictionary<BasicType, string> BasicTypeNames = new() {
            { BasicType.IT_BOOL, "bool" },
            { BasicType.IT_CHAR, "char" },
            { BasicType.IT_SCHAR, "signed char" },
            { BasicType.IT_UCHAR, "unsigned char" },
            { BasicType.IT_WCHAR_T, "wchar_t" },
            { BasicType.IT_SHORT, "short" },
            { BasicType.IT_USHORT, "unsigned short" },
            { BasicType.IT_INT, "int" },
            { BasicType.IT_UINT, "unsigned int" },
            { BasicType.IT_LONG, "long" },
            { BasicType.IT_ULONG, "unsigned long" },
            { BasicType.IT_LONGLONG, "long long" },
            { BasicType.IT_ULONGLONG, "unsigned long long" },
            { BasicType.IT_FLOAT, "float" },
            { BasicType.IT_SHORTDOUBLE, "short double" },
            { BasicType.IT_DOUBLE, "double" },
            { BasicType.IT_LONGDOUBLE, "long double" }
        };

        public BasicType BasicType { get; }

        public TypeBasicType(DebugClient client, uint address) : base(client, address) {
            var data = client.DataSpaces.ReadVirtual<TypeBasicTypeRaw>(address);
            BasicType = data.BasicType;
        }

        public override string ToString() {
            return BasicTypeNames[BasicType];
        }
    }

    class TypeClass : TypeBase {
        public NameSpace NameSpace { get; }
        public HashNameNode ClassName { get; }
        public TypeClass(DebugClient client, uint address) : base(client, address) {
            var data = client.DataSpaces.ReadVirtual<TypeClassRaw>(address);
            NameSpace = Read<NameSpace>(client, data.NameSpacePtr);
            ClassName = Read<HashNameNode>(client, data.ClassNamePtr);
        }
        public override string ToString() {
            return $"class {ClassName.Name}";
        }
    }

    class TypeFunc : TypeBase {
        public class FuncArg : MwccType<FuncArgRaw> {
            public uint NextPtr => RawData.NextPtr;
            public HashNameNode? Name { get; }
            public TypeBase Type { get; }
            public FuncArg(DebugClient client, uint address) : base(client, address) {
                if (RawData.NamePtr != 0) {
                    Name = Read<HashNameNode>(client, RawData.NamePtr);
                }
                Type = MwccType.ReadType(client, RawData.TypePtr);
            }
            public override string ToString() {
                if (Name == null) {
                    return Type.ToString();
                }
                return $"{Type} {Name.Name}";
            }
        }

        public List<FuncArg> FuncArgs { get; } = [];
        public TypeBase FuncType { get; }
        public TypeFunc(DebugClient client, uint address) : base(client, address) {
            var data = client.DataSpaces.ReadVirtual<TypeFuncRaw>(address);
            var argPtr = data.ArgsPtr;
            while (argPtr != 0) {
                var arg = Read<FuncArg>(client, argPtr);
                FuncArgs.Add(arg);
                argPtr = arg.NextPtr;
            }
            FuncType = MwccType.ReadType(client, data.FuncTypePtr);
        }
        public override string ToString() {
            var argsStr = string.Join(", ", FuncArgs);
            return $"function({argsStr})[returns {FuncType}]";
        }
    }

    class TypePointer : TypeBase {
        public TypeBase TargetType { get; }
        public TypePointer(DebugClient client, uint address) : base(client, address) {
            var data = client.DataSpaces.ReadVirtual<TypePointerRaw>(address);
            TargetType = MwccType.ReadType(client, data.TargetTypePtr);
        }
        public override string ToString() {
            return $"{TargetType}*";
        }
    }

    static class MwccType {
        public static TypeBase ReadType(DebugClient client, uint address) {
            var baseRaw = client.DataSpaces.ReadVirtual<TypeBaseRaw>(address);
            return baseRaw.Type switch {
                TypeType.TYPEVOID => new TypeVoid(client, address),
                TypeType.TYPEINT or
                TypeType.TYPEFLOAT => MwccCachedType.Read<TypeBasicType>(client, address),
                TypeType.TYPECLASS => MwccCachedType.Read<TypeClass>(client, address),
                TypeType.TYPEFUNC => MwccCachedType.Read<TypeFunc>(client, address),
                TypeType.TYPEPOINTER => MwccCachedType.Read<TypePointer>(client, address),
                _ => new TypeBase(client, address),
            };
        }
    }
}
