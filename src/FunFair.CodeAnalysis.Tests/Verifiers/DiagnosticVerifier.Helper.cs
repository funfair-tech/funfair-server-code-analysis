using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using FunFair.CodeAnalysis.Tests.Exceptions;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Text;
using Xunit.Sdk;

namespace FunFair.CodeAnalysis.Tests.Verifiers
{
    /// <summary>
    ///     Class for turning strings into documents and getting the diagnostics on them
    ///     All methods are static
    /// </summary>
    public abstract partial class DiagnosticVerifier
    {
        private static readonly string? AssemblyPath = Path.GetDirectoryName(typeof(object).Assembly.Location);
        private static readonly MetadataReference CorlibReference = MetadataReference.CreateFromFile(typeof(object).Assembly.Location);
        private static readonly MetadataReference SystemCoreReference = MetadataReference.CreateFromFile(typeof(Enumerable).Assembly.Location);
        private static readonly MetadataReference CSharpSymbolsReference = MetadataReference.CreateFromFile(typeof(CSharpCompilation).Assembly.Location);
        private static readonly MetadataReference CodeAnalysisReference = MetadataReference.CreateFromFile(typeof(Compilation).Assembly.Location);
        private static readonly MetadataReference SystemRuntimeReference = MetadataReference.CreateFromFile(Path.Combine(AssemblyPath ?? string.Empty, path2: "System.Runtime.dll"));
        private static readonly MetadataReference SystemReference = MetadataReference.CreateFromFile(Path.Combine(AssemblyPath ?? string.Empty, path2: "System.dll"));
        private static readonly MetadataReference SystemConsoleReference = MetadataReference.CreateFromFile(typeof(Console).Assembly.Location);

        internal static string DefaultFilePathPrefix = "Test";
        internal static string CSharpDefaultFileExt = "cs";
        internal static string VisualBasicDefaultExt = "vb";
        internal static string TestProjectName = "TestProject";

        #region Get Diagnostics

        /// <summary>
        ///     Given classes in the form of strings, their language, and an IDiagnosticAnalyzer to apply to it, return the diagnostics found in the string after converting it to a document.
        /// </summary>
        /// <param name="sources">Classes in the form of strings</param>
        /// <param name="references">Metadata references</param>
        /// <param name="language">The language the source classes are in</param>
        /// <param name="analyzer">The analyzer to be run on the sources</param>
        /// <returns>An IEnumerable of Diagnostics that surfaced in the source code, sorted by Location</returns>
        private static Task<Diagnostic[]> GetSortedDiagnosticsAsync(string[] sources, MetadataReference[] references, string language, DiagnosticAnalyzer analyzer)
        {
            return GetSortedDiagnosticsFromDocumentsAsync(analyzer: analyzer, GetDocuments(sources: sources, references: references, language: language));
        }

        /// <summary>
        ///     Given an analyzer and a document to apply it to, run the analyzer and gather an array of diagnostics found in it.
        ///     The returned diagnostics are then ordered by location in the source document.
        /// </summary>
        /// <param name="analyzer">The analyzer to run on the documents</param>
        /// <param name="documents">The Documents that the analyzer will be run on</param>
        /// <returns>An IEnumerable of Diagnostics that surfaced in the source code, sorted by Location</returns>
        protected static async Task<Diagnostic[]> GetSortedDiagnosticsFromDocumentsAsync(DiagnosticAnalyzer analyzer, Document[] documents)
        {
            HashSet<Project> projects = new HashSet<Project>();

            foreach (Document document in documents)
            {
                projects.Add(document.Project);
            }

            List<Diagnostic> diagnostics = new List<Diagnostic>();

            foreach (Project project in projects)
            {
                Compilation? compilation = await project.GetCompilationAsync();

                if (compilation == null)
                {
                    continue;
                }

                ImmutableArray<Diagnostic> compilerErrors = compilation.GetDiagnostics();

                if (compilerErrors.Length > 0)
                {
                    StringBuilder errors = new StringBuilder();

                    foreach (Diagnostic compilerError in compilerErrors.Where(compilerError => !compilerError.ToString()
                                                                                                             .Contains("netstandard") && !compilerError.ToString()
                                                                                                                                                       .Contains("static 'Main' method")))
                    {
                        errors.Append(compilerError);
                    }

                    if (errors.Length > 0)
                    {
                        throw new UnitTestSourceException("Please correct following compiler errors in your unit test source:" + errors);
                    }
                }

                CompilationWithAnalyzers compilationWithAnalyzers = compilation.WithAnalyzers(ImmutableArray.Create(analyzer));
                ImmutableArray<Diagnostic> diags = await compilationWithAnalyzers.GetAnalyzerDiagnosticsAsync();

                foreach (Diagnostic diag in diags)
                {
                    if (diag.Location == Location.None || diag.Location.IsInMetadata)
                    {
                        diagnostics.Add(diag);
                    }
                    else
                    {
                        for (int i = 0; i < documents.Length; i++)
                        {
                            Document document = documents[i];
                            SyntaxTree? tree = await document.GetSyntaxTreeAsync();

                            if (tree != null && tree == diag.Location.SourceTree)
                            {
                                diagnostics.Add(diag);
                            }
                        }
                    }
                }
            }

            Diagnostic[] results = SortDiagnostics(diagnostics);
            diagnostics.Clear();

            return results;
        }

        /// <summary>
        ///     Sort diagnostics by location in source document
        /// </summary>
        /// <param name="diagnostics">The list of Diagnostics to be sorted</param>
        /// <returns>An IEnumerable containing the Diagnostics in order of Location</returns>
        private static Diagnostic[] SortDiagnostics(IEnumerable<Diagnostic> diagnostics)
        {
            return diagnostics.OrderBy(keySelector: d => d.Location.SourceSpan.Start)
                              .ToArray();
        }

        #endregion

        #region Set up compilation and documents

        /// <summary>
        ///     Given an array of strings as sources and a language, turn them into a project and return the documents and spans of it.
        /// </summary>
        /// <param name="sources">Classes in the form of strings</param>
        /// <param name="references">Metadata references.</param>
        /// <param name="language">The language the source code is in</param>
        /// <returns>A Tuple containing the Documents produced from the sources and their TextSpans if relevant</returns>
        private static Document[] GetDocuments(string[] sources, MetadataReference[] references, string language)
        {
            if (language != LanguageNames.CSharp && language != LanguageNames.VisualBasic)
            {
                throw new ArgumentException(message: "Unsupported Language");
            }

            Project project = CreateProject(sources: sources, references: references, language: language);
            Document[] documents = project.Documents.ToArray();

            if (sources.Length != documents.Length)
            {
                throw new InvalidOperationException(message: "Amount of sources did not match amount of Documents created");
            }

            return documents;
        }

        /// <summary>
        ///     Create a Document from a string through creating a project that contains it.
        /// </summary>
        /// <param name="source">Classes in the form of a string</param>
        /// <param name="language">The language the source code is in</param>
        /// <returns>A Document created from the source string</returns>
        protected static Document CreateDocument(string source, string language = LanguageNames.CSharp)
        {
            return CreateDocument(source: source, Array.Empty<MetadataReference>(), language: language);
        }

        /// <summary>
        ///     Create a Document from a string through creating a project that contains it.
        /// </summary>
        /// <param name="source">Classes in the form of a string</param>
        /// <param name="language">The language the source code is in</param>
        /// <returns>A Document created from the source string</returns>
        protected static Document CreateDocument(string source, MetadataReference[] references, string language = LanguageNames.CSharp)
        {
            return CreateProject(new[] {source}, references: references, language: language)
                   .Documents.First();
        }

        /// <summary>
        ///     Create a project using the inputted strings as sources.
        /// </summary>
        /// <param name="sources">Classes in the form of strings</param>
        /// <param name="references">Metadata References.</param>
        /// <param name="language">The language the source code is in</param>
        /// <returns>A Project created out of the Documents created from the source strings</returns>
        [SuppressMessage(category: "Microsoft.Reliability", checkId: "CA2000:DisposeObjectsBeforeLosingScope", Justification = "Test code")]
        private static Project CreateProject(string[] sources, MetadataReference[] references, string language = LanguageNames.CSharp)
        {
            string fileNamePrefix = DefaultFilePathPrefix;
            string fileExt = language == LanguageNames.CSharp ? CSharpDefaultFileExt : VisualBasicDefaultExt;

            ProjectId projectId = ProjectId.CreateNewId(TestProjectName);

            Solution solution = new AdhocWorkspace().CurrentSolution.AddProject(projectId: projectId, name: TestProjectName, assemblyName: TestProjectName, language: language)
                                                    .AddMetadataReference(projectId: projectId, metadataReference: CorlibReference)
                                                    .AddMetadataReference(projectId: projectId, metadataReference: SystemCoreReference)
                                                    .AddMetadataReference(projectId: projectId, metadataReference: CSharpSymbolsReference)
                                                    .AddMetadataReference(projectId: projectId, metadataReference: CodeAnalysisReference)
                                                    .AddMetadataReference(projectId: projectId, metadataReference: SystemRuntimeReference)
                                                    .AddMetadataReference(projectId: projectId, metadataReference: SystemReference)
                                                    .AddMetadataReference(projectId: projectId, metadataReference: SystemConsoleReference);

            foreach (MetadataReference reference in references)
            {
                solution = solution.AddMetadataReference(projectId: projectId, metadataReference: reference);
            }

            int count = 0;

            foreach (string source in sources)
            {
                string newFileName = fileNamePrefix + count + "." + fileExt;
                DocumentId documentId = DocumentId.CreateNewId(projectId: projectId, debugName: newFileName);
                solution = solution.AddDocument(documentId: documentId, name: newFileName, SourceText.From(source));
                count++;
            }

            Project? project = solution.GetProject(projectId);

            if (project == null)
            {
                throw new NotNullException();
            }

            return project;
        }

        #endregion
    }
}