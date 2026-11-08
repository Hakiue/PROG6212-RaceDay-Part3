# RaceDay MVC web app (RaceDayMvc)
# Build context = the repository root (the folder that holds RaceDayApi.sln).

# ---------- Stage 1: build and publish ----------
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src

# Copy only the project file first so the restore layer is cached until dependencies change
COPY RaceDayMvc/RaceDayMvc.csproj RaceDayMvc/
RUN dotnet restore RaceDayMvc/RaceDayMvc.csproj

# Then copy the rest of the source and publish
COPY RaceDayMvc/ RaceDayMvc/
RUN dotnet publish RaceDayMvc/RaceDayMvc.csproj \
    --configuration Release \
    --no-restore \
    --output /app/publish \
    /p:UseAppHost=false

# ---------- Stage 2: small runtime image ----------
FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS final
WORKDIR /app

# Listen on 8080 inside the container (the run command maps it to 5100 on your PC).
# The MVC app reaches the API through Api__BaseUrl, which can be overridden at run time.
ENV ASPNETCORE_URLS=http://+:8080 \
    ASPNETCORE_ENVIRONMENT=Production \
    Api__BaseUrl=http://host.docker.internal:5000/
EXPOSE 8080

COPY --from=build /app/publish .

# Run as the non-root user that ships with the ASP.NET image
USER app

ENTRYPOINT ["dotnet", "RaceDayMvc.dll"]
