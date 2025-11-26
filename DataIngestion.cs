#:package Microsoft.Extensions.AI.OpenAI@10.0.1-preview.1.25571.5
#:package Microsoft.Extensions.DataIngestion@10.0.1-preview.1.25571.5
#:package Microsoft.Extensions.DataIngestion.Markdig@10.0.1-preview.1.25571.5
#:package Microsoft.Extensions.Logging.Console@10.0.0
#:package Microsoft.ML.Tokenizers.Data.Cl100kBase@2.0.0
#:package Microsoft.SemanticKernel.Connectors.SqliteVec@1.67.1-preview

using System.ClientModel;
using System.Numerics;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.DataIngestion;
using Microsoft.Extensions.DataIngestion.Chunkers;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.VectorData;
using Microsoft.ML.Tokenizers;
using Microsoft.SemanticKernel.Connectors.SqliteVec;
using OpenAI;

// Configure document reader
IngestionDocumentReader reader = new MarkdownReader();

using ILoggerFactory loggerFactory = LoggerFactory.Create(builder => builder.AddSimpleConsole());

// Configure IChatClient to use GitHub Models
OpenAIClient openAIClient = new(
    new ApiKeyCredential(Environment.GetEnvironmentVariable("GITHUB_TOKEN")!),
    new OpenAIClientOptions { Endpoint = new Uri("https://models.github.ai/inference") });

IChatClient chatClient =
    openAIClient.GetChatClient("gpt-4.1").AsIChatClient();


// Configure document processor
EnricherOptions enricherOptions = new(chatClient)
{
    // Enricher failures should not fail the whole ingestion pipeline, as they are best-effort enhancements.
    // This logger factory can be used to create loggers to log such failures.
    LoggerFactory = loggerFactory
};

IngestionDocumentProcessor imageAlternativeTextEnricher = new ImageAlternativeTextEnricher(enricherOptions);

// Configure embedding generator
IEmbeddingGenerator<string, Embedding<float>> embeddingGenerator =
    openAIClient.GetEmbeddingClient("text-embedding-3-small").AsIEmbeddingGenerator();

// Configure chunker to split text into semantic chunks
IngestionChunkerOptions chunkerOptions = new(TiktokenTokenizer.CreateForModel("gpt-4"))
{
    MaxTokensPerChunk = 2000,
    OverlapTokens = 0
};

IngestionChunker<string> chunker = new SemanticSimilarityChunker(embeddingGenerator, chunkerOptions);

// Configure chunk processor to generate summaries for each chunk
IngestionChunkProcessor<string> summaryEnricher = new SummaryEnricher(enricherOptions);

// Configure SQLite Vector Store
using SqliteVectorStore vectorStore = new(
    "Data Source=vectors.db;Pooling=false",
    new()
    {
        EmbeddingGenerator = embeddingGenerator
    });

// The writer requires the embedding dimension count to be specified.
// For OpenAI's `text-embedding-3-small`, the dimension count is 1536.
using VectorStoreWriter<string> writer = new(vectorStore, dimensionCount: 1536, new VectorStoreWriterOptions {CollectionName = "data"});

// Compose data ingestion pipeline
using IngestionPipeline<string> pipeline = new(reader, chunker, writer, loggerFactory: loggerFactory)
{
    DocumentProcessors = { imageAlternativeTextEnricher },
    ChunkProcessors = { summaryEnricher }
};

await foreach (var result in pipeline.ProcessAsync(new DirectoryInfo("./data"), searchPattern: "*.md"))
{
    Console.WriteLine($"Completed processing '{result.DocumentId}'. Succeeded: '{result.Succeeded}'.");
}

// Search the vector store collection and display results
var collection = writer.VectorStoreCollection;

while (true)  
{
    Console.Write("Enter your question (or 'exit' to quit): ");
    string? searchValue = Console.ReadLine();
    if (string.IsNullOrEmpty(searchValue) || searchValue == "exit")
    {
        break;
    }

    Console.WriteLine("Searching...\n");
    await foreach (var result in collection.SearchAsync(searchValue, top: 3))
    {
        Console.WriteLine($"Score: {result.Score}\n\tContent: {result.Record["content"]}");
    }
}