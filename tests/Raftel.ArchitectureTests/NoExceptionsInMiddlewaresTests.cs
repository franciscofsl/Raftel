using System.Reflection;
using System.Reflection.Emit;
using Raftel.Application;
using Raftel.Application.Exceptions;
using Shouldly;

namespace Raftel.ArchitectureTests;

/// <summary>
/// Statically verifies that no type in <c>Raftel.Application.Middlewares</c> constructs a
/// <see cref="ValidationException"/> or <see cref="UnauthorizedException"/> (i.e. throws one),
/// by scanning each method's IL for a <c>newobj</c> instruction targeting either constructor.
/// </summary>
public class NoExceptionsInMiddlewaresTests
{
    private static readonly Assembly ApplicationAssembly = typeof(RaftelApplicationBuilder).Assembly;

    private static readonly Type[] ForbiddenExceptionTypes = [typeof(ValidationException), typeof(UnauthorizedException)];

    [Fact]
    public void MiddlewareTypes_Should_NotConstructValidationOrUnauthorizedException()
    {
        var middlewareTypes = ApplicationAssembly.GetTypes()
            .Where(type => type.Namespace == "Raftel.Application.Middlewares")
            .Where(type => type is { IsInterface: false, IsAbstract: false })
            .ToList();

        var offenders = new List<string>();

        foreach (var type in middlewareTypes)
        {
            foreach (var method in type.GetMethods(BindingFlags.Public | BindingFlags.NonPublic |
                                                     BindingFlags.Instance | BindingFlags.Static |
                                                     BindingFlags.DeclaredOnly))
            {
                foreach (var constructedType in GetConstructedTypes(method))
                {
                    if (ForbiddenExceptionTypes.Contains(constructedType))
                    {
                        offenders.Add($"{type.FullName}.{method.Name} constructs {constructedType.Name}");
                    }
                }
            }
        }

        offenders.ShouldBeEmpty(
            $"No type in Raftel.Application.Middlewares may throw ValidationException or UnauthorizedException. Offenders: {string.Join(", ", offenders)}");
    }

    private static IEnumerable<Type> GetConstructedTypes(MethodBase method)
    {
        var body = method.GetMethodBody();
        if (body is null)
        {
            yield break;
        }

        var il = body.GetILAsByteArray();
        if (il is null)
        {
            yield break;
        }

        var index = 0;
        while (index < il.Length)
        {
            var opCode = ReadOpCode(il, ref index);
            var operandSize = OperandSizeOf(opCode, il, index);

            if (opCode == OpCodes.Newobj)
            {
                var token = BitConverter.ToInt32(il, index);
                Type? declaringType = null;
                try
                {
                    declaringType = (method.Module.ResolveMember(token,
                        method.DeclaringType?.GetGenericArguments(),
                        method is MethodInfo { IsGenericMethod: true } mi ? mi.GetGenericArguments() : null) as ConstructorInfo)?.DeclaringType;
                }
                catch
                {
                    // Token could not be resolved (e.g. generic context quirks); skip rather than fail the scan.
                }

                if (declaringType is not null)
                {
                    yield return declaringType;
                }
            }

            index += operandSize;
        }
    }

    private static OpCode ReadOpCode(byte[] il, ref int index)
    {
        var code = il[index];
        if (code != 0xFE)
        {
            index += 1;
            return SingleByteOpCodes[code];
        }

        var second = il[index + 1];
        index += 2;
        return MultiByteOpCodes[second];
    }

    private static int OperandSizeOf(OpCode opCode, byte[] il, int index)
    {
        switch (opCode.OperandType)
        {
            case OperandType.InlineNone:
                return 0;
            case OperandType.ShortInlineBrTarget:
            case OperandType.ShortInlineI:
            case OperandType.ShortInlineVar:
                return 1;
            case OperandType.InlineVar:
                return 2;
            case OperandType.InlineSwitch:
                var caseCount = BitConverter.ToInt32(il, index);
                return 4 + (caseCount * 4);
            case OperandType.InlineI:
            case OperandType.InlineBrTarget:
            case OperandType.InlineField:
            case OperandType.InlineMethod:
            case OperandType.InlineSig:
            case OperandType.InlineString:
            case OperandType.InlineTok:
            case OperandType.InlineType:
            case OperandType.ShortInlineR:
                return 4;
            case OperandType.InlineI8:
            case OperandType.InlineR:
                return 8;
            default:
                throw new NotSupportedException($"Unsupported operand type: {opCode.OperandType}");
        }
    }

    private static readonly OpCode[] SingleByteOpCodes = new OpCode[0x100];
    private static readonly OpCode[] MultiByteOpCodes = new OpCode[0x100];

    static NoExceptionsInMiddlewaresTests()
    {
        foreach (var field in typeof(OpCodes).GetFields(BindingFlags.Public | BindingFlags.Static))
        {
            if (field.GetValue(null) is not OpCode opCode)
            {
                continue;
            }

            var value = unchecked((ushort)opCode.Value);
            if (value < 0x100)
            {
                SingleByteOpCodes[value] = opCode;
            }
            else
            {
                MultiByteOpCodes[value & 0xFF] = opCode;
            }
        }
    }
}
