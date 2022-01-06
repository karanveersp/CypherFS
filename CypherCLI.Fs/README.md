# CypherFs

A command line password manager and secret store. 

## Build Release Artifact

```shell
dotnet publish -r win-x64 -c Release /p:IncludeNativeLibrariesForSelfExtract=true --self-contained true /p:PublishSingleFile=true
```

