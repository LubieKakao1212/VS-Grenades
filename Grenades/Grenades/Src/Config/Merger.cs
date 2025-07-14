using System;
using System.Reflection;
using Vintagestory.API.Common;

namespace Grenades.Config;

public static class Merger {

    private static readonly MethodInfo MergerRecursiveInfo = typeof(Merger).GetMethod("MergeRecursive")!;
    private static readonly MethodInfo MergerNotNullRecursiveInfo = typeof(Merger).GetMethod("MergeNotNullRecursive")!;

    public static TA MergeNotNullRecursive<TA, TB>(this TA a, TB? b, ILogger? logger = null) where TA : notnull, new() where TB : struct {
        if (MergePrimitive<TA, TB, float>(ref a, b) ||
            MergePrimitive<TA, TB, int>(ref a, b) ||
            MergePrimitive<TA, TB, bool>(ref a, b)
            ) {
            return a;
        }
        else {
            if (b == null) {
                return a;
            }
            
            var typeA = typeof(TA);
            var typeB = typeof(TB);
            object result = new TA();
            
            foreach (var fieldA in typeA.GetFields(BindingFlags.Public | BindingFlags.Instance | BindingFlags.NonPublic)) {
                var fieldB = typeB.GetField(fieldA.Name, BindingFlags.Public | BindingFlags.Instance | BindingFlags.NonPublic);
                if (fieldB == null) {
                    logger?.Warning($"Field {fieldA} from {typeA} is missing in type {typeB}");
                    continue;
                }
                
                var fTypeA = fieldA.FieldType;
                var fTypeB = fieldB.FieldType;
                fTypeB = Nullable.GetUnderlyingType(fTypeB);
                if (fTypeB != null && (fTypeA == fTypeB || 
                                       (fTypeA.IsGenericType && fTypeB.IsGenericType && 
                                        fTypeA.GetGenericTypeDefinition() == fTypeB.GetGenericTypeDefinition()))) {
                    var fVb = fieldB.GetValue(b.Value);
                    var fVa = fieldA.GetValue(a);
                
                    if (fVb != null) {
                        var method =  MergerNotNullRecursiveInfo.MakeGenericMethod(fTypeA, fTypeB);

                        var mergedValue = method.Invoke(null, new object?[] {fVa, fVb, logger});
                        fieldA.SetValue(result, mergedValue);
                    }
                    else {
                        fieldA.SetValue(result, fVa);
                    }
                }
                else {
                    logger?.Warning($"Unmergeable fields. Types  {fTypeA}, {fTypeB} are not equal, is second field ({fieldB}) not nullable?");
                }
            }
            return (TA)result;
        }
    }

    public static bool MergePrimitive<TA, TB, TPrimitive>(ref TA a, TB? b) where TB: struct  {
        if (typeof(TA) == typeof(TPrimitive)) {
            if (typeof(TB) != typeof(TPrimitive)) {
                throw new ArgumentException("Invalid primitive type");
            }
            a = MergeNotNull(a, b);
            return true;
        }
        return false;
    }
    
    public static TA MergeNotNull<TA, TB>(this TA a, TB? b) where TB: struct {
        return b is TA ba ? ba : a; //Should work?
    }
    
    public static T? MergeRecursive<T>(this T? a, T? b, ILogger? logger = null) where T: struct {
        if (typeof(T) == typeof(float) || 
            typeof(T) == typeof(bool) || 
            typeof(T) == typeof(int)) {
            return a.Merge(b);
        }
        else {
            if (a == null) {
                return b;
            }
            if (b == null) {
                return a;
            }
            
            var type = typeof(T);
            object result = new T();
            
            foreach (var field in type.GetFields(BindingFlags.Public | BindingFlags.Instance | BindingFlags.NonPublic)) {
                var fType = field.FieldType;

                fType = Nullable.GetUnderlyingType(fType);
                if (fType == null) {
                    logger?.Warning($"Unmergable field type! This should never happen {field}");
                    continue;
                }

                var fVa = field.GetValue(a.Value);
                var fVb = field.GetValue(b.Value);
                
                var method = MergerRecursiveInfo.MakeGenericMethod(fType);
                var mergedValue = method.Invoke(null, new object?[] {fVa, fVb, logger});
                
                field.SetValue(result, mergedValue);
            }
            return (T?)result;
        }
    }
    
    public static T? Merge<T>(this T? a, T? b) where T : struct {
        return a ?? b;
    }
}