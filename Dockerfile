FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
COPY Project.csproj .
RUN dotnet restore
COPY . .
RUN dotnet publish Project.csproj -c Release -o /app/publish

FROM mcr.microsoft.com/dotnet/aspnet:10.0
WORKDIR /app
COPY --from=build /app/publish .
ENV ASPNETCORE_ENVIRONMENT=Production
# ไฟล์อัปโหลดเก็บที่ /data/uploads ให้ mount volume ที่ /data เพื่อไม่ให้หายตอน deploy ใหม่
ENV Uploads__Path=/data/uploads
# Railway/Render ส่งพอร์ตมาทางตัวแปร PORT
CMD ["sh", "-c", "ASPNETCORE_URLS=http://+:${PORT:-8080} dotnet Project.dll"]
