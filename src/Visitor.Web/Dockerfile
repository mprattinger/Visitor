# See https://aka.ms/customizecontainer to learn how to customize your debug container and how Visual Studio uses this Dockerfile to build your images for faster debugging.

# This stage is used when running from VS in fast mode (Default for Debug configuration)
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS base
WORKDIR /app
EXPOSE 8080
EXPOSE 8081

# Create /data and set permissions before switching user
RUN mkdir -p /data && chmod -R 777 /data
USER $APP_UID


# This stage is used to build the service project
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build

ARG BUILD_CONFIGURATION=Release
ARG BUILD_VERSION=0.0.0
ENV BUILD_VERSION=$BUILD_VERSION

WORKDIR /src
COPY ["Directory.Packages.props", "."]
COPY ["src/Visitor.Web/Visitor.Web.csproj", "src/Visitor.Web/"]
RUN dotnet restore "./src/Visitor.Web/Visitor.Web.csproj"
COPY . .
WORKDIR "/src/src/Visitor.Web"
RUN dotnet build "./Visitor.Web.csproj" -c $BUILD_CONFIGURATION -o /app/build

# This stage is used to publish the service project to be copied to the final stage
FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "./Visitor.Web.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false \
	/p:Version=$BUILD_VERSION /p:AssemblyVersion=$BUILD_VERSION /p:FileVersion=$BUILD_VERSION

# This stage is used in production or when running from VS in regular mode (Default when not using the Debug configuration)
FROM base AS final

WORKDIR /app

ARG BUILD_VERSION=0.0.0
ENV BUILD_VERSION=$BUILD_VERSION

COPY --from=publish /app/publish .

RUN mkdir -p /data
RUN chmod -R 777 /data

ENTRYPOINT ["dotnet", "Visitor.Web.dll"]