using System.Reflection;
using System.Reflection.Emit;
using Microsoft.EntityFrameworkCore;

namespace Russkyc.MinimalApi.Framework.Server.Data;

public static class DbContextBuilder
{
    public static Type CreateDynamicDbContext(IEnumerable<Type> entityTypes, AssemblyName assemblyName)
    {
        var assemblyBuilder = AssemblyBuilder.DefineDynamicAssembly(assemblyName, AssemblyBuilderAccess.Run);
        var moduleBuilder = assemblyBuilder.DefineDynamicModule("MainModule");

        var typeBuilder = moduleBuilder.DefineType("BaseDbContext",
            TypeAttributes.Public | TypeAttributes.Class,
            typeof(BaseDbContext));

        var constructorBuilder = typeBuilder.DefineConstructor(
            MethodAttributes.Public,
            CallingConventions.Standard,
            new[] { typeof(DbContextOptions) });

        var ctorIlGenerator = constructorBuilder.GetILGenerator();
        ctorIlGenerator.Emit(OpCodes.Ldarg_0);
        ctorIlGenerator.Emit(OpCodes.Ldarg_1);
        ctorIlGenerator.Emit(OpCodes.Call,
            typeof(BaseDbContext).GetConstructor(new[] { typeof(DbContextOptions<BaseDbContext>) })!);

        foreach (var entityType in entityTypes)
        {
            var dbSetType = typeof(DbSet<>).MakeGenericType(entityType);
            var fieldBuilder = typeBuilder.DefineField(
                $"_{entityType.Name.ToLower()}collection",
                dbSetType,
                FieldAttributes.Private);

            ctorIlGenerator.Emit(OpCodes.Ldarg_0);
            ctorIlGenerator.Emit(OpCodes.Ldarg_0);
            
            var setMethod = typeof(DbContext).GetMethods()
                .First(m => m.Name == "Set" && m.GetGenericArguments().Length == 1);

            ctorIlGenerator.Emit(OpCodes.Call, setMethod.MakeGenericMethod(entityType));
            ctorIlGenerator.Emit(OpCodes.Stfld, fieldBuilder);

            var dbSetProperty = typeBuilder.DefineProperty(
                $"{entityType.Name}Collection",
                PropertyAttributes.None,
                dbSetType,
                null);

            var propertyGetter = typeBuilder.DefineMethod(
                $"get_{entityType.Name}Collection",
                MethodAttributes.Public | MethodAttributes.SpecialName | MethodAttributes.HideBySig,
                dbSetType,
                Type.EmptyTypes);

            var ilGenerator = propertyGetter.GetILGenerator();
            ilGenerator.Emit(OpCodes.Ldarg_0);
            ilGenerator.Emit(OpCodes.Ldfld, fieldBuilder);
            ilGenerator.Emit(OpCodes.Ret);

            dbSetProperty.SetGetMethod(propertyGetter);
        }

        ctorIlGenerator.Emit(OpCodes.Ret);

        return typeBuilder.CreateTypeInfo().AsType();
    }
}