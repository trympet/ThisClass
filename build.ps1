$BUILD = (
    "src\NLog.Extensions.ThisClass\NLog.Extensions.ThisClass.csproj",
    "src\ThisClass\ThisClass.csproj"
);

foreach ($target in $BUILD) {
    dotnet pack $target
}