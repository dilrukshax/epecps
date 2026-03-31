FROM node:20-alpine AS build
WORKDIR /app

COPY frontend/epecps-web/package*.json ./
RUN npm ci

COPY frontend/epecps-web/ ./
RUN npm run build -- --configuration docker && \
    mkdir -p /tmp/nginx-html && \
    if [ -d dist/epecps-web/browser ]; then \
      cp -r dist/epecps-web/browser/. /tmp/nginx-html/; \
    else \
      cp -r dist/epecps-web/. /tmp/nginx-html/; \
    fi

FROM nginx:1.27-alpine AS final
WORKDIR /usr/share/nginx/html

COPY frontend/epecps-web/nginx.conf /etc/nginx/conf.d/default.conf
COPY --from=build /tmp/nginx-html/ ./

EXPOSE 80
