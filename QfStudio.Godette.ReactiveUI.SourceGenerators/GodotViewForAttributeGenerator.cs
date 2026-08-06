using System.Linq;
using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;

namespace QfStudio.Godette.ReactiveUI.SourceGenerators;

[Generator]
public sealed class GodotViewForAttributeGenerator : IIncrementalGenerator
{
    private const string AttributeName = "GodotViewFor";
    private const string AttributeNamespace = "QfStudio.Godette.ReactiveUI";

    private readonly struct TypeToGenerate
    {
        public readonly string? Namespace;
        public readonly string ClassName;
        public readonly string ViewModelName;
        public readonly bool ImplementsIReactiveObject;

        public TypeToGenerate(string? @namespace, string className, string viewModelName, bool implementsIReactiveObject)
        {
            Namespace = @namespace;
            ClassName = className;
            ViewModelName = viewModelName;
            ImplementsIReactiveObject = implementsIReactiveObject;
        }
    }

    private static string AttributeSource =>
        $$"""
          #nullable enable

          using System;

          namespace {{AttributeNamespace}};

          [AttributeUsage(AttributeTargets.Class, Inherited = false)]
          public class {{AttributeName}}Attribute<TViewModel> : Attribute where TViewModel : class
          {}
          """;

    private static string GetSourceText(TypeToGenerate type)
    {
        var namespaceBlock = string.IsNullOrEmpty(type.Namespace) ? "" : $"\nnamespace {type.Namespace};\n";

        var usingComponentModel = type.ImplementsIReactiveObject ? "" : "using System.ComponentModel;\n";
        var iroInterface = type.ImplementsIReactiveObject
            ? ""
            : ", global::ReactiveUI.IReactiveObject";

        var iroBodyPrefix = type.ImplementsIReactiveObject
            ? ""
            : """
                  public event PropertyChangedEventHandler? PropertyChanged;
                  public event PropertyChangingEventHandler? PropertyChanging;
                  
                  void global::ReactiveUI.IReactiveObject.RaisePropertyChanged(PropertyChangedEventArgs args) => PropertyChanged?.Invoke(this, args);
                  void global::ReactiveUI.IReactiveObject.RaisePropertyChanging(PropertyChangingEventArgs args) => PropertyChanging?.Invoke(this, args);
              """;

        return $$"""
          #nullable enable

          {{usingComponentModel}}
          using ReactiveUI;
          {{namespaceBlock}}
          public partial class {{type.ClassName}} : global::ReactiveUI.IViewFor<{{type.ViewModelName}}>, global::ReactiveUI.IActivatableView{{iroInterface}}
          {
          {{iroBodyPrefix}}

              private {{type.ViewModelName}}? _viewModel;
              public {{type.ViewModelName}}? ViewModel
              {
                  get => _viewModel;
                  set => this.RaiseAndSetIfChanged(ref _viewModel, value);
              }

              object? global::ReactiveUI.IViewFor.ViewModel
              {
                  get => ViewModel;
                  set => ViewModel = ({{type.ViewModelName}}?)value;
              }
          }
          """;
    }

    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        context.RegisterPostInitializationOutput(static ctx =>
        {
            ctx.AddSource($"{AttributeName}Attribute.g.cs", SourceText.From(AttributeSource, Encoding.UTF8));
        });

        var typesToGenerate = context.SyntaxProvider
            .ForAttributeWithMetadataName(
                $"{AttributeNamespace}.{AttributeName}Attribute`1",
                predicate: static (s, _) => true,
                transform: static (ctx, _) => FilterForTypesToGenerate(ctx.SemanticModel, ctx.TargetNode))
            .Where(static x => x is not null);

        context.RegisterSourceOutput(typesToGenerate, static (spc, type) => GenerateSource(spc, type));
    }

    private static TypeToGenerate? FilterForTypesToGenerate(SemanticModel semantic, SyntaxNode node)
    {
        if (node is not ClassDeclarationSyntax classDeclaration)
            return null;

        var className = classDeclaration.Identifier.ValueText;

        var viewModelName = string.Empty;
        foreach (var attr in classDeclaration.AttributeLists.SelectMany(attrList => attrList.Attributes))
        {
            if (attr.Name is not GenericNameSyntax { TypeArgumentList.Arguments.Count: 1 } value
                || value.Identifier.ValueText != AttributeName)
            {
                continue;
            }

            var typeInfo = semantic.GetTypeInfo(value.TypeArgumentList.Arguments[0]);
            if (typeInfo.Type is {} viewModelSymbol)
            {
                viewModelName = viewModelSymbol.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
            }
            break;
        }

        if (viewModelName == string.Empty)
            return null;

        var classSymbol = semantic.GetDeclaredSymbol(classDeclaration);
        if (classSymbol is null)
            return null;

        var implementsIReactiveObject = classSymbol
            .AllInterfaces
            .Any(i => i.ToDisplayString() == "ReactiveUI.IReactiveObject");

        var @namespace = classSymbol.ContainingNamespace.ToDisplayString();
        return new TypeToGenerate(@namespace, className, viewModelName, implementsIReactiveObject);
    }

    private static void GenerateSource(SourceProductionContext context, TypeToGenerate? typeOrNull)
    {
        if (typeOrNull is not { } type)
            return;

        if (type.ClassName == string.Empty || type.ViewModelName == string.Empty)
            return;

        var source = GetSourceText(type);
        context.AddSource($"{type.Namespace}.{type.ClassName}_{AttributeName}.g.cs", SourceText.From(source, Encoding.UTF8));
    }
}
