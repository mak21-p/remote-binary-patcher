#See https://aka.ms/customizecontainer to learn how to customize your debug container and how Visual Studio uses this Dockerfile to build your images for faster debugging.

FROM mcr.microsoft.com/dotnet/aspnet:7.0 AS base
WORKDIR /app
EXPOSE 80
EXPOSE 443
EXPOSE 5000
ENV ASPNETCORE_URLS=http://+:5000

FROM mcr.microsoft.com/dotnet/sdk:7.0 AS build
WORKDIR /src
COPY ["PatcherServer/PatcherServer.csproj", "PatcherServer/"]
RUN dotnet restore "PatcherServer/PatcherServer.csproj"
RUN dotnet dev-certs https
COPY . .
WORKDIR "/src/PatcherServer"
RUN dotnet build "PatcherServer.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "PatcherServer.csproj" -c Release -o /app/publish /p:UseAppHost=true

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet","watch", "run", "--server.urls", "http://localhost:5000"]
ENTRYPOINT ["dotnet", "PatcherServer.dll"]