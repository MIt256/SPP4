using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using static Microsoft.CodeAnalysis.CSharp.SyntaxFactory;

namespace TestsGenerator
{
    public class TestCreator
    {
        static public Task<List<TestStructure>> Generate(string content)
        {
            return Task.Run(() =>
            {
                List<TestStructure> tests = new List<TestStructure>();

                string testClassName;
                string testCode;
                string namespaceName;

                NamespaceDeclarationSyntax testNamespace;

                SyntaxNode root = CSharpSyntaxTree.ParseText(content).GetRoot();

                //Вытаскиваем все классы
                var classDeclarations = root.DescendantNodes().OfType<ClassDeclarationSyntax>();

                foreach (ClassDeclarationSyntax _class in classDeclarations)
                {

                    testClassName = _class.Identifier.ValueText + "Tests";

                    namespaceName = ((NamespaceDeclarationSyntax)_class.Parent).Name.ToString();

                    testNamespace = NamespaceDeclaration(QualifiedName
                        (IdentifierName(namespaceName), IdentifierName("Test")));


                    //Создание тестового класса
                    var _testclass = SyntaxFactory.CompilationUnit()
                        .WithUsings(CreateUsings(namespaceName))
                        .WithMembers(SingletonList<MemberDeclarationSyntax>(testNamespace
                            .WithMembers(SingletonList<MemberDeclarationSyntax>(ClassDeclaration(testClassName)
                                         .WithAttributeLists(
                                             SingletonList(
                                                 AttributeList(
                                                     SingletonSeparatedList(
                                                         Attribute(
                                                             IdentifierName("TestFixture"))))))

                                         .WithModifiers(TokenList(Token(SyntaxKind.PublicKeyword)))         //public 
                                         .WithMembers(CreateTestMethods(_class))
                                         ))));



                    //Добавление в список тестов
                    testCode = _testclass.NormalizeWhitespace().ToFullString();
                    tests.Add(new TestStructure(testClassName, testCode));
                }
                return tests;
            });
        }

        //Возврат списка директив using
        private static SyntaxList<UsingDirectiveSyntax> CreateUsings(string namespaceName)
        {
            List<UsingDirectiveSyntax> usings = new List<UsingDirectiveSyntax>
            {
                UsingDirective(
                    IdentifierName("System")),

                UsingDirective(
                    QualifiedName(
                        IdentifierName("System"),
                        IdentifierName("Linq"))),

                UsingDirective(
                    QualifiedName(
                        QualifiedName(
                            IdentifierName("System"),
                            IdentifierName("Collections")),
                            IdentifierName("Generic"))),

                UsingDirective(
                    QualifiedName(
                        IdentifierName("NUnit"),
                        IdentifierName("Framework"))),

                UsingDirective(IdentifierName(namespaceName))
            };

            return List(usings);
        }

        //Возврат списка всех публичных методов с пустым телом (Assert.Fail)
        private static SyntaxList<MemberDeclarationSyntax> CreateTestMethods(ClassDeclarationSyntax classInfo)
        {
            List<MemberDeclarationSyntax> classMethods = new List<MemberDeclarationSyntax>();

            AttributeSyntax attr = Attribute(ParseName("Test"));

            string methodName;

            //Находим публичные методы
            var publicMethods = classInfo.DescendantNodes().OfType<MethodDeclarationSyntax>()
                .Where(method => method.Modifiers.Any(modifier => modifier.ValueText == "public"));

            foreach (var method in publicMethods)
            {
                methodName = method.Identifier.ValueText;

                MethodDeclarationSyntax testMethod = MethodDeclaration(ParseTypeName("void"), methodName + "Test")
                    .AddModifiers(Token(SyntaxKind.PublicKeyword))      //public
                    .AddBodyStatements(FormMethodBody())                //body
                    .AddAttributeLists(
                        AttributeList().AddAttributes(attr));           //[Test]
                classMethods.Add(testMethod);

            }

            return List(classMethods);
        }

        //Создание тела метода
        private static StatementSyntax[] FormMethodBody()
        {
            StatementSyntax[] body = { ParseStatement("Assert.Fail(\"auto\");") };
            return body;
        }
    }
}
