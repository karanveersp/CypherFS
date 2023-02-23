# CypherFS

CLI password manager and secret store.

## Build Release Artifact

For Windows

```shell
dotnet publish -r win-x64 -c Release /p:IncludeNativeLibrariesForSelfExtract=true --self-contained true /p:PublishSingleFile=true
```
