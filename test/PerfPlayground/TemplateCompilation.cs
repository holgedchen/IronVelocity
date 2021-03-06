﻿using IronVelocity.Binders;
using IronVelocity.Compilation;
using IronVelocity.Directives;
using IronVelocity.Parser;
using IronVelocity.Runtime;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Reflection.Emit;

namespace IronVelocity.PerfPlayground
{
    [TestFixture(Category = "Performance")]
    [Explicit]
    public class TemplateCompilation
    {
        public static readonly string OutputDir  = $"Compilation/{DateTime.Now.ToString("yyy-MM-dd_HH-mm-ss")}/";
        public static ICollection<string> TemplateDirectories { get; } = new List<string>(){
            "../../templates/",
            "../../../IronVelocity.Tests/Regression/templates/",
        };

        public bool Compile { get; set; } = false;
        public bool SaveDlls { get; set; } = false;
        public bool SaveIl { get; set; } = false;
        public bool ExecuteTemplate { get; set; } = false;
        public string TestNamePrefix { get; set; } = "Compilation";
        public ICollection<string> BlockDirectives { get; } = new List<string>();
        private ReusableBinderFactory _binderFactory;

        [SetUp]
        public void SetUp()
        {

            if (SaveDlls || SaveIl)
            {
                if (!Directory.Exists(OutputDir))
                    Directory.CreateDirectory(OutputDir);
            }
            _binderFactory = new ReusableBinderFactory(new BinderFactory());
        }

        [TestCaseSource(nameof(CreateTemplateTestCases))]
        public void TemplateCompilationTests(string path, string assemblyName)
        {
			using (var file = File.OpenRead(path))
			using (var reader = new StreamReader(file))
			{
				var antlrDirectives = BlockDirectives
					.Select(x => new AntlrBlockDirectiveBuilder(x))
					.Concat(new CustomDirectiveBuilder[] { new ForeachDirectiveBuilder() })
					.ToImmutableList();

				var expressionTreeFactory = new VelocityExpressionFactory(_binderFactory);
				var parser = new AntlrVelocityParser(antlrDirectives, expressionTreeFactory);


				var expressionTree = parser.Parse(reader, assemblyName);
				if (Compile || ExecuteTemplate)
				{
					VelocityDiskCompiler diskCompiler = null;
					VelocityCompiler compiler;

					if (SaveDlls || SaveIl)
					{
						diskCompiler = new VelocityDiskCompiler(new AssemblyName(assemblyName), OutputDir);
						compiler = diskCompiler;
					}
					else
					{
						compiler = new VelocityCompiler();
					}

					var result = compiler.CompileWithSymbols(expressionTree, assemblyName, path);

					if (ExecuteTemplate)
					{
						var context = new VelocityContext();
						using (var writer = new StringWriter())
						{
							var output = new VelocityOutput(writer);
							result(context, output);
						}
					}

					if (SaveDlls || SaveIl)
					{
						diskCompiler.SaveDll();

						if (SaveIl)
						{
							diskCompiler.SaveIl();
						}
					}
				}
			}
        }

        public static IEnumerable<TestCaseData> CreateTemplateTestCases()
        {
            foreach (var path in TemplateDirectories)
            {
                var directory = new DirectoryInfo(path);
                if (directory.Exists)
                {
                    foreach (var file in directory.GetFiles("*.vm", SearchOption.AllDirectories))
                    {
                        var relativePath = file.FullName.Replace(directory.FullName, "");
                        var assemblyName = relativePath.Replace(Path.DirectorySeparatorChar, '_').Replace(".vm", "");
                        yield return new TestCaseData(file.FullName, assemblyName)
                           .SetName(nameof(TemplateCompilation) + " " + relativePath);
                    }
                }
            }
        }

        private class AntlrBlockDirectiveBuilder : CustomDirectiveBuilder
        {
            public AntlrBlockDirectiveBuilder(string name)
            {
                Name = name;
            }

            public override bool IsBlockDirective => true;
            public override string Name { get; }

            public override Expression Build(IImmutableList<Expression> arguments, Expression body)
                => body;
        }
    }

}
