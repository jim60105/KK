#See https://aka.ms/containerfastmode to understand how Visual Studio uses this Dockerfile to build your images for faster debugging.

FROM mcr.microsoft.com/dotnet/sdk:5.0-buster-slim AS build
WORKDIR /src
COPY SaveLoadCompression SaveLoadCompression
COPY SaveLoadCompressionWeb SaveLoadCompressionWeb
COPY Extension Extension
COPY PngCompression PngCompression
RUN dotnet restore "SaveLoadCompressionWeb/SaveLoadCompressionWeb.csproj"
WORKDIR "/src/SaveLoadCompressionWeb"
RUN dotnet build "SaveLoadCompressionWeb.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "SaveLoadCompressionWeb.csproj" -c Release -o /app/publish -p:PublishTrimmed=false

FROM nginx:alpine AS final
EXPOSE 80
WORKDIR /usr/share/nginx/html
ENV BLAZOR_ENVIRONMENT=Production
COPY --from=publish "/app/publish/wwwroot" .
COPY "SaveLoadCompressionWeb/nginx.conf" /etc/nginx/nginx.conf
COPY "SaveLoadCompressionWeb/start.sh" ./
RUN sed -i -e "s/\r$//" start.sh
CMD /bin/sh ./start.sh