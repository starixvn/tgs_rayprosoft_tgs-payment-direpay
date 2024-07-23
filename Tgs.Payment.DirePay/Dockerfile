#See https://aka.ms/customizecontainer to learn how to customize your debug container and how Visual Studio uses this Dockerfile to build your images for faster debugging.

FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
USER app
WORKDIR /app
EXPOSE 8080
EXPOSE 8081

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src
COPY ["nuget.config", "."]
COPY ["Tgs.Payment.DirePay/Tgs.Payment.DirePay.csproj", "Tgs.Payment.DirePay/"]
COPY ["DirePay.Core/DirePay.Core.csproj", "DirePay.Core/"]
COPY ["DirePay.Client/DirePay.Client.csproj", "DirePay.Client/"]
RUN dotnet restore "./Tgs.Payment.DirePay/Tgs.Payment.DirePay.csproj"
COPY . .
WORKDIR "/src/Tgs.Payment.DirePay"
RUN dotnet build "./Tgs.Payment.DirePay.csproj" -c $BUILD_CONFIGURATION -o /app/build

FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "./Tgs.Payment.DirePay.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "Tgs.Payment.DirePay.dll"]