# DataIngestion - RAG Pipeline Example

A .NET 10 console application demonstrating a complete Retrieval-Augmented Generation (RAG) pipeline using Microsoft Extensions for AI and Data Ingestion.

## Overview

This project showcases how to build an end-to-end data ingestion pipeline that:
- Reads Markdown documents from a directory
- Enriches content with AI-generated image descriptions
- Chunks text using semantic similarity
- Generates AI summaries for each chunk
- Stores embeddings in a SQLite vector database
- Enables semantic search and question answering

## Features

- **Document Reading**: Processes Markdown files using the Markdig reader
- **AI-Powered Enrichment**: Generates alternative text for images using GPT-4.1
- **Semantic Chunking**: Intelligently splits documents using embedding-based semantic similarity
- **Summary Generation**: Creates AI summaries for each chunk
- **Vector Storage**: SQLite-based vector database for efficient similarity search
- **Interactive Q&A**: Query the ingested documents using natural language

## Prerequisites

- [.NET 10 SDK](https://dotnet.microsoft.com/download/dotnet/10.0)
- GitHub Personal Access Token (for accessing GitHub Models)

## Setup

1. **Set the GitHub Token Environment Variable**:
   ```powershell
   $env:GITHUB_TOKEN = "your_github_token_here"
   ```

2. **Restore Dependencies**:
   ```powershell
   dotnet restore
   ```

3. **Add Your Documents**:
   Place Markdown files in the `./data` directory. A sample primer is already included.

## Running the Application

```powershell
dotnet run
```

The application will:
1. Process all `.md` files in the `./data` directory
2. Display processing status for each document
3. Enter interactive mode for querying

## Usage

### Interactive Query Mode

After ingestion completes, enter questions at the prompt:

```
Enter your question (or 'exit' to quit): What is RAG?
Searching...

Score: 0.89
    Content: Retrieval-Augmented Generation (RAG) extends LLMs by letting them look up information...
```

Type `exit` to quit the application.

## Architecture

### Pipeline Components

```
Markdown Files → Document Reader → Image Enricher → Semantic Chunker → Summary Enricher → Vector Store
```

1. **MarkdownReader**: Parses Markdown documents
2. **ImageAlternativeTextEnricher**: Adds AI-generated descriptions for images
3. **SemanticSimilarityChunker**: Splits text into ~2000 token chunks using semantic boundaries
4. **SummaryEnricher**: Generates summaries for each chunk
5. **VectorStoreWriter**: Stores chunks with embeddings in SQLite

### AI Models Used

- **Chat Model**: `gpt-4.1` (via GitHub Models)
- **Embedding Model**: `text-embedding-3-small` (1536 dimensions)
- **Tokenizer**: Tiktoken (GPT-4 encoding)

## Project Structure

```
DataIngestion/
├── Program.cs              # Main pipeline implementation
├── DataIngestion.csproj    # Project dependencies
├── data/                   # Input documents directory
│   └── Data-Ingestion-Primer.md
└── vectors.db              # Generated SQLite vector database
```

## Dependencies

- **Microsoft.Extensions.AI.OpenAI** (10.1.0-preview.1.25574.1)
- **Microsoft.Extensions.DataIngestion** (10.1.0-preview.1.25574.1)
- **Microsoft.Extensions.DataIngestion.Markdig** (10.1.0-preview.1.25574.1)
- **Microsoft.Extensions.Logging.Console** (10.0.0)
- **Microsoft.ML.Tokenizers.Data.Cl100kBase** (2.0.0)
- **Microsoft.SemanticKernel.Connectors.SqliteVec** (1.67.1-preview)

## Configuration

### Chunking Options
- **MaxTokensPerChunk**: 2000 tokens
- **OverlapTokens**: 0 (no overlap between chunks)

### Search Results
- **Top K**: 3 (returns top 3 most relevant chunks)

### Vector Store
- **Database**: `vectors.db` (SQLite)
- **Collection**: `data`
- **Embedding Dimensions**: 1536

## Customization

### Adjust Chunk Size
```csharp
IngestionChunkerOptions chunkerOptions = new(TiktokenTokenizer.CreateForModel("gpt-4"))
{
    MaxTokensPerChunk = 1000,  // Smaller chunks
    OverlapTokens = 100        // Add overlap
};
```

### Change Search Results Count
```csharp
await foreach (var result in collection.SearchAsync(searchValue, top: 5))
```

### Process Different File Types
Replace `MarkdownReader` with another document reader implementation.

## License

This is a sample project for educational purposes.

## Troubleshooting

**Issue**: `GITHUB_TOKEN` not found
- **Solution**: Ensure the environment variable is set in your current PowerShell session

**Issue**: Database locked errors
- **Solution**: The SQLite connection uses `Pooling=false` to prevent this. Ensure only one instance is running.

**Issue**: Model not found
- **Solution**: Verify your GitHub token has access to GitHub Models

