# Build stage
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src

# Copy csproj and restore to leverage Docker layer caching
COPY ["ChatServer/ChatServer.csproj", "ChatServer/"]
RUN dotnet restore "ChatServer/ChatServer.csproj"

# Copy remaining source and publish
COPY . .
WORKDIR "/src/ChatServer"
RUN dotnet publish "ChatServer.csproj" -c Release -o /app/publish /p:PublishTrimmed=false

# Runtime stage
FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS runtime
WORKDIR /app
COPY --from=build /app/publish .
ENV ASPNETCORE_URLS=http://+:31777
EXPOSE 31777
ENTRYPOINT ["dotnet", "ChatServer.dll"]