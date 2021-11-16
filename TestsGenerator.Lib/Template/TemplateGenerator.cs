using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using static Microsoft.CodeAnalysis.CSharp.SyntaxFactory;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using TestsGenerator.Lib.TreeStructure;
using TestsGenerator.Lib.TreeStructure.SyntaxTree;

namespace TestsGenerator.Lib.Template
{
    public class TemplateGenerator : ITemplateGenerator
    {
        private const string ActualVariableName = "actual";
        private const string ExpectedVariableName = "expected";
        
        private readonly SyntaxToken _emptyLineToken;
        private readonly ExpressionStatementSyntax _failExpression;

        private readonly ISyntaxTreeGenerator _treeGenerator;
        
        public TemplateGenerator([NotNull]ISyntaxTreeGenerator treeGenerator)
        {
            _treeGenerator = treeGenerator;
            
            _emptyLineToken = CreateEmptyLineToken();
            _failExpression = CreateFailExpression();
        }

        public IEnumerable<KeyValuePair<string, string>> Generate([NotNull]string source)
        {
            var fileInfo = _treeGenerator.Generate(source);
            var usings = fileInfo.Usings.Select((usingStr) => UsingDirective(IdentifierName(usingStr))).ToList();
            usings.Add(UsingDirective(IdentifierName("NUnit.Framework")));
            usings.Add(UsingDirective(IdentifierName("Moq")));

            return fileInfo.Classes.Select(typeInfo => new KeyValuePair<string, string>(typeInfo.Name + "Test.cs", CompilationUnit()
                    .WithUsings(List(CreateClassUsings(typeInfo, usings)))
                    .WithMembers(SingletonList(CreateTestClassWithNamespaceDeclaration(typeInfo)))
                    .NormalizeWhitespace()
                    .ToFullString()))
                .ToList();
        }

        private IEnumerable<UsingDirectiveSyntax> CreateClassUsings(ClassInfoNode typeInfo, IEnumerable<UsingDirectiveSyntax> fileUsings)
        {
            return new List<UsingDirectiveSyntax>(fileUsings) { UsingDirective(IdentifierName(typeInfo.Namespace)) }.ToList();
        }

        private MemberDeclarationSyntax CreateTestClassWithNamespaceDeclaration(ClassInfoNode classInfo)
        {
            return NamespaceDeclaration
                    (
                        IdentifierName(classInfo.Namespace + ".Tests")
                    )
                    .WithMembers
                    (
                        SingletonList<MemberDeclarationSyntax>
                        (
                        CreateClassDeclaration(classInfo)
                        )
                    );
        }

        private ClassDeclarationSyntax CreateClassDeclaration(ClassInfoNode classInfoNode)
        {
            return ClassDeclaration(classInfoNode.Name + "Tests")
                     .WithAttributeLists(
                         SingletonList(
                             AttributeList(
                                 SingletonSeparatedList(
                                     Attribute(
                                        IdentifierName("TestFixture")
                                    )
                                )
                            )
                        )
                     )
                     .WithModifiers(
                         TokenList(
                             Token(SyntaxKind.PublicKeyword)
                         )
                     )
                     .WithMembers(
                        List(
                        System.Array.Empty<MemberDeclarationSyntax>()
                            .Concat(CreateFieldDeclaration(classInfoNode))
                            .Concat(CreateInjectedFieldsDeclaration(classInfoNode))
                            .Concat(CreateConstructorFieldsDeclaration(classInfoNode))
                            .Concat(CreateTestInitializeMethodDeclaration(classInfoNode))
                            .Concat(classInfoNode.Methods.Select(methodInfo => CreateTestMethodDeclaration(methodInfo, classInfoNode)))
                        )
                     );
        }

        private MethodDeclarationSyntax CreateTestMethodDeclaration(MethodInfoNode methodInfo, ClassInfoNode classInfo)
        {
            var arrangeBody = methodInfo.Parameters.Select(CreateVariableInitializeExpression).ToList();

            if (arrangeBody.Count != 0)
            {
                arrangeBody[^1] = arrangeBody[^1].WithSemicolonToken(_emptyLineToken);
            }

            var actAssertBody = new List<StatementSyntax>();

            if (methodInfo.ReturnType.Typename != "void")
            {
                actAssertBody.Add(CreateActualDeclaration(methodInfo, classInfo).WithSemicolonToken(_emptyLineToken));
                actAssertBody.Add(CreateExpectedDeclaration(methodInfo.ReturnType));
                actAssertBody.Add(CreateAreEqualExpression(methodInfo.ReturnType));
            }
            actAssertBody.Add(_failExpression);

            return MethodDeclaration(
                       PredefinedType(
                           Token(
                               SyntaxKind.VoidKeyword
                           )
                       ),
                       Identifier(methodInfo.Name + "Test")
                    )
                    .WithAttributeLists(
                        SingletonList(
                            AttributeList(
                                SingletonSeparatedList(
                                    Attribute(
                                        IdentifierName("Test")
                                    )
                                )
                            )
                        )
                    )
                    .WithModifiers(
                        TokenList(
                            Token(SyntaxKind.PublicKeyword)
                        )
                    )
                    .WithBody(
                        Block(
                            arrangeBody.Concat(actAssertBody)
                        )
                    );
        }

        private string CreateVariableName(string parameterName, bool isPrivate = false, bool isUnderTest = false)
        {
            var variableName = char.ToLower(parameterName[0]) + (parameterName.Length == 1 ? string.Empty : parameterName[1..]);

            variableName = isPrivate ? "_" + variableName : variableName;
            variableName = isUnderTest ? variableName + "UnderTest" : variableName;
            
            return variableName;
        }

        private IEnumerable<FieldDeclarationSyntax> CreateFieldDeclaration(ClassInfoNode classInfo)
        {
            var fieldDeclarations = new List<FieldDeclarationSyntax>();

            if (classInfo.Constructor.Parameters.Count > 0)
            {
                var fieldDeclaration = FieldDeclaration(
                        VariableDeclaration(
                            IdentifierName(classInfo.Name)
                        )
                        .WithVariables(
                            SingletonSeparatedList(
                                VariableDeclarator(
                                        Identifier(CreateVariableName(classInfo.Name, true, true)))
                                )
                            )
                        )
                        .WithModifiers(
                            TokenList(
                                Token(SyntaxKind.PrivateKeyword)
                            )
                        );
                
                fieldDeclarations.Add(fieldDeclaration.WithSemicolonToken(_emptyLineToken));
            }

            return fieldDeclarations;
        }

        private IEnumerable<FieldDeclarationSyntax> CreateInjectedFieldsDeclaration(ClassInfoNode classInfo)
        {
            var fields = classInfo.Constructor.Parameters
                .Where(parameter => parameter.Type.IsInterface)
                .Select(parameter => CreateInjectedFieldDeclaration(parameter.Type.Typename, parameter.Name))
                .ToList();

            if (fields.Count > 0)
            {
                fields[^1] = fields[^1].WithSemicolonToken(_emptyLineToken);
            }
            
            return fields;
        }
        
        private FieldDeclarationSyntax CreateConstructorFieldDeclaration(string type, string name)
        {
            return FieldDeclaration(
                VariableDeclaration(
                        IdentifierName(type)
                    )
                    .WithVariables(
                        SingletonSeparatedList(
                            VariableDeclarator(
                                    Identifier(CreateVariableName(name, true))
                                )
                                .WithInitializer(
                                    EqualsValueClause(
                                        DefaultExpression
                                            (
                                                IdentifierName(type)
                                            )
                                    )
                                )
                        )
                    )
            ).WithModifiers(
                TokenList(
                    Token(SyntaxKind.PrivateKeyword)
                )
            );
        }
        
        private IEnumerable<FieldDeclarationSyntax> CreateConstructorFieldsDeclaration(ClassInfoNode classInfo)
        {
            var fields = classInfo.Constructor.Parameters
                .Where(parameter => !parameter.Type.IsInterface)
                .Select(parameter => CreateConstructorFieldDeclaration(parameter.Type.Typename, parameter.Name))
                .ToList();
            
            if (fields.Count > 0)
            {
                fields[^1] = fields[^1].WithSemicolonToken(_emptyLineToken);
            }
            
            return fields;
        }
        
        private FieldDeclarationSyntax CreateInjectedFieldDeclaration(string type, string name)
        {
            return FieldDeclaration(
                        VariableDeclaration(
                            GenericName(
                                Identifier("Mock")
                            )
                            .WithTypeArgumentList(
                                TypeArgumentList(
                                    SingletonSeparatedList<TypeSyntax>(
                                        IdentifierName(type)
                                    )
                                )
                            )
                        )
                        .WithVariables(
                            SingletonSeparatedList(
                                VariableDeclarator(
                                    Identifier(CreateVariableName(name, true))
                                )
                            )
                        )
                    )
                    .WithModifiers(
                        TokenList(
                            Token(SyntaxKind.PrivateKeyword)));
        }

        private IEnumerable<MethodDeclarationSyntax> CreateTestInitializeMethodDeclaration(ClassInfoNode classInfo)
        {
            var methodDeclarations = new List<MethodDeclarationSyntax>();

            if (classInfo.Constructor.Parameters.Count > 0)
            {
                var methodDeclaration = MethodDeclaration(
                            PredefinedType(
                                Token(SyntaxKind.VoidKeyword)
                            ),
                            Identifier("SetUp")
                        )
                        .WithAttributeLists(
                            SingletonList(
                                AttributeList(
                                    SingletonSeparatedList(
                                        Attribute(
                                            IdentifierName("SetUp")
                                        )
                                    )
                                )
                            )
                        )
                        .WithModifiers(
                            TokenList(
                                Token(SyntaxKind.PublicKeyword)
                            )
                        )
                        .WithBody(
                            Block(
                            System.Array.Empty<ExpressionStatementSyntax>()
                                .Concat(CreateInjectedExpressions(classInfo))
                                .Concat(new List<ExpressionStatementSyntax> { CreateTestClassInitializeExpression(classInfo) })
                            )
                        );
                
                methodDeclarations.Add(methodDeclaration);
            }

            return methodDeclarations;
        }

        private LocalDeclarationStatementSyntax CreateVariableInitializeExpression(ParameterInfoNode parameterInfo)
        {
            ExpressionSyntax initializer;
            if (parameterInfo.Type.IsInterface)
            {
                initializer = ObjectCreationExpression(
                        GenericName(
                            Identifier("Mock")
                        )
                        .WithTypeArgumentList(
                            TypeArgumentList(
                                SingletonSeparatedList<TypeSyntax>(
                                    IdentifierName(parameterInfo.Type.Typename)
                                )
                            )
                        )
                    )
                    .WithArgumentList(
                        ArgumentList()
                    );
            }
            else
            {
                initializer = DefaultExpression(IdentifierName(parameterInfo.Type.Typename));
            }
        
            return LocalDeclarationStatement(
                        VariableDeclaration(
                            IdentifierName(parameterInfo.Type.Typename)
                        )
                        .WithVariables(
                            SingletonSeparatedList(
                                VariableDeclarator(
                                    Identifier(CreateVariableName(parameterInfo.Name))
                                )
                                .WithInitializer(
                                    EqualsValueClause(initializer)
                                )
                            )
                        )
                    );
        }
        
        private ExpressionStatementSyntax[] CreateInjectedExpressions(ClassInfoNode classInfo)
        {
            return classInfo.Constructor.Parameters
                .Where(parameter => parameter.Type.IsInterface)
                .Select(CreateInjectedExpression)
                .ToArray();
        }
        
        private ExpressionStatementSyntax CreateInjectedExpression(ParameterInfoNode parameterInfo)
        {
            return ExpressionStatement(
                        AssignmentExpression(
                            SyntaxKind.SimpleAssignmentExpression,
                            IdentifierName(CreateVariableName(parameterInfo.Name, true)),
                            ObjectCreationExpression(
                                GenericName(
                                        Identifier("Mock")
                                )
                                .WithTypeArgumentList(
                                    TypeArgumentList(
                                        SingletonSeparatedList<TypeSyntax>(
                                            IdentifierName(parameterInfo.Type.Typename)
                                        )
                                    )
                                )
                            )
                        )
                    );
        }
        
        private ExpressionStatementSyntax CreateTestClassInitializeExpression(ClassInfoNode classInfo)
        {
            return ExpressionStatement(
                        AssignmentExpression(
                            SyntaxKind.SimpleAssignmentExpression,
                            IdentifierName(CreateVariableName(classInfo.Name, true, true)),
                            ObjectCreationExpression(
                                    IdentifierName(classInfo.Name)
                                )
                                .WithArgumentList(
                                    ArgumentList(
                                        SeparatedList<ArgumentSyntax>(
                                            CreateArguments(classInfo.Constructor.Parameters)
                                        )
                                    )
                                )
                            )
                        );
        }

        private IEnumerable<SyntaxNodeOrToken> CreateArguments(IList<ParameterInfoNode> parameters)
        {
            var commaToken = Token(SyntaxKind.CommaToken);
            var arguments = new List<SyntaxNodeOrToken>();

            if (parameters.Count > 0)
            {
                arguments.Add(CreateArgument(parameters[0]));
            }

            for (var i = 1; i < parameters.Count; ++i)
            {
                arguments.Add(commaToken);
                arguments.Add(CreateArgument(parameters[i]));
            }

            return arguments;
        }

        private SyntaxNodeOrToken CreateArgument(ParameterInfoNode parameterInfo)
        {
            if (parameterInfo.Type.IsInterface)
            {
                return Argument(
                            MemberAccessExpression(
                                SyntaxKind.SimpleMemberAccessExpression,
                                IdentifierName(CreateVariableName(parameterInfo.Name, true)),
                                IdentifierName("Object")
                            )
                        );
            }

            return Argument(IdentifierName(CreateVariableName(parameterInfo.Name, true)));
        }

        private LocalDeclarationStatementSyntax CreateActualDeclaration(MethodInfoNode methodInfo, ClassInfoNode classInfo)
        {
            return LocalDeclarationStatement(
                    VariableDeclaration(
                        IdentifierName(methodInfo.ReturnType.Typename)
                    )
                    .WithVariables(
                        SingletonSeparatedList(
                            VariableDeclarator(
                                Identifier(ActualVariableName)
                            )
                            .WithInitializer(
                                EqualsValueClause(
                                    InvocationExpression(
                                        MemberAccessExpression(
                                            SyntaxKind.SimpleMemberAccessExpression,
                                            IdentifierName(CreateVariableName(classInfo.Name, true)),
                                            IdentifierName(methodInfo.Name)
                                        )
                                    )
                                    .WithArgumentList(
                                        ArgumentList(
                                            SeparatedList<ArgumentSyntax>(
                                                CreateArguments(methodInfo.Parameters)
                                            )
                                        )
                                    )
                                )
                            )
                        )
                    )
                );
        }

        private LocalDeclarationStatementSyntax CreateExpectedDeclaration(TypeInfoNode methodReturnType)
        {
            return CreateVariableInitializeExpression(new ParameterInfoNode(ExpectedVariableName, methodReturnType));
        }

        private ExpressionStatementSyntax CreateAreEqualExpression(TypeInfoNode methodReturnType)
        {
            return ExpressionStatement(
                    InvocationExpression(
                        MemberAccessExpression(
                            SyntaxKind.SimpleMemberAccessExpression,
                            IdentifierName("Assert"),
                            IdentifierName("AreEqual")
                        )
                    )
                    .WithArgumentList(
                        ArgumentList(
                            SeparatedList<ArgumentSyntax>(
                                new[] 
                                {
                                    CreateArgument(new ParameterInfoNode(ExpectedVariableName, methodReturnType)),
                                    Token(SyntaxKind.CommaToken),
                                    Argument(
                                        IdentifierName(ActualVariableName)
                                    )
                                }
                            )
                        )
                    )
                );
        }

        private ExpressionStatementSyntax CreateFailExpression()
        {
            return ExpressionStatement(
                    InvocationExpression(
                        MemberAccessExpression(
                            SyntaxKind.SimpleMemberAccessExpression,
                            IdentifierName("Assert"),
                            IdentifierName("Fail")
                        )
                    )
                    .WithArgumentList(
                        ArgumentList(
                            SingletonSeparatedList(
                                Argument(
                                    LiteralExpression(
                                        SyntaxKind.StringLiteralExpression,
                                        Literal("autogenerated")
                                    )
                                )
                            )
                        )
                    )
                );
        }

        private SyntaxToken CreateEmptyLineToken()
        {
            return Token(
                    TriviaList(),
                    SyntaxKind.SemicolonToken,
                    TriviaList(
                        Trivia(
                            SkippedTokensTrivia()
                            .WithTokens(
                                TokenList(
                                    BadToken(
                                        TriviaList(),
                                        "\n",
                                        TriviaList()
                                    )
                                )
                            )
                        )
                    )
                );
        }
    }
}