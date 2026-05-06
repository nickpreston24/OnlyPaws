dotnet build;
dotnet pack;
dotnet tool install --add-source ./nupkg onlypaws --global | grep --invert-match warning --line-buffered
