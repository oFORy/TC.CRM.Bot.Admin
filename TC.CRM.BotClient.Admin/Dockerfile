#See https://aka.ms/customizecontainer to learn how to customize your debug container and how Visual Studio uses this Dockerfile to build your images for faster debugging.

FROM mcr.microsoft.com/dotnet/aspnet:7.0 AS base
WORKDIR /app
EXPOSE 80
EXPOSE 443

FROM mcr.microsoft.com/dotnet/sdk:7.0 AS build
WORKDIR /src
COPY ["TC.CRM.BotClient.Admin/TC.CRM.Bot.Admin.csproj", "TC.CRM.BotClient.Admin/"]
RUN dotnet restore "TC.CRM.BotClient.Admin/TC.CRM.Bot.Admin.csproj"
COPY . .
WORKDIR "/src/TC.CRM.BotClient.Admin"
RUN dotnet build "TC.CRM.Bot.Admin.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "TC.CRM.Bot.Admin.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "TC.CRM.Bot.Admin.dll"]