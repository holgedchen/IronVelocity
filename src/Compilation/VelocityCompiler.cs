﻿#if NETFRAMEWORK
using IronVelocity.Runtime;
using System;
using System.Diagnostics;
using System.Linq.Expressions;
using System.Reflection;
using System.Reflection.Emit;
using System.Runtime.CompilerServices;

namespace IronVelocity.Compilation
{

    public class VelocityCompiler
    {
        private const string _methodName = "Execute";
        private static readonly Type[] _signature = new[] { typeof(VelocityContext), typeof(VelocityOutput) };
        private static readonly ConstructorInfo _debugAttributeConstructorInfo = typeof(DebuggableAttribute).GetConstructor(new Type[] { typeof(DebuggableAttribute.DebuggingModes) });

        private readonly AssemblyName _assemblyName;
		protected readonly bool DebugMode;

        public VelocityCompiler(string assemblyName = null, bool debugMode = false)
        {
			DebugMode = debugMode;
            _assemblyName = string.IsNullOrEmpty(assemblyName)
                ? new AssemblyName("IronVelocityTemplate")
                : new AssemblyName(assemblyName);
        }



        protected virtual ModuleBuilder CreateModuleBuilder()
        {
            var assemblyBuilder = AppDomain.CurrentDomain.DefineDynamicAssembly(_assemblyName, AssemblyBuilderAccess.RunAndCollect);
            var moduleBuilder = assemblyBuilder.DefineDynamicModule(_assemblyName.Name, true);

            if (DebugMode)
                AddDebugAttributes(assemblyBuilder, moduleBuilder);

            return moduleBuilder;
        }


        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1011:ConsiderPassingBaseTypesAsParameters", Justification = "Final cast will fail if the Expression does not conform to VelocityTemplateMethod's signature")]
        public VelocityTemplateMethod CompileWithSymbols(Expression<VelocityTemplateMethod> expressionTree, string name, string fileName)
        {
            var log = TemplateGenerationEventSource.Log;
            log.LogParsedExpressionTree(name, expressionTree);
            var moduleBuilder = CreateModuleBuilder();

            var typeBuilder = moduleBuilder.DefineType(name, TypeAttributes.Public | TypeAttributes.Sealed | TypeAttributes.BeforeFieldInit);

            var meth = typeBuilder.DefineMethod(
                    _methodName,
                    MethodAttributes.Public | MethodAttributes.Static,
                    typeof(void),
                    _signature);

            
            log.GenerateDebugInfoStart(name);
            var debugVisitor = new DynamicToExplicitCallSiteConvertor(typeBuilder, fileName);
            expressionTree = debugVisitor.VisitAndConvert(expressionTree, "DynamicMethod Debug");
            log.GenerateDebugInfoStop(name);

            var localReducer = new TemporaryLocalReuse();
            expressionTree = localReducer.VisitAndConvert(expressionTree, "TemporaryLocalREducer");

            var body = Expression.Block(localReducer.TemporaryVariables, expressionTree.Body);
            expressionTree = expressionTree.Update(body, expressionTree.Parameters);


            log.LogProcessedExpressionTree(name, expressionTree);

            log.CompileMethodStart(name);
            var debugInfo = DebugInfoGenerator.CreatePdbGenerator();
            expressionTree.CompileToMethod(meth, debugInfo);
            log.CompileMethodStop(name);


            var compiledType = typeBuilder.CreateType();
            log.InitaliseCallSitesStart(name);
            debugVisitor.InitaliseConstants(compiledType);
            log.InitaliseCallSitesStop(name);

            var compiledMethod = compiledType.GetMethod(_methodName, _signature);
            return (VelocityTemplateMethod)Delegate.CreateDelegate(typeof(VelocityTemplateMethod), compiledMethod);
        }

        protected static void AddDebugAttributes(AssemblyBuilder assemblyBuilder, ModuleBuilder moduleBuilder)
        {
            if (assemblyBuilder == null)
                throw new ArgumentNullException(nameof(assemblyBuilder));
            if (moduleBuilder == null)
                throw new ArgumentNullException(nameof(moduleBuilder));

            var debugAttributes =
                DebuggableAttribute.DebuggingModes.Default |
                DebuggableAttribute.DebuggingModes.DisableOptimizations;

            var cab = new CustomAttributeBuilder(_debugAttributeConstructorInfo, new object[] { debugAttributes });
            assemblyBuilder.SetCustomAttribute(cab);
            moduleBuilder.SetCustomAttribute(cab);
        }
    }
}
#endif