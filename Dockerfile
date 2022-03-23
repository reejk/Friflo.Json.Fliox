FROM mcr.microsoft.com/dotnet/sdk:5.0 AS build
WORKDIR /app
# EXPOSE 8010
# ENV ASPNETCORE_URLS=http://+:8010
#
# copy csproj and restore as distinct layers
COPY *.sln .
COPY Json/Burst/*.csproj ./Json/Burst/
COPY Json/Fliox/*.csproj ./Json/Fliox/
COPY Json/Fliox.Hub/*.csproj ./Json/Fliox.Hub/
COPY Json/Fliox.Hub.AspNetCore/*.csproj ./Json/Fliox.Hub.AspNetCore/
COPY Json/Fliox.Hub.Explorer/*.csproj ./Json/Fliox.Hub.Explorer/
COPY DemoHub/*.csproj ./DemoHub/
#
COPY Json/Fliox.Hub.Cosmos/*.csproj ./Json/Fliox.Hub.Cosmos/
COPY Json.Tests/*.csproj ./Json.Tests/
COPY Playground/*.csproj ./Playground/
#
RUN dotnet restore 
#
# copy everything else and build app
COPY Json/Burst/. ./Json/Burst/
COPY Json/Fliox/. ./Json/Fliox/
COPY Json/Fliox.Hub/. ./Json/Fliox.Hub/
COPY Json/Fliox.Hub.AspNetCore/. ./Json/Fliox.Hub.AspNetCore/
COPY Json/Fliox.Hub.Explorer/. ./Json/Fliox.Hub.Explorer/
COPY DemoHub/. ./DemoHub/
#
COPY Json/Fliox.Hub.Cosmos/. ./Json/Fliox.Hub.Cosmos/
COPY Json.Tests/. ./Json.Tests/
COPY Playground/. ./Playground/
#
WORKDIR /app/DemoHub
RUN dotnet publish -c Release -o out
#
FROM mcr.microsoft.com/dotnet/core/aspnet:3.1 AS runtime
WORKDIR /app 
#
COPY --from=build /app/DemoHub/out ./
ENTRYPOINT ["dotnet", "Fliox.DemoHub.dll"]

# --- usage
# docker build -t demo-hub:v1 .
# docker run -it --rm -p 80:8010 demo-hub:v1


 
 