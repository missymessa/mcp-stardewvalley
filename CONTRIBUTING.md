# Contributing

Thanks for considering contributing to the MCP Stardew Valley project.

How to contribute:
- Fork the repo and create a feature branch off `main`.
- Run tests locally: `dotnet test`.
- Open a PR with a clear description, include tests for new functionality.
- Add reviewers and link any relevant issues.

Coding style:
- Follow common C# conventions; add unit tests for logic changes.

CI & testing:
- The project includes a GitHub Actions workflow `.github/workflows/dotnet-ci.yml` that runs `dotnet build` and `dotnet test`.
- Ensure your PR passes the CI workflow before merging.
