# Stage 1: Build the Blazor WebAssembly app
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
COPY . .
RUN dotnet publish -c Release -o /app/publish

# Stage 2: Serve with nginx
FROM nginx:alpine

# Copy the published Blazor WASM static files
COPY --from=build /app/publish/wwwroot /usr/share/nginx/html

# Copy nginx config template (uses envsubst for backend proxy)
COPY nginx.conf.template /etc/nginx/templates/default.conf.template

# Only substitute BACKEND_* env vars, leave nginx variables ($uri, $host, etc.) intact
ENV NGINX_ENVSUBST_FILTER=BACKEND

EXPOSE 80
