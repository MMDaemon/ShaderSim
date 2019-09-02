using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using OpenTK.Graphics.OpenGL4;
using ShaderRenderer;
using ShaderUtils;
using ShaderUtils.Attributes;
using ShaderUtils.Mathematics;

namespace ShaderTranslator
{
    public class RenderTranslator : RenderWrapper
    {
        private bool _depthEnabled;

        public override bool DepthEnabled
        {
            get => _depthEnabled;
            set
            {
                _depthEnabled = value;
                if (_depthEnabled)
                {
                    GL.Enable(EnableCap.DepthTest);
                }
                else
                {
                    GL.Disable(EnableCap.DepthTest);
                }
            }
        }

        public Dictionary<Tuple<VertexShader, FragmentShader>, ShaderProgram> _translatedShaders;

        private VertexShader _activeVertexShader;
        private FragmentShader _activeFragmentShader;

        public RenderTranslator()
        {
            DepthEnabled = true;
            _translatedShaders = new Dictionary<Tuple<VertexShader, FragmentShader>, ShaderProgram>();
        }

        public override void ActivateShader(VertexShader vertex, FragmentShader fragment)
        {
            _activeVertexShader = vertex;
            _activeFragmentShader = fragment;
            _translatedShaders[new Tuple<VertexShader, FragmentShader>(_activeVertexShader, _activeFragmentShader)].Activate();
        }

        public override void DeactivateShader()
        {
            _translatedShaders[new Tuple<VertexShader, FragmentShader>(_activeVertexShader, _activeFragmentShader)].Deactivate();
            _activeVertexShader = null;
            _activeFragmentShader = null;
        }

        public override void SetUniform<T>(string name, T value)
        {
            int bindingID = _translatedShaders[new Tuple<VertexShader, FragmentShader>(_activeVertexShader, _activeFragmentShader)].GetResourceLocation(ShaderResourceType.Uniform, name);

            if (typeof(T) == typeof(float))
            {
                GL.Uniform1(bindingID, (float)(object)value);
            }
            if (typeof(T) == typeof(Vector2))
            {
                GL.Uniform2(bindingID, ((Vector2)(object)value).ToOpenTK());
            }
            if (typeof(T) == typeof(Vector3))
            {
                GL.Uniform3(bindingID, ((Vector3)(object)value).ToOpenTK());
            }
            if (typeof(T) == typeof(Vector4))
            {
                GL.Uniform4(bindingID, ((Vector4)(object)value).ToOpenTK());
            }
            if (typeof(T) == typeof(Matrix4x4))
            {
                GL.UniformMatrix4(bindingID, 1, false, ((Matrix4x4)(object)value).ToArray());
            }
        }

        public void RegisterShader(VertexShader vertex, FragmentShader fragment, string vertexShaderFilePath, string fragmentShaderFilePath)
        {
            string programText = File.ReadAllText(vertexShaderFilePath);
            SyntaxTree tree = CSharpSyntaxTree.ParseText(programText).WithFilePath(vertexShaderFilePath);
            CompilationUnitSyntax root = tree.GetCompilationUnitRoot();
            string vertexTranslation = Translate(root);
            Console.WriteLine(vertexTranslation);

            programText = File.ReadAllText(fragmentShaderFilePath);
            tree = CSharpSyntaxTree.ParseText(programText).WithFilePath(fragmentShaderFilePath);
            root = tree.GetCompilationUnitRoot();
            ListMembers(root);
            string fragmentTranslation = Translate(root);
            Console.WriteLine(fragmentTranslation);

            _translatedShaders.Add(new Tuple<VertexShader, FragmentShader>(vertex, fragment), new ShaderProgram(vertexTranslation, fragmentTranslation));
        }

        public int GetAttributeBindingID(Tuple<VertexShader, FragmentShader> shader, string name)
        {
            return _translatedShaders[shader].GetResourceLocation(ShaderResourceType.Attribute, name);
        }

        private void ListMembers(SyntaxNode node, int depth = 0)
        {
            for (int i = 1; i < depth; i++)
            {
                Console.Write("| ");
            }

            if (depth > 0)
            {
                Console.Write("|-");
            }
            Console.Write(node.Kind());
            switch (node)
            {
                case IdentifierNameSyntax test:
                    Console.Write($": {test.Identifier}");
                    break;
                case ClassDeclarationSyntax test:
                    Console.Write($": {test.Identifier}");
                    break;
                case PropertyDeclarationSyntax test:
                    Console.Write($": {test.Identifier}");
                    break;
                case ConstructorDeclarationSyntax test:
                    Console.Write($": {test.Identifier}");
                    break;
                case MethodDeclarationSyntax test:
                    Console.Write($": {test.Identifier}");
                    break;
                case PredefinedTypeSyntax test:
                    Console.Write($": {test.Keyword}");
                    break;
                case ParameterSyntax test:
                    Console.Write($": {test.Identifier}");
                    break;
                case VariableDeclaratorSyntax test:
                    Console.Write($": {test.Identifier}");
                    break;
                case BlockSyntax test:
                    Console.Write($": {test.Statements}");
                    break;
            }
            Console.WriteLine("");

            foreach (var child in node.ChildNodes())
            {
                ListMembers(child, depth + 1);
            }
        }

        private string Translate(CompilationUnitSyntax root)
        {
            String code = "#version 430 core\n";
            SyntaxNode classNode = FindFirstNode<ClassDeclarationSyntax>(root);
            foreach (var child in classNode.ChildNodes())
            {
                code += TranslateNode(child);
            }
            return code;
        }

        private T FindFirstNode<T>(SyntaxNode node) where T : SyntaxNode
        {
            return node is T searchNode ? searchNode : node.ChildNodes().Select(FindFirstNode<T>).FirstOrDefault(result => result != null);
        }

        private string TranslateNode(SyntaxNode node)
        {
            string code = "";
            switch (node)
            {
                case BaseListSyntax syntax:
                    if (syntax.Types.Any(b => b.ChildNodes().Any(c =>
                        c is IdentifierNameSyntax && ((IdentifierNameSyntax)c).Identifier.ValueText == ("FragmentShader"))))
                    {
                        code += "out vec4 Color;\n";
                    }
                    break;
                case PropertyDeclarationSyntax syntax:
                    foreach (var child in node.ChildNodes())
                    {
                        code += TranslateNode(child);
                    }
                    code += $" {syntax.Identifier};\n";
                    break;
                case MethodDeclarationSyntax syntax:
                    code += TranslateNode(syntax.ReturnType);
                    MethodInfo m = typeof(Shader).GetMethod(syntax.Identifier.ValueText);
                    if (m == null)
                    {
                        m = typeof(VertexShader).GetMethod(syntax.Identifier.ValueText);
                    }
                    if (m == null)
                    {
                        m = typeof(FragmentShader).GetMethod(syntax.Identifier.ValueText);
                    }
                    if (m == null)
                    {
                        code += syntax.Identifier;
                    }
                    else
                    {
                        TranslationAttribute attribute = (TranslationAttribute)m.GetCustomAttributes(typeof(TranslationAttribute), true).FirstOrDefault();
                        if (attribute != null)
                        {
                            code += attribute.Term;
                        }
                        else
                        {
                            code += syntax.Identifier;
                        }
                    }

                    code += "(";
                    code += TranslateNode(syntax.ParameterList) + ")\n{\n";
                    code += TranslateNode(syntax.Body) + "}\n";
                    break;
                case BlockSyntax syntax:
                    foreach (var statement in syntax.Statements)
                    {
                        code += "\t" + TranslateNode(statement);
                    }
                    break;
                case LocalDeclarationStatementSyntax syntax:
                    foreach (var child in node.ChildNodes())
                    {
                        code += TranslateNode(child);
                    }
                    code += ";\n";
                    break;
                case ExpressionStatementSyntax syntax:
                    foreach (var child in node.ChildNodes())
                    {
                        code += TranslateNode(child);
                    }
                    code += ";\n";
                    break;
                case ReturnStatementSyntax syntax:
                    code += syntax.ReturnKeyword + " " + TranslateNode(syntax.Expression) + ";\n";
                    break;
                case AssignmentExpressionSyntax syntax:
                    code += TranslateNode(syntax.Left) + " " + syntax.OperatorToken + " " + TranslateNode(syntax.Right);
                    break;
                case VariableDeclarationSyntax syntax:
                    code += TranslateNode(syntax.Type) + " " + TranslateNode(syntax.Variables.First());
                    break;
                case VariableDeclaratorSyntax syntax:
                    code += syntax.Identifier + " ";
                    code += TranslateNode(syntax.Initializer);
                    break;
                case ObjectCreationExpressionSyntax syntax:
                    code += TranslateNode(syntax.Type) + "(" + TranslateNode(syntax.ArgumentList) + ")";
                    break;
                case InvocationExpressionSyntax syntax:
                    code += TranslateNode(syntax.Expression) + "(" + TranslateNode(syntax.ArgumentList) + ")";
                    break;
                case ParenthesizedExpressionSyntax syntax:
                    code += syntax.OpenParenToken + TranslateNode(syntax.Expression) + syntax.CloseParenToken;
                    break;
                case PrefixUnaryExpressionSyntax syntax:
                    code += syntax.OperatorToken.ValueText + TranslateNode(syntax.Operand);
                    break;
                case BinaryExpressionSyntax syntax:
                    code += TranslateNode(syntax.Left) + " " + syntax.OperatorToken + " " + TranslateNode(syntax.Right);
                    break;
                case MemberAccessExpressionSyntax syntax:
                    code += TranslateNode(syntax.Expression) + syntax.OperatorToken + TranslateNode(syntax.Name);
                    break;
                case EqualsValueClauseSyntax syntax:
                    code += syntax.EqualsToken + " " + TranslateNode(syntax.Value);
                    break;
                case AttributeSyntax syntax:
                    switch (((IdentifierNameSyntax)syntax.ChildNodes().First()).Identifier.ValueText)
                    {
                        case "Uniform":
                            code += "uniform ";
                            break;
                        case "In":
                            code += "in ";
                            break;
                        case "Out":
                            code += "out ";
                            break;
                    }
                    break;
                case IdentifierNameSyntax syntax:
                    Assembly a = Assembly.GetAssembly(typeof(ShaderUtils.Shader));

                    if (syntax.Parent is PropertyDeclarationSyntax || syntax.Parent is ObjectCreationExpressionSyntax ||
                        syntax.Parent is VariableDeclarationSyntax || syntax.Parent is ParameterSyntax)
                    {
                        Type t = a.GetType("ShaderUtils.Mathematics." + syntax.Identifier);
                        if (t != null)
                        {
                            TranslationAttribute translation =
                                t.GetCustomAttributes(typeof(TranslationAttribute), true).FirstOrDefault() as
                                    TranslationAttribute;
                            code += translation.Term;
                        }
                        else
                        {
                            code += syntax.Identifier;
                        }
                    }
                    else if (syntax.Parent is MemberAccessExpressionSyntax)
                    {
                        code += syntax.Identifier.ValueText.ToLower();
                    }
                    else if (syntax.Parent is InvocationExpressionSyntax)
                    {
                        MethodInfo method = (from info in typeof(Shader).GetMethods(BindingFlags.Instance | BindingFlags.NonPublic) where (info.Name == syntax.Identifier.ValueText) select info).FirstOrDefault();
                        if (method == null)
                        {
                            code += syntax.Identifier;
                        }
                        else
                        {
                            TranslationAttribute attribute = (TranslationAttribute)method.GetCustomAttributes(typeof(TranslationAttribute), true).FirstOrDefault();
                            if (attribute != null)
                            {
                                code += attribute.Term;
                            }
                            else
                            {
                                code += syntax.Identifier;
                            }
                        }
                    }
                    else
                    {
                        PropertyInfo p = typeof(Shader).GetProperty(syntax.Identifier.ValueText);
                        if (p == null)
                        {
                            p = typeof(VertexShader).GetProperty(syntax.Identifier.ValueText);
                        }
                        if (p == null)
                        {
                            p = typeof(FragmentShader).GetProperty(syntax.Identifier.ValueText);
                        }
                        if (p == null)
                        {
                            code += syntax.Identifier;
                        }
                        else
                        {
                            TranslationAttribute attribute = (TranslationAttribute)p.GetCustomAttributes(typeof(TranslationAttribute), true).FirstOrDefault();
                            if (attribute != null)
                            {
                                code += attribute.Term;
                            }
                            else
                            {
                                code += syntax.Identifier;
                            }
                        }
                    }
                    break;
                case LiteralExpressionSyntax syntax:
                    code += syntax.Token.ValueText;
                    break;
                case PredefinedTypeSyntax syntax:
                    code += syntax.Keyword.ValueText + " ";
                    break;
                case ParameterListSyntax syntax:
                    for (int i = 0; i < syntax.Parameters.Count; i++)
                    {
                        code += TranslateNode(syntax.Parameters[i].Type);
                        code += " ";
                        code += syntax.Parameters[i].Identifier;
                        code += i + 1 == syntax.Parameters.Count ? "" : ", ";
                    }
                    break;
                case ArgumentListSyntax syntax:
                    for (int i = 0; i < syntax.Arguments.Count; i++)
                    {
                        code += TranslateNode(syntax.Arguments[i].Expression);
                        code += i + 1 == syntax.Arguments.Count ? "" : ", ";
                    }
                    break;
                default:
                    foreach (var child in node.ChildNodes())
                    {
                        code += TranslateNode(child);
                    }
                    break;
            }
            return code;
        }
    }
}
