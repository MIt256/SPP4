#nullable enable
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace TestsGenerator.Lib.TreeStructure.SyntaxTree
{
    public class SyntaxTreeGenerator:ISyntaxTreeGenerator
    {
        public TestFileNode Generate(string code)
        {
            var fileNode = new TestFileNode();
            var root = CSharpSyntaxTree.ParseText(code).GetCompilationUnitRoot();

            var usings = root.Usings
                .Select(usingValue => usingValue.Name.ToString())
                .ToList();

            var classDeclarations = root.DescendantNodes().OfType<ClassDeclarationSyntax>()
                .Select(CreateClassInfo)
                .ToList();
            
            fileNode.Usings.AddRange(usings);
            fileNode.Classes.AddRange(classDeclarations);
            return fileNode;
        }

        private ClassInfoNode CreateClassInfo(ClassDeclarationSyntax classDeclaration)
        {
            var methods = classDeclaration.DescendantNodes()
                .OfType<MethodDeclarationSyntax>()
                .Where(methodDeclaration => methodDeclaration.Modifiers.Any((modifier) => modifier.IsKind(SyntaxKind.PublicKeyword)))
                .Select(CreateMethodInfoNode)
                .ToList();
            
            var classInfoNode = new ClassInfoNode(
                classDeclaration.Identifier.ValueText, 
                ((NamespaceDeclarationSyntax)classDeclaration.Parent!).Name.ToString(), 
                CreateConstructorInfoNode(classDeclaration));

            classInfoNode.Methods.AddRange(methods);
            return classInfoNode;
        }

        private MethodInfoNode CreateMethodInfoNode(MethodDeclarationSyntax methodDeclaration)
        {
            var parameters = methodDeclaration.ParameterList.Parameters
                .Select(parameter => new ParameterInfoNode(parameter.Identifier.ValueText, new TypeInfoNode(parameter.Type?.ToString())))
                .ToList();
            
            var methodNode = new MethodInfoNode(methodDeclaration.Identifier.ValueText, new TypeInfoNode(methodDeclaration.ReturnType.ToString()));
            methodNode.Parameters.AddRange(parameters);
            return methodNode;
        }

        private ConstructorInfoNode? CreateConstructorInfoNode(SyntaxNode classDeclaration)
        {
            var constructor = GetConstructorWithMaxParametersCount(classDeclaration);
            var constructorNode = new ConstructorInfoNode();
            
            if (constructor != null)
            {
                var parameters = constructor.ParameterList.Parameters
                    .Select(parameter => new ParameterInfoNode(parameter.Identifier.ValueText, new TypeInfoNode(parameter.Type?.ToString())))
                    .ToList();
                
                constructorNode.Parameters.AddRange(parameters);
            }
            
            return constructorNode;
        }
        
        private ConstructorDeclarationSyntax? GetConstructorWithMaxParametersCount(SyntaxNode classDeclaration)
        {
            return classDeclaration.DescendantNodes()
                .OfType<ConstructorDeclarationSyntax>()
                .Where(constructor => constructor.Modifiers.Any((modifier) => modifier.IsKind(SyntaxKind.PublicKeyword)))
                .OrderByDescending(constructor => constructor.ParameterList.Parameters.Count)
                .FirstOrDefault();
        }
    
    }
}