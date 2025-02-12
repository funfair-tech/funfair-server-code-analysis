using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using FunFair.CodeAnalysis.Tests.Exceptions;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Text;

namespace FunFair.CodeAnalysis.Tests.Verifiers;

public abstract partial class DiagnosticVerifier
{
    private const string DEFAULT_FILE_PATH_PREFIX = "Test";
    private const string C_SHARP_DEFAULT_FILE_EXT = "cs";
    private const string VISUAL_BASIC_DEFAULT_EXT = "vb";
    private const string TEST_PROJECT_NAME = "TestProject";
    private static readonly string? AssemblyPath = Path.GetDirectoryName(typeof(object).Assembly.Location);
    private static readonly MetadataReference CorlibReference = MetadataReference.CreateFromFile(typeof(object).Assembly.Location);
    private static readonly MetadataReference SystemCoreReference = MetadataReference.CreateFromFile(typeof(Enumerable).Assembly.Location);
    private static readonly MetadataReference CSharpSymbolsReference = MetadataReference.CreateFromFile(typeof(CSharpCompilation).Assembly.Location);
    private static readonly MetadataReference CodeAnalysisReference = MetadataReference.CreateFromFile(typeof(Compilation).Assembly.Location);

    private static readonly MetadataReference SystemRuntimeReference = MetadataReference.CreateFromFile(Path.Combine(AssemblyPath ?? string.Empty, path2: "System.Runtime.dll"));

    private static readonly MetadataReference SystemReference = MetadataReference.CreateFromFile(Path.Combine(AssemblyPath ?? string.Empty, path2: "System.dll"));
    private static readonly MetadataReference SystemConsoleReference = MetadataReference.CreateFromFile(typeof(Console).Assembly.Location);

    #region Get Diagnostics

    private static ValueTask<IReadOnlyList<Diagnostic>> GetSortedDiagnosticsAsync(
        IReadOnlyList<string> sources,
        IReadOnlyList<MetadataReference> references,
        string language,
        DiagnosticAnalyzer analyzer
    )
    {
        return GetSortedDiagnosticsFromDocumentsAsync(analyzer: analyzer, GetDocuments(sources: sources, references: references, language: language));
    }

    protected static async ValueTask<IReadOnlyList<Diagnostic>> GetSortedDiagnosticsFromDocumentsAsync(DiagnosticAnalyzer analyzer, IReadOnlyList<Document> documents)
    {
        HashSet<Project> projects = BuildProjects(documents);

        IReadOnlyList<Diagnostic> diagnostics = [];

        foreach (Project project in projects)
        {
            Compilation? compilation = await project.GetCompilationAsync(CancellationToken.None);

            if (compilation is null)
            {
                continue;
            }

            EnsureNoCompilationErrors(compilation);

            CompilationWithAnalyzers compilationWithAnalyzers = CreateCompilationWithAnalyzers(analyzer: analyzer, compilation: compilation);
            diagnostics = await CollectDiagnosticsAsync(documents: documents, compilationWithAnalyzers: compilationWithAnalyzers);
        }

        return SortDiagnostics(diagnostics);
    }

    private static HashSet<Project> BuildProjects(IReadOnlyList<Document> documents)
    {
        HashSet<Project> projects = [];

        foreach (Document document in documents)
        {
            projects.Add(document.Project);
        }

        return projects;
    }

    private static CompilationWithAnalyzers CreateCompilationWithAnalyzers(DiagnosticAnalyzer analyzer, Compilation compilation)
    {
        return compilation.WithAnalyzers([analyzer], options: null);
    }

    private static async ValueTask<IReadOnlyList<Diagnostic>> CollectDiagnosticsAsync(IReadOnlyList<Document> documents, CompilationWithAnalyzers compilationWithAnalyzers)
    {
        ImmutableArray<Diagnostic> diagnostics = await compilationWithAnalyzers.GetAnalyzerDiagnosticsAsync(CancellationToken.None);

        return await ExtractDiagnosticsAsync(documents: documents, diagnostics: diagnostics);
    }

    private static async ValueTask<IReadOnlyList<Diagnostic>> ExtractDiagnosticsAsync(IReadOnlyList<Document> documents, IReadOnlyList<Diagnostic> diagnostics)
    {
        List<Diagnostic> results = [];

        foreach (Diagnostic diagnostic in diagnostics)
        {
            bool add = diagnostic.Location == Location.None || diagnostic.Location.IsInMetadata || await ShouldAddDocumentDiagnosticAsync(documents: documents, diagnostic: diagnostic);

            if (add)
            {
                results.Add(diagnostic);
            }
        }

        return results;
    }

    private static async ValueTask<bool> ShouldAddDocumentDiagnosticAsync(IReadOnlyList<Document> documents, Diagnostic diagnostic)
    {
        foreach (Document document in documents)
        {
            bool add = await ShouldAddDiagnosticAsync(document: document, diagnostic: diagnostic);

            if (add)
            {
                return true;
            }
        }

        return false;
    }

    private static async ValueTask<bool> ShouldAddDiagnosticAsync(Document document, Diagnostic diagnostic)
    {
        SyntaxTree? tree = await document.GetSyntaxTreeAsync(CancellationToken.None);

        return tree is not null && tree == diagnostic.Location.SourceTree;
    }

    private static void EnsureNoCompilationErrors(Compilation compilation)
    {
        ImmutableArray<Diagnostic> compilerErrors = compilation.GetDiagnostics(CancellationToken.None);

        if (compilerErrors.IsEmpty)
        {
            return;
        }

        StringBuilder errors = compilerErrors.Where(IsReportableCSharpError).Aggregate(new StringBuilder(), func: (current, compilerError) => current.Append(compilerError));

        if (errors.Length != 0)
        {
            throw new UnitTestSourceException("Please correct following compiler errors in your unit test source:" + errors);
        }
    }

    private static bool IsReportableCSharpError(Diagnostic compilerError)
    {
        return !compilerError.ToString().Contains(value: "netstandard", comparisonType: StringComparison.Ordinal)
            && !compilerError.ToString().Contains(value: "static 'Main' method", comparisonType: StringComparison.Ordinal)
            && !compilerError.ToString().Contains(value: "CS1002", comparisonType: StringComparison.Ordinal)
            && !compilerError.ToString().Contains(value: "CS1702", comparisonType: StringComparison.Ordinal);
    }

    private static IReadOnlyList<Diagnostic> SortDiagnostics(IEnumerable<Diagnostic> diagnostics)
    {
        return [.. diagnostics.OrderBy(keySelector: d => d.Location.SourceSpan.Start)];
    }

    #endregion

    #region Set up compilation and documents

    private static IReadOnlyList<Document> GetDocuments(IReadOnlyList<string> sources, IReadOnlyList<MetadataReference> references, string language)
    {
        if (!IsSupportedLanguage(language))
        {
            throw new ArgumentException(message: "Unsupported Language", nameof(language));
        }

        Project project = CreateProject(sources: sources, references: references, language: language);
        IReadOnlyList<Document> documents = [.. project.Documents];

        if (sources.Count != documents.Count)
        {
            throw new InvalidOperationException(message: "Amount of sources did not match amount of Documents created");
        }

        return documents;
    }

    private static bool IsSupportedLanguage(string language)
    {
        return IsCSharp(language) || IsVisualBasic(language);
    }

    private static bool IsVisualBasic(string language)
    {
        return StringComparer.Ordinal.Equals(x: language, y: LanguageNames.VisualBasic);
    }

    private static bool IsCSharp(string language)
    {
        return StringComparer.Ordinal.Equals(x: language, y: LanguageNames.CSharp);
    }

    private static Project CreateProject(IReadOnlyList<string> sources, IReadOnlyList<MetadataReference> references, string language = LanguageNames.CSharp)
    {
        const string fileNamePrefix = DEFAULT_FILE_PATH_PREFIX;
        string fileExt = IsCSharp(language) ? C_SHARP_DEFAULT_FILE_EXT : VISUAL_BASIC_DEFAULT_EXT;

        ProjectId projectId = ProjectId.CreateNewId(TEST_PROJECT_NAME);

        Solution solution = references.Aggregate(
            BuildSolutionWithStandardReferences(language: language, projectId: projectId),
            func: (current, reference) => current.AddMetadataReference(projectId: projectId, metadataReference: reference)
        );

        int count = 0;

        foreach (string source in sources)
        {
            string newFileName = fileNamePrefix + count.ToString(CultureInfo.InvariantCulture) + "." + fileExt;
            DocumentId documentId = DocumentId.CreateNewId(projectId: projectId, debugName: newFileName);
            solution = solution.AddDocument(documentId: documentId, name: newFileName, SourceText.From(source));
            count++;
        }

        return AssertReallyNotNull(solution.GetProject(projectId));
    }

    [SuppressMessage(category: "codecracker.CSharp", checkId: "CC0022:DisposeObjectsBeforeLosingScope", Justification = "Test code")]
    [SuppressMessage(category: "Microsoft.Reliability", checkId: "CA2000:DisposeObjectsBeforeLosingScope", Justification = "Test code")]
    private static Solution BuildSolutionWithStandardReferences(string language, ProjectId projectId)
    {
        return new AdhocWorkspace()
            .CurrentSolution.AddProject(projectId: projectId, name: TEST_PROJECT_NAME, assemblyName: TEST_PROJECT_NAME, language: language)
            .AddMetadataReference(projectId: projectId, metadataReference: CorlibReference)
            .AddMetadataReference(projectId: projectId, metadataReference: SystemCoreReference)
            .AddMetadataReference(projectId: projectId, metadataReference: CSharpSymbolsReference)
            .AddMetadataReference(projectId: projectId, metadataReference: CodeAnalysisReference)
            .AddMetadataReference(projectId: projectId, metadataReference: SystemRuntimeReference)
            .AddMetadataReference(projectId: projectId, metadataReference: SystemReference)
            .AddMetadataReference(projectId: projectId, metadataReference: SystemConsoleReference);
    }

    #endregion
}
